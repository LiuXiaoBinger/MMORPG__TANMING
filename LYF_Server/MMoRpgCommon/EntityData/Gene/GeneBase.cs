using System;
using System.Collections.Generic;

/// <summary>一次创建对应一个独立运行节点；自身行为由具体类型实现，全部类型均可关联子基因。</summary>
public abstract class GeneBase
{
    /// <summary>工厂使用的配置编号；节点不保留配置行引用。</summary>
    public int GeneID { get; set; }
    /// <summary>配置或服务器提供的基因名称，仅用于展示，不参与效果执行。</summary>
    public string DisplayName { get; set; } = string.Empty;
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
    private ulong sourceId;
    /// <summary>当前根节点的来源编号。普通子节点读取其父节点的值。</summary>
    public ulong SourceId
    {
        get { if (Parent != null) return Parent.SourceId; return sourceId; }
        private set { sourceId = value; }
    }
    /// <summary>同一来源的独立效果槽；0 为普通施加，装备槽用于区分重复编号。</summary>
    private int sourceSlot;
    /// <summary>当前根节点的来源槽位。普通子节点读取其父节点的值。</summary>
    public int SourceSlot
    {
        get { if (Parent != null) return Parent.SourceSlot; return sourceSlot; }
        private set { sourceSlot = value; }
    }
    /// <summary>实际施加角色编号，资源归因使用它而不是装备 UID。</summary>
    private ulong firerId;
    /// <summary>当前根节点实际施加角色编号。普通子节点读取其父节点的值。</summary>
    public ulong FirerId
    {
        get { if (Parent != null) return Parent.FirerId; return firerId; }
        private set { firerId = value; }
    }
    /// <summary>根节点当前叠加层数；已激活的普通子项共用此层数。</summary>
    private int stackCount;
    /// <summary>当前根节点叠加层数。普通子节点读取其父节点的值。</summary>
    public int StackCount
    {
        get { if (Parent != null) return Parent.StackCount; return stackCount; }
        internal set { stackCount = value; }
    }
    /// <summary>根节点剩余寿命，单位毫秒；-1 为常驻，0 为已到期。</summary>
    public long RemainingMilliseconds { get; internal set; }
    /// <summary>魔神塔等已有外部寿命的只读查询；仅同步时读取，不参与本容器到期删除。</summary>
    public Func<long> ReadExternalRemainingMilliseconds { get; set; }
    /// <summary>取得真实展示寿命；外部 Buff 保持自己的 End，普通节点直接使用运行寿命。</summary>
    /// <returns>剩余毫秒，-1 为永久；不会推进或撤销任何节点。</returns>
    public long GetDisplayRemainingMilliseconds()
    {
        if (ReadExternalRemainingMilliseconds != null) return ReadExternalRemainingMilliseconds();
        return RemainingMilliseconds;
    }
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
    /// <summary>属性、状态和即时效果在挂载时调用 OnCall；周期与事件类型自行决定调用时机。</summary>
    public virtual bool CallsOnAttach { get { return true; } }
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

    /// <summary>客户端接收权威来源、层数与显示寿命，只赋值，不调用施加或撤销行为。</summary>
    /// <param name="source">服务器确认的施加者或装备 UID。</param>
    /// <param name="slot">同来源中的独立效果槽。</param>
    /// <param name="firer">实际施加角色编号。</param>
    /// <param name="stacks">服务器当前层数。</param>
    /// <param name="remaining">剩余显示毫秒，-1 表示永久；到零等待服务器移除。</param>
    public void ApplyDisplayState(ulong source, int slot, ulong firer, int stacks, long remaining)
    {
        SourceId = source;
        SourceSlot = slot;
        FirerId = firer;
        StackCount = stacks;
        RemainingMilliseconds = remaining;
    }

    /// <summary>客户端仅推进显示寿命，不能由本地到期撤销服务器属性或移除节点。</summary>
    /// <param name="milliseconds">本帧经过的正毫秒数。</param>
    public void AdvanceDisplayTime(int milliseconds)
    {
        if (RemainingMilliseconds > 0) RemainingMilliseconds = Math.Max(0, RemainingMilliseconds - milliseconds);
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

    /// <summary>具体基因独立执行行为，可以通过角色属性组件连续修改多个属性。</summary>
    /// <param name="role">当前实际承受效果的角色或怪物。</param>
    /// <param name="args">事件基因接收的战斗事件；普通施加和周期调用使用默认事件值。</param>
    /// <returns>事件实际激活的子节点数；普通效果成功执行返回 1。</returns>
    public abstract int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs));

    /// <summary>移除或刷新前撤销本节点施加的持续修正；即时与周期资源不回滚。</summary>
    /// <param name="role">本节点所属的真实角色。</param>
    public virtual void OnRemove(IGeneOwner role) { }

    /// <summary>周期节点推进自身计时，到点后直接执行自己的 OnCall。</summary>
    /// <param name="milliseconds">本帧仍在根寿命内的毫秒数。</param>
    /// <param name="role">本节点所属的真实角色。</param>
    public virtual void AdvanceTime(long milliseconds, IGeneOwner role) { }

    /// <summary>把本节点的每层资源数值换算为实际点数，百分数 10 表示上限的 10%。</summary>
    /// <param name="maximum">当前生效的生命或法力上限。</param>
    /// <returns>计入层数后的资源点数，向零取整。</returns>
    protected int ResolveAmount(int maximum)
    {
        if (ValueType == 0) return (int)(Value * StackCount);
        return (int)(maximum * (double)(Value * StackCount) / 100);
    }

    /// <summary>事件的普通子项以独立节点施加到目标，继续保留原效果来源。</summary>
    /// <param name="role">收到本次事件的原角色。</param>
    /// <param name="args">事件类型和实际对方。</param>
    /// <param name="target">上层事件选中的承受效果容器。</param>
    /// <returns>成功施加返回 1，目标或场景不适用返回 0。</returns>
    internal virtual int ActivateFromEvent(IGeneOwner role, GeneTriggerArgs args, GeneRuntime target)
    {
        if (TargetOpponent) target = args.TargetRuntime;
        if (target == null) return 0;
        if (target.AddGene(Clone(), SourceId, 0, FirerId) != null) return 1;
        return 0;
    }
}
