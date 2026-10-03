using System;
using System.Collections.Generic;
using cfg;

/// <summary>在线角色的已学技能、权威 MP、冷却和单次施法状态。</summary>
public sealed class RoleSkillComponent : RoleComponentBase
{
    /// <summary>一个服务器逻辑帧的毫秒数，与 GameGlobal 的 20Hz 固定帧一致。</summary>
    private const int LogicFrameMilliseconds = 50;
    /// <summary>RO 服务端 SkillComboTime 默认窗口，超过六秒后从第零段重新开始。</summary>
    private const int SkillComboMilliseconds = 6000;

    /// <summary>技能 ID 到中心服装载的学习等级，零级表示尚未学会。</summary>
    private Dictionary<int, int> _learnedLevels = new Dictionary<int, int>();
    /// <summary>单技能下次可释放的逻辑 Tick，CD 不跨重新登录持久化。</summary>
    private readonly Dictionary<int, long> _skillCooldownEndTicks = new Dictionary<int, long>();
    /// <summary>公共 CD 组下次可释放的逻辑 Tick，组 ID 零不进入此表。</summary>
    private readonly Dictionary<int, long> _groupCooldownEndTicks = new Dictionary<int, long>();
    /// <summary>各技能上次成功施法的零起始表现下标，只随成功请求推进。</summary>
    private readonly Dictionary<int, int> _usingTimes = new Dictionary<int, int>();
    /// <summary>各技能上次成功使用的单调逻辑 Tick，用于 RO 连续使用窗口判断。</summary>
    private readonly Dictionary<int, long> _lastUseTicks = new Dictionary<int, long>();
    /// <summary>中心服技能列表成功装载后才允许检查具体施法。</summary>
    private bool _skillsLoaded;
    /// <summary>当前正在释放的客户端技能 UUID，零表示没有活动施法。</summary>
    private ulong _activeSkillUuid;
    /// <summary>当前活动施法的稳定技能 ID，用于结束通知。</summary>
    private int _activeSkillId;
    /// <summary>当前活动施法截止逻辑 Tick；多动作技能取本段状态时长，其他技能取施法与后摇之和。</summary>
    private long _activeEndTick;

    /// <summary>为一个在线角色创建技能运行时组件。</summary>
    /// <param name="owner">拥有中心服角色快照和网关会话的在线角色。</param>
    public RoleSkillComponent(OnlineRole owner) : base(owner)
    {
    }

    /// <summary>中心服成功回包是否已建立完整学习等级快照。</summary>
    public bool SkillsLoaded
    {
        get { return _skillsLoaded; }
    }

    /// <summary>验证并原子替换中心服下发的已学等级快照，不清除现有 CD。</summary>
    /// <param name="response">中心服 11080 技能列表；只有成功结果可装载。</param>
    /// <returns>全部行有效且没有重复技能 ID 时返回 true。</returns>
    public bool LoadLearnedSkills(RoleSkillInfoRet response)
    {
        if (response == null || response.CmdCode != CmdCode.Succeed)
        {
            return false;
        }

        // 临时索引验证完全部技能后才替换旧状态，避免部分脏数据进入权威校验。
        Dictionary<int, int> levels = new Dictionary<int, int>();
        for (int index = 0; index < response.RoleSkillInfoList.Count; index++)
        {
            // 中心服一行代表该角色对一个稳定技能 ID 的当前学习等级。
            RoleSkillInfo skill = response.RoleSkillInfoList[index];
            if (skill == null || skill.SkillId <= 0 || skill.Level < 0 ||
                levels.ContainsKey(skill.SkillId))
            {
                return false;
            }
            levels.Add(skill.SkillId, skill.Level);
        }

        _learnedLevels = levels;
        _skillsLoaded = true;
        return true;
    }

    /// <summary>在 GameServer 逻辑线程校验并提交一次无目标技能释放。</summary>
    /// <param name="request">只携 RO 式技能 UUID 和稳定技能 ID 的请求。</param>
    /// <param name="result">成功或拒绝时均携带权威 MP/CD 的响应。</param>
    /// <param name="startNotice">成功时生成的表现开始通知；拒绝时为空。</param>
    /// <returns>MP/CD 已同时提交并以请求 UUID 建立活动施法时返回 true。</returns>
    public bool TryCast(SkillCastReq request, out SkillCastRet result,
        out SkillCastStartNtf startNotice)
    {
        startNotice = null;
        // UTC 只供客户端对时；CD 和施法结束判断始终使用单调逻辑 Tick。
        long serverUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // 当前逻辑 Tick 在网络队列消费阶段保持不变，同帧请求按队列顺序结算。
        long nowTick = GameGlobal.Instance.LogicTick;
        result = new SkillCastRet
        {
            CmdCode = CmdCode.ReqParamError,
            ServerTimeUnixMs = serverUnixMilliseconds,
            CurrentMp = GetCurrentMp()
        };
        if (request == null)
        {
            return false;
        }

        result.SkillUuid = request.SkillUuid;
        result.SkillId = request.SkillId;
        if (request.SkillUuid == 0UL || request.SkillId <= 0)
        {
            return false;
        }

        if (!_skillsLoaded || Owner == null || Owner.State != OnlineRoleState.Online ||
            !Owner.IsRoleDataReady() || Owner.mainRoleInfo == null ||
            Owner.mainRoleInfo.BaseInfo == null)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        if (Owner.mainRoleInfo.BaseInfo.CurrHp <= 0)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }

