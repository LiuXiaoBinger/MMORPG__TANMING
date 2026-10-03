/// <summary>
/// 出生动作状态组件。
/// 实体刚生成/重生时播放出生动画。
/// 监听出生事件切入本状态，出生动画播放完毕后回到默认状态。
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

    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
