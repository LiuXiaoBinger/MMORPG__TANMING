/// <summary>
/// 一个已学习技能在当前等级下的配置和运行数据，供技能字典与单次施法共用。
/// 保存效果、消耗、动作段和最近施法编号；装配到实体后，冷却统一存放在 MSkillComponent。
/// 所有持有者释放后才归还对象池，旧施法可以继续使用升级前的配置。
/// </summary>
public sealed class MSkillCore
{

    #region 字段与属性
    /// <summary>两次动作间隔超过十秒后，下一次从第一段重新开始。</summary>
    private const float ComboResetSeconds = 10f;

    /// <summary>当前职业默认武器的有序动作列表，生命周期随核心结束。</summary>
    private readonly System.Collections.Generic.List<MSkillData> _presentations;

    /// <summary>上次已启动表现的动作序号，负一表示本核心尚未释放。</summary>
    private int _usingTimes = -1;

    /// <summary>上次开始动作的 Time.realtimeSinceStartup 秒数，用于判断是否需要从第一段重新开始。</summary>
    private float _fireRealtime;

    /// <summary>技能配置 ID，同一技能的不同等级共享该编号。</summary>
    public int SkillId { get; private set; }

    /// <summary>当前已学习的技能等级，从 1 开始。</summary>
    public int Level { get; private set; }

    /// <summary>当前等级对应的技能效果配置 ID。</summary>
    public int EffectId { get; private set; }

    /// <summary>目标模式；本期 0 为无目标表现，其他模式由服务器拒绝。</summary>
    public int TargetMode { get; private set; }

    /// <summary>单次释放后的基础冷却时长，单位为秒。</summary>
    public float CooldownSeconds { get; private set; }

    /// <summary>共享冷却组 ID，零表示本技能不属于公共冷却组。</summary>
    public int CooldownGroupId { get; private set; }

    /// <summary>该组触发后的公共冷却时长，单位为秒。</summary>
    public float GroupCooldownSeconds { get; private set; }

    /// <summary>服务器技能施法占用时长，单位为秒；不等于动画剪辑长度。</summary>
    public float CastDurationSeconds { get; private set; }

    /// <summary>单次释放需要的法力值，非负整数。</summary>
    public int MpCost { get; private set; }

    /// <summary>当前选中动作的动画资源地址；是否能加载由后续动画准备流程确认。</summary>
    public string AnimationLocation { get; private set; }

    /// <summary>技能表现的保底时长，单位为秒，资源尚未加载时用于结束动作。</summary>
    public float AnimationDurationSeconds { get; private set; }

    /// <summary>当前动作占用技能状态的时长，单位秒；多段技能可用它提前交出动作控制权。</summary>
    public float SkillStateTimeSeconds { get; private set; }

    /// <summary>持有此核心的技能组件；绑定后由组件保存单技能冷却，本核心仍保存最近施法及已处理结果的编号。</summary>
    private MSkillComponent _cooldownOwner;

    /// <summary>尚未装配到实体的独立核心使用的冷却截止秒数；装配后不再作为 CD 数据源。</summary>
    private float _cooldownEndRealtime;

    /// <summary>该核心最后启动的施法 UUID，仅此 UUID 可更新当前冷却快照。</summary>
    private ulong _latestSkillUuid;

    /// <summary>最近已应用权威结果的 UUID，仅用于重复结果去重，不阻止施法或按帧等待回包。</summary>
    private ulong _latestResultUuid;

    /// <summary>字典和存活施法实例合计持有数；为零时由工厂归还。</summary>
    private int _ownerCount;

    /// <summary>是否配置多段武器动作，用于本地次数轮换和每段动作解锁窗口。</summary>
    public bool HasMultiplePresentations { get { return _presentations.Count > 1; } }

    /// <summary>本地或服务器最近选择的动作序号，从零开始。</summary>
    public int UsingTimes { get { return System.Math.Max(0, _usingTimes); } }

    /// <summary>当前动作段的表现表主键，核心尚无表现段时为零。</summary>
    public int PresentationId
    {
        get
        {
            if (_presentations.Count == 0)
            {
                return 0;
            }
            return _presentations[UsingTimes].PresentationId;
        }
    }

