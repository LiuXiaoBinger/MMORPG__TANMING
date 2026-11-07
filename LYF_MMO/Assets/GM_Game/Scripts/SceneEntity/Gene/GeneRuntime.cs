using System;
using System.Collections.Generic;

/// <summary>实体持有的基因运行模块，参考 RO 的组件持有实例、实例持有效果的结构。</summary>
public class GeneRuntime
{
    /// <summary>从 Luban 装载的配置索引，运行期间仅查询。</summary>
    private readonly Dictionary<int, cfg.Gene> definitions;
    /// <summary>当前实体的活跃实例，按施加顺序保存。</summary>
    private readonly List<GeneInstance> instances = new List<GeneInstance>();
    /// <summary>活跃实例的只读视图；移除或到期后自动反映最新列表。</summary>
    public IReadOnlyList<GeneInstance> ActiveGenes { get; private set; }
    /// <summary>当前使用场景：0 公共场景，1 副本，2 PvP；配置 0 在所有场景生效。</summary>
    public int Scenario { get; private set; }

    /// <summary>为一个实体建立独立的基因容器。</summary>
    /// <param name="geneDefinitions">当前版本的基因配置，所有引用均应已通过配置校验。</param>
    /// <param name="scenario">实体当前环境，0 公共、1 副本、2 PvP。</param>
    public GeneRuntime(Dictionary<int, cfg.Gene> geneDefinitions, int scenario = 0)
    {
        definitions = geneDefinitions;
        Scenario = scenario;
        ActiveGenes = instances.AsReadOnly();
    }

    /// <summary>添加基因；同编号同来源按规则叠加并刷新时间，不同来源独立。</summary>
    /// <param name="geneId">基因配置编号；不存在时由字典明确报出配置错误。</param>
    /// <param name="sourceId">施加者或装备实例编号，0 为默认来源。</param>
    /// <param name="value">可选的根效果数值覆盖；关联效果沿用各自表值。</param>
    /// <returns>本次添加或刷新的实例；场景不适用时返回 null。</returns>
    public virtual GeneInstance AddGene(int geneId, ulong sourceId = 0, float? value = null)
    {
        // 父定义只控制这组效果的寿命、叠加与适用场景。
        cfg.Gene definition = definitions[geneId];
        if (!AppliesToScenario(definition)) return null;
        // 相同施加来源的既有实例，重复施加时不产生第二份属性增量。
        GeneInstance existing = FindGene(geneId, sourceId);
        if (existing != null)
        {
            if (definition.IsStack == 0)
            {
                if (definition.MaxStack == 0 || existing.StackCount < definition.MaxStack)
                {
                    existing.StackCount++;
                }
            }
            if (value.HasValue && definition.EffectType != (int)GeneEffectType.Composite)
            {
                existing.Effects[0].Value = value.Value;
            }
            // 刷新实例时补满事件子树中的次数，周期累计值仍然保留。
            for (int i = 0; i < existing.Effects.Count; i++)
            {
                if (IsTriggered(existing.Effects[i].Definition)) existing.Effects[i].ResetTriggers();
            }
            existing.RefreshDuration();
            return existing;
        }
        // 实例保留当前激活的效果；事件子树只有在匹配事件到来后才施加。
        List<GeneEffect> effects = new List<GeneEffect>();
        // 覆盖数值只作用于根节点，不能改变其他被组合配置的原始数值。
        float effectValue = definition.Value;
        if (value.HasValue) effectValue = value.Value;
        // 路径集合只检测当前递归链，允许不同分支引用同一基因。
        GeneEffect root = CreateEffect(definition, effectValue, new HashSet<int>());
        CollectActiveEffects(root, effects);
        // 普通关联项共同存亡；事件后产生的基因按自己的配置独立计时。
        GeneInstance instance = new GeneInstance(definition, sourceId, effects);
        instances.Add(instance);
        return instance;
    }