        // 已经过了结束 Tick 的上一技能先完成，避免网络消息先于本轮 Update 阻塞新请求。
        if (_activeSkillUuid != 0UL && nowTick >= _activeEndTick)
        {
            InterruptActiveCast(false);
        }
        if (_activeSkillUuid != 0UL)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }

        // 学习等级只能来自中心服 11080；客户端不能指定等级或效果 ID。
        int learnedLevel;
        if (!_learnedLevels.TryGetValue(request.SkillId, out learnedLevel) || learnedLevel <= 0)
        {
            return Reject(result, CmdCode.SkillNotLearned);
        }

        // 旧 SkillInfo 只用于职业与技能类型身份检查，不读取旧 CD/MP/伤害数值。
        SkillInfo skillDefinition = LubanMgr.Instance.GetSkillInfoById(request.SkillId);
        SkillEffectInfo effect = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(
            request.SkillId, learnedLevel);
        if (skillDefinition == null || effect == null || effect.Id <= 0)
        {
            return Reject(result, CmdCode.SkillConfigMissing);
        }
        result.EffectId = effect.Id;
        if (skillDefinition.JobId != Owner.mainRoleInfo.BaseInfo.JobId ||
            (skillDefinition.Type != 1 && skillDefinition.Type != 2))
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        if (effect.TargetMode != 0)
        {
            return Reject(result, CmdCode.SkillTargetUnsupported);
        }

        // 拒绝结果也包含当前技能与公共组的剩余 CD 快照。
        FillCooldownSnapshot(result, effect.CooldownGroupId, nowTick, serverUnixMilliseconds);
        if (result.CooldownEndUnixMs > serverUnixMilliseconds ||
            result.GroupCooldownEndUnixMs > serverUnixMilliseconds)
        {
            return Reject(result, CmdCode.SkillOnCooldown);
        }
        if (result.CurrentMp < effect.MpCost)
        {
            return Reject(result, CmdCode.SkillInsufficientMp);
        }

        // 当前职业模型与默认武器的表现列表，对应 RO GetUsingTimes 的动作列表取模。
        List<SkillPresentationInfo> presentations = FindPresentations(effect.Id);
        // 只有全部校验通过后才推进；拒绝请求不能消耗下一段动作。
        int usingTimes = CheckUsingTimes(request.SkillId, nowTick, presentations.Count);
        // 单动作技能沿用施法与后摇；多动作技能按本段状态时间结束占用，动画长度独立保留。
        int castDurationMilliseconds = effect.CastMs + effect.RecoveryMs;
        if (presentations.Count > 1)
        {
            // 本次选中的表现段保留原配置引用；零状态时间兼容旧行，仍按完整动画时长结束。
            SkillPresentationInfo selectedPresentation = presentations[usingTimes];
            castDurationMilliseconds = selectedPresentation.DurationMs;
            if (selectedPresentation.SkillStateTimeMs > 0)
            {
                castDurationMilliseconds = selectedPresentation.SkillStateTimeMs;
            }
        }

        // 所有拒绝条件已结束；同一逻辑线程内一次性提交 MP、单技能 CD 与组 CD。
        Owner.mainRoleInfo.BaseInfo.CurrMp -= effect.MpCost;
        result.CurrentMp = Owner.mainRoleInfo.BaseInfo.CurrMp;
        _skillCooldownEndTicks[request.SkillId] = CalculateEndTick(nowTick, effect.CooldownMs);
        if (effect.CooldownGroupId > 0)
        {
            _groupCooldownEndTicks[effect.CooldownGroupId] =
                CalculateEndTick(nowTick, effect.GroupCooldownMs);
        }

        _activeSkillUuid = request.SkillUuid;
        _activeSkillId = request.SkillId;
        // 零时长技能仍保持至少一个逻辑帧，保证开始通知先于结束通知。
        _activeEndTick = CalculateEndTick(nowTick, castDurationMilliseconds);
        if (_activeEndTick == nowTick)
        {
            _activeEndTick++;
        }

        result.CmdCode = CmdCode.Succeed;
        result.CastDurationMs = castDurationMilliseconds;
        result.UsingTimes = usingTimes;
        FillCooldownSnapshot(result, effect.CooldownGroupId, nowTick, serverUnixMilliseconds);
        startNotice = new SkillCastStartNtf
        {
            CasterRoleId = (int)Owner.GetID(),
            SkillUuid = _activeSkillUuid,
            SkillId = request.SkillId,
            EffectId = effect.Id,
            StartServerTimeUnixMs = serverUnixMilliseconds,
            CastDurationMs = castDurationMilliseconds,
            UsingTimes = usingTimes
        };
        return true;
    }

    /// <summary>固定帧到期后广播正常结束；角色死亡时广播中断。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数，当前结束判断使用全局 Tick。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (_activeSkillUuid == 0UL)
        {
            return;
        }
        if (Owner == null || Owner.mainRoleInfo == null || Owner.mainRoleInfo.BaseInfo == null ||
            Owner.mainRoleInfo.BaseInfo.CurrHp <= 0 || Owner.State != OnlineRoleState.Online)
        {
            InterruptActiveCast(true);
            return;
        }
        if (GameGlobal.Instance.LogicTick >= _activeEndTick)
        {
            InterruptActiveCast(false);
        }
    }

    /// <summary>结束当前施法；离线、死亡或切图调用时标记为中断。</summary>
    /// <param name="interrupted">非自然到期时为 true，已扣 MP/CD 默认不退还。</param>
    public void InterruptActiveCast(bool interrupted)
    {
        if (_activeSkillUuid == 0UL || Owner == null)
        {
            return;
        }

        // 先清理活动 ID，再广播，避免同步回调重复发送结束事件。
        SkillCastEndNtf endNotice = new SkillCastEndNtf
        {
            CasterRoleId = (int)Owner.GetID(),
            SkillUuid = _activeSkillUuid,
            SkillId = _activeSkillId,
            EndServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Interrupted = interrupted
        };
        _activeSkillUuid = 0UL;
        _activeSkillId = 0;
        _activeEndTick = 0L;
        RoleManager.Instance.BroadcastSkillEnd(Owner, endNotice);
    }

    /// <summary>释放在线角色专属缓存；离线重连后的 CD 不从数据库恢复。</summary>
    public override void Dispose()
    {
        InterruptActiveCast(true);
        _learnedLevels.Clear();
        _skillCooldownEndTicks.Clear();
        _groupCooldownEndTicks.Clear();
        _usingTimes.Clear();
        _lastUseTicks.Clear();
        _skillsLoaded = false;
        base.Dispose();
    }

    /// <summary>生成本次施法的拒绝结果，不保存跨施法历史状态。</summary>
    /// <param name="result">待填写结果码的权威响应。</param>
    /// <param name="code">具体业务拒绝原因。</param>
    /// <returns>拒绝结果始终为 false。</returns>
    private bool Reject(SkillCastRet result, CmdCode code)
    {
        result.CmdCode = code;
        return false;
    }

    /// <summary>按 RO 六秒窗口推进成功使用次数，并返回当前动作列表的零起始下标。</summary>
    /// <param name="skillId">角色已学习的稳定技能 ID，各技能独立计数。</param>
    /// <param name="nowTick">本次成功施法的单调逻辑 Tick，每 Tick 为 50 毫秒。</param>
    /// <param name="presentationCount">当前职业模型默认武器的动作数量，零表示无表现。</param>
    /// <returns>首次或超过窗口为零，否则按动作数量循环。</returns>
    private int CheckUsingTimes(int skillId, long nowTick, int presentationCount)
    {
        // 首次使用必须从零开始，不依赖服务器运行时间是否已经超过六秒。
        int usingTimes = 0;
        // 上次成功施法 Tick；没有记录时不会进入递增分支。
        long lastUseTick;
        if (_lastUseTicks.TryGetValue(skillId, out lastUseTick) &&
            (nowTick - lastUseTick) * LogicFrameMilliseconds <= SkillComboMilliseconds &&
            presentationCount > 0)
        {
            _usingTimes.TryGetValue(skillId, out usingTimes);
            usingTimes = (usingTimes + 1) % presentationCount;
        }
        _usingTimes[skillId] = usingTimes;
        _lastUseTicks[skillId] = nowTick;
        return usingTimes;
    }

    /// <summary>收集当前职业模型默认武器的动作，按 VariantIndex 对齐客户端段数。</summary>
    /// <param name="effectId">本次已学等级解析出的效果配置 ID。</param>
    /// <returns>排序后的表现行快照，未配置时为空，不修改原配置集合。</returns>
    private List<SkillPresentationInfo> FindPresentations(int effectId)
    {
        // 只读候选列表，行对象仍指向 Luban 原始配置。
        List<SkillPresentationInfo> result = new List<SkillPresentationInfo>();
        // 本项目尚未接入武器表现属性，与客户端一样使用默认武器零。
        ProfessionInfo profession = LubanMgr.Instance.GetProfessionInfoById(
            Owner.mainRoleInfo.BaseInfo.JobId);
        if (profession == null)
        {
            return result;
        }
        // 职业表现表提供当前模型资源键。
        PresentTable present = LubanMgr.Instance.GetPresentTableById(profession.PresentTableID);
        // 技能表现配置的只读索引，缺表时保持无表现技能的原施法时长。
        Dictionary<int, SkillPresentationInfo> presentations = LubanMgr.Instance.GetSkillPresentationInfos();
        if (present == null || string.IsNullOrEmpty(present.Prefab) || presentations == null)
        {
            return result;
        }
        // 字典行快照供索引遍历，不复制或修改原始行内容。
        List<SkillPresentationInfo> rows = new List<SkillPresentationInfo>(presentations.Values);
        // 候选行下标，范围为零到表现行数量减一。
        for (int index = 0; index < rows.Count; index++)
        {
            // 必须同时匹配效果、职业、模型和默认武器才参与该技能动作轮换。
            SkillPresentationInfo row = rows[index];
            if (row != null && row.EffectId == effectId && row.ProfessionId == profession.Id &&
                row.ModelKey == present.Prefab && row.WeaponType == 0 && row.VariantIndex >= 0)
            {
                result.Add(row);
            }
        }
        result.Sort(ComparePresentationOrder);
        return result;
    }

    /// <summary>按配置动作变体顺序排序，使客户端与服务端使用相同的列表下标。</summary>
    /// <param name="left">左侧候选表现配置。</param>
    /// <param name="right">右侧候选表现配置。</param>
    /// <returns>左行变体小于右行时返回负数，相同时为零。</returns>
    private static int ComparePresentationOrder(SkillPresentationInfo left, SkillPresentationInfo right)
    {
        return left.VariantIndex.CompareTo(right.VariantIndex);
    }

    /// <summary>按角色技能和公共组的剩余逻辑 Tick 生成 UTC 毫秒结束时间。</summary>
    /// <param name="result">需要填入当前权威 CD 快照的施法响应。</param>
    /// <param name="groupId">效果配置的共享 CD 组，零表示无公共 CD。</param>
    /// <param name="nowTick">处理当前请求时的服务器逻辑 Tick。</param>
    /// <param name="serverUnixMilliseconds">同一次请求采样的服务器 UTC 毫秒。</param>
    private void FillCooldownSnapshot(SkillCastRet result, int groupId,
        long nowTick, long serverUnixMilliseconds)
    {
        // 技能或组没有记录、已经到期时返回零，客户端可直接清除对应 UI 冷却。
        long skillEndTick;
        result.CooldownEndUnixMs = 0L;
        if (_skillCooldownEndTicks.TryGetValue(result.SkillId, out skillEndTick) && skillEndTick > nowTick)
        {
            result.CooldownEndUnixMs = serverUnixMilliseconds +
                (skillEndTick - nowTick) * LogicFrameMilliseconds;
        }
        result.GroupCooldownEndUnixMs = 0L;
        long groupEndTick;
        if (groupId > 0 && _groupCooldownEndTicks.TryGetValue(groupId, out groupEndTick) &&
            groupEndTick > nowTick)
        {
            result.GroupCooldownEndUnixMs = serverUnixMilliseconds +
                (groupEndTick - nowTick) * LogicFrameMilliseconds;
        }
    }

    /// <summary>把毫秒持续时间向上取整为服务端逻辑帧截止 Tick。</summary>
    /// <param name="nowTick">开始时的单调逻辑 Tick。</param>
    /// <param name="durationMilliseconds">非负持续时间，单位毫秒。</param>
    /// <returns>可再次释放或结束施法的逻辑 Tick。</returns>
    private static long CalculateEndTick(long nowTick, int durationMilliseconds)
    {
        // 向上取整可避免不足 50ms 的 CD 被错误当作零帧。
        long durationTicks = ((long)durationMilliseconds + LogicFrameMilliseconds - 1L) /
            LogicFrameMilliseconds;
        return nowTick + durationTicks;
    }

    /// <summary>读取在线角色的权威内存 MP，不把客户端上报值作为来源。</summary>
    /// <returns>当前非负法力值；角色快照尚未就绪时为零。</returns>
    private int GetCurrentMp()
    {
        if (Owner == null || Owner.mainRoleInfo == null || Owner.mainRoleInfo.BaseInfo == null ||
            Owner.mainRoleInfo.BaseInfo.CurrMp < 0)
        {
            return 0;
        }
        return Owner.mainRoleInfo.BaseInfo.CurrMp;
    }
}
