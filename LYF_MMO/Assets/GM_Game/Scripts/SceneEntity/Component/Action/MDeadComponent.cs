/// <summary>
/// 收到 Death 时申请进入死亡状态，状态期间禁止正常移动。
/// 收到 Revived 时结束此状态；死亡动画名由基类播放流程使用。
/// </summary>
public sealed class MDeadComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化死亡状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MDeadComponent(ROObject owner) : base(owner)
    {
        _selfState = StateDefine.kStateDead;
    }

    /// <summary>死亡状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_DEAD; } }

    /// <summary>死亡状态下实体不可移动。</summary>
    public override bool IsActiveMovable { get { return false; } }

    /// <summary>注册死亡事件以申请进入状态，注册复活事件以调用 Finish 结束状态。</summary>
    protected override void RegisterActionEvents()
    {
        RegisterActionEvent(MEventType.Death);
        RegisterFinishEvent(MEventType.Revived);
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
