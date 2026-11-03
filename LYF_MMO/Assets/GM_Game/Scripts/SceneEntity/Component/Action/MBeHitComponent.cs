/// <summary>
/// 收到 DamageReceived 时申请进入受击状态，并使用 BeHit 动画。
/// 真实受击动作播放完成后结束状态，由状态机回到默认待机。
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

    /// <summary>按实际 Animator 状态播放进度结束受击，不使用固定时长截断不同模型。</summary>
    /// <param name="deltaTime">本帧秒数；动画进度由已有 MAnimator 驱动。</param>
    protected override void OnUpdate(float deltaTime)
    {
        // 取得当前实体真实模型上的动画器。
        UnityEngine.Animator animator = Entity.Model.Ator.UnityAnimator;
        if (animator == null) return;
        // 过渡完成后才判断当前受击状态结束，避免读取上一状态的 normalizedTime。
        UnityEngine.AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(StateName) && state.normalizedTime >= 1f && !animator.IsInTransition(0)) Finish();
    }

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
