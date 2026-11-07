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
    /// <param name="value">可选的单效果数值覆盖，用于原有装备词条；组合沿用各子效果表值。</param>
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
            existing.RefreshDuration();
            return existing;
        }
        // 每个实例独享周期累计值，避免两个实体或两个来源相互影响。
        List<GeneEffect> effects = new List<GeneEffect>();
        if (definition.EffectType == (int)GeneEffectType.Composite)
        {
            // 沿用原表五个组合槽位，叶子不递归，寿命和叠加始终属于父基因。
            int[] attachedIds = { definition.AttachGeneId0, definition.AttachGeneId1, definition.AttachGeneId2,
                definition.AttachGeneId3, definition.AttachGeneId4 };
            for (int i = 0; i < attachedIds.Length; i++)
            {
                if (attachedIds[i] == 0) continue;
                // 组合槽位对应的属性、状态或周期效果定义。
                cfg.Gene child = definitions[attachedIds[i]];
                effects.Add(CreateEffect(child, child.Value));
            }
        }
        else
        {
            // 没有调用方覆盖时直接采用表内每层数值。
            float effectValue = definition.Value;
            if (value.HasValue) effectValue = value.Value;
            effects.Add(CreateEffect(definition, effectValue));
        }
        // 将完整实例登记到当前实体；组合的子项不作为独立基因再次添加。
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
                if (!AppliesToScenario(effect.Definition)) continue;
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
                cfg.Gene effect = instances[i].Effects[j].Definition;
                if (effect.EffectType == (int)GeneEffectType.State && AppliesToScenario(effect))
                {
                    states |= (GeneState)effect.State;
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
                if (!AppliesToScenario(effect.Definition)) continue;
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

    /// <summary>判断效果是否需要按毫秒周期产生脉冲。</summary>
    /// <param name="definition">待检查的叶子效果定义。</param>
    /// <returns>持续伤害、持续治疗或持续回蓝返回 true。</returns>
    private static bool IsPeriodic(cfg.Gene definition)
    {
        return definition.EffectType >= (int)GeneEffectType.PeriodicDamage
            && definition.EffectType <= (int)GeneEffectType.PeriodicMana;
    }

    /// <summary>创建叶子运行状态，并明确拒绝不受支持的递归组合或无周期配置。</summary>
    /// <param name="definition">已通过编号索引找到的叶子定义。</param>
    /// <param name="value">当前实例每层采用的效果数值。</param>
    /// <returns>与其他实例不共享计时器的叶子效果。</returns>
    private static GeneEffect CreateEffect(cfg.Gene definition, float value)
    {
        if (definition.EffectType == (int)GeneEffectType.Composite)
        {
            throw new InvalidOperationException("组合基因只允许引用叶子效果：" + definition.Id);
        }
        if (IsPeriodic(definition) && definition.Interval <= 0)
        {
            throw new InvalidOperationException("周期基因的 Interval 必须大于零：" + definition.Id);
        }
        return new GeneEffect(definition, value);
    }
}
