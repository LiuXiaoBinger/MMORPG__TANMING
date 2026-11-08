using System;
using System.Collections.Generic;

/// <summary>一次创建对应一个独立运行节点；自身行为由具体类型实现，全部类型均可关联子基因。</summary>
public abstract class GeneBase
{
    /// <summary>工厂使用的配置编号；节点不保留配置行引用。</summary>
    public int GeneID { get; set; }
    /// <summary>本次施加每层采用的数值；百分比时 10 表示 10%。</summary>
    public float Value { get; set; }
    /// <summary>0 为固定点数，1 为相对基础属性或资源上限的百分数。</summary>
    public int ValueType { get; set; }
    /// <summary>作为根节点施加时的寿命，单位秒；0 表示常驻，普通子项共用根寿命。</summary>
    public float DurationSeconds { get; set; }
    /// <summary>适用环境：0 为全部，1 为副本，2 为 PvP。</summary>
    public int Scenario { get; set; }
    /// <summary>同编号、来源和槽位重复施加时是否增加层数。</summary>
    public bool AllowsStacking { get; set; }
    /// <summary>允许的最大层数；0 表示不限制，只有 AllowsStacking 为真时使用。</summary>
    public int MaxStack { get; set; }
    /// <summary>事件施加此节点时是否选择事件对方，否则沿用上层选择的宿主。</summary>
    public bool TargetOpponent { get; set; }
    /// <summary>当前具体节点的效果分类，由类型固定，不由运行器切换行为。</summary>
    public abstract GeneEffectType EffectType { get; }
    /// <summary>根节点的施加者或装备实例编号；普通子项沿用所属根节点的来源。</summary>
    public ulong SourceId { get; private set; }
    /// <summary>同一来源的独立效果槽；0 为普通施加，装备槽用于区分重复编号。</summary>
    public int SourceSlot { get; private set; }
    /// <summary>实际施加角色编号，资源归因使用它而不是装备 UID。</summary>
    public ulong FirerId { get; private set; }
    /// <summary>根节点当前叠加层数；已激活的普通子项共用此层数。</summary>
    public int StackCount { get; internal set; }
    /// <summary>根节点剩余寿命，单位毫秒；-1 为常驻，0 为已到期。</summary>
    public long RemainingMilliseconds { get; internal set; }
    /// <summary>根节点当前直接生效的原节点引用；事件下的待触发子树不在其中。</summary>
    internal IReadOnlyList<GeneBase> ActiveNodes { get; private set; }
    /// <summary>按配置槽顺序保存的子节点；工厂每次创建均使用独立对象。</summary>
    private List<GeneBase> children = new List<GeneBase>();
    /// <summary>完整关联子树的只读视图，事件子项可以尚未激活。</summary>
    public IReadOnlyList<GeneBase> Children { get; private set; }
    /// <summary>组合中的直接父节点，根为 null；场景检查沿此引用保留全部祖先约束。</summary>
    public GeneBase Parent { get; private set; }
    /// <summary>纯组合没有自身效果，但它的子节点仍参与激活。</summary>
    public virtual bool HasOwnEffect { get { return true; } }
    /// <summary>事件节点在匹配事件之前不激活关联子树。</summary>
    public virtual bool WaitsForEvent { get { return false; } }
    /// <summary>即时资源节点无需在施加后常驻；组合中的其他节点仍可常驻。</summary>
    public virtual bool IsInstant { get { return false; } }
    /// <summary>次数根耗尽时从容器移除，普通节点不会因触发次数结束。</summary>
    public virtual bool IsExhausted { get { return false; } }

    /// <summary>建立节点自己的子树视图，避免不同施加共享可变列表。</summary>
    protected GeneBase()
    {
        Children = children.AsReadOnly();
    }

    /// <summary>首次挂载时把来源、寿命和活跃子项直接放在根节点上。</summary>
    /// <param name="sourceId">施加者或装备实例编号。</param>
    /// <param name="sourceSlot">同来源中的独立效果槽。</param>
    /// <param name="firerId">实际施加角色编号。</param>
    internal void Initialize(ulong sourceId, int sourceSlot, ulong firerId)
    {
        SourceId = sourceId;
        SourceSlot = sourceSlot;
        FirerId = firerId;
        StackCount = 1;
        RefreshDuration();
        ResetTriggers();
        // 只缓存本树的原节点引用，避免每次属性查询重复展开组合。
        List<GeneBase> activeNodes = new List<GeneBase>();
        CollectActiveNodes(activeNodes);
        ActiveNodes = activeNodes.AsReadOnly();
    }

    /// <summary>刷新根节点寿命，保留已有周期进度与事件冷却。</summary>
    public virtual void RefreshDuration()
    {
        RemainingMilliseconds = -1;
        if (DurationSeconds > 0) RemainingMilliseconds = (long)(DurationSeconds * 1000);
    }

