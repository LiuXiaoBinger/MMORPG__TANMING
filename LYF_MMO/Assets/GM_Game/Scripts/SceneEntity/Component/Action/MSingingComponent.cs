/// <summary>
/// 把 SingingStarted 事件接入吟唱状态，并指定吟唱动画。
/// 本类没有吟唱计时或结束事件；技能自身的阶段推进由技能代码处理。
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
    
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