    /// <summary>是否有可由动画索引解析的真实技能剪辑配置。</summary>
    public bool HasPresentation
    {
        get { return !string.IsNullOrEmpty(AnimationLocation) && AnimationDurationSeconds > 0f; }
    }
    #endregion

    #region 构造与初始化
    /// <summary>建立空的可复用核心，动作段容器在多次租借间复用。</summary>
    public MSkillCore()
    {
        _presentations = new System.Collections.Generic.List<MSkillData>();
    }

    /// <summary>初始化从工厂借出的核心，初始引用归装配调用方所有。</summary>
    /// <param name="skillId">旧技能主表的正数 ID。</param>
    /// <param name="level">已学习的正数等级。</param>
    /// <param name="effectId">当前等级效果配置 ID。</param>
    /// <param name="targetMode">目标模式，零为当前无目标技能。</param>
    /// <param name="cooldownSeconds">单技能冷却秒数。</param>
    /// <param name="cooldownGroupId">公共冷却组 ID，零为独立技能。</param>
    /// <param name="groupCooldownSeconds">公共组冷却秒数。</param>
    /// <param name="castDurationSeconds">服务器占用时长秒数。</param>
    /// <param name="mpCost">单次法力消耗。</param>
    /// <param name="animationLocation">默认段动画资源 Location。</param>
    /// <param name="animationDurationSeconds">默认段完整动画秒数。</param>
    /// <param name="presentations">由核心接管的各动作段实例；列表容器仍由调用方归还。</param>
    public void Initialize(int skillId, int level, int effectId, int targetMode,
        float cooldownSeconds, int cooldownGroupId, float groupCooldownSeconds,
        float castDurationSeconds, int mpCost, string animationLocation,
        float animationDurationSeconds,
        System.Collections.Generic.List<MSkillData> presentations = null)
    {
        SkillId = skillId;
        Level = level;
        EffectId = effectId;
        TargetMode = targetMode;
        CooldownSeconds = cooldownSeconds;
        CooldownGroupId = cooldownGroupId;
        GroupCooldownSeconds = groupCooldownSeconds;
        CastDurationSeconds = castDurationSeconds;
        MpCost = mpCost;
        AnimationLocation = animationLocation;
        AnimationDurationSeconds = animationDurationSeconds;
        _presentations.Clear();
        if (presentations != null)
        {
            _presentations.AddRange(presentations);
        }
        else if (!string.IsNullOrEmpty(animationLocation))
        {
            // 旧单段调用入口也必须借用可归还的动作数据。
            MSkillData fallbackData = MCommonObjectPool<MSkillData>.Get();
            fallbackData.Initialize(0, 0, animationLocation,
                animationDurationSeconds, animationDurationSeconds);
            _presentations.Add(fallbackData);
        }
        _cooldownOwner = null;
        _cooldownEndRealtime = 0f;
        _latestSkillUuid = 0;
        _latestResultUuid = 0;
        _ownerCount = 1;
    }

    /// <summary>最终归还时清除动作段及最近施法身份，旧施法引用已全部解除。</summary>
    public void Reset()
    {
        // index 为核心独占动作段列表的零起始下标，逐项归还后统一清空容器。
        for (int index = 0; index < _presentations.Count; index++)
        {
            // 当前核心独占的动作段实例。
            MSkillData data = _presentations[index];
            data.Reset();
            MCommonObjectPool<MSkillData>.Release(data);
        }
        _presentations.Clear();
        SkillId = 0;
        Level = 0;
        EffectId = 0;
        TargetMode = 0;
        CooldownSeconds = 0f;
        CooldownGroupId = 0;
        GroupCooldownSeconds = 0f;
        CastDurationSeconds = 0f;
        MpCost = 0;
        AnimationLocation = null;
        AnimationDurationSeconds = 0f;
        SkillStateTimeSeconds = 0f;
        _cooldownOwner = null;
        _cooldownEndRealtime = 0f;
        _latestSkillUuid = 0;
        _latestResultUuid = 0;
        _usingTimes = -1;
        _fireRealtime = 0f;
    }
    #endregion

    /// <summary>解除字典或施法实例的一个持有，最后持有者触发工厂回池。</summary>
    public void Release()
    {
        if (_ownerCount <= 0)
        {
            throw new System.InvalidOperationException("技能核心重复归还。");
        }
        _ownerCount--;
        if (_ownerCount == 0)
        {
            MSkillFactory.ReleaseCore(this);
        }
    }

