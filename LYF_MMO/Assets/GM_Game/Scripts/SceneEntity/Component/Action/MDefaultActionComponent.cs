using MMoRpgCommon.SceneEntity;

/// <summary>
/// 默认动作状态组件。
/// 实体未进入其他动作状态时停留在默认状态，根据实体是否移动自动切换 idle/move 动画。
/// 监听移动开始/停止、战斗开始/结束事件，触发状态机标记刷新。
/// </summary>
public sealed class MDefaultActionComponent : MBasicActionComponent
{
    /// <summary>默认动作组件内部的子状态机，决定当前应选用普通/战斗/自拍/搬运/射击/组合技哪一套待机与移动动画槽位。</summary>
    public enum State : int
    {
        /// <summary>普通待机/移动。</summary>
        Normal = 0,
        /// <summary>战斗待机/移动。</summary>
        Battle = 1,
        /// <summary>自拍模式。</summary>
        Selfy = 2,
        /// <summary>搬运模式。</summary>
        Carry = 3,
        /// <summary>射击模式。</summary>
        Shoot = 4,
        /// <summary>组合技待机。</summary>
        Combo = 5, //组合技
    }
    /// <summary>移动状态变化事件处理器，用于监听移动和战斗状态切换。</summary>
    private readonly MEventHandler _onMoveChanged;
    /// <summary>进入战斗事件处理器，负责把实体默认待机切换为战斗待机。</summary>
    private readonly MEventHandler _onCombatStarted;
    /// <summary>退出战斗事件处理器，负责把实体默认待机恢复为普通待机。</summary>
    private readonly MEventHandler _onCombatEnded;
    /// <summary>当前默认动作子状态。</summary>
    private State _defaultState = State.Normal;
    /// <summary>跑步脚步声触发的动画归一化时间点表。</summary>
    private float[] _footStepsTimes;
    /// <summary>当前应触发的脚步声时间点下标。</summary>
    private int _footStepTimeIdx = 0;
    /// <summary>上一帧记录的动画归一化时间，用于检测动画回绕。</summary>
    private float _lastStepTimeIdx = 0;
    /// <summary>获取或设置移动音效是否循环播放。</summary>
    public bool MoveAudioLoop { get; set; } = false;
    /// <summary>构造函数，初始化默认状态并注册移动事件回调。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MDefaultActionComponent(ROObject owner) : base(owner)
    {
        _selfState = StateDefine.kStateDefault;
        _onMoveChanged = new MEventHandler(OnMoveChanged);
        _onCombatStarted = new MEventHandler(OnCombatStarted);
        _onCombatEnded = new MEventHandler(OnCombatEnded);
    }

    /// <summary>
    /// 根据战斗状态、移动状态和 Shift 走跑输入选择 Animator 槽位；静止时返回对应待机槽位。
    /// </summary>
    public override string StateName
    {
        get
        {
            // 默认动作是否应显示移动；本地主角同时识别真实方向与有效导航，保留落地运行态判断。
            bool shouldPlayMovement = ShouldPlayMovementAnimation();
            // 射击姿态优先级高于战斗待机，战斗事件不能改写射击中的静止或移动表现。
            if (_defaultState == State.Shoot)
            {
                if (shouldPlayMovement)
                {
                    if (Entity.MoveComp != null && Entity.MoveComp.IsRunning)
                    {
                        return MAnimator.STATE_ZD_MOVE_RUN;
                    }

                    return MAnimator.STATE_ZD_MOVE_WALK;
                }

                return MAnimator.STATE_SHOOT_IDLE;
            }

            // 战斗中静止时固定使用 FIdle；普通 Idle 和 FIdle 的真实剪辑由 MIdleComponent 预先装配。
            // 是否属于静止待机姿态：不播移动、非跳跃、非搬运。
            bool battleIdlePose = !shouldPlayMovement && !Entity.IsJump && !Entity.InCarrying;
            if (Entity.BattleIdle && battleIdlePose)
            {
                return MAnimator.STATE_FIDLE;
            }
            switch (_defaultState)
            {
                default:
                    if (shouldPlayMovement)
                    {
                        // 当前输入是否要求跑步；跳跃落地后仍由移动组件保留该模式。
                        bool isRunning = Entity.MoveComp != null && Entity.MoveComp.IsRunning;
                        if (Entity.BattleIdle)
                        {
                            if (isRunning)
                            {
                                return MAnimator.STATE_ZD_MOVE_RUN;
                            }

                            return MAnimator.STATE_ZD_MOVE_WALK;
                        }

                        if (isRunning)
                        {
                            return MAnimator.STATE_NONZD_MOVE_RUN;
                        }

                        return MAnimator.STATE_NONZD_MOVE_WALK;
                    }

                    return MAnimator.STATE_IDLE;
            }
        }
    }

