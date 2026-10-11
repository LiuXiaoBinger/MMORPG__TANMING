using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单次技能释放的客户端运行时基类，保存施法 UUID、核心和动作时钟。
/// 实例由工厂池借出并在单次 UUID 生命周期结束后归还；伤害和资源消耗始终以服务器结果为准。
/// </summary>
public abstract class MSkill
{
    /// <summary>RO 正式技能在表现范围之后的最大生命周期宽限秒数，不是客户端施法门禁。</summary>
    private const float ServerEndGraceSeconds = 10f;

    /// <summary>RO 预表现实例在表现范围之后的最大生命周期宽限秒数；不维护逐笔确认期限。</summary>
    private const float PerformanceGraceSeconds = 2f;

    /// <summary>本次施法尚未触发的正延迟计时器票据，触发后移除、取消时统一注销。</summary>
    private readonly List<uint> _tokens = new List<uint>();

    /// <summary>实例生命周期内复用的 RO 时间轴回调委托，注册时不重复创建方法委托。</summary>
    protected readonly MTimerMgr.TimerEventHandler _onProcessTimer;

    /// <summary>每次 Reset 更新的实例施法代次，区分同一池对象甚至同 UUID 的同步重新 Fire。</summary>
    protected ulong _fireGeneration;

    /// <summary>构造池内技能实例并绑定强类型时间轴回调，后续 Fire 重用该委托。</summary>
    protected MSkill()
    {
        _onProcessTimer = OnProcessTimer;
    }

    /// <summary>本次释放持有的技能核心引用，直到该施法实例归还才解除。</summary>
    protected MSkillCore _core;

    /// <summary>本次释放的场景实体，回收时清空以免持有已销毁实体。</summary>
    protected MEntity _firer;

    /// <summary>实际持有本次 Skill 槽位剪辑租约的动画器，模型重建后仍指向旧实例。</summary>
    protected MAnimator _boundAnimator;
    /// <summary>统一动画入口登记归属的模型，模型迟到或重载时恢复当前动作，退出时解除。</summary>
    protected MModel _animationModel;

    /// <summary>本次释放选中的动画 Location，后续连击改变核心时仍按原资源归还租约。</summary>
    protected string _animationLocation;

    /// <summary>本次 Fire 时选中的表现行主键，不受核心后续连击换段影响。</summary>
    protected int _runtimePresentationId;

    /// <summary>本次表现行在旧表登记的完整时长，单位为秒，仅用于导出资产一致性校验。</summary>
    protected float _runtimePresentationDurationSeconds;
    /// <summary>程序指令校验采用本次 Fire 冻结的完整动作秒数，权威连击确认改共享核心时也不改本段范围。</summary>
    internal float DisplacementDurationSeconds { get { return _runtimePresentationDurationSeconds; } }

    /// <summary>本次施法锁定的完整编辑器表现资产；冷加载就绪后仍只读取原表现行。</summary>
    protected SkillRuntimePresentation _runtimePresentation;

    /// <summary>本次释放从开始到当前的本地经过时长，单位为秒。</summary>
    private float _elapsedSeconds;

    /// <summary>本次表现预计播放时长，单位为秒，优先使用动画索引长度。</summary>
    protected float _durationSeconds;

    /// <summary>本段技能动作状态应结束的时间，单位为秒；完整剪辑时长仍由 _durationSeconds 保存。</summary>
    protected float _skillStateTimeSeconds;

    /// <summary>本次 Fire 冻结的全局计时毫秒原点，程序位移与 timeline 共用，不按技能帧重复积分。</summary>
    protected long _startedTimerMilliseconds;
    /// <summary>本次 Fire 冻结的计时器初始化版本，全局重新 Init 后旧程序运动必须停止。</summary>
    private ulong _timerSessionVersion;
    /// <summary>本次 Fire 冻结的水平技能单位方向，程序位移各段不跟随手动输入转向。</summary>
    private Vector3 _skillForward;
    /// <summary>RO 同名施法方向接口，Y Forward 程序段沿该固定世界方向移动。</summary>
    public Vector3 SkillTowardsTo { get { return _skillForward; } }
    /// <summary>计时器原点至今经过的秒数，程序位移独占该时钟；FX/声音既有补发时钟保持。</summary>
    protected float TimelineElapsedSeconds { get { return (MTimerMgr.singleton.ElapsedTime - _startedTimerMilliseconds) * 0.001f; } }