    #region 查询与判定
    /// <summary>检查本技能配置、MP 和单技能冷却；公共组冷却及角色状态由技能组件另查。</summary>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的时间，单位为秒。</param>
    /// <param name="currentMp">角色当前已知法力值，非负整数；最终由服务器校验。</param>
    /// <returns>配置有效、法力充足且冷却结束时返回 true；不要求先收到上一次施法的回包。</returns>
    public bool CanStart(float realtimeSeconds, int currentMp)
    {
        // 主键、等级与效果 ID 必须为正，否则配置不完整不能发起。
        bool identityInvalid = SkillId <= 0 || Level <= 0 || EffectId <= 0;
        // 目标模式与冷却相关字段必须落在合法范围。
        bool cooldownInvalid = TargetMode != 0 || CooldownSeconds < 0f ||
            CooldownGroupId < 0;
        // 组冷却、施法时长与法力消耗不得为负。
        bool costInvalid = GroupCooldownSeconds < 0f || CastDurationSeconds < 0f ||
            MpCost < 0;
        if (identityInvalid || cooldownInvalid || costInvalid)
        {
            return false;
        }
        if (currentMp < MpCost)
        {
            return false;
        }
        return GetRemainingCooldown(realtimeSeconds) <= 0f;
    }

    /// <summary>读取目前剩余的冷却时长，供控制器刷新技能槽。</summary>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的时间，单位为秒。</param>
    /// <returns>非负剩余秒数。</returns>
    public float GetRemainingCooldown(float realtimeSeconds)
    {
        if (_cooldownOwner != null)
        {
            return _cooldownOwner.GetSkillCD(SkillId, realtimeSeconds);
        }
        // 未挂实体的独立核心保留原不缩放截止时间契约，正式组件不使用该后备。
        float remainingSeconds = _cooldownEndRealtime - realtimeSeconds;
        if (remainingSeconds <= 0f)
        {
            return 0f;
        }
        return remainingSeconds;
    }

    /// <summary>判断结果是否属于本核心最后发起的一段，用于防止旧回包覆盖更新的 MP/CD。</summary>
    /// <param name="skillUuid">待比较的非零施法 UUID。</param>
    /// <returns>UUID 与最近施法一致时返回 true。</returns>
    public bool IsLatestCast(ulong skillUuid)
    {
        return skillUuid != 0 && skillUuid == _latestSkillUuid;
    }
    #endregion

    #region 核心逻辑
    /// <summary>将已学核心的 CD 接到实体组件唯一记录；等级更换继续使用同技能原记录，不复制倒计时。</summary>
    /// <param name="owner">持有本核心的技能组件，不能跨实体共享。</param>
    public void BindCooldown(MSkillComponent owner)
    {
        // 独立核心装配前的剩余秒数，仅在组件尚无该技能记录时移交。
        float remainingSeconds = GetRemainingCooldown(UnityEngine.Time.realtimeSinceStartup);
        _cooldownOwner = owner;
        if (owner != null && !owner.SkillCDDic.ContainsKey(SkillId) && remainingSeconds > 0f)
        {
            owner.SetSkillCD(SkillId, remainingSeconds);
        }
        _cooldownEndRealtime = 0f;
    }

    /// <summary>施法实例持有核心直到其 UUID 生命周期结束。</summary>
    public void Retain()
    {
        if (_ownerCount <= 0)
        {
            throw new System.InvalidOperationException("已归还的技能核心不能再次持有。");
        }
        _ownerCount++;
    }

    /// <summary>等级配置更新时继承冷却、最近施法及已应用结果身份，旧回包不能覆盖更新一次施法。</summary>
    /// <param name="previous">更新前仍在同一角色上的技能核心。</param>
    public void AdoptRuntimeState(MSkillCore previous)
    {
        if (previous == null || previous.SkillId != SkillId)
        {
            return;
        }
        _cooldownEndRealtime = UnityEngine.Time.realtimeSinceStartup +
            previous.GetRemainingCooldown(UnityEngine.Time.realtimeSinceStartup);
        _latestSkillUuid = previous._latestSkillUuid;
        _latestResultUuid = previous._latestResultUuid;
        _usingTimes = previous._usingTimes;
        _fireRealtime = previous._fireRealtime;
    }

