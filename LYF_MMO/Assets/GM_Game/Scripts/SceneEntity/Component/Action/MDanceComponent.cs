/// <summary>
/// 把 DanceStarted 事件接入跳舞状态，并指定 Dance 动画名。
/// </summary>
public sealed class MDanceComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化跳舞状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MDanceComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateDance; }

    /// <summary>跳舞状态对应的动画机状态名。</summary>
    public override string StateName { get { return "Dance"; } }

    /// <summary>注册跳舞开始事件：收到跳舞触发消息时切入跳舞状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.DanceStarted); }
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