    /// <summary>本次施法是否已经执行 Start，保证表现事件只初始化一次。</summary>
    private bool _execute;

    /// <summary>本次施法是否已退出动作槽；退出后禁止旧实例重新安装动画。</summary>
    protected bool _animationReleased;

    /// <summary>本次施法从开始累计的本地秒数，供具体技能按编辑器特效时间驱动表现。</summary>
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

    /// <summary>已收到服务器自然结束，动作槽立即退出，剩余制作事件在原旧施法列表完成后回池。</summary>
    public bool HasServerEnded { get; private set; }

    /// <summary>当前实例没有未来计时器；子技能须额外检查冷资产、待派发请求和活动声音均已收尾。</summary>
    public virtual bool IsPresentationFinished
    {
        get { return _tokens.Count == 0; }
    }

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

    /// <summary>RO 同名动作解锁判断，仅表示当前动作窗结束，独立于技能制作尾段及服务器生命周期。</summary>
    /// <returns>本次活动技能已运行到动作状态解锁秒数时为真。</returns>
    public abstract bool IsEnd();

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
        // 实体引用只属于本次施法，回池时解除。
        _firer = firer;
        // 本段动画路径锁定后不受后续连击选择影响。
        _animationLocation = core.AnimationLocation;
        // 只锁定本段身份；资源迟到时仍按该 ID 取轨道，不读取已被下一段改动的核心选择。
        _runtimePresentationId = core.PresentationId;
        _runtimePresentationDurationSeconds = core.AnimationDurationSeconds;
        // 状态时钟仍来自旧表现表，Y 编辑资产不改写 Q 接段时间。
        _durationSeconds = core.AnimationDurationSeconds;

        _skillStateTimeSeconds = _durationSeconds;
        if (core.HasMultiplePresentations && core.SkillStateTimeSeconds > 0f)
        {
            _skillStateTimeSeconds = core.SkillStateTimeSeconds;
        }
        _startedTimerMilliseconds = MTimerMgr.singleton.ElapsedTime;
        _timerSessionVersion = MTimerMgr.singleton.SessionVersion;
        _skillForward = Vector3.forward;
        if (firer.Model != null)
        {
            _skillForward = firer.Model.Forward;
            _skillForward.y = 0f;
            if (_skillForward.sqrMagnitude < 0.000001f)
            {
                _skillForward = Vector3.forward;
            }
            else
            {
                _skillForward = _skillForward.normalized;
            }
        }
        SkillUuid = skillUuid;
        SkillId = core.SkillId;
        UsingTimes = core.UsingTimes;
        TargetUid = targetUid;
        CastPoint = castPoint;
        IsPredicted = predicted;
        IsActive = true;
        // 启动期间零秒回调可能取消或重新 Fire，外层只确认自己这一代施法是否仍有效。
        ulong fireGeneration = _fireGeneration;
        Execute();
        return IsActive && SkillUuid == skillUuid && _fireGeneration == fireGeneration;
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
    /// <returns>动作仍占槽或制作事件、完整声尾未完成时为真；到期或完成后调用方应 Cease。</returns>
    public virtual bool Update(float deltaSeconds)
    {
        if (!IsActive) return false;
        if (!_execute || MTimerMgr.singleton.IsPause) return true;
        if (deltaSeconds > 0f)
        {
            _elapsedSeconds += deltaSeconds;
        }
        return !IsLifetimeOverdue() &&
            (!_animationReleased || !IsPresentationFinished);
    }

