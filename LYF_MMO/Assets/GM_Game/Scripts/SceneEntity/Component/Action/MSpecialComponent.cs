/// <summary>
/// 特殊动作状态组件。
/// 用于播放特殊表情、交互动作等非战斗类特殊动画。
/// 监听特殊动作开始事件切入本状态。
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
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
