using cfg;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 角色跳跃表现组件。
/// 组件只负责读取 PresentTable 的三段跳动画并驱动动画器，
/// 一段、二段和三段跳统一复用 JumpStart、JumpLoop、JumpEnd 三个状态槽。
/// 跳跃位移和重力仍由移动组件负责，本组件不修改实体坐标。
/// </summary>
public sealed class MJumpComponent : EntityComponentBase
{
    /// <summary>本组件使用独立类型，避免覆盖待机表现组件的唯一索引。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Jump; }
    }

    /// <summary>跳跃三条动画是否已登记到实体动画器。</summary>
    private bool _isPrepared;

    /// <summary>当前是否处于空中阶段，起跳和循环动画都属于该阶段。</summary>
    private bool _isJumping;

    /// <summary>当前是否正在播放落地动画。</summary>
    private bool _isEnding;

    /// <summary>获取当前是否处于落地动画阶段；默认动作状态据此避免提前覆盖 Jump_End。</summary>
    public bool IsLandingAnimationPlaying
    {
        get { return _isEnding; }
    }

    /// <summary>当前起跳或落地动画已经播放的秒数。</summary>
    private float _phaseElapsedSeconds;

    /// <summary>等待动画资源准备完成后需要播放的状态名。</summary>
    private string _pendingStateName = string.Empty;

    /// <summary>本轮实体生命周期是否已经记录动画配置缺失日志。</summary>
    private bool _hasLoggedPrepareFailure;

    /// <summary>跳跃开始事件处理器，由移动组件的 IsJump 状态变化触发。</summary>
    private readonly MEventHandler _onJumpStarted;

    /// <summary>跳跃结束事件处理器，由移动组件的 IsJump 状态变化触发。</summary>
    private readonly MEventHandler _onJumpEnded;

    /// <summary>创建跳跃表现组件并固定事件处理器实例。</summary>
    /// <param name="owner">组件所属的场景实体。</param>
    public MJumpComponent(ROObject owner) : base(owner)
    {
        _onJumpStarted = new MEventHandler(OnJumpStarted);
        _onJumpEnded = new MEventHandler(OnJumpEnded);
    }

    /// <summary>初始化时尝试读取表现表并登记跳跃动画。</summary>
    public override void Initialize()
    {
        TryPrepareJumpAnimations();
    }

    /// <summary>
    /// 等待异步模型和动画器就绪；同时推进起跳、空中循环和落地动画的表现阶段。
    /// </summary>
    /// <param name="deltaMilliseconds">当前逻辑帧间隔，单位为毫秒。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (!_isPrepared)
        {
            TryPrepareJumpAnimations();
        }

        if (!_isPrepared || Entity == null || Entity.Model == null ||
            Entity.Model.Ator == null)
        {
            return;
        }

        if (Entity.CurrentState == StateDefine.kStateSkill)
        {
            // 技能动作接管表现后清除旧跳跃待播项；物理跳跃和落地仍由移动组件推进。
            ClearJumpPresentation();
            return;
        }

        TryPlayPendingState();

        // 当前帧间隔换算为秒，用于推进起跳和落地动画阶段。
        float deltaSeconds = deltaMilliseconds * 0.001f;
        if (deltaSeconds < 0f)
        {
            deltaSeconds = 0f;
        }
        _phaseElapsedSeconds += deltaSeconds;

        if (_isJumping && Entity.Model.Ator.CurrentState == MAnimator.STATE_JUMP_START)
        {
            // 起跳阶段已登记的动画信息，用于读取真实剪辑时长。
            MAnimator.MAnimInfo startInfo = Entity.Model.Ator.GetAnimInfo(
                MAnimator.STATE_JUMP_START);
            // 起跳动画播放长度，单位为秒；缺少元数据时由保护值兜底。
            float startLength = GetPhaseLength(startInfo);
            if (_phaseElapsedSeconds >= startLength)
            {
                _phaseElapsedSeconds = 0f;
                _pendingStateName = MAnimator.STATE_JUMP_LOOP;
                TryPlayPendingState();
            }
        }

        if (_isEnding && Entity.Model.Ator.CurrentState == MAnimator.STATE_JUMP_END)
        {
            // 落地阶段已登记的动画信息，用于读取真实剪辑时长。
            MAnimator.MAnimInfo endInfo = Entity.Model.Ator.GetAnimInfo(
                MAnimator.STATE_JUMP_END);
            // 落地动画播放长度，单位为秒；缺少元数据时由保护值兜底。
            float endLength = GetPhaseLength(endInfo);
            if (_phaseElapsedSeconds >= endLength)
            {
                _isEnding = false;
                _phaseElapsedSeconds = 0f;
                _pendingStateName = string.Empty;
                // 落地阶段结束后交还默认状态机；它会按当前方向输入和 Shift 状态恢复待机、行走或跑步。
                if (Entity.Machine != null)
                {
                    Entity.Machine.ForceToDefault();
                    Entity.Machine.MarkTriggered();
                }
            }
        }
    }

    /// <summary>对象池回收时清空跳跃阶段和资源准备标记。</summary>
    public override void Shutdown()
    {
        _isPrepared = false;
        _isJumping = false;
        _isEnding = false;
        _phaseElapsedSeconds = 0f;
        _pendingStateName = string.Empty;
        _hasLoggedPrepareFailure = false;
        base.Shutdown();
    }

    /// <summary>注册跳跃开始和跳跃结束事件。</summary>
    protected override void EventSubscribe()
    {
        RegisterEvent(MEventType.JumpStarted, _onJumpStarted);
        RegisterEvent(MEventType.JumpEnded, _onJumpEnded);
    }

    /// <summary>注销跳跃开始和跳跃结束事件。</summary>
    protected override void EventUnsubscribe()
    {
        RemoveEventListener(MEventType.JumpStarted, _onJumpStarted);
        RemoveEventListener(MEventType.JumpEnded, _onJumpEnded);
    }

    /// <summary>收到起跳事件后从起跳动画开始进入空中表现阶段。</summary>
    /// <param name="eventArg">跳跃事件载荷；当前跳跃表现不需要额外数据。</param>
    private void OnJumpStarted(IEventArg eventArg)
    {
        if (Entity != null && Entity.CurrentState == StateDefine.kStateSkill)
        {
            ClearJumpPresentation();
            return;
        }
        _isJumping = true;
        _isEnding = false;
        _phaseElapsedSeconds = 0f;
        _pendingStateName = MAnimator.STATE_JUMP_START;
        TryPlayPendingState();
    }

    /// <summary>收到落地事件后按方向输入交接表现；仍按方向时直接移动，松开时播放落地动画后待机。</summary>
    /// <param name="eventArg">落地事件载荷；当前跳跃表现不需要额外数据。</param>
    private void OnJumpEnded(IEventArg eventArg)
    {
        _isJumping = false;

        if (Entity != null && Entity.CurrentState == StateDefine.kStateSkill)
        {
            // 空中施法落地只结束物理跳跃，不播放 Jump_End 或强制切回默认动作。
            ClearJumpPresentation();
            return;
        }

        // 方向键仍按住时直接交还默认状态；默认动作会按方向输入和 Shift 状态选择 Walk 或 Run。
        if (Entity != null && Entity.MoveComp != null &&
            Entity.MoveComp.HasDirectionalInput)
        {
            if (Entity.Model != null)
            {
                // 移动中落地不播放Jump_End动画，零秒落地声仍由相同编辑轨道触发一次。
                Entity.Model.Sound.Play(MAnimator.STATE_JUMP_END);
            }
            _isEnding = false;
            _phaseElapsedSeconds = 0f;
            _pendingStateName = string.Empty;
            if (Entity.Machine != null)
            {
                Entity.Machine.ForceToDefault();
                Entity.Machine.MarkTriggered();
            }
            return;
        }

        // 没有方向输入时保留一次落地动画，播放完成后再交回待机状态。
        _isEnding = true;
        _phaseElapsedSeconds = 0f;
        _pendingStateName = MAnimator.STATE_JUMP_END;
        TryPlayPendingState();
    }

    /// <summary>读取表现表并登记跳跃三阶段动画，三段跳不重复读取或创建动画。</summary>
    private void TryPrepareJumpAnimations()
    {
        if (Entity == null || !Entity.IsRole || Entity.IsRideVehicle ||
            Entity.IsRidePubVehicle || Entity.Model == null ||
            Entity.Model.Ator == null || Entity.AttrComp == null)
        {
            return;
        }

        // 动画索引仍在异步加载时暂不解析短名，避免提前输出动画未登记错误。
        if (!MAnimationMgr.singleton.IsPreloaded)
        {
            return;
        }

        // 当前职业对应的表现表，提供三阶段跳跃动画短名。
        PresentTable presentData = Entity.AttrComp.PresentDataView;
        if (presentData == null || string.IsNullOrEmpty(presentData.JumpStartAnim) ||
            string.IsNullOrEmpty(presentData.JumpLoopAnim) ||
            string.IsNullOrEmpty(presentData.JumpEndAnim))
        {
            LogPrepareFailure("角色表现表缺少 JumpStartAnim、JumpLoopAnim 或 JumpEndAnim 配置。");
            return;
        }

        // 起跳动画绑定信息，三段跳的每次起跳均复用该状态。
        MAnimator.MAnimInfo startInfo = Entity.OverrideAnim(
            MAnimator.STATE_JUMP_START, presentData.JumpStartAnim);
        // 空中循环动画绑定信息，二段和三段跳不重复加载该资源。
        MAnimator.MAnimInfo loopInfo = Entity.OverrideAnim(
            MAnimator.STATE_JUMP_LOOP, presentData.JumpLoopAnim);
        // 落地动画绑定信息，每次落地只播放一次。
        MAnimator.MAnimInfo endInfo = Entity.OverrideAnim(
            MAnimator.STATE_JUMP_END, presentData.JumpEndAnim);
        if (startInfo == null || loopInfo == null || endInfo == null)
        {
            LogPrepareFailure("角色跳跃动画未登记到 animInfo.txt。");
            return;
        }

        // Controller 尚未绑定时保持登记结果，等待 MAnimator 完成异步绑定后再确认槽位。
        if (!Entity.Model.Ator.IsControllerReady)
        {
            return;
        }
        if (!Entity.Model.Ator.HasOverrideSlot(MAnimator.STATE_JUMP_START) ||
            !Entity.Model.Ator.HasOverrideSlot(MAnimator.STATE_JUMP_LOOP) ||
            !Entity.Model.Ator.HasOverrideSlot(MAnimator.STATE_JUMP_END))
        {
            LogPrepareFailure("Animator Controller 缺少跳跃覆盖槽位。");
            return;
        }

        _isPrepared = true;
        _hasLoggedPrepareFailure = false;
        TryPlayPendingState();
    }

    /// <summary>在资源准备完成后尝试播放等待中的动画状态。</summary>
    private void TryPlayPendingState()
    {
        if (string.IsNullOrEmpty(_pendingStateName) || Entity == null ||
            Entity.Model == null || Entity.Model.Ator == null ||
            Entity.CurrentState == StateDefine.kStateSkill)
        {
            return;
        }

        // 动画器是否已经完成资源准备并接受本次播放请求。
        bool played = Entity.Model.Ator.Play(_pendingStateName);
        if (played)
        {
            _pendingStateName = string.Empty;
        }
    }

    /// <summary>技能接管动画时清除旧跳跃表现阶段，不触碰移动组件的垂直速度与接地状态。</summary>
    private void ClearJumpPresentation()
    {
        _isJumping = false;
        _isEnding = false;
        _phaseElapsedSeconds = 0f;
        _pendingStateName = string.Empty;
    }

    /// <summary>获取阶段动画时长，资源元数据无效时使用 0.2 秒保护值。</summary>
    /// <param name="animInfo">动画绑定信息，可为空。</param>
    /// <returns>动画时长，单位为秒。</returns>
    private float GetPhaseLength(MAnimator.MAnimInfo animInfo)
    {
        if (animInfo != null && animInfo.Length > 0f)
        {
            return animInfo.Length;
        }
        return 0.2f;
    }

    /// <summary>记录一次动画配置错误，避免模型未就绪时每帧刷屏。</summary>
    /// <param name="failureReason">配置或资源索引缺失原因。</param>
    private void LogPrepareFailure(string failureReason)
    {
        if (_hasLoggedPrepareFailure)
        {
            return;
        }
        _hasLoggedPrepareFailure = true;
        UnityEngine.Debug.LogWarning("角色跳跃动画装配等待或失败，实体UID=" +
            Entity.UID + "，原因=" + failureReason);
    }
}
