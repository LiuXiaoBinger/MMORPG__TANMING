/// <summary>
/// 死亡动作状态组件。
/// 实体死亡时播放死亡动画，死亡状态下实体不可移动。
/// 监听死亡事件切入死亡状态，监听复活事件结束死亡状态。
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

    /// <summary>注册死亡和复活事件：死亡时切入状态，复活时正常结束回到默认状态。</summary>
    protected override void RegisterActionEvents()
    {
        RegisterActionEvent(MEventType.Death);
        RegisterFinishEvent(MEventType.Revived);
    }
    
    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
