/// <summary>
/// 收到 BornStarted 时申请进入出生状态，并使用 Born 动画。
/// 本类只配置状态与事件，没有按动画时长自动结束的逻辑。
/// </summary>
public sealed class MBornComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化出生状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MBornComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateBorn; }

    /// <summary>出生状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_BORN; } }

    /// <summary>注册出生事件：收到出生触发消息时切入出生状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.BornStarted); }

    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
