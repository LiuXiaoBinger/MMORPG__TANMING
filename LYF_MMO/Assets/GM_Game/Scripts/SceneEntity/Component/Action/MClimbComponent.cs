/// <summary>
/// 攀爬动作状态组件。
/// 实体进行攀爬动作（如爬梯子、攀岩）时的状态。
/// 监听攀爬开始事件切入本状态。
/// </summary>
public sealed class MClimbComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化攀爬状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MClimbComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateClimbing; }

    /// <summary>攀爬状态对应的动画机状态名。</summary>
    public override string StateName { get { return "Climb"; } }

    /// <summary>注册攀爬开始事件：收到攀爬触发消息时切入攀爬状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.ClimbStarted); }
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
