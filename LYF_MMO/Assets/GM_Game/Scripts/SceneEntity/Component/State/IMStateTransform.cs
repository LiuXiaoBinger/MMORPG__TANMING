

/// <summary>
/// 动作状态接口，定义一个可被状态机调度的动作状态所需的全部契约。
/// 所有具体动作状态（默认、移动、吟唱、技能、受击、死亡等）都实现该接口，
/// 由 <see cref="MStateMachine"/> 统一调度其生命周期、权限和动画播放。
/// </summary>
public interface IMStateTransform
{
    /// <summary>状态获得执行权时调用，初始化状态并开始动作。</summary>
    void OnGetPermission();
    /// <summary>外部强制停止状态；toDefault 表示是否回默认状态，selfTrans 表示是否同状态切换。</summary>
    void Stop(bool toDefault = true,bool selfTrans = false);
    /// <summary>收到移动请求时调用，状态可据此决定是否被移动打断。</summary>
    void Move();
    /// <summary>状态正常结束，执行完成回调。</summary>
    void Finish();
    /// <summary>每帧推进状态逻辑。</summary>
    void StateUpdate(float deltaTime);
    /// <summary>当前状态是否允许实体正常移动。</summary>
    bool IsActiveMovable { get; }
    /// <summary>当前状态是否允许被外部动作主动打断。</summary>
    bool IsActiveInterrupt { get; }
    /// <summary>状态期间是否显示/保留战斗载具。</summary>
    bool WithBattleVehicle { get; }
    /// <summary>播放该状态对应的 Animator 状态，返回本次动画状态名。</summary>
    string PlayAnim(string preState);
    /// <summary>是否使用状态自定义的速度曲线。</summary>
    bool IsUsingCurve { get; }
    /// <summary>状态动画或动作速度倍率。</summary>
    float Speed { get; }
    /// <summary>是否处于动作前置表现阶段。</summary>
    bool IsPreperformance { get; }
    /// <summary>状态对应的公共状态枚举。</summary>
    StateDefine SelfState { get; }
    /// <summary>Animator 使用的状态名称。</summary>
    string StateName { get; }
    /// <summary>状态是否已经结束。</summary>
    bool IsFinished { get; }

    /// <summary>该状态使用的碰撞层。</summary>
    int CollisionLayer { get; }
}