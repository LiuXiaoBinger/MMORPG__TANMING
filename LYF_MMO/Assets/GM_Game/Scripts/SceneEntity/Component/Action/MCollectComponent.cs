/// <summary>
/// 把 CollectionStarted 事件接入采集动作状态，并指定 Collect 动画名。
/// 这里只负责动作状态，不发放采集奖励或计算采集进度。
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
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