    /// <summary>判断默认动作是否应播放移动动画；本地主角识别方向或导航，其他实体沿用同步移动状态。</summary>
    /// <returns>true 表示默认状态应直接播放 Walk 或 Run；false 表示播放对应 Idle。</returns>
    private bool ShouldPlayMovementAnimation()
    {
        if (Entity == null)
        {
            return false;
        }
        if (Entity.IsPlayer && Entity.MoveComp != null)
        {
            if (!Entity.MoveComp.IsMoving)
            {
                return false;
            }
            // 导航不会伪造按键保持；有效路线也应沿原走跑槽位播放移动动画。
            if (Entity.NavComp != null && Entity.NavComp.IsNavigation)
            {
                return true;
            }
            return Entity.MoveComp.HasDirectionalInput;
        }
        return Entity.IsMoving;
    }

    /// <summary>判断跳跃表现组件是否正在播放落地动画，避免默认动作在同帧覆盖 Jump_End。</summary>
    /// <returns>true 表示落地动画尚未完成，当前帧必须保持跳跃表现。</returns>
    private bool IsLandingAnimationPlaying()
    {
        if (Entity == null || Entity.Components == null)
        {
            return false;
        }

        // 当前实体的跳跃表现组件；未注册跳跃组件的实体不需要落地动画保护。
        MJumpComponent jumpComponent;
        if (!Entity.Components.TryGet(EntityComponentType.Jump, out jumpComponent))
        {
            return false;
        }
        return jumpComponent.IsLandingAnimationPlaying;
    }

    /// <summary>
    /// 注册默认状态关心的事件：移动开始/停止、战斗开始/结束。
    /// 这些事件触发时需要刷新状态机的动画表现。
    /// </summary>
    protected override void RegisterActionEvents()
    {
        RegisterEvent(MEventType.MoveStarted, _onMoveChanged);
        RegisterEvent(MEventType.MoveStopped, _onMoveChanged);
        RegisterEvent(MEventType.CombatStarted, _onCombatStarted);
        RegisterEvent(MEventType.CombatEnded, _onCombatEnded);
    }

    /// <summary>
    /// 移动状态变化时的回调：如果当前正处于默认状态，则请求刷新动画播放。
    /// </summary>
    /// <param name="eventArg">触发事件的参数。</param>
    private void OnMoveChanged(IEventArg eventArg)
    {
        if (Entity == null || Entity.IsJump)
        {
            return;
        }
        RefreshDefaultAnimation();
    }

    /// <summary>
    /// 处理进入战斗事件。
    /// 仅更新实体的默认待机标记；特殊状态仍由状态机的当前组件控制，不会被本组件抢占。
    /// </summary>
    /// <param name="eventArg">进入战斗事件参数，当前事件不携带额外数据。</param>
    private void OnCombatStarted(IEventArg eventArg)
    {
        if (Entity == null)
        {
            return;
        }

        Entity.BattleIdle = true;
        RefreshDefaultAnimation();
    }

    /// <summary>
    /// 处理退出战斗事件，将默认待机标记恢复为普通状态。
    /// </summary>
    /// <param name="eventArg">退出战斗事件参数，当前事件不携带额外数据。</param>
    private void OnCombatEnded(IEventArg eventArg)
    {
        if (Entity == null)
        {
            return;
        }

        Entity.BattleIdle = false;
        RefreshDefaultAnimation();
    }

    /// <summary>
    /// 当前状态确实是默认动作时才请求状态机重播动画，防止战斗事件覆盖技能、受击等特殊状态。
    /// </summary>
    private void RefreshDefaultAnimation()
    {
        if (Entity != null && Entity.Machine != null && 
            Entity.Machine.CurrentState == this)
        {
            Entity.Machine.MarkTriggered();
        }
    }
    protected override void EventSubscribe()
    {
    }

    protected override void EventUnsubscribe()
    {
    }

