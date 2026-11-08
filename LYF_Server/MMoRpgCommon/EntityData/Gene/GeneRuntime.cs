using System;
using System.Collections.Generic;

/// <summary>实体持有的基因运行模块，管理来源、寿命和节点遍历；具体效果由公共 GeneBase 子类执行。</summary>
public class GeneRuntime
{
    /// <summary>按配置编号创建独立运行节点的工厂，不在运行器中持有配置行。</summary>
    private readonly GeneFactory factory;
    /// <summary>当前实体直接持有的基因根节点，按施加顺序保存。</summary>
    private readonly List<GeneBase> genes = new List<GeneBase>();
    /// <summary>本容器触发概率使用的随机源；测试可传入确定序列。</summary>
    private readonly Random random;
    /// <summary>由宿主固定帧累加的逻辑毫秒，仅用于事件冷却。</summary>
    internal long ElapsedMilliseconds { get; private set; }
    /// <summary>即时生命或法力效果在施加时直接交给宿主结算。</summary>
    public event Action<GenePulse> Pulse;
    /// <summary>活跃实例的只读视图；移除或到期后自动反映最新列表。</summary>
    public IReadOnlyList<GeneBase> ActiveGenes { get; private set; }
    /// <summary>当前使用场景：0 公共场景，1 副本，2 PvP；配置 0 在所有场景生效。</summary>
    public int Scenario { get; private set; }

    /// <summary>为一个实体建立独立的基因容器。</summary>
    /// <param name="geneFactory">按编号添加时使用的节点工厂；只添加现有节点时可省略。</param>
    /// <param name="scenario">实体当前环境，0 公共、1 副本、2 PvP。</param>
    /// <param name="triggerRandom">概率触发的随机源；省略时为本容器创建独立随机源。</param>
    public GeneRuntime(GeneFactory geneFactory = null, int scenario = 0, Random triggerRandom = null)
    {
        factory = geneFactory;
        Scenario = scenario;
        ActiveGenes = genes.AsReadOnly();
        random = triggerRandom;
        if (random == null) random = new Random();
    }

    /// <summary>添加基因；同编号同来源按规则叠加并刷新时间，不同来源独立。</summary>
    /// <param name="geneId">基因配置编号；不存在时由字典明确报出配置错误。</param>
    /// <param name="sourceId">施加者或装备实例编号，0 为默认来源。</param>
    /// <param name="value">可选的根效果数值覆盖；关联效果沿用各自表值。</param>
    /// <returns>本次添加或刷新的实例；场景不适用时返回 null。</returns>
    /// <param name="sourceSlot">同一装备内的效果槽，0 为普通施加，1 至 9 为装备词条。</param>
    /// <param name="firerId">施加角色编号，独立于来源装备 UID。</param>
    public virtual GeneBase AddGene(int geneId, ulong sourceId = 0, float? value = null, int sourceSlot = 0, ulong firerId = 0)
    {
        // 编号入口只负责创建；已经挂载时保留原节点及其周期、冷却状态。
        GeneBase existing = FindGene(geneId, sourceId, sourceSlot);
        if (existing != null) return RefreshGene(existing, value);
        return AddGene(factory.CreateGene(geneId, value), sourceId, sourceSlot, firerId);
    }

    /// <summary>直接挂载具体基因节点；集合保存传入的原对象，不再套实例包装。</summary>
    /// <param name="gene">本次独立创建、尚未属于其他宿主的基因根节点。</param>
    /// <param name="sourceId">施加者或装备实例编号。</param>
    /// <param name="sourceSlot">同一来源的独立效果槽。</param>
    /// <param name="firerId">实际施加角色编号。</param>
    /// <returns>挂载或刷新的原节点；场景不适用时返回 null。</returns>
    public virtual GeneBase AddGene(GeneBase gene, ulong sourceId = 0, int sourceSlot = 0, ulong firerId = 0)
    {
        // 同来源同槽仍按既有叠加规则刷新，避免装备刷新产生重复节点。
        GeneBase existing = FindGene(gene.GeneID, sourceId, sourceSlot);
        if (existing != null) return RefreshGene(existing, gene.Value);
        if (!gene.AppliesToScenario(Scenario)) return null;
        gene.Initialize(sourceId, sourceSlot, firerId);
        // 全为即时资源的组合只兑现一次结果；有持续节点才保留在集合中。
        bool persistent = false;
        // 关联子项引用原节点，自身类型决定是否需要常驻。
        for (int index = 0; index < gene.ActiveNodes.Count; index++)
        {
            if (!gene.ActiveNodes[index].IsInstant) persistent = true;
        }
        if (persistent) genes.Add(gene);
        PublishInstantEffects(gene);
        return gene;
    }

