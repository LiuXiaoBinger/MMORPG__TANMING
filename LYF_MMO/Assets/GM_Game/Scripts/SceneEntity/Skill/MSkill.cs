using UnityEngine;

/// <summary>
/// 单次技能释放的客户端运行时基类，保存施法 UUID、核心和动作时钟。
/// 实例由工厂池借出并在单次 UUID 生命周期结束后归还；伤害和资源消耗始终以服务器结果为准。
/// </summary>
public abstract class MSkill
{
    /// <summary>RO 已确认施法等待服务器结束通知的额外秒数。</summary>
    private const float ServerEndGraceSeconds = 10f;

    /// <summary>本次释放持有的技能核心引用，直到该施法实例归还才解除。</summary>
    protected MSkillCore _core;

    /// <summary>本次释放的场景实体，回收时清空以免持有已销毁实体。</summary>
    protected MEntity _firer;

    /// <summary>实际持有本次 Skill 槽位剪辑租约的动画器，模型重建后仍指向旧实例。</summary>
    protected MAnimator _boundAnimator;

    /// <summary>本次释放选中的动画 Location，后续连击改变核心时仍按原资源归还租约。</summary>
    protected string _animationLocation;

    /// <summary>本次释放从开始到当前的本地经过时长，单位为秒。</summary>
    private float _elapsedSeconds;

    /// <summary>本次表现预计播放时长，单位为秒，优先使用动画索引长度。</summary>
    protected float _durationSeconds;

    /// <summary>本段技能动作状态应结束的时间，单位为秒；完整剪辑时长仍由 _durationSeconds 保存。</summary>
    protected float _skillStateTimeSeconds;

    /// <summary>本次表现启动时的本机不缩放时间，单位为秒。</summary>
    private float _startedRealtime;

    /// <summary>本次施法是否已经执行 Start，保证表现事件只初始化一次。</summary>
    private bool _execute;

    /// <summary>本次施法是否已退出动作槽；退出后禁止旧实例重新安装动画。</summary>
    protected bool _animationReleased;

    /// <summary>本次施法从开始累计的本地秒数，供具体技能按动画事件时间驱动表现。</summary>
    protected float ElapsedSeconds { get { return _elapsedSeconds; } }

    /// <summary>RO 技能运行类型，由具体表现子类提供。</summary>
    public abstract int SkillType { get; }

    /// <summary>客户端生成并由服务器原样广播的单次施法 UUID，零表示未绑定。</summary>
    public ulong SkillUuid { get; private set; }

    /// <summary>本次释放使用的技能 ID。</summary>
    public int SkillId { get; private set; }

    /// <summary>本次动作启动时选中的段号，服务器确认更新核心不会改变正在播放的段。</summary>
    public int UsingTimes { get; private set; }

    /// <summary>本次释放目标实体的运行时 UID；无目标时为零。</summary>
    public ulong TargetUid { get; private set; }

    /// <summary>本次释放的世界空间施法点，单位为 Unity 世界单位。</summary>
    public Vector3 CastPoint { get; private set; }

    /// <summary>实例是否正处于技能动作生命周期内。</summary>
    public bool IsActive { get; private set; }

    /// <summary>客户端预表现尚未被服务端开始广播确认时为 true。</summary>
    public bool IsPredicted { get; private set; }

    /// <summary>本次技能表现是否已达到动画时长；不代表服务端伤害结算结束。</summary>
    public bool IsAnimationFinished
    {
        get { return IsActive && _elapsedSeconds >= _durationSeconds; }
    }

    /// <summary>RO 技能动作状态是否到期；连击可在完整剪辑结束前接下一段。</summary>
    public bool IsSkillStateFinished
    {
        get { return IsActive && _elapsedSeconds >= _skillStateTimeSeconds; }
    }

    /// <summary>当前表现绑定的已学习技能核心，实例无效时为空。</summary>
    public MSkillCore Core
    {
        get { return _core; }
    }

    /// <summary>按 RO 启动一次技能逻辑，锁定施法数据后执行 Start；动画由动作状态统一触发。</summary>
    /// <param name="core">已按技能 ID 与等级解析的技能配置核心。</param>
    /// <param name="firer">拥有本次技能动作的场景实体。</param>
    /// <param name="skillUuid">客户端生成并由服务器回传的非零单次施法 UUID。</param>
    /// <param name="targetUid">目标实体运行时 UID，无目标时为零。</param>
    /// <param name="castPoint">世界空间施法点，单位为 Unity 世界单位。</param>
    /// <param name="predicted">本次表现是否先于服务器确认开始。</param>
    /// <returns>配置与实体有效时返回 true；动画器延迟加载不阻止启动。</returns>
    public bool Fire(MSkillCore core, MEntity firer, ulong skillUuid,
        ulong targetUid, Vector3 castPoint, bool predicted)
    {
        Reset();
        if (core == null || firer == null || skillUuid == 0 || core.SkillId <= 0 ||
            core.AnimationDurationSeconds <= 0f ||
            string.IsNullOrEmpty(core.AnimationLocation))
        {
            return false;
        }
        _core = core;
        // 旧施法列表可能跨越等级同步，独立持有可阻止旧核心提前回池。
        _core.Retain();
        //释放者
        _firer = firer;
        //动画路径
        _animationLocation = core.AnimationLocation;
        //动画时长
        _durationSeconds = core.AnimationDurationSeconds;
        
        _skillStateTimeSeconds = _durationSeconds;
        if (core.HasMultiplePresentations && core.SkillStateTimeSeconds > 0f)
        {
            _skillStateTimeSeconds = core.SkillStateTimeSeconds;
        }
        _startedRealtime = Time.realtimeSinceStartup;
        SkillUuid = skillUuid;
        SkillId = core.SkillId;
        UsingTimes = core.UsingTimes;
        TargetUid = targetUid;
        CastPoint = castPoint;
        IsPredicted = predicted;
        IsActive = true;
        Execute();
        return true;
    }

