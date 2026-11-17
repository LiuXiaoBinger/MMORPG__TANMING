

/// <summary>
/// 状态机调用动作状态时使用的现有接口。
/// 它约定开始、更新、结束、动画播放及移动许可；具体行为由动作组件实现。
/// </summary>
public interface IMStateTransform
{
    /// <summary>状态获得执行权时调用，初始化状态并开始动作。</summary>
    void OnGetPermission();
    /// <summary>外部强制停止状态；toDefault 表示是否回默认状态，selfTrans 表示是否同状态切换。</summary>
    /// <param name="toDefault">停止后是否回到默认状态。</param>
    /// <param name="selfTrans">是否切到同一种状态，供状态决定哪些数据可以保留。</param>
    void Stop(bool toDefault = true,bool selfTrans = false);
    /// <summary>收到移动请求时调用，状态可据此决定是否被移动打断。</summary>
    void Move();
    /// <summary>状态正常结束，执行完成回调。</summary>
    void Finish();
    /// <summary>每帧推进状态逻辑。</summary>
    /// <param name="deltaTime">本帧经过的时间，单位为秒。</param>
    void StateUpdate(float deltaTime);
    /// <summary>当前状态是否允许实体正常移动。</summary>
    bool IsActiveMovable { get; }
    /// <summary>当前状态是否允许被外部动作主动打断。</summary>
    bool IsActiveInterrupt { get; }
    /// <summary>状态期间是否显示/保留战斗载具。</summary>
    bool WithBattleVehicle { get; }
    /// <summary>播放该状态对应的 Animator 状态，返回本次动画状态名。</summary>
    /// <param name="preState">切换前的动画状态名，供实现决定过渡方式。</param>
    /// <returns>实际选择播放的动画状态名。</returns>
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