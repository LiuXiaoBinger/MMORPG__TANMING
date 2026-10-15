using System;
using System.Collections.Generic;
using cfg;

/// <summary>对应 RO 服务器技能释放组件；以既有 OnlineRole 生命周期驱动核心、施法、冷却和权威通知。</summary>
public abstract class ROSkillComponent : RoleComponentBase
{
    /// <summary>一个服务器逻辑帧的毫秒数，与 GameGlobal 的 20Hz 固定帧一致。</summary>
    private const int LogicFrameMilliseconds = 50;
    /// <summary>RO 服务端 SkillComboTime 默认窗口，超过六秒后从第零段重新开始。</summary>
    private const int SkillComboMilliseconds = 6000;

    /// <summary>单技能下次可释放的逻辑 Tick，CD 不跨重新登录持久化。</summary>
    private readonly Dictionary<int, long> skill_cd_ = new Dictionary<int, long>();
    /// <summary>公共 CD 组下次可释放的逻辑 Tick，组 ID 零不进入此表。</summary>
    private readonly Dictionary<int, long> skill_cd_group_cd_ = new Dictionary<int, long>();
    /// <summary>各技能上次成功施法的零起始表现下标，只随成功请求推进。</summary>
    private readonly Dictionary<int, int> using_times_ = new Dictionary<int, int>();
    /// <summary>各技能上次成功使用的单调逻辑 Tick，用于 RO 连续使用窗口判断。</summary>
    private readonly Dictionary<int, long> last_use_time_ = new Dictionary<int, long>();
    /// <summary>已装载效果 ID 到主动技能核心的索引，各等级配置保留原引用。</summary>
    private readonly Dictionary<int, ROSkillCore> skill_core_list_ = new Dictionary<int, ROSkillCore>();
    /// <summary>当前占用角色动作的技能实例；空表示当前没有活动施法。</summary>
    private ROSkill skill_;
    /// <summary>最近成功施法的等级效果 ID，零表示本次在线生命周期尚未释放技能。</summary>
    private int last_used_skill_effect_id_;

    /// <summary>为一个在线角色创建技能运行时组件。</summary>
    /// <param name="owner">拥有中心服角色快照和网关会话的在线角色。</param>
    protected ROSkillComponent(OnlineRole owner) : base(owner)
    {
        // 角色开放前同步装载一次编辑器导出的权限，不依赖客户端异步表现资产。
        SkillCastingRulesCatalog.Initialize();
    }

    /// <summary>角色扩展确认中心服学习快照已完整装载后才允许施法。</summary>
    public abstract bool SkillsLoaded { get; }

    /// <summary>由角色扩展提供中心服装载的权威学习等级。</summary>
    /// <param name="skill_id">待查询的稳定主表技能 ID。</param>
    /// <returns>正数为已学等级，零表示尚未学习。</returns>
    protected abstract int GetLearnedLevel(int skill_id);

    /// <summary>按稳定技能和等级查询主表、效果并装载核心，重复查询复用原配置核心。</summary>
    /// <param name="skill_id">服务端请求使用的稳定技能主表 ID。</param>
    /// <param name="skill_lv">中心服确认的正整数学习等级。</param>
    /// <returns>配置有效时返回已装载核心，缺配置时为空。</returns>
    public ROSkillCore CheckAndAttachSkillCore(int skill_id, int skill_lv)
    {
        // 主表与等级效果来自同一正式 Luban 索引，不接受客户端效果 ID。
        SkillInfo skill_table = LubanMgr.Instance.GetSkillInfoById(skill_id);
        // 此等级唯一对应的权威效果，缺行时保持原配置缺失拒绝语义。
        SkillEffectInfo skill_effect_data = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(skill_id, skill_lv);
        if (skill_table == null || skill_effect_data == null || skill_effect_data.Id <= 0)
        {
            return null;
        }
        // 已装载核心按效果 ID 复用，等级变更可装载新的效果而不清角色原 CD。
        ROSkillCore core = GetSkillCore(skill_effect_data.Id);
        if (core == null)
        {
            core = new ROSkillCore(skill_table, skill_effect_data);
            skill_core_list_.Add(skill_effect_data.Id, core);
        }
        return core;
    }

    /// <summary>查询已经装载的主动技能核心，查询本身不装载新配置。</summary>
    /// <param name="skill_effect_id">技能等级效果的唯一 ID。</param>
    /// <returns>已装载的原核心引用，未装载时为空。</returns>
    public ROSkillCore GetSkillCore(int skill_effect_id)
    {
        // 效果索引中的唯一核心引用，不复制主表或效果配置。
        ROSkillCore core;
        skill_core_list_.TryGetValue(skill_effect_id, out core);
        return core;
    }

    /// <summary>按技能和等级查询已装载核心，与 RO 两参数查询职责一致。</summary>
    /// <param name="skill_id">稳定技能主表 ID。</param>
    /// <param name="skill_lv">待查询的正整数等级。</param>
    /// <returns>该等级已装载的核心，缺行或未装载时为空。</returns>
    public ROSkillCore GetSkillCore(int skill_id, int skill_lv)
    {
        // 当前技能等级对应的只读效果行，用于转为效果 ID 索引查询。
        SkillEffectInfo effect = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(skill_id, skill_lv);
        if (effect == null)
        {
            return null;
        }
        return GetSkillCore(effect.Id);
    }

    /// <summary>返回当前正在占用动作的技能实例，结束后为空。</summary>
    public ROSkill CurrentSkill() { return skill_; }

    /// <summary>按本次施法 UUID 查询当前活动技能，不保存已经结束的施法历史。</summary>
    /// <param name="skill_uuid">待查询的客户端施法 UUID。</param>
    /// <returns>UUID 与当前活动技能一致时返回原实例，否则为空。</returns>
    public ROSkill GetSkill(ulong skill_uuid)
    {
        if (IsCasting() && skill_.GetSkillUUID() == skill_uuid)
        {
            return skill_;
        }
        return null;
    }

    /// <summary>当前存在且仍在释放的动作技能时为 true。</summary>
    public bool IsCasting() { return skill_ != null && skill_.Casting(); }

    /// <summary>检查当前动作是否可由该核心接管，并检查单技能及公共组冷却。</summary>
    /// <param name="core">已经装载的主动技能核心。</param>
    public bool CheckDoSkill(ROSkillCore core)
    {
        if (core == null)
        {
            return false;
        }
        return CheckDoSkill(core.GetSkillTable(), 0UL) &&
            GetSkillCoolDown(core.SkillID()) == 0 &&
            GetSkillGroupCoolDown(core.GetEffect().CooldownGroupId) == 0;
    }

    /// <summary>复用原活动技能取消规则，尚未通过全部请求校验时只判断而不结束旧动作。</summary>
    /// <param name="skill_table">新技能稳定主表，配置尚未校验时允许为空。</param>
    /// <param name="skill_uuid">新请求 UUID，零只用于核心查询而不代表可提交请求。</param>
    private bool CheckDoSkill(SkillInfo skill_table, ulong skill_uuid)
    {
        if (!IsCasting())
        {
            return true;
        }
        // 当前动作是否允许新技能中断；仅值一的旧滑行取消旗标允许 Type3 接管。
        bool can_interrupt;
        if (skill_table != null && skill_table.Type == 3)
        {
            // 当前活动技能主表只负责滑行取消权限，不退还它已扣的 MP/CD。
            SkillInfo active_definition = LubanMgr.Instance.GetSkillInfoById(skill_.SkillID());
            can_interrupt = active_definition != null && active_definition.CanSlideCancel == 1;
        }
        else
        {
            // 缺新配置时沿用原普通互断拒绝结果，不制造另一份权限表。
            int new_skill_id = 0;
            if (skill_table != null)
            {
                new_skill_id = skill_table.Id;
            }
            can_interrupt = SkillCastingRulesCatalog.CanInterrupt(new_skill_id, skill_.SkillID());
        }
        return skill_.GetSkillUUID() != skill_uuid && can_interrupt;
    }

    /// <summary>检查本项目已接入的无目标模式与单技能、公共组冷却，不伪造 RO 目标/Buff/QTE 玩法。</summary>
    /// <param name="core">本次已学习等级解析的核心。</param>
    /// <param name="reason">失败时的现有协议拒绝码，成功时为 Succeed。</param>
    public bool CanSkill(ROSkillCore core, out CmdCode reason)
    {
        reason = CmdCode.Succeed;
        if (core == null)
        {
            reason = CmdCode.SkillConfigMissing;
            return false;
        }
        if (core.GetEffect().TargetMode != 0)
        {
            reason = CmdCode.SkillTargetUnsupported;
            return false;
        }
        if (GetSkillCoolDown(core.SkillID()) > 0 ||
            GetSkillGroupCoolDown(core.GetEffect().CooldownGroupId) > 0)
        {
            reason = CmdCode.SkillOnCooldown;
            return false;
        }
        return true;
    }