    /// <summary>重复施加时更新原节点的层数、数值和寿命，保留周期进度与冷却。</summary>
    /// <param name="gene">集合中原有的根节点。</param>
    /// <param name="value">本次明确指定的根效果数值；为空时保留原数值。</param>
    /// <returns>刷新的原节点；当前场景不适用时返回 null。</returns>
    private GeneBase RefreshGene(GeneBase gene, float? value)
    {
        if (!gene.AppliesToScenario(Scenario)) return null;
        if (gene.AllowsStacking)
        {
            if (gene.MaxStack == 0 || gene.StackCount < gene.MaxStack) gene.StackCount++;
        }
        if (value.HasValue && gene.HasOwnEffect) gene.Value = value.Value;
        gene.ResetTriggers();
        gene.RefreshDuration();
        PublishInstantEffects(gene);
        return gene;
    }

    /// <summary>首次施加和刷新组合时都兑现其中的即时资源节点，不重复启动周期。</summary>
    /// <param name="instance">本次实际施加或刷新的原实例。</param>
    private void PublishInstantEffects(GeneBase instance)
    {
        // 只遍历已激活节点，事件子树中的即时资源仍等待对应事件。
        for (int i = 0; i < instance.ActiveNodes.Count; i++)
        {
            // 组合中的实际叶子，场景约束仍沿父引用检查。
            GeneBase node = instance.ActiveNodes[i];
            if (node.AppliesToScenario(Scenario)) node.PublishInstant(instance, Pulse);
        }
    }

