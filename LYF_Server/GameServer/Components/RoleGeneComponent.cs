/// <summary>在线角色自己的基因组件；装备、技能和临时效果均进入此集合。</summary>
public class RoleGeneComponent : RoleComponentBase
{
    /// <summary>当前角色直接持有的 GeneBase 节点容器。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>绑定拥有独立属性组件的真实在线角色。</summary>
    /// <param name="owner">本组件所属角色。</param>
    public RoleGeneComponent(OnlineRole owner) : base(owner) { }
    /// <summary>把真实角色交给公共节点模块，具体效果直接调用该角色的属性组件。</summary>
    public override void Initialize() { Runtime = new GeneRuntime(Owner, LubanMgr.Instance.GetGeneFactory()); }
    /// <summary>推进节点并在属性、资源或寿命变化后发布最终属性。</summary>
    /// <param name="deltaMilliseconds">本次逻辑帧的毫秒跨度。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 到期移除可能改变属性；资源由周期节点在推进时直接结算。
        int previousCount = Runtime.ActiveGenes.Count;
        Runtime.Update(deltaMilliseconds);
        if (Owner.AttrComp.ResourcesChanged || previousCount != Runtime.ActiveGenes.Count) Owner.AttrComp.NotifyAttributeUpdate();
    }
    /// <summary>把真实战斗事件交给各事件节点的 OnCall。</summary>
    /// <param name="args">实际事件类型与目标。</param>
    /// <returns>成功激活的子节点数量。</returns>
    public int Trigger(GeneTriggerArgs args)
    {
        // 节点已直接完成属性和资源修改，事件结束统一通知最终面板。
        int activated = Runtime.Trigger(args);
        if (activated > 0) Owner.AttrComp.NotifyAttributeUpdate();
        return activated;
    }
    /// <summary>先执行节点撤销，再由角色组件容器释放属性组件。</summary>
    public override void Dispose() { Runtime.Clear(); base.Dispose(); }
}