    /// <summary>按已提交的权威数据建立唯一当前动作实例；本期仅释放表现，不执行命中伤害。</summary>
    /// <param name="core">本次已装载的技能等级核心。</param>
    /// <param name="skill_uuid">客户端本次施法 UUID。</param>
    /// <param name="duration_ms">当前表现段占用动作的毫秒数。</param>
    /// <param name="using_times">权威选定的零起始表现段下标。</param>
    private void FireSkill(ROSkillCore core, ulong skill_uuid, int duration_ms, int using_times)
    {
        // 向上取整为固定帧期限；零时长也留一帧，保持 Start 先于 End 的原顺序。
        long end_tick = CalculateEndTick(GameGlobal.Instance.LogicTick, duration_ms);
        if (end_tick == GameGlobal.Instance.LogicTick)
        {
            end_tick++;
        }
        skill_ = new ROSkill(core, skill_uuid, end_tick, using_times);
        last_used_skill_effect_id_ = core.SkillEffectID();
    }

    /// <summary>返回最近一次成功施法的等级效果 ID，尚未施法时为零。</summary>
    public int GetLastSkillEffectId() { return last_used_skill_effect_id_; }

    /// <summary>写入单技能的剩余冷却，归零只影响该稳定技能，不结束动作。</summary>
    /// <param name="skill_id">稳定技能主表 ID，同一技能的不同等级共用冷却。</param>
    /// <param name="cool_down">剩余冷却毫秒数，零表示清除。</param>
    public void SetSkillCoolDown(int skill_id, int cool_down)
    {
        skill_cd_[skill_id] = CalculateEndTick(GameGlobal.Instance.LogicTick, cool_down);
    }

    /// <summary>返回单技能当前剩余冷却毫秒数，已到期或没有记录时为零。</summary>
    /// <param name="skill_id">稳定技能主表 ID。</param>
    public int GetSkillCoolDown(int skill_id)
    {
        // 已提交的单调冷却截止 Tick，不依赖动作实例是否仍存活。
        long end_tick;
        if (skill_cd_.TryGetValue(skill_id, out end_tick) && end_tick > GameGlobal.Instance.LogicTick)
        {
            return (int)((end_tick - GameGlobal.Instance.LogicTick) * LogicFrameMilliseconds);
        }
        return 0;
    }

    /// <summary>写入一个已有公共组的剩余冷却，零号表示无公共组。</summary>
    /// <param name="skill_cd_group">正数公共组 ID，零不登记。</param>
    /// <param name="cool_down">本组剩余冷却，单位毫秒。</param>
    public void SetSkillGroupCoolDown(int skill_cd_group, int cool_down)
    {
        if (skill_cd_group > 0)
        {
            skill_cd_group_cd_[skill_cd_group] = CalculateEndTick(GameGlobal.Instance.LogicTick, cool_down);
        }
    }

    /// <summary>返回当前公共组剩余冷却毫秒数，零号或到期时为零。</summary>
    /// <param name="skill_cd_group">公共组 ID，零表示没有公共冷却。</param>
    public int GetSkillGroupCoolDown(int skill_cd_group)
    {
        // 公共组的单调截止 Tick，与单技能记录相互独立。
        long end_tick;
        if (skill_cd_group > 0 && skill_cd_group_cd_.TryGetValue(skill_cd_group, out end_tick) &&
            end_tick > GameGlobal.Instance.LogicTick)
        {
            return (int)((end_tick - GameGlobal.Instance.LogicTick) * LogicFrameMilliseconds);
        }
        return 0;
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

        // 技能未装载或归属角色缺失时拒绝释放。
        if (!SkillsLoaded || Owner == null)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        // 角色不在线或角色数据尚未准备好时拒绝释放。
        if (Owner.State != OnlineRoleState.Online || !Owner.IsRoleDataReady())
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        // 主角色信息或其基础信息缺失时拒绝释放。
        if (Owner.mainRoleInfo == null || Owner.mainRoleInfo.BaseInfo == null)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        if (Owner.mainRoleInfo.BaseInfo.CurrHp <= 0)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }

