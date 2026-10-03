/// <summary>
/// 实体已装配的单个技能等级配置与冷却状态，由技能字典及存活施法分别持有。
/// 释放前的预测冷却与服务器回包按施法 UUID 配对，旧回包不能覆盖新一次释放。
/// </summary>
public sealed class MSkillCore
{
    /// <summary>RO 预表现时长结束后等待服务器确认的秒数。</summary>
    private const float PerformanceGraceSeconds = 2f;

    /// <summary>RO 本地动作轮换间隔超过十秒时重新使用第一段。</summary>
    private const float ComboResetSeconds = 10f;

    /// <summary>当前职业默认武器的有序动作列表，生命周期随核心结束。</summary>
    private readonly System.Collections.Generic.List<MSkillData> _presentations;

    /// <summary>上次已启动表现的动作序号，负一表示本核心尚未释放。</summary>
    private int _usingTimes = -1;

    /// <summary>上次启动表现时的本机不缩放时间，单位为秒，用于 RO 十秒重置。</summary>
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

    /// <summary>动画索引登记的真实剪辑 Location。</summary>
    public string AnimationLocation { get; private set; }

    /// <summary>技能表现的保底时长，单位为秒，资源尚未加载时用于结束动作。</summary>
    public float AnimationDurationSeconds { get; private set; }

    /// <summary>当前选中动作占用技能状态的时长，单位为秒；与完整剪辑长度分开。</summary>
    public float SkillStateTimeSeconds { get; private set; }

    /// <summary>当前冷却截止的本机不受时间缩放影响的时间，单位为秒。</summary>
    private float _cooldownEndRealtime;

    /// <summary>尚待确认的独立施法记录，连击动画结束后不丢弃上一段 UUID。</summary>
    private readonly System.Collections.Generic.List<MSkillPrediction> _predictions =
        new System.Collections.Generic.List<MSkillPrediction>();

    /// <summary>该核心最后启动的施法 UUID，仅此 UUID 可更新当前冷却快照。</summary>
    private ulong _latestSkillUuid;

