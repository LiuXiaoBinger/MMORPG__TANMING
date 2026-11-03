/// <summary>
/// 把 SpecialActionStarted 事件接入特殊动作状态。
/// 实际动画由基类根据表现表中的休闲动作或 NPC 欢迎动作查找。
/// </summary>
public sealed class MSpecialComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化特殊动作状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MSpecialComponent(ROObject owner) : base(owner) 
    { _selfState = StateDefine.kStateSpecial; }

    /// <summary>特殊动作状态对应的动画机状态名。</summary>
    public override string StateName
    {
        get { return MAnimator.STATE_SPECIAL; }
    }

    /// <summary>注册特殊动作开始事件：收到特殊动作触发消息时切入本状态。</summary>
    protected override void RegisterActionEvents() 
    { RegisterActionEvent(MEventType.SpecialActionStarted); }
    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