    /// <summary>本地请求后立即设置预测 CD 并选动作，使用次数不等待服务器回包推进。</summary>
    /// <param name="skillUuid">客户端生成并由服务器原样回传的非零施法 UUID。</param>
    /// <param name="realtimeSeconds">请求发起时的本机不受时间缩放影响的秒数。</param>
    /// <param name="currentMp">本地已知法力值；该检查仅影响预表现。</param>
    /// <returns>预测登记成功返回 true。</returns>
    public bool BeginPrediction(ulong skillUuid, float realtimeSeconds, int currentMp)
    {
        if (skillUuid == 0 || !CanStart(realtimeSeconds, currentMp))
        {
            return false;
        }
        ApplyAuthoritativeCooldown(CooldownSeconds, realtimeSeconds);
        _latestSkillUuid = skillUuid;
        _latestResultUuid = 0;
        AdvancePresentation(realtimeSeconds);
        return true;
    }

    /// <summary>按释放次数轮换动作段；首次释放或间隔超过十秒时使用第一段。</summary>
    /// <param name="realtimeSeconds">本次释放的本机不缩放时间，单位为秒。</param>
    private void AdvancePresentation(float realtimeSeconds)
    {
        if (_presentations.Count == 0)
        {
            return;
        }
        if (_usingTimes < 0 || realtimeSeconds - _fireRealtime > ComboResetSeconds)
        {
            _usingTimes = 0;
        }
        else
        {
            _usingTimes = (_usingTimes + 1) % _presentations.Count;
        }
        SelectPresentation(_usingTimes, realtimeSeconds);
    }

    /// <summary>按指定序号选择核心的当前动作配置；已启动的施法保存了自己的配置，不会因此重播。</summary>
    /// <param name="usingTimes">RO 对应的非负动作序号，按当前武器动作数量取模。</param>
    /// <param name="realtimeSeconds">应用服务器确认时的本机不缩放时间，单位为秒。</param>
    public void SelectPresentation(int usingTimes, float realtimeSeconds)
    {
        if (_presentations.Count == 0 || usingTimes < 0)
        {
            return;
        }
        _usingTimes = usingTimes % _presentations.Count;
        _fireRealtime = realtimeSeconds;
        // 当前段的资源与保底时长必须一同切换，避免继续使用第一段的结束时钟。
        MSkillData data = _presentations[_usingTimes];
        AnimationLocation = data.AnimationLocation;
        AnimationDurationSeconds = data.DurationSeconds;
        SkillStateTimeSeconds = data.SkillStateTimeSeconds;
    }

    /// <summary>最新施法的首次权威结果校正单技能 CD；旧 UUID 或重复结果不会改写后续施法。</summary>
    /// <param name="skillUuid">服务器原样回传的非零施法 UUID。</param>
    /// <param name="remainingCooldownSeconds">权威剩余秒数，非正数清除冷却。</param>
    /// <param name="realtimeSeconds">应用结果的本机不缩放秒数。</param>
    /// <returns>属于最新施法且尚未应用过该结果时为真，供组件决定是否同步 MP 和组 CD。</returns>
    public bool ApplyCastResult(ulong skillUuid, float remainingCooldownSeconds,
        float realtimeSeconds)
    {
        if (!IsLatestCast(skillUuid) || _latestResultUuid == skillUuid) return false;
        _latestResultUuid = skillUuid;
        ApplyAuthoritativeCooldown(remainingCooldownSeconds, realtimeSeconds);
        return true;
    }

    /// <summary>应用当前权威冷却到唯一 CD 记录，直接供施法校验和正式 UI 查询。</summary>
    /// <param name="remainingCooldownSeconds">服务器给出的非负剩余冷却秒数。</param>
    /// <param name="realtimeSeconds">应用同步时的本机不受时间缩放影响的秒数。</param>
    public void ApplyAuthoritativeCooldown(float remainingCooldownSeconds,
        float realtimeSeconds)
    {
        if (remainingCooldownSeconds < 0f)
        {
            remainingCooldownSeconds = 0f;
        }
        if (_cooldownOwner != null)
        {
            _cooldownOwner.SetSkillCD(SkillId, remainingCooldownSeconds, realtimeSeconds);
            return;
        }
        _cooldownEndRealtime = realtimeSeconds + remainingCooldownSeconds;
    }
    #endregion
}