    /// <summary>按配置编号及来源定位唯一实例。</summary>
    /// <param name="geneId">父基因配置编号。</param>
    /// <param name="sourceId">施加者或装备实例编号。</param>
    /// <returns>现有实例；未施加或已移除时返回 null。</returns>
    /// <param name="sourceSlot">要查找的同来源槽，默认 0 保留普通效果接口。</param>
    public GeneBase FindGene(int geneId, ulong sourceId = 0, int sourceSlot = 0)
    {
        // 顺序查询当前少量生效基因，保持与 ActiveGenes 一致的对象引用。
        for (int i = 0; i < genes.Count; i++)
        {
            if (genes[i].GeneID == geneId && genes[i].SourceId == sourceId && genes[i].SourceSlot == sourceSlot)
            {
                return genes[i];
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
        return genes.Remove(FindGene(geneId, sourceId));
    }

    /// <summary>移除指定原实例，供装备单个词条或特技等级变更使用。</summary>
    /// <param name="instance">此前 AddGene 返回的原实例。</param>
    /// <returns>当前仍在容器中并已移除时返回 true。</returns>
    public virtual bool RemoveGene(GeneBase instance)
    {
        return genes.Remove(instance);
    }

    /// <summary>卸装时移除该装备来源的全部槽及其自身临时效果，保留其他来源。</summary>
    /// <param name="sourceId">需要卸载的装备实例 UID。</param>
    public virtual void RemoveGenesBySource(ulong sourceId)
    {
        // 反向删除不会跳过同一来源的相邻词条。
        for (int i = genes.Count - 1; i >= 0; i--)
        {
            if (genes[i].SourceId == sourceId) genes.RemoveAt(i);
        }
    }

    /// <summary>切换适用环境，立即移除在新环境不再生效的实例。</summary>
    /// <param name="scenario">0 公共、1 副本、2 PvP。</param>
    public virtual void SetScenario(int scenario)
    {
        Scenario = scenario;
        // 反向移除不改变尚待检查元素的索引。
        for (int i = genes.Count - 1; i >= 0; i--)
        {
            if (!genes[i].AppliesToScenario(Scenario)) genes.RemoveAt(i);
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
        // 按施加顺序合并所有来源，每个索引对应一个原根实例。
        for (int i = 0; i < genes.Count; i++)
        {
            // 当前实例贡献的全部叶子效果。
            GeneBase instance = genes[i];
            // 节点索引只遍历当前已激活的普通节点和事件入口。
            for (int j = 0; j < instance.ActiveNodes.Count; j++)
            {
                // 属性、状态及周期效果共存在实例中，这里只读取匹配的属性项。
                GeneBase node = instance.ActiveNodes[j];
                if (!node.AppliesToScenario(Scenario)) continue;
                node.AccumulateAttribute(attribute, instance.StackCount, ref flat, ref percent);
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
        // 根实例索引保留不同来源的状态独立性。
        for (int i = 0; i < genes.Count; i++)
        {
            // 节点索引覆盖组合中已经激活的状态节点。
            for (int j = 0; j < genes[i].ActiveNodes.Count; j++)
            {
                // 当前待合并的叶子效果；子项也遵守自己的场景限制。
                GeneBase node = genes[i].ActiveNodes[j];
                if (node.AppliesToScenario(Scenario)) states |= node.GetStates();
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
        ElapsedMilliseconds += deltaMilliseconds;
        // 反向遍历可在到期时直接删除当前实例，不移动尚未处理项的索引。
        for (int i = genes.Count - 1; i >= 0; i--)
        {
            // 本帧需要推进的实例及其叶子效果。
            GeneBase instance = genes[i];
            // 本帧实际处于生效状态的时间，最多推进到本实例的到期时刻。
            long activeMilliseconds = deltaMilliseconds;
            if (instance.RemainingMilliseconds >= 0)
            {
                activeMilliseconds = Math.Min(activeMilliseconds, instance.RemainingMilliseconds);
            }
            // 依次推进组合中的各个叶子周期，非周期项不参与计时。
            for (int j = 0; j < instance.ActiveNodes.Count; j++)
            {
                // 周期项各自累计时间，刷新父寿命不会把该累计值归零。
                GeneBase node = instance.ActiveNodes[j];
                if (node.AppliesToScenario(Scenario)) node.AdvanceTime(activeMilliseconds, instance, pulses);
            }
            if (instance.RemainingMilliseconds >= 0)
            {
                instance.RemainingMilliseconds -= activeMilliseconds;
                if (instance.RemainingMilliseconds == 0) genes.RemoveAt(i);
            }
        }
        return pulses;
    }

    /// <summary>向当前宿主传入事件，施加匹配的关联基因；次数节点每次匹配消耗一次。</summary>
    /// <param name="geneEvent">本次实际发生的宿主事件，None 不触发任何效果。</param>
    /// <returns>本次实际施加或刷新的关联基因数量。</returns>
    public virtual int Trigger(GeneEventType geneEvent)
    {
        return Trigger(new GeneTriggerArgs(geneEvent));
    }

    /// <summary>处理真实事件参数；只有成功施加关联效果才消耗次数并开始冷却。</summary>
    /// <param name="args">事件类型、实际伤害和事件对方的运行容器。</param>
    /// <returns>本事件实际施加或刷新关联基因的数量。</returns>
    public virtual int Trigger(GeneTriggerArgs args)
    {
        if (args.Event == GeneEventType.None) return 0;
        // 从原列表末尾向前处理，新增实例追加到末尾，不在同一事件中再次响应。
        int activated = 0;
        // 反向根索引避免本事件新追加的实例在同一次通知中再次响应。
        for (int i = genes.Count - 1; i >= 0; i--)
        {
            // 当前原有实例及它已经激活的普通效果和触发节点。
            GeneBase instance = genes[i];
            // 事件子树内部的次数保存在原节点上，不因每次通知而重新创建。
            for (int j = 0; j < instance.ActiveNodes.Count; j++)
            {
                // 非触发节点仍由属性查询或逻辑帧驱动，不响应事件。
                GeneBase node = instance.ActiveNodes[j];
                activated += node.Trigger(args, instance, this, this);
            }
            if (instance.IsExhausted) genes.RemoveAt(i);
        }
        return activated;
    }

    /// <summary>实体销毁或对象池回收时移除所有基因及其周期进度。</summary>
    public virtual void Clear()
    {
        genes.Clear();
    }

    /// <summary>事件节点使用原宿主的随机序列进行一次万分比判定。</summary>
    /// <returns>0 至 9999 的随机值。</returns>
    internal int NextTriggerRoll() { return random.Next(10000); }
}
