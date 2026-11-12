/// <summary>
/// 静止玩家收到普通伤害时播放 BeHit，移动或跳跃继续原动作，怪物保留原受击规则。
/// 受击动作播完或玩家主动移动后结束状态，由默认动作继续待机或走跑。
/// </summary>
public sealed class MBeHitComponent : MBasicActionComponent
{
    /// <summary>普通伤害的专用回调；拒绝受击时保留当前技能、死亡及移动状态。</summary>
    private readonly MEventHandler damageReceivedHandler;

    /// <summary>构造函数，初始化受击状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MBeHitComponent(ROObject owner) : base(owner)
    {
        _selfState = StateDefine.kStateBehit;
        damageReceivedHandler = new MEventHandler(OnDamageReceived);
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

    /// <summary>注册普通受伤入口，玩家忽略动画的分支不经过基类强制回默认状态。</summary>
    protected override void RegisterActionEvents()
    {
        RegisterEvent(MEventType.DamageReceived, damageReceivedHandler);
    }

    /// <summary>普通伤害不抢占玩家移动、跳跃及状态机禁止打断的动作；怪物沿原流程受击。</summary>
    /// <param name="eventArg">现有伤害表现事件无额外载荷，血量由实体在派发前更新。</param>
    private void OnDamageReceived(IEventArg eventArg)
    {
        if (Entity is RoleEntity)
        {
            if (Entity.IsMoving || Entity.IsJump) return;
            if (!Entity.Machine.CanTransfer(SelfState)) return;
        }
        OnActionEvent(eventArg);
    }

    /// <summary>玩家主动移动立即结束普通受击，让默认状态选择走跑动画；怪物规则保持原样。</summary>
    public override void Move()
    {
        if (Entity is RoleEntity)
        {
            Stop();
            return;
        }
        base.Move();
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