        // 已经过了结束 Tick 的上一技能先完成，避免网络消息先于本轮 Update 阻塞新请求。
        if (IsCasting() && skill_.IsStateTimeEnd())
        {
            InterruptActiveCast(false);
        }
        // 主表类别决定滑行取消规则；普通技能继续使用编辑器导出的互断名单。
        SkillInfo skillDefinition = LubanMgr.Instance.GetSkillInfoById(request.SkillId);
        if (!CheckDoSkill(skillDefinition, request.SkillUuid))
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }

        // 学习等级只能来自中心服 11080；客户端不能指定等级或效果 ID。
        int learnedLevel = GetLearnedLevel(request.SkillId);
        if (learnedLevel <= 0)
        {
            return Reject(result, CmdCode.SkillNotLearned);
        }

        // 主表只用于技能身份与取消权限，CD、MP 和伤害数值仍读取当前等级效果表。
        ROSkillCore core = CheckAndAttachSkillCore(request.SkillId, learnedLevel);
        if (core == null)
        {
            return Reject(result, CmdCode.SkillConfigMissing);
        }
        // 本次请求只使用服务端学习等级解析并装载的核心效果。
        SkillEffectInfo effect = core.GetEffect();
        result.EffectId = core.SkillEffectID();
        // 滑行按主表类别接入，仍走完整学习、职业、MP 和 CD 校验，不开放轻功类型。
        // 技能所属职业与当前角色职业不符时拒绝释放。
        if (skillDefinition.JobId != Owner.mainRoleInfo.BaseInfo.JobId)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        // 技能类型不属于近战、远程、召唤三类合法类型时拒绝释放。
        if (skillDefinition.Type != 1 && skillDefinition.Type != 2 && skillDefinition.Type != 3)
        {
            return Reject(result, CmdCode.SkillStateBlocked);
        }
        // 无目标规则和冷却由同一释放组件验证，拒绝码沿用原协议。
        CmdCode canSkillReason;
        if (!CanSkill(core, out canSkillReason))
        {
            FillCooldownSnapshot(result, effect.CooldownGroupId, serverUnixMilliseconds);
            return Reject(result, canSkillReason);
        }

        // 拒绝结果也包含当前技能与公共组的剩余 CD 快照。
        FillCooldownSnapshot(result, effect.CooldownGroupId, serverUnixMilliseconds);
        if (result.CurrentMp < effect.MpCost)
        {
            return Reject(result, CmdCode.SkillInsufficientMp);
        }

        if (IsCasting())
        {
            // 新技能的全部拒绝条件已验证；只有真正可提交的请求才能中断旧 UUID。
            // 先发旧技能 End，再由处理器回结果和发新技能 Start；已扣的 MP/CD 不退还。
            InterruptActiveCast(true);
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
        SetSkillCoolDown(request.SkillId, effect.CooldownMs);
        if (effect.CooldownGroupId > 0)
        {
            SetSkillGroupCoolDown(effect.CooldownGroupId, effect.GroupCooldownMs);
        }

        FireSkill(core, request.SkillUuid, castDurationMilliseconds, usingTimes);

        result.CmdCode = CmdCode.Succeed;
        result.CastDurationMs = castDurationMilliseconds;
        result.UsingTimes = usingTimes;
        FillCooldownSnapshot(result, effect.CooldownGroupId, serverUnixMilliseconds);
        startNotice = new SkillCastStartNtf
        {
            CasterRoleId = (int)Owner.GetID(),
            SkillUuid = skill_.GetSkillUUID(),
            SkillId = request.SkillId,
            EffectId = effect.Id,
            StartServerTimeUnixMs = serverUnixMilliseconds,
            CastDurationMs = castDurationMilliseconds,
            UsingTimes = usingTimes
        };
        return true;
    }

    /// <summary>固定帧到期后广播正常结束；角色死亡或离线时停止当前动作。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数，当前结束判断使用全局 Tick。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (!IsCasting())
        {
            return;
        }
        // 归属角色或主角色信息缺失时停止当前动作。
        if (Owner == null || Owner.mainRoleInfo == null)
        {
            StopAll();
            return;
        }
        // 基础信息缺失、血量已空或角色已离线时停止当前动作。
        if (Owner.mainRoleInfo.BaseInfo == null || Owner.mainRoleInfo.BaseInfo.CurrHp <= 0 ||
            Owner.State != OnlineRoleState.Online)
        {
            StopAll();
            return;
        }
        OnUpdate(deltaMilliseconds);
    }

    /// <summary>对应 RO 当前技能动作推进；自然到期通过同一 SkillCease 收尾。</summary>
    /// <param name="delta_time">固定逻辑帧的毫秒跨度。</param>
    protected void OnUpdate(int delta_time)
    {
        if (!skill_.Update(delta_time))
        {
            SkillCease(skill_);
        }
    }

    /// <summary>保留原离线、切图和网络调用接口，结束逻辑统一交给 RO EndSkill。</summary>
    /// <param name="interrupted">非自然结束为 true，已扣 MP/CD 不退还。</param>
    public void InterruptActiveCast(bool interrupted) { EndSkill(interrupted); }

    /// <summary>结束当前技能并广播一次权威通知；先清动作引用，再调用原附近广播。</summary>
    /// <param name="interrupted">主动取消、死亡或离线为 true，自然到期为 false。</param>
    public void EndSkill(bool interrupted)
    {
        if (!IsCasting())
        {
            return;
        }
        // 原实例保留本次 UUID/ID/结束原因，避免结束后误读取新的当前技能。
        ROSkill ended_skill = skill_;
        ended_skill.Freeze(interrupted);
        skill_ = null;
        // 只结束动作，角色的技能和公共组冷却仍按单调 Tick 保留。
        SkillCastEndNtf endNotice = new SkillCastEndNtf
        {
            CasterRoleId = (int)Owner.GetID(),
            SkillUuid = ended_skill.GetSkillUUID(),
            SkillId = ended_skill.SkillID(),
            EndServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Interrupted = ended_skill.Interrupted
        };
        RoleManager.Instance.BroadcastSkillEnd(Owner, endNotice);
    }

    /// <summary>当前技能自然完成时释放动作槽，重复收尾不会再次广播。</summary>
    /// <param name="skill">本次推进到期的原技能实例引用。</param>
    public void SkillCease(ROSkill skill)
    {
        if (skill_ == skill)
        {
            EndSkill(false);
        }
    }

    /// <summary>对应 RO 死亡与离线的停止入口，终止当前动作并保留已经提交的 MP/CD。</summary>
    public void StopAll() { EndSkill(true); }

    /// <summary>释放在线角色专属缓存；离线重连后的 CD 不从数据库恢复。</summary>
    public override void Dispose()
    {
        InterruptActiveCast(true);
        skill_cd_.Clear();
        skill_cd_group_cd_.Clear();
        using_times_.Clear();
        last_use_time_.Clear();
        skill_core_list_.Clear();
        last_used_skill_effect_id_ = 0;
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
        if (last_use_time_.TryGetValue(skillId, out lastUseTick) &&
            (nowTick - lastUseTick) * LogicFrameMilliseconds <= SkillComboMilliseconds &&
            presentationCount > 0)
        {
            using_times_.TryGetValue(skillId, out usingTimes);
            usingTimes = (usingTimes + 1) % presentationCount;
        }
        using_times_[skillId] = usingTimes;
        last_use_time_[skillId] = nowTick;
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
            // 行对象非空且效果、职业与目标配置一致后才继续比对表现字段。
            bool baseMatch = row != null && row.EffectId == effectId && row.ProfessionId == profession.Id;
            if (baseMatch)
            {
                // 模型前缀、默认武器类型与变体下标满足轮换条件。
                bool presentationMatch = row.ModelKey == present.Prefab && row.WeaponType == 0 && row.VariantIndex >= 0;
                if (presentationMatch)
                {
                    result.Add(row);
                }
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
    /// <param name="serverUnixMilliseconds">同一次请求采样的服务器 UTC 毫秒。</param>
    private void FillCooldownSnapshot(SkillCastRet result, int groupId,
        long serverUnixMilliseconds)
    {
        // 统一使用 RO 冷却查询接口，零表示没有记录或当前逻辑帧已到期。
        int skill_cool_down = GetSkillCoolDown(result.SkillId);
        result.CooldownEndUnixMs = 0L;
        if (skill_cool_down > 0)
        {
            result.CooldownEndUnixMs = serverUnixMilliseconds + skill_cool_down;
        }
        // 公共组剩余毫秒与单技能独立，不受当前动作实例回收影响。
        int group_cool_down = GetSkillGroupCoolDown(groupId);
        result.GroupCooldownEndUnixMs = 0L;
        if (group_cool_down > 0)
        {
            result.GroupCooldownEndUnixMs = serverUnixMilliseconds + group_cool_down;
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
        // 归属角色或主角色信息缺失时按零法力处理。
        if (Owner == null || Owner.mainRoleInfo == null)
        {
            return 0;
        }
        // 基础信息缺失或法力值为负时按零法力处理。
        if (Owner.mainRoleInfo.BaseInfo == null || Owner.mainRoleInfo.BaseInfo.CurrMp < 0)
        {
            return 0;
        }
        return Owner.mainRoleInfo.BaseInfo.CurrMp;
    }
}