    /// <summary>按配置编号及来源定位唯一实例。</summary>
    /// <param name="geneId">父基因配置编号。</param>
    /// <param name="sourceId">施加者或装备实例编号。</param>
    /// <returns>现有实例；未施加或已移除时返回 null。</returns>
    public GeneInstance FindGene(int geneId, ulong sourceId = 0)
    {
        // 顺序查询当前少量生效基因，保持与 ActiveGenes 一致的对象引用。
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i].Definition.Id == geneId && instances[i].SourceId == sourceId)
            {
                return instances[i];
            }
        }
        return null;
    }

    /// <summary>移除一个来源的基因及其全部组合效果；不修改调用方的基础属性。</summary>
    /// <param name="geneId">父基因配置编号。</param>
    /// <param name="sourceId">要移除的来源编号。</param>
    /// <returns>找到并移除实例时返回 true。</returns>
    public virtual bool RemoveGene(int geneId, ulong sourceId = 0)
    {
        return instances.Remove(FindGene(geneId, sourceId));
    }

    /// <summary>切换适用环境，立即移除在新环境不再生效的实例。</summary>
    /// <param name="scenario">0 公共、1 副本、2 PvP。</param>
    public virtual void SetScenario(int scenario)
    {
        Scenario = scenario;
        // 反向移除不改变尚待检查元素的索引。
        for (int i = instances.Count - 1; i >= 0; i--)
        {
            if (!AppliesToScenario(instances[i].Definition)) instances.RemoveAt(i);
        }
    }

    /// <summary>计算属性最终值：基础值 ×（1 + 百分比修正之和）+ 固定修正之和。</summary>
    /// <param name="attribute">本次读取的属性类型。</param>
    /// <param name="baseValue">不含当前基因效果的基础值，不能传入已经计算后的值。</param>
    /// <returns>当前所有来源与层数的合计结果；移除后自然回到基础值，不产生回滚误差。</returns>
    public virtual double CalculateAttribute(GeneAttributeType attribute, double baseValue)
    {
        // 固定值修正和百分比修正分别累计，同类百分比使用加法。
        double flat = 0;
        // 10 表示相对基础值增加 10%。
        double percent = 0;
        for (int i = 0; i < instances.Count; i++)
        {
            // 当前实例贡献的全部叶子效果。
            GeneInstance instance = instances[i];
            for (int j = 0; j < instance.Effects.Count; j++)
            {
                // 属性、状态及周期效果共存在实例中，这里只读取匹配的属性项。
                GeneEffect effect = instance.Effects[j];
                if (effect.Definition.EffectType != (int)GeneEffectType.Attribute) continue;
                if (effect.Definition.AttributeId != (int)attribute) continue;
                if (!AppliesToEffect(effect)) continue;
                if (effect.Definition.ValueType == 1) percent += effect.Value * instance.StackCount;
                else flat += effect.Value * instance.StackCount;
            }
        }
        return baseValue * (1 + percent / 100) + flat;
    }

    /// <summary>合并当前状态；霸体存续期间屏蔽眩晕和定身，但保留沉默。</summary>
    /// <returns>业务可读取的有效状态位。</returns>
    public GeneState GetStates()
    {
        // 多个来源提供的同一状态按位合并，某一来源到期不会解除其他来源的状态。
        GeneState states = GeneState.None;
        for (int i = 0; i < instances.Count; i++)
        {
            for (int j = 0; j < instances[i].Effects.Count; j++)
            {
                // 当前待合并的叶子效果；子项也遵守自己的场景限制。
                GeneEffect effect = instances[i].Effects[j];
                if (effect.Definition.EffectType == (int)GeneEffectType.State && AppliesToEffect(effect))
                {
                    states |= (GeneState)effect.Definition.State;
                }
            }
        }
        if ((states & GeneState.SuperArmor) != 0) states &= ~(GeneState.Stun | GeneState.Root);
        return states;
    }

    /// <summary>眩晕或定身时不能移动；这里只给出判定结果，不直接接管移动系统。</summary>
    public bool CanMove { get { return (GetStates() & (GeneState.Stun | GeneState.Root)) == 0; } }
    /// <summary>眩晕或沉默时不能施法；具体技能请求在后续业务接入时读取。</summary>
    public bool CanCast { get { return (GetStates() & (GeneState.Stun | GeneState.Silence)) == 0; } }

    /// <summary>推进基因寿命与周期；补齐本帧跨过的周期，但不结算寿命以外的时间。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧经过的非负毫秒数。</param>
    /// <returns>本帧实际发生的周期结果，交由宿主按业务结算；无结果时为空列表。</returns>
    public virtual List<GenePulse> Update(int deltaMilliseconds)
    {
        // 强类型周期结果与计时推进分离，宿主处理结果时可正常增删基因。
        List<GenePulse> pulses = new List<GenePulse>();
        if (deltaMilliseconds <= 0) return pulses;
        // 反向遍历可在到期时直接删除当前实例，不移动尚未处理项的索引。
        for (int i = instances.Count - 1; i >= 0; i--)
        {
            // 本帧需要推进的实例及其叶子效果。
            GeneInstance instance = instances[i];
            // 本帧实际处于生效状态的时间，最多推进到本实例的到期时刻。
            long activeMilliseconds = deltaMilliseconds;
            if (instance.RemainingMilliseconds >= 0)
            {
                activeMilliseconds = Math.Min(activeMilliseconds, instance.RemainingMilliseconds);
            }
            // 依次推进组合中的各个叶子周期，非周期项不参与计时。
            for (int j = 0; j < instance.Effects.Count; j++)
            {
                // 周期项各自累计时间，刷新父寿命不会把该累计值归零。
                GeneEffect effect = instance.Effects[j];
                if (!IsPeriodic(effect.Definition)) continue;
                if (!AppliesToEffect(effect)) continue;
                effect.ElapsedMilliseconds += activeMilliseconds;
                while (effect.ElapsedMilliseconds >= effect.Definition.Interval)
                {
                    effect.ElapsedMilliseconds -= effect.Definition.Interval;
                    pulses.Add(new GenePulse(instance, effect));
                }
            }
            if (instance.RemainingMilliseconds >= 0)
            {
                instance.RemainingMilliseconds -= activeMilliseconds;
                if (instance.RemainingMilliseconds == 0) instances.RemoveAt(i);
            }
        }
        return pulses;
    }

    /// <summary>向当前宿主传入事件，施加匹配的关联基因；次数节点每次匹配消耗一次。</summary>
    /// <param name="geneEvent">本次实际发生的宿主事件，None 不触发任何效果。</param>
    /// <returns>本次实际施加或刷新的关联基因数量。</returns>
    public virtual int Trigger(GeneEventType geneEvent)
    {
        if (geneEvent == GeneEventType.None) return 0;
        // 从原列表末尾向前处理，新增实例追加到末尾，不在同一事件中再次响应。
        int activated = 0;
        for (int i = instances.Count - 1; i >= 0; i--)
        {
            // 当前原有实例及它已经激活的普通效果和触发节点。
            GeneInstance instance = instances[i];
            // 事件子树内部的次数保存在原节点上，不因每次通知而重新创建。
            for (int j = 0; j < instance.Effects.Count; j++)
            {
                // 非触发节点仍由属性查询或逻辑帧驱动，不响应事件。
                GeneEffect effect = instance.Effects[j];
                if (IsTriggered(effect.Definition)) activated += TriggerEffect(effect, geneEvent, instance.SourceId);
            }
            if (instance.Definition.EffectType == (int)GeneEffectType.Count)
            {
                if (instance.Effects[0].RemainingTriggers == 0) instances.RemoveAt(i);
            }
        }
        return activated;
    }

    /// <summary>实体销毁或对象池回收时移除所有基因及其周期进度。</summary>
    public virtual void Clear()
    {
        instances.Clear();
    }

    /// <summary>判断配置是否适用于实体当前所在环境。</summary>
    /// <param name="definition">父基因或叶子效果的配置。</param>
    /// <returns>全局配置或与当前环境相同的配置返回 true。</returns>
    private bool AppliesToScenario(cfg.Gene definition)
    {
        return definition.Scenario == 0 || definition.Scenario == Scenario;
    }

    /// <summary>检查节点及其全部组合父项的场景要求，避免嵌套后跳过中间条件。</summary>
    /// <param name="effect">待使用的原运行节点。</param>
    /// <returns>整条路径均适用于当前场景时返回 true。</returns>
    private bool AppliesToEffect(GeneEffect effect)
    {
        // 沿父引用逐级检查，不创建副本或改变任何节点状态。
        GeneEffect current = effect;
        while (current != null)
        {
            if (!AppliesToScenario(current.Definition)) return false;
            current = current.Parent;
        }
        return true;
    }

    /// <summary>判断效果是否需要按毫秒周期产生脉冲。</summary>
    /// <param name="definition">待检查的叶子效果定义。</param>
    /// <returns>持续伤害、持续治疗或持续回蓝返回 true。</returns>
    private static bool IsPeriodic(cfg.Gene definition)
    {
        return definition.EffectType >= (int)GeneEffectType.PeriodicDamage
            && definition.EffectType <= (int)GeneEffectType.PeriodicMana;
    }

    /// <summary>创建完整关联树；允许任意类型嵌套，循环引用不能展开为有效组合。</summary>
    /// <param name="definition">当前基因定义。</param>
    /// <param name="value">当前实例每层采用的效果数值。</param>
    /// <param name="path">当前递归路径的编号，用于定位直接或间接自引用。</param>
    /// <returns>包含独立周期和次数的效果树。</returns>
    private GeneEffect CreateEffect(cfg.Gene definition, float value, HashSet<int> path)
    {
        if (!path.Add(definition.Id))
        {
            throw new InvalidOperationException("基因组合存在循环引用：" + definition.Id);
        }
        if (IsPeriodic(definition) && definition.Interval <= 0)
        {
            throw new InvalidOperationException("周期基因的 Interval 必须大于零：" + definition.Id);
        }
        if (definition.EffectType == (int)GeneEffectType.Count && definition.TriggerCount <= 0)
        {
            throw new InvalidOperationException("次数基因的 TriggerCount 必须大于零：" + definition.Id);
        }
        if (definition.EffectType == (int)GeneEffectType.Event && definition.TriggerEvent == 0)
        {
            throw new InvalidOperationException("事件基因必须指定 TriggerEvent：" + definition.Id);
        }
        // 五个槽位在全部类型上有效，子节点顺序与表中槽位一致。
        int[] attachedIds = { definition.AttachGeneId0, definition.AttachGeneId1, definition.AttachGeneId2,
            definition.AttachGeneId3, definition.AttachGeneId4 };
        // 完整子树保留事件下的次数状态，事件触发时不重新初始化。
        List<GeneEffect> children = new List<GeneEffect>();
        for (int i = 0; i < attachedIds.Length; i++)
        {
            if (attachedIds[i] == 0) continue;
            // 关联定义仍由统一的 TbGene 索引读取，与它位于哪个 Sheet 无关。
            cfg.Gene child = definitions[attachedIds[i]];
            children.Add(CreateEffect(child, child.Value, path));
        }
        path.Remove(definition.Id);
        return new GeneEffect(definition, value, children);
    }

    /// <summary>收集立即生效的节点，事件和次数节点的子树保持待触发状态。</summary>
    /// <param name="effect">当前遍历的关联节点。</param>
    /// <param name="activeEffects">供属性、状态、计时及事件入口访问的原节点引用。</param>
    private static void CollectActiveEffects(GeneEffect effect, List<GeneEffect> activeEffects)
    {
        if (effect.Definition.EffectType != (int)GeneEffectType.Composite) activeEffects.Add(effect);
        if (IsTriggered(effect.Definition)) return;
        // 非触发节点可以在自身效果之外，继续叠加所有关联类型的效果。
        for (int i = 0; i < effect.Children.Count; i++) CollectActiveEffects(effect.Children[i], activeEffects);
    }

    /// <summary>判断节点是否必须等待宿主事件才能施加关联项。</summary>
    /// <param name="definition">待判断的基因定义。</param>
    /// <returns>事件和次数节点返回 true。</returns>
    private static bool IsTriggered(cfg.Gene definition)
    {
        return definition.EffectType == (int)GeneEffectType.Event || definition.EffectType == (int)GeneEffectType.Count;
    }

    /// <summary>执行一个匹配事件节点；相邻事件／次数节点沿当前事件继续触发。</summary>
    /// <param name="effect">保存剩余次数和关联子树的原节点。</param>
    /// <param name="geneEvent">由宿主传入的实际事件。</param>
    /// <param name="sourceId">继承施加者身份，避免不同来源相互覆盖。</param>
    /// <returns>本节点实际产生的关联基因施加次数。</returns>
    private int TriggerEffect(GeneEffect effect, GeneEventType geneEvent, ulong sourceId)
    {
        if (!AppliesToEffect(effect)) return 0;
        if (effect.Definition.TriggerEvent != 0 && effect.Definition.TriggerEvent != (int)geneEvent) return 0;
        if (effect.Definition.EffectType == (int)GeneEffectType.Count)
        {
            if (effect.RemainingTriggers == 0) return 0;
            effect.RemainingTriggers--;
        }
        // 关联的普通基因从本次触发时刻开始独立计时，后续按自己的叠加规则刷新。
        int activated = 0;
        for (int i = 0; i < effect.Children.Count; i++)
        {
            // 相邻触发节点保留原次数；其他节点作为新基因施加到当前宿主。
            GeneEffect child = effect.Children[i];
            if (!AppliesToScenario(child.Definition)) continue;
            if (IsTriggered(child.Definition)) activated += TriggerEffect(child, geneEvent, sourceId);
            else
            {
                AddGene(child.Definition.Id, sourceId);
                activated++;
            }
        }
        return activated;
    }
}