    /// <summary>RO 同名施法结束入口：先解除所属组件的当前槽或脱手引用，再注销回调并归还具体技能池。</summary>
    /// <param name="cleanUp">RO 兼容的额外清理标记；本项目没有额外分支，任意值都终止并注销本次回调，自然尾段由组件延后调用 Cease。</param>
    public virtual void Cease(bool cleanUp = false)
    {
        if (!IsActive) return;
        // 表现回收只释放实例，角色冷却由施放、倒计时及权威结果维护。
        // 先置为非存活，组件切状态的同步回调不能再次结束并重复归还此实例。
        IsActive = false;
        if (_firer != null && _firer.Skill != null)
        {
            _firer.Skill.OnSkillEnd(this);
        }
        MSkillFactory.ReturnSkill(this);
    }

    /// <summary>运动提交前核对原施法仍占用当前角色动作槽，取消和池复用后不能继续推动。</summary>
    /// <param name="entity">移动组件当前宿主，组件池重绑时可能已改变。</param>
    /// <param name="skillUuid">开始运动时保存的原非零施法 UUID。</param>
    /// <param name="fireGeneration">开始运动时锁定的池实例施法代次。</param>
    /// <returns>实体、当前槽、UUID 与代次都匹配且动作未释放时返回 true。</returns>
    internal bool OwnsDisplacement(MEntity entity, ulong skillUuid, ulong fireGeneration)
    {
        return IsActive && !_animationReleased && _fireGeneration == fireGeneration &&
            _timerSessionVersion == MTimerMgr.singleton.SessionVersion &&
            SkillUuid == skillUuid && skillUuid != 0 && ReferenceEquals(_firer, entity) &&
            entity != null && entity.Skill != null && ReferenceEquals(entity.Skill.CurrentSkill, this);
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

    /// <summary>RO 实例存活上限：累计技能时钟超过表现范围加预表现两秒或正式十秒宽限时兜底，暂停时不积分。</summary>
    /// <returns>实例仍活动且超过其最大生命周期时为真，不承担确认等待、CD回滚或施法阻断。</returns>
    private bool IsLifetimeOverdue()
    {
        // 未来制作事件可以晚于动画，宽限不能在该事件之前清除其计时器。
        float presentationSeconds = _durationSeconds;
        if (_runtimePresentation != null)
        {
            presentationSeconds = Mathf.Max(presentationSeconds, _runtimePresentation.EffectiveTimelineDurationSeconds);
            // 单次声音可能长于十秒宽限，实际完整音尾也计入兜底起点，避免合法长音提前被切掉。
            for (int index = 0; index < _runtimePresentation.AudioEvents.Count; index++)
            {
                // 已校验并锁定的声音事件，完整剪辑尾部不依赖作者结束帧标记。
                SkillRuntimeAudioEvent audioEvent = _runtimePresentation.AudioEvents[index];
                presentationSeconds = Mathf.Max(presentationSeconds, audioEvent.TimeSeconds + audioEvent.Clip.length);
            }
        }
        // 实例预表现身份仅选择 RO 原有存活宽限，不创建额外结果事务或确认定时器。
        float graceSeconds = ServerEndGraceSeconds;
        if (IsPredicted) graceSeconds = PerformanceGraceSeconds;
        return IsActive && SkillUuid != 0 && _elapsedSeconds > presentationSeconds + graceSeconds;
    }

    /// <summary>记录服务器自然结束，仅解除预测身份；动画槽由技能组件释放，计时器继续直到表现收尾。</summary>
    public void MarkServerEnded()
    {
        if (!IsActive)
        {
            return;
        }
        HasServerEnded = true;
        IsPredicted = false;
    }

    /// <summary>当前技能被拒绝、打断或正常结束时清理本次施法引用。</summary>
    public void Reset()
    {
        // 先推进实例代次，同 UUID 重入也不能让旧回调或旧注册批次接管新生命周期。
        unchecked { _fireGeneration++; }
        // 必须先取消尚未触发回调，再清理资产和施法身份，防止池复用后旧事件串入新施法。
        KillTimerAll();
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
        _animationModel = null;
        _animationLocation = null;
        _runtimePresentationId = 0;
        _runtimePresentationDurationSeconds = 0f;
        _runtimePresentation = null;
        _elapsedSeconds = 0f;
        _durationSeconds = 0f;
        _skillStateTimeSeconds = 0f;
        _startedTimerMilliseconds = 0;
        _timerSessionVersion = 0;
        _skillForward = Vector3.zero;
        _execute = false;
        _animationReleased = false;
        SkillUuid = 0;
        SkillId = 0;
        UsingTimes = 0;
        TargetUid = 0;
        CastPoint = Vector3.zero;
        IsPredicted = false;
        HasServerEnded = false;
        IsActive = false;
    }

    /// <summary>归还或重新借用前清理具体技能持有的表现资源。</summary>
    protected virtual void OnReset()
    {
    }

    /// <summary>按 RO 记录正延迟票据；零票据已同步执行，无需纳入取消列表。</summary>
    /// <param name="token">MTimerMgr.SetTimer 返回的单次票据，零表示同步事件。</param>
    protected void AddedTimerToken(uint token)
    {
        if (token != 0)
        {
            _tokens.Add(token);
        }
    }

    /// <summary>按 RO 注销本次施法的全部待触发事件，不影响同角色下一段或其他角色计时器。</summary>
    protected virtual void KillTimerAll()
    {
        // index 为本次施法持有票据的零起始下标，管理器移除条目不会修改该列表。
        for (int index = 0; index < _tokens.Count; index++)
        {
            MTimerMgr.singleton.KillTimer(_tokens[index]);
        }
        _tokens.Clear();
    }

    /// <summary>RO 时间轴统一入口，先解除已触发票据，再按锁定 UUID 校验本次施法身份。</summary>
    /// <param name="token">到期的单次票据，零表示同步补发。</param>
    /// <param name="args">注册时捕获的具名强类型技能事件载荷。</param>
    protected void OnProcessTimer(uint token, MSkillTimerArgs args)
    {
        if (token != 0)
        {
            _tokens.Remove(token);
        }
        if (!IsActive || _firer == null || args == null || args.SkillUuid != SkillUuid ||
            args.FireGeneration != _fireGeneration)
        {
            return;
        }
        InnerProcessTimer(args.Id, args);
    }

    /// <summary>RO 子技能时间轴分发扩展点；基类不解释任何具体表现事件。</summary>
    /// <param name="id">RO 回调枚举对应的整数编号。</param>
    /// <param name="param">已通过施法 UUID 检查的强类型粒子或声音参数。</param>
    /// <returns>具体子技能识别并接管该事件时返回 true。</returns>
    protected virtual bool InnerProcessTimer(int id, MSkillTimerArgs param)
    {
        return false;
    }

    /// <summary>按 RO 从统一动作播放入口覆盖并播放技能动画，迟到触发按施法时钟续播。</summary>
    public virtual void TriggerAnim()
    {
        if (!IsActive || _animationReleased || _core == null || _firer == null ||
            _firer.Model == null || _firer.Skill == null ||
            !ReferenceEquals(_firer.Skill.CurrentSkill, this))
        {
            return;
        }
        _animationModel = _firer.Model;
        _animationModel.BindSkillAnimation(this);
        if (_animationModel.Ator == null)
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

    /// <summary>本段退出动作状态时归还动画槽租约，保留未完表现及原 UUID，完成后无需回包也可回池。</summary>
    public abstract void ReleaseAnimation();

    /// <summary>按组合技能内的子技能编号查找实例；普通技能返回自身。</summary>
    /// <param name="skillNum">组合技能内从零起的子技能编号。</param>
    /// <returns>编号对应的技能实例，找不到时返回空。</returns>
    public abstract MSkill GetSkill(int skillNum);

    /// <summary>由具体技能子类归还自身类型的对象池，调用前必须完成 Reset。</summary>
    public abstract void Recycle();
}
