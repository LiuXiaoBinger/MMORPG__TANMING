using MMoRpgCommon.SceneEntity;

/// <summary>客户端实体的基因组件；具体节点直接获得宿主并通过其 AttrComp 执行效果。</summary>
public class MGeneComponent : EntityComponentBase
{
    /// <summary>复用项目既有事件处理器，订阅和解绑始终使用同一实例。</summary>
    private readonly MEventHandler destroyingHandler;
    /// <summary>当前宿主独立持有的基因节点集合。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>沿用原 Gene 组件编号。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Gene; } }
    /// <summary>绑定实际客户端实体。</summary>
    /// <param name="owner">拥有当前基因组件的实体。</param>
    public MGeneComponent(ROObject owner) : base(owner) { destroyingHandler = new MEventHandler(OnDestroying); }
    /// <summary>将实际实体传入公共模块，不生成资源结果中转对象。</summary>
    public override void Initialize() { Runtime = new GeneRuntime(Entity, LubanMgr.Instance.GetGeneFactory()); }
    /// <summary>推进周期与寿命，节点到点直接调用自己的 OnCall。</summary>
    /// <param name="deltaMilliseconds">本帧毫秒数。</param>
    public override void Update(int deltaMilliseconds) { Runtime.Update(deltaMilliseconds); }
    /// <summary>使用既有销毁事件，在普通属性组件回收前撤销节点。</summary>
    protected override void EventSubscribe() { RegisterEvent(MEventType.Destroying, destroyingHandler); }
    /// <summary>专用组件卸载时解除销毁事件订阅。</summary>
    protected override void EventUnsubscribe() { RemoveEventListener(MEventType.Destroying, destroyingHandler); }
    /// <summary>角色正式销毁顺序先通知此事件，再回收属性和专用组件。</summary>
    /// <param name="args">销毁通知无额外载荷。</param>
    private void OnDestroying(IEventArg args) { Runtime.Clear(); }
    /// <summary>对象回收时执行节点撤销并清空集合；正常销毁时集合已由销毁事件清空。</summary>
    public override void Shutdown() { Runtime.Clear(); }
}