    /// <summary>
    /// 播放一次脚步声：移动中且动画机有效时播放移动音效，并前进到下一个脚步声时间点。
    /// </summary>
    private void FootSteps()
    {
        // 任一前置条件不满足即跳过本次脚步检查；按原 || 短路顺序逐组早退。
        if (Entity == null || !Entity.IsMoving || Entity.Model == null)
        {
            return;
        }
        if (Entity.Model.Ator == null || Entity.Model.Ator.Speed <= 0 ||
            Entity.Model.Ator.GetCurrentAnimInfo() == null)
        {
            return;
        }
        if (_footStepsTimes == null || _footStepsTimes.Length == 0)
        {
            return;
        }

        // 当前项目尚未接入脚步音效，仅维护动画归一化进度和下一个触发下标。
        _footStepTimeIdx++;
        _footStepTimeIdx %= _footStepsTimes.Length;
        _lastStepTimeIdx = Entity.Model.Ator.GetCurrentAnimInfo().NormalizedTime % 1;
    }
    /// <summary>
    /// 逐帧更新：移动且处于跑步模式时同步动画机速度到属性跑速；
    /// 非循环移动音效模式下，根据当前动画归一化时间是否越过脚步声时间点来触发 FootSteps。
    /// </summary>
    /// <param name="fDeltaT">距上一帧时间（秒）。</param>
    protected override void OnUpdate(float fDeltaT)
    {
        // 实体是否仍处于移动状态。
        bool stillMoving = Entity.IsMoving;
        // 仅在移动且处于跑步模式时读取属性跑速，保持原条件的短路顺序。
        if (stillMoving && Entity.MoveComp != null && Entity.MoveComp.IsRunning)
        {
            if (!MCommonFunctions.IsEqual(Entity.AtorSpeed, Entity.AttrComp.RunAtorSpeed))
            {
                Entity.AtorSpeed = Entity.AttrComp.RunAtorSpeed;
            }
        }
        if (MoveAudioLoop)
        {
            return;
        }

        // 静止时不读取移动动画信息；原条件首先检查移动状态。
        if (!stillMoving)
        {
            return;
        }
        if (Entity.Model.Ator != null
            && Entity.Model.Ator.InState(StateName)
            && Entity.Model.Ator.GetCurrentAnimInfo() != null)
        {
            // 当前移动动画的归一化进度，用于跨过配置脚步点时触发一次音效。
            float time = Entity.Model.Ator.GetCurrentAnimInfo().NormalizedTime % 1;
            if (MCommonFunctions.IsEqual(_lastStepTimeIdx, time))
            {
                return;
            }

            if (MCommonFunctions.IsGreater(_lastStepTimeIdx, time))
            {
                _lastStepTimeIdx -= 1;
            }
            // 脚步点表与当前下标本身是否有效：表非空、下标非负且不越界。
            bool footTimesValid = _footStepsTimes != null
                && _footStepTimeIdx >= 0
                && _footStepTimeIdx < _footStepsTimes.Length;
            // 只有表和下标有效才读取脚步点；未接脚步配置的角色仍更新动画进度。
            if (footTimesValid)
            {
                if (time >= _footStepsTimes[_footStepTimeIdx]
                    && _lastStepTimeIdx <= _footStepsTimes[_footStepTimeIdx])
                {
                    FootSteps();
                }
            }

            _lastStepTimeIdx = time;
        }
    }

    /// <summary>
    /// 播放默认动作动画：已在目标状态则直接返回；
    /// 技能动作结束后直接恢复默认动作，并按移动/静止、是否公共坐骑设置动画机速度与音效。
    /// </summary>
    /// <param name="preState">上一个动画机状态名。</param>
    /// <returns>实际播放的动画机状态名。</returns>
    public override string PlayAnim(string preState)
    {
        if (Entity != null && (Entity.IsJump ||
                               IsLandingAnimationPlaying()))
        {
            return preState;
        }
        if (Entity.Model.Ator == null)
        {
            return preState;
        }
        if (Entity.Model.Ator.InState(StateName))
        {
            return StateName;
        }
        _lastStepTimeIdx = 0;
        // 技能动作窗已结束；本项目未调度RO后摇回调，不能只返回状态名而实际不切动画。
        Entity.IsHouyao = false;
        // 基类完成剪辑装配和 Animator 切换后返回的实际状态名。
        string result = base.PlayAnim(StateName);
        if (Entity.IsMoving)
        {
            if (Entity.IsRidePubVehicle)
            {
                Entity.AtorSpeed = 1;
            }
            else if (Entity.MoveComp != null && Entity.MoveComp.IsRunning)
            {
                Entity.AtorSpeed = Entity.AttrComp.RunAtorSpeed;
            }
            else
            {
                Entity.AtorSpeed = 1f;
            }
            if (MoveAudioLoop)
            {
                FootSteps();
            }
        }
        else
        {
            Entity.AtorSpeed = 1;
        }

        return result;
    }
}
