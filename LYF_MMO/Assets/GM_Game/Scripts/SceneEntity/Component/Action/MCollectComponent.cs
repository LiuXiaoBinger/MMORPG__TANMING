/// <summary>
/// 采集动作状态组件。
/// 实体进行资源采集（如采药、挖矿）时的状态。
/// 监听采集开始事件切入本状态。
/// </summary>
public sealed class MCollectComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化采集状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MCollectComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateCollect; }

    /// <summary>采集状态对应的动画机状态名。</summary>
    public override string StateName { get { return "Collect"; } }

    /// <summary>注册采集开始事件：收到采集触发消息时切入采集状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.CollectionStarted); }
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
