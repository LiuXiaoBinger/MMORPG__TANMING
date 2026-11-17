/// <summary>
/// 把 FearRunStarted 事件接入独立的恐惧状态，动画复用战斗跑步。
/// 本类没有选择逃跑方向或驱动实体移动。
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
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
