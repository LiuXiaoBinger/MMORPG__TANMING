/// <summary>
/// 吟唱动作状态组件。
/// 实体处于施法吟唱阶段时的状态，播放吟唱动画。
/// 监听吟唱开始事件切入本状态，吟唱结束后回到默认状态。
/// </summary>
public sealed class MSingingComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化吟唱状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MSingingComponent(ROObject owner) : base(owner)
    {
        _selfState = StateDefine.kStateSinging;
    }

    /// <summary>吟唱状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_SINGING; } }

    /// <summary>注册吟唱开始事件：收到吟唱开始消息时切入吟唱状态。</summary>
    protected override void RegisterActionEvents()
    {
        RegisterActionEvent(MEventType.SingingStarted);
    }
    
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
