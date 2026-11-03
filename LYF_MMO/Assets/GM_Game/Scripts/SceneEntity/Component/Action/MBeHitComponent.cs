/// <summary>
/// 收到 DamageReceived 时申请进入受击状态，并使用 BeHit 动画。
/// 本类没有动画结束计时，也没有自行返回默认状态的逻辑。
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

    /// <summary>状态事件由 RegisterActionEvents 统一登记，这里不增加订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>已登记的状态事件由组件基类统一解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
