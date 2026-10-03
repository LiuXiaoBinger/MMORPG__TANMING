/// <summary>
/// 受击动作状态组件。
/// 实体受到伤害时播放受击动画。
/// 监听受伤事件切入受击状态，受击动画播放完毕后自动回到默认状态。
/// </summary>
public sealed class MBeHitComponent : MBasicActionComponent
{
    /// <summary>构造函数，初始化受击状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MBeHitComponent(ROObject owner) : base(owner)
    {
        _selfState = StateDefine.kStateBehit;
    }

    /// <summary>受击状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_BEHIT; } }

    /// <summary>注册受伤事件：收到伤害事件时切入受击状态。</summary>
    protected override void RegisterActionEvents()
    {
        RegisterActionEvent(MEventType.DamageReceived);
    }

    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }
}
