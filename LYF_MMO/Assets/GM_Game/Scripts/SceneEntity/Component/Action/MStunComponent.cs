/// <summary>
/// 眩晕动作状态组件。
/// 实体被眩晕控制时播放眩晕动画，眩晕状态下不可移动。
/// 监听眩晕开始事件切入本状态。
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
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
