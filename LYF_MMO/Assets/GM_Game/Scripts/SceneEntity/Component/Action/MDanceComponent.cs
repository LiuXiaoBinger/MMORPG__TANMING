/// <summary>
/// 跳舞动作状态组件。
/// 实体进行跳舞表演时的状态，属于社交/休闲类动作。
/// 监听跳舞开始事件切入本状态。
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
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
