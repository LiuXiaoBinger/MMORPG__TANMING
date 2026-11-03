/// <summary>
/// 把 ClimbStarted 事件接入攀爬状态，并指定 Climb 动画名。
/// 本类不处理梯子检测、攀爬位移或结束条件。
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
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
