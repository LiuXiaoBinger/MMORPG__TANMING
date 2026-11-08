using System;
using MMoRpgCommon.SceneEntity;

/// <summary>客户端实体的基因组件；持有服务器同步的具体节点，只展示状态和倒计时。</summary>
public class MGeneComponent : EntityComponentBase
{
    /// <summary>复用项目既有事件处理器，订阅和解绑始终使用同一实例。</summary>
    private readonly MEventHandler destroyingHandler;
    /// <summary>当前宿主独立持有的基因节点集合。</summary>
    public ClientGeneRuntime Runtime { get; private set; }
    /// <summary>列表、层数、名称、次数等真实展示变化；普通倒计时不触发。</summary>
    public event Action Changed;
    /// <summary>沿用原 Gene 组件编号。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Gene; } }
    /// <summary>绑定实际客户端实体。</summary>
    /// <param name="owner">拥有当前基因组件的实体。</param>
    public MGeneComponent(ROObject owner) : base(owner) { destroyingHandler = new MEventHandler(OnDestroying); }
    /// <summary>绑定实际实体的联机展示运行器，不能再次结算服务器属性。</summary>
    public override void Initialize() { Runtime = new ClientGeneRuntime(Entity, LubanMgr.Instance.GetGeneFactory()); }
    /// <summary>只推进有限节点的显示倒计时，永久节点不做逐帧业务。</summary>
    /// <param name="deltaMilliseconds">本帧毫秒数。</param>
    public override void Update(int deltaMilliseconds) { Runtime.Update(deltaMilliseconds); }
    /// <summary>把本角色权威通知应用到具体节点，只有可见数据变化才通知界面。</summary>
    /// <param name="notification">已验证角色身份的11162通知。</param>
    public void ApplySynchronization(RoleAttributeUpdateNtf notification)
    {
        if (Runtime.ApplySynchronization(notification)) Changed?.Invoke();
    }
    /// <summary>使用既有销毁事件，在属性组件回收前清空显示节点。</summary>
    protected override void EventSubscribe() { RegisterEvent(MEventType.Destroying, destroyingHandler); }
    /// <summary>专用组件卸载时解除销毁事件订阅。</summary>
    protected override void EventUnsubscribe() { RemoveEventListener(MEventType.Destroying, destroyingHandler); }
    /// <summary>角色正式销毁顺序先通知此事件，再回收属性和专用组件。</summary>
    /// <param name="args">销毁通知无额外载荷。</param>
    private void OnDestroying(IEventArg args) { Runtime.Clear(); }
    /// <summary>对象回收时清空展示集合和订阅；正常销毁时集合已清空，不回滚服务器属性。</summary>
    public override void Shutdown() { Runtime.Clear(); Changed = null; }
}
