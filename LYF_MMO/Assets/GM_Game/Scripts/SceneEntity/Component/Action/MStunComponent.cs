/// <summary>
/// 把 StunStarted 事件接入眩晕状态，使用眩晕动画并禁止正常移动。
/// 本类不记录眩晕持续时间。
/// </summary>
public sealed class MStunComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化眩晕状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MStunComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateStun; }

    /// <summary>眩晕状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_STUN; } }

    /// <summary>眩晕状态下实体不可移动。</summary>
    public override bool IsActiveMovable { get { return false; } }

    /// <summary>注册眩晕开始事件：收到眩晕触发消息时切入眩晕状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.StunStarted); }
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