    /// <summary>字典和存活施法实例合计持有数；为零时由工厂归还。</summary>
    private int _ownerCount;

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
            fallbackData.Initialize(0, animationLocation,
                animationDurationSeconds, animationDurationSeconds);
            _presentations.Add(fallbackData);
        }
        _cooldownEndRealtime = 0f;
        _latestSkillUuid = 0;
        _ownerCount = 1;
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

    /// <summary>最终归还时清除动作段和预测记录，旧施法引用已全部解除。</summary>
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
        // index 为预测记录列表的零起始下标，逐项归还尚未确认的临时记录。
        for (int index = 0; index < _predictions.Count; index++)
        {
            // 尚未确认的预测记录，随核心一起失效。
            MSkillPrediction prediction = _predictions[index];
            prediction.Reset();
            MCommonObjectPool<MSkillPrediction>.Release(prediction);
        }
        _predictions.Clear();
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
        _cooldownEndRealtime = 0f;
        _latestSkillUuid = 0;
        _usingTimes = -1;
        _fireRealtime = 0f;
    }

    /// <summary>当前是否存在等待服务器接受或拒绝的预测施法。</summary>
    public bool HasPendingPrediction
    {
        get { return _predictions.Count > 0; }
    }

    /// <summary>是否配置多段武器动作；只有此类普通连击可在旧段等待确认时续下一段。</summary>
    public bool HasMultiplePresentations { get { return _presentations.Count > 1; } }

    /// <summary>本地或服务器最近选择的动作序号，从零开始。</summary>
    public int UsingTimes { get { return System.Math.Max(0, _usingTimes); } }

    /// <summary>等级配置更新时继承旧核心的冷却和待确认 UUID，避免技能快照吞掉结果回包。</summary>
    /// <param name="previous">更新前仍在同一角色上的技能核心。</param>
    public void AdoptRuntimeState(MSkillCore previous)
    {
        if (previous == null || previous.SkillId != SkillId)
        {
            return;
        }
        _cooldownEndRealtime = previous._cooldownEndRealtime;
        _predictions.AddRange(previous._predictions);
        previous._predictions.Clear();
        _latestSkillUuid = previous._latestSkillUuid;
        _usingTimes = previous._usingTimes;
        _fireRealtime = previous._fireRealtime;
    }

    /// <summary>是否有可由动画索引解析的真实技能剪辑配置。</summary>
    public bool HasPresentation
    {
        get { return !string.IsNullOrEmpty(AnimationLocation) && AnimationDurationSeconds > 0f; }
    }

    /// <summary>查询在给定本机时间和法力值下能否发起技能请求。</summary>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的时间，单位为秒。</param>
    /// <param name="currentMp">角色当前已知法力值，非负整数；最终由服务器校验。</param>
    /// <returns>配置有效、法力充足且冷却结束时返回 true；单段技能还要求没有待确认请求。</returns>
    public bool CanStart(float realtimeSeconds, int currentMp)
    {
        if (SkillId <= 0 || Level <= 0 || EffectId <= 0 ||
            TargetMode != 0 || CooldownSeconds < 0f ||
            CooldownGroupId < 0 || GroupCooldownSeconds < 0f ||
            CastDurationSeconds < 0f || MpCost < 0)
        {
            return false;
        }
        if ((!HasMultiplePresentations && HasPendingPrediction) || currentMp < MpCost)
        {
            return false;
        }
        return realtimeSeconds >= _cooldownEndRealtime;
    }

    /// <summary>读取目前剩余的冷却时长，供控制器刷新技能槽。</summary>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的时间，单位为秒。</param>
    /// <returns>非负剩余秒数。</returns>
    public float GetRemainingCooldown(float realtimeSeconds)
    {
        // 冷却剩余秒数，过期时钳制为零，避免界面出现负倒计时。
        float remainingSeconds = _cooldownEndRealtime - realtimeSeconds;
        if (remainingSeconds <= 0f)
        {
            return 0f;
        }
        return remainingSeconds;
    }

    /// <summary>本地请求发送前登记预测冷却，只有对应施法结果可以确认或回滚。</summary>
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
        // 保存预测前的截止时间；只有最后一段超时才能据此回滚，旧段不能覆盖新段 CD。
        float previousCooldownEnd = _cooldownEndRealtime;
        _cooldownEndRealtime = realtimeSeconds + CooldownSeconds;
        _latestSkillUuid = skillUuid;
        AdvancePresentation(realtimeSeconds);
        // 有动画时按剪辑时长，无动画时按服务器施法时长，均留 RO 的两秒确认宽限。
        float presentationSeconds = CastDurationSeconds;
        if (HasPresentation)
        {
            presentationSeconds = AnimationDurationSeconds;
        }
        // 本次记录由核心持有，直到确认、拒绝、超时或核心清理。
        MSkillPrediction prediction = MCommonObjectPool<MSkillPrediction>.Get();
        prediction.Initialize(skillUuid, previousCooldownEnd,
            realtimeSeconds + presentationSeconds + PerformanceGraceSeconds);
        _predictions.Add(prediction);
        return true;
    }

    /// <summary>按 RO 使用次数推进本次动作；首次和间隔超过十秒时从第一段开始。</summary>
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

    /// <summary>服务器开始通知使用权威动作序号选择表现；本地确认不得重播已经开始的动作。</summary>
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

    /// <summary>预表现超过确认期限时回滚本次预测冷却。</summary>
    /// <param name="realtimeSeconds">当前本机不缩放时间，单位为秒。</param>
    /// <returns>已撤销的施法 UUID，尚未到期时返回零。</returns>
    public ulong ExpirePrediction(float realtimeSeconds)
    {
        // 按启动顺序查找已超过本段确认期限的请求。
        for (int index = 0; index < _predictions.Count; index++)
        {
            // 当前等待确认的单次施法；每次返回一笔，组件继续保留其他待确认记录。
            MSkillPrediction prediction = _predictions[index];
            if (realtimeSeconds >= prediction.DeadlineRealtime)
            {
                // 撤销前保存 UUID，用于清理对应旧表现而不是当前新段。
                ulong expiredSkillUuid = prediction.SkillUuid;
                CancelPrediction(expiredSkillUuid);
                return expiredSkillUuid;
            }
        }
        return 0;
    }

    /// <summary>仅对匹配的预测施法应用服务器给出的剩余冷却。</summary>
    /// <param name="skillUuid">服务器原样回传的单次施法 UUID。</param>
    /// <param name="remainingCooldownSeconds">权威剩余冷却秒数，负数按零处理。</param>
    /// <param name="realtimeSeconds">收到结果时的本机不受时间缩放影响的秒数。</param>
    /// <returns>找到待确认记录时返回 true；旧段可确认实例，但不覆盖最新段的冷却。</returns>
    public bool ApplyCastResult(ulong skillUuid, float remainingCooldownSeconds,
        float realtimeSeconds)
    {
        // 按索引确认唯一施法 UUID，移除匹配记录后立即返回。
        for (int index = 0; index < _predictions.Count; index++)
        {
            if (_predictions[index].SkillUuid != skillUuid)
            {
                continue;
            }
            // 匹配记录已脱离核心列表，旧 UUID 不再可被重复确认。
            MSkillPrediction prediction = _predictions[index];
            _predictions.RemoveAt(index);
            prediction.Reset();
            MCommonObjectPool<MSkillPrediction>.Release(prediction);
            if (IsLatestCast(skillUuid))
            {
                ApplyAuthoritativeCooldown(remainingCooldownSeconds, realtimeSeconds);
            }
            return true;
        }
        return false;
    }

    /// <summary>发送失败或确认超时后撤销该笔预测，仅最新请求可以回滚当前冷却。</summary>
    /// <param name="skillUuid">失败请求的单次施法 UUID。</param>
    /// <returns>UUID 与当前预测一致时返回 true。</returns>
    public bool CancelPrediction(ulong skillUuid)
    {
        // 按索引匹配待撤销请求，旧段与新段分别保存回滚数据。
        for (int index = 0; index < _predictions.Count; index++)
        {
            // 配对的旧段只能撤销自身；最新段才允许恢复预测前的冷却。
            MSkillPrediction prediction = _predictions[index];
            if (prediction.SkillUuid != skillUuid)
            {
                continue;
            }
            if (IsLatestCast(skillUuid))
            {
                _cooldownEndRealtime = prediction.PreviousCooldownEnd;
            }
            _predictions.RemoveAt(index);
            prediction.Reset();
            MCommonObjectPool<MSkillPrediction>.Release(prediction);
            return true;
        }
        return false;
    }

    /// <summary>判断结果是否属于本核心最后发起的一段，用于防止旧回包覆盖更新的 MP/CD。</summary>
    /// <param name="skillUuid">待比较的非零施法 UUID。</param>
    /// <returns>UUID 与最近施法一致时返回 true。</returns>
    public bool IsLatestCast(ulong skillUuid)
    {
        return skillUuid != 0 && skillUuid == _latestSkillUuid;
    }

    /// <summary>应用服务器主动同步的冷却；不会解除另一笔尚待结果的预测请求。</summary>
    /// <param name="remainingCooldownSeconds">服务器给出的非负剩余冷却秒数。</param>
    /// <param name="realtimeSeconds">应用同步时的本机不受时间缩放影响的秒数。</param>
    public void ApplyAuthoritativeCooldown(float remainingCooldownSeconds,
        float realtimeSeconds)
    {
        if (remainingCooldownSeconds < 0f)
        {
            remainingCooldownSeconds = 0f;
        }
        _cooldownEndRealtime = realtimeSeconds + remainingCooldownSeconds;
    }
}
