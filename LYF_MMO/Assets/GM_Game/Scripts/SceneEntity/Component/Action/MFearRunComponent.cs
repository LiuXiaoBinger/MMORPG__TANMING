/// <summary>
/// 恐惧逃跑动作状态组件。
/// 实体被恐惧控制时自动逃跑的状态，播放移动动画。
/// 与普通移动状态共用移动动画，但通过独立的状态枚举区分，
/// 避免与 MMoveActionComponent 的状态注册冲突。
/// 监听恐惧逃跑开始事件切入本状态。
/// </summary>
public sealed class MFearRunComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化恐惧逃跑状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MFearRunComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateFearRun; }

    /// <summary>恐惧逃跑复用移动动画状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_ZD_MOVE_RUN; } }

    /// <summary>注册恐惧逃跑开始事件：收到恐惧触发消息时切入逃跑状态。</summary>
    protected override void RegisterActionEvents() { RegisterActionEvent(MEventType.FearRunStarted); }
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