    /// <summary>按 RO 只执行一次技能启动钩子，重复调用不会重新注册表现事件。</summary>
    public void Execute()
    {
        if (IsActive && !_execute)
        {
            _execute = true;
            Start();
        }
    }

    /// <summary>具体技能登记本次表现事件；此入口不播放角色动画。</summary>
    protected virtual void Start()
    {
    }

    /// <summary>逐帧推进存活技能的逻辑时钟，动画统一由动作状态的 PlayAnim 触发。</summary>
    /// <param name="deltaSeconds">距上一帧的非负时间，单位为秒。</param>
    public virtual void Update(float deltaSeconds)
    {
        if (!IsActive || !_execute)
        {
            return;
        }
        if (deltaSeconds > 0f)
        {
            _elapsedSeconds += deltaSeconds;
        }
    }

    /// <summary>按同一个施法 UUID 确认预表现，避免服务器广播重复播放动画。</summary>
    /// <param name="skillUuid">请求与广播共同携带的非零单次施法 UUID。</param>
    /// <returns>UUID 匹配且实例仍有效时返回 true。</returns>
    public bool Confirm(ulong skillUuid)
    {
        if (!IsActive || skillUuid == 0 || SkillUuid != skillUuid)
        {
            return false;
        }
        IsPredicted = false;
        return true;
    }

    /// <summary>确认后的技能超过表现时长加十秒仍无结束通知时允许兜底回收。</summary>
    /// <param name="realtimeSeconds">当前本机不缩放时间，单位为秒。</param>
    /// <returns>已确认且超过 RO 结束宽限时返回 true。</returns>
    public bool IsServerEndOverdue(float realtimeSeconds)
    {
        return IsActive && !IsPredicted && SkillUuid != 0 &&
            realtimeSeconds >= _startedRealtime + _durationSeconds +
            ServerEndGraceSeconds;
    }

    /// <summary>当前技能被拒绝、打断或正常结束时清理本次施法引用。</summary>
    public void Reset()
    {
        // 子类先解除本次施法的表现引用；已触发粒子按自身寿命销毁，不再依赖池实例。
        OnReset();
        // 动画释放必须发生在核心引用解除前，旧实例不再触及后续借出的核心。
        if (_boundAnimator != null && _core != null)
        {
            // Skill 状态尚在退出过渡时保留真实剪辑，避免立即换回占位动作造成姿态跳变。
            ReleaseAnimation();
        }
        if (_core != null)
        {
            _core.Release();
        }
        _core = null;
        _firer = null;
        _boundAnimator = null;
        _animationLocation = null;
        _elapsedSeconds = 0f;
        _durationSeconds = 0f;
        _skillStateTimeSeconds = 0f;
        _startedRealtime = 0f;
        _execute = false;
        _animationReleased = false;
        SkillUuid = 0;
        SkillId = 0;
        UsingTimes = 0;
        TargetUid = 0;
        CastPoint = Vector3.zero;
        IsPredicted = false;
        IsActive = false;
    }

    /// <summary>归还或重新借用前清理具体技能持有的表现资源。</summary>
    protected virtual void OnReset()
    {
    }

    /// <summary>按 RO 从统一动作播放入口覆盖并播放技能动画，迟到触发按施法时钟续播。</summary>
    public virtual void TriggerAnim()
    {
        if (!IsActive || _animationReleased || _core == null || _firer == null ||
            _firer.Model == null || _firer.Model.Ator == null)
        {
            return;
        }
        // 当前技能真实剪辑的覆盖信息，异步加载由统一动画管理器负责。
        MAnimator.MAnimInfo animationInfo = _firer.OverrideAnim(
            MAnimator.STATE_SKILL, _animationLocation);
        if (animationInfo == null)
        {
            return;
        }
        _boundAnimator = _firer.Model.Ator;
        if (animationInfo.Length > 0f)
        {
            _durationSeconds = animationInfo.Length;
            if (!_core.HasMultiplePresentations)
            {
                _skillStateTimeSeconds = _durationSeconds;
            }
        }
        // 当前表未配置技能时间倍率，沿用原有正常播放速度。
        _firer.AtorSpeed = 1f;
        // 模型或状态触发延迟时，从技能已运行的归一化位置接续。
        float normalizedTime = 0f;
        if (_durationSeconds > 0f)
        {
            normalizedTime = _elapsedSeconds / _durationSeconds;
        }
        if (_elapsedSeconds > 0.1f)
        {
            _boundAnimator.Play(MAnimator.STATE_SKILL, 0, normalizedTime);
        }
        else
        {
            _boundAnimator.CrossFade(MAnimator.STATE_SKILL, 0.1f);
        }
    }

    /// <summary>本段退出动作状态时归还动画槽租约，保留施法 UUID 等待确认或结束通知。</summary>
    public abstract void ReleaseAnimation();

    /// <summary>按组合技能内的子技能编号查找实例；普通技能返回自身。</summary>
    /// <param name="skillNum">组合技能内从零起的子技能编号。</param>
    /// <returns>编号对应的技能实例，找不到时返回空。</returns>
    public abstract MSkill GetSkill(int skillNum);

    /// <summary>由具体技能子类归还自身类型的对象池，调用前必须完成 Reset。</summary>
    public abstract void Recycle();
}