    /// <summary>事件再次施加时复制具体节点及其子树，不重新读取配置或共享运行状态。</summary>
    /// <returns>类型和效果参数相同、来源和运行进度重新初始化的独立树。</returns>
    public virtual GeneBase Clone()
    {
        // 现有具体类型的效果参数均为标量；唯一可变引用子树在下面逐项复制。
        GeneBase copy = (GeneBase)MemberwiseClone();
        copy.Parent = null;
        copy.children = new List<GeneBase>();
        copy.Children = copy.children.AsReadOnly();
        // 每个关联槽都创建独立子节点，事件父树继续保留原次数和冷却。
        for (int index = 0; index < children.Count; index++) copy.AddChild(children[index].Clone());
        copy.Initialize(0, 0, 0);
        return copy;
    }

    /// <summary>工厂按槽顺序连接新建子节点，并保留原节点的父引用。</summary>
    /// <param name="child">本次工厂创建的独立子节点。</param>
    public void AddChild(GeneBase child)
    {
        child.Parent = this;
        children.Add(child);
    }

    /// <summary>收集当前直接生效的节点；事件以下的关联项等待事件再施加。</summary>
    /// <param name="nodes">本次施加持有的原节点引用列表。</param>
    internal void CollectActiveNodes(List<GeneBase> nodes)
    {
        if (HasOwnEffect) nodes.Add(this);
        if (WaitsForEvent) return;
        // 槽位顺序决定相同事件内的子项处理顺序。
        for (int index = 0; index < children.Count; index++) children[index].CollectActiveNodes(nodes);
    }

    /// <summary>判断当前节点及全部组合祖先是否适用于宿主环境。</summary>
    /// <param name="scenario">宿主当前场景分类。</param>
    /// <returns>路径上全部场景要求都满足时返回 true。</returns>
    internal bool AppliesToScenario(int scenario)
    {
        // 沿原对象引用检查，不复制祖先状态。
        GeneBase current = this;
        while (current != null)
        {
            if (current.Scenario != 0 && current.Scenario != scenario) return false;
            current = current.Parent;
        }
        return true;
    }

    /// <summary>重复施加时恢复子树次数，周期进度与事件冷却保持原值。</summary>
    public virtual void ResetTriggers()
    {
        // 遍历尚未激活的事件子树，保证嵌套次数也得到补充。
        for (int index = 0; index < children.Count; index++) children[index].ResetTriggers();
    }

    /// <summary>属性节点覆盖此入口，向本次查询累加固定值与百分数。</summary>
    /// <param name="attribute">本次查询的属性。</param>
    /// <param name="stacks">所在根实例的当前层数。</param>
    /// <param name="flat">累计的固定点数修正。</param>
    /// <param name="percent">累计的百分数修正，10 表示 10%。</param>
    public virtual void AccumulateAttribute(GeneAttributeType attribute, int stacks, ref double flat, ref double percent) { }

    /// <summary>状态节点覆盖此入口，普通节点不提供动作限制。</summary>
    /// <returns>此节点持续提供的状态位。</returns>
    public virtual GeneState GetStates() { return GeneState.None; }

    /// <summary>周期节点覆盖此入口，在根寿命内推进独立周期。</summary>
    /// <param name="milliseconds">本帧仍处于根寿命内的毫秒数。</param>
    /// <param name="instance">提供来源和层数的根实例。</param>
    /// <param name="pulses">本帧待宿主结算的强类型结果。</param>
    public virtual void AdvanceTime(long milliseconds, GeneBase instance, List<GenePulse> pulses) { }

    /// <summary>即时资源节点覆盖此入口，在首次施加和刷新时兑现资源效果。</summary>
    /// <param name="instance">提供来源与层数的根实例。</param>
    /// <param name="publish">宿主提供的即时结算回调，未订阅时允许为空。</param>
    public virtual void PublishInstant(GeneBase instance, Action<GenePulse> publish) { }

    /// <summary>事件节点覆盖此入口；普通已激活节点不响应宿主事件。</summary>
    /// <param name="args">权威宿主提供的实际事件。</param>
    /// <param name="instance">保存原始装备来源和施加角色的根实例。</param>
    /// <param name="host">管理此节点冷却时间和随机源的原宿主。</param>
    /// <param name="target">上层选中的效果承接容器。</param>
    /// <returns>成功施加或刷新的子基因数量。</returns>
    internal virtual int Trigger(GeneTriggerArgs args, GeneBase instance, GeneRuntime host, GeneRuntime target) { return 0; }

    /// <summary>普通事件子项作为新根独立施加；事件类型覆盖后继续沿同一原子树触发。</summary>
    /// <param name="args">包含实际事件对方的事件参数。</param>
    /// <param name="instance">保留来源及施加角色的原根实例。</param>
    /// <param name="host">管理当前原子树的宿主。</param>
    /// <param name="target">继承自上层的目标容器。</param>
    /// <returns>施加成功返回 1，没有目标或场景不符返回 0。</returns>
    internal virtual int ActivateFromEvent(GeneTriggerArgs args, GeneBase instance, GeneRuntime host, GeneRuntime target)
    {
        if (TargetOpponent) target = args.TargetRuntime;
        if (target == null) return 0;
        if (target.AddGene(Clone(), instance.SourceId, 0, instance.FirerId) != null) return 1;
        return 0;
    }
}
