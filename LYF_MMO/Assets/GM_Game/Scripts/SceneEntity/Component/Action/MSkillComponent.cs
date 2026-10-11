using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 技能动作状态组件。
/// 既是实体的快捷组件（ComponentType.Skill），也是技能动作状态。
/// 维护已学效果、预测与权威冷却和单次技能动画表现，数值结算只由服务器执行。
/// </summary>
public sealed class MSkillComponent : MBasicActionComponent
{
    /// <summary>实体当前已学习/可用的技能 ID 集合。</summary>
    private readonly HashSet<int> _learnedSkillIds = new HashSet<int>();

    /// <summary>按技能主表 ID 索引的已学等级配置与单技能 CD。</summary>
    private readonly Dictionary<int, MSkillCore> _cores =
        new Dictionary<int, MSkillCore>();

    /// <summary>RO 公共组冷却记录，按索引递减并到期归池，元素与 SkillCDGroupDic 为同一对象。</summary>
    private readonly List<SkillCDGroupInfo> SkillCDGroupList = new List<SkillCDGroupInfo>();
    /// <summary>RO 单技能冷却记录，按索引递减并到期归池，元素与 SkillCDDic 为同一对象。</summary>
    private readonly List<SkillCDGroupInfo> SkillCDList = new List<SkillCDGroupInfo>();
    /// <summary>公共组 ID 到当前冷却记录；与单技能 ID 即使数值相同也独立维护。</summary>
    public readonly Dictionary<int, SkillCDGroupInfo> SkillCDGroupDic = new Dictionary<int, SkillCDGroupInfo>();
    /// <summary>技能主表 ID 到唯一单技能冷却记录，已装配核心的预测与权威结果共同读写此处。</summary>
    public readonly Dictionary<int, SkillCDGroupInfo> SkillCDDic = new Dictionary<int, SkillCDGroupInfo>();

    /// <summary>实体当前动作槽借出的单次表现；空值表示槽位未占用。</summary>
    private MSkill _currentSkill;

    /// <summary>已脱离主动作槽或独立运行的存活技能，保留各自的时间轴与 UUID。</summary>
    private readonly List<MSkill> _skills = new List<MSkill>();

    /// <summary>本实体最后一次本地施法 UUID，用于防止旧段回包覆盖更新的 MP。</summary>
    private ulong _latestSkillUuid;

    /// <summary>构造函数，初始化技能状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MSkillComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateSkill; }

    /// <summary>组件类型为技能快捷组件，不进入普通组件集合。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Skill; } }

    /// <summary>按当前技能 RO MoveCasting 控制普通水平移动；主动起跳仍禁止，已有重力和落地继续。</summary>
    public override bool IsActiveMovable
    {
        get
        {
            return _currentSkill != null && _currentSkill.IsActive &&
                SkillCastingRulesCatalog.IsMoveCasting(_currentSkill.SkillId);
        }
    }

    /// <summary>技能状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_SKILL; } }

    /// <summary>当前动作槽中的施法实例，空值表示没有动作；旧施法保存在列表。</summary>
    public MSkill CurrentSkill { get { return _currentSkill; } }

    /// <summary>
    /// 查询实体是否拥有指定技能。
    /// </summary>
    /// <param name="skillId">技能配置 ID。</param>
    /// <returns>true 表示已拥有该技能。</returns>
    public bool HasSkill(int skillId) { return _learnedSkillIds.Contains(skillId); }

    /// <summary>
    /// 设置技能的启用/禁用状态。
    /// </summary>
    /// <param name="skillId">技能配置 ID，小于等于 0 时忽略。</param>
    /// <param name="enabled">true 添加技能，false 移除技能。</param>
    public void SetSkill(int skillId, bool enabled)
    {
        if (skillId <= 0) return;
        if (enabled) _learnedSkillIds.Add(skillId);
        else
        {
            _learnedSkillIds.Remove(skillId);
            // 先解除本技能旧施法持有，再解除字典持有；最后一个引用才归还核心。
            // 被移除技能的字典持有核心，可能仍由旧施法独立持有。
            MSkillCore removedCore;
            _cores.TryGetValue(skillId, out removedCore);
            _cores.Remove(skillId);
            SetSkillCD(skillId, 0f);
            // index 从最后一项递减，回收被移除技能的旧施法后仍能检查前面的原实例。
            for (int index = _skills.Count - 1; index >= 0; index--)
            {
                if (_skills[index].SkillId == skillId)
                {
                    _skills[index].Cease(true);
                }
            }
            if (_currentSkill != null && _currentSkill.SkillId == skillId)
            {
                _currentSkill.Cease(true);
            }
            if (removedCore != null)
            {
                removedCore.Release();
            }
        }
    }

    /// <summary>登记已学技能的效果配置；同等级重复同步时保留已有预测和权威 CD。</summary>
    /// <param name="core">工厂借出的技能核心；本方法消费其初始持有权。</param>
    public void SetSkillCore(MSkillCore core)
    {
        if (core == null || core.SkillId <= 0 || core.Level <= 0)
        {
            if (core != null)
            {
                core.Release();
            }
            return;
        }
        // previous 是同技能的原字典核心，携带已有 CD 与最近施法身份，替换后解除其字典持有。
        MSkillCore previous;
        if (_cores.TryGetValue(core.SkillId, out previous) && previous != null)
        {
            if (previous.Level == core.Level && previous.EffectId == core.EffectId)
            {
                _learnedSkillIds.Add(core.SkillId);
                core.Release();
                return;
            }
            // 等级升级沿用唯一 CD 记录及最近施法身份，不重新阻断已可释放的技能。
            core.AdoptRuntimeState(previous);
        }
        _cores[core.SkillId] = core;
        core.BindCooldown(this);
        _learnedSkillIds.Add(core.SkillId);
        if (previous != null)
        {
            previous.Release();
        }
    }

    /// <summary>按最新已学技能快照移除失效核心，保留仍有效技能的冷却与当前表现。</summary>
    /// <param name="skillIds">11080 回包中当前等级大于零的技能 ID 集合。</param>
    public void RetainSkillCores(HashSet<int> skillIds)
    {
        if (skillIds == null)
        {
            return;
        }
        // 临时技能 ID 快照借自列表池，允许删除原字典中的失效核心。
        List<int> existingSkillIds = MListPool<int>.Get();
        try
        {
            existingSkillIds.AddRange(_cores.Keys);
            // index 为旧技能 ID 快照的零起始下标，快照内容不会随字典删除而变化。
            for (int index = 0; index < existingSkillIds.Count; index++)
            {
                // 已装配核心对应的旧技能主表 ID。
                int skillId = existingSkillIds[index];
                if (!skillIds.Contains(skillId))
                {
                    SetSkill(skillId, false);
                }
            }
        }
        finally
        {
            MListPool<int>.Release(existingSkillIds);
        }
    }

    /// <summary>按旧技能主键获取当前已学等级的配置核心。</summary>
    /// <param name="skillId">旧 SkillInfo.Id。</param>
    /// <returns>对应核心，未装配时为空。</returns>
    public MSkillCore GetSkillCore(int skillId)
    {
        // 技能主键对应的长期技能核心。
        MSkillCore core;
        if (_cores.TryGetValue(skillId, out core))
        {
            return core;
        }
        return null;
    }

    /// <summary>清理技能列表、公共 CD 和施法表现，供角色重载或实体回收使用。</summary>
    public void ClearSkillCores()
    {
        if (_currentSkill != null)
        {
            _currentSkill.Cease(true);
        }
        // 倒序清理存活技能，Cease 从原列表移除自身。
        for (int index = _skills.Count - 1; index >= 0; index--)
        {
            _skills[index].Cease(true);
        }
        _skills.Clear();
        _latestSkillUuid = 0;
        if (Entity != null && Entity.Machine != null &&
            Entity.Machine.CurrentState == this)
        {
            Entity.Machine.ForceToDefault();
        }
        _learnedSkillIds.Clear();
        // 字典值快照使每个核心恰好解除一次持有，旧实例已先解除其独立持有。
        List<MSkillCore> coreSnapshot = MListPool<MSkillCore>.Get();
        coreSnapshot.AddRange(_cores.Values);
        _cores.Clear();
        // index 为核心引用快照的零起始下标，每项仅解除一次字典持有。
        for (int index = 0; index < coreSnapshot.Count; index++)
        {
            coreSnapshot[index].Release();
        }
        MListPool<MSkillCore>.Release(coreSnapshot);
        ClearGroupCd();
    }

    /// <summary>RO 同名单技能剩余冷却查询，使用当前本机不缩放时钟。</summary>
    /// <param name="skill_id">技能主表正数 ID，不是效果 ID 或公共组 ID。</param>
    /// <returns>未登记或已到期时为零，否则为非负剩余秒数。</returns>
    public float GetSkillCD(int skill_id)
    {
        return GetSkillCD(skill_id, Time.realtimeSinceStartup);
    }

    /// <summary>在调用方提供的同一不缩放时钟上查询单技能冷却，供核心预测及原 UI 接口共同读取。</summary>
    /// <param name="skill_id">技能主表 ID。</param>
    /// <param name="realtimeSeconds">查询的不缩放秒数；早于登记时刻时不反向增加冷却。</param>
    /// <returns>唯一 RO 记录的非负剩余秒数。</returns>
    internal float GetSkillCD(int skill_id, float realtimeSeconds)
    {
        // 原单技能记录引用，查询只计算帧间剩余值，不重复扣减列表数据。
        SkillCDGroupInfo info;
        if (!SkillCDDic.TryGetValue(skill_id, out info)) return 0f;
        return Mathf.Max(0f, info.CurrentCD - Mathf.Max(0f, realtimeSeconds - info.UpdatedRealtime));
    }

    /// <summary>RO 同名总冷却查询，用于技能图标进度；记录已到期时返回零。</summary>
    /// <param name="skill_id">技能主表 ID。</param>
    /// <returns>最近一次 SetSkillCD 写入的总秒数。</returns>
    public float GetSkillTotalCD(int skill_id)
    {
        // 当前有效记录；即使列表尚未下一帧移除，过期查询也不展示旧进度。
        SkillCDGroupInfo info;
        if (GetSkillCD(skill_id) <= 0f || !SkillCDDic.TryGetValue(skill_id, out info)) return 0f;
        return info.CD;
    }

    /// <summary>RO 同名设置单技能冷却；相同值也刷新本次冷却，非正数删除并立即归池。</summary>
    /// <param name="skill_id">技能主表正数 ID；与组冷却的数字键独立。</param>
    /// <param name="cd">需要同步的剩余秒数，非正值表示清除。</param>
    public void SetSkillCD(int skill_id, float cd)
    {
        SetSkillCD(skill_id, cd, Time.realtimeSinceStartup);
    }

    /// <summary>按预测或权威结果的原时间戳设置唯一单技能记录，不再保留核心的第二个截止值。</summary>
    /// <param name="skill_id">技能主表正数 ID。</param>
    /// <param name="cd">需要同步的剩余秒数；无效浮点或非正数清除。</param>
    /// <param name="realtimeSeconds">写入发生的本机不缩放秒数。</param>
    internal void SetSkillCD(int skill_id, float cd, float realtimeSeconds)
    {
        if (skill_id <= 0) return;
        // 已有记录始终复用原引用，清除时必须从两处解除再归池。
        SkillCDGroupInfo info;
        SkillCDDic.TryGetValue(skill_id, out info);
        if (cd <= 0f || float.IsNaN(cd) || float.IsInfinity(cd))
        {
            if (info != null)
            {
                SkillCDList.Remove(info);
                SkillCDDic.Remove(skill_id);
                MSkillFactory.singleton.ReturnSkillCdGroupInfo(info);
            }
            return;
        }
        if (info == null)
        {
            info = MSkillFactory.singleton.GetSkillCdGroupInfo();
            info.SkillGroupType = skill_id;
            SkillCDDic.Add(skill_id, info);
            SkillCDList.Add(info);
        }
        // 同名设置接口的总量与剩余一起刷新，不以配置基础值覆盖本次权威同步值。
        info.CD = cd;
        info.CurrentCD = cd;
        info.UpdatedRealtime = realtimeSeconds;
    }

    /// <summary>RO 同名按核心设置公共组冷却，组归属来自当前等级效果配置，时长采用同步值。</summary>
    /// <param name="core">当前实体的技能核心，CooldownGroupId 为零时不设置。</param>
    /// <param name="cd">公共组的权威剩余秒数；非正值清除。</param>
    public void SetGroupCd(MSkillCore core, float cd)
    {
        if (core == null || Entity == null || !Entity.IsPlayer) return;
        SetGroupCdByType(core.CooldownGroupId, cd, Time.realtimeSinceStartup);
    }

    /// <summary>RO 同名按公共组 ID 同步剩余冷却，独立于任何一个单技能记录。</summary>
    /// <param name="type">正数 CooldownGroupId，零表示没有公共组。</param>
    /// <param name="cd">该组剩余秒数；非正或无效浮点清除。</param>
    public void SetGroupCdByType(int type, float cd)
    {
        SetGroupCdByType(type, cd, Time.realtimeSinceStartup);
    }

    /// <summary>权威结果在其接收时钟设置公共组冷却，同值刷新、归零清除都只操作该组。</summary>
    /// <param name="type">正数公共组 ID。</param>
    /// <param name="cd">服务器给出的剩余秒数。</param>
    /// <param name="realtimeSeconds">接收结果的本机不缩放秒数。</param>
    private void SetGroupCdByType(int type, float cd, float realtimeSeconds)
    {
        if (type <= 0) return;
        // 公共组原引用，更新不会在列表里重复插入同一键。
        SkillCDGroupInfo info;
        SkillCDGroupDic.TryGetValue(type, out info);
        if (cd <= 0f || float.IsNaN(cd) || float.IsInfinity(cd))
        {
            if (info != null)
            {
                SkillCDGroupList.Remove(info);
                SkillCDGroupDic.Remove(type);
                MSkillFactory.singleton.ReturnSkillCdGroupInfo(info);
            }
            return;
        }
        if (info == null)
        {
            info = MSkillFactory.singleton.GetSkillCdGroupInfo();
            info.SkillGroupType = type;
            SkillCDGroupDic.Add(type, info);
            SkillCDGroupList.Add(info);
        }
        info.CD = cd;
        info.CurrentCD = cd;
        info.UpdatedRealtime = realtimeSeconds;
    }

    /// <summary>RO 同名清场入口，同时清除单技能和公共组记录；已装配核心也立即读到零。</summary>
    public void ClearGroupCd()
    {
        // 组记录归池后立刻清容器，不保留可复用对象引用。
        for (int index = SkillCDGroupList.Count - 1; index >= 0; index--)
        {
            MSkillFactory.singleton.ReturnSkillCdGroupInfo(SkillCDGroupList[index]);
        }
        SkillCDGroupList.Clear();
        SkillCDGroupDic.Clear();
        // 单技能列表中的记录与组记录互不共享，每项仅归还一次。
        for (int index = SkillCDList.Count - 1; index >= 0; index--)
        {
            MSkillFactory.singleton.ReturnSkillCdGroupInfo(SkillCDList[index]);
        }
        SkillCDList.Clear();
        SkillCDDic.Clear();
    }

    /// <summary>RO 技能结束通知，只解除原实例引用；具体技能在回调后自行清理并归还工厂。</summary>
    /// <param name="skill">正在 Cease 的原实例，不能按技能 ID 猜测或解除其他 UUID。</param>
    public void OnSkillEnd(MSkill skill)
    {
        if (skill == null) return;
        _skills.Remove(skill);
        if (!ReferenceEquals(_currentSkill, skill)) return;
        _currentSkill = null;
        if (Entity != null && Entity.Machine != null && Entity.Machine.CurrentState == this)
        {
            Entity.Machine.ForceToDefault();
        }
    }

    /// <summary>按 RO 规则先读单技能 CD，其结束后再读服务器同步的组 CD。</summary>
    /// <param name="skillId">旧技能主表 ID。</param>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的秒数。</param>
    /// <returns>非负剩余秒数。</returns>
    public float GetRemainingCooldown(int skillId, float realtimeSeconds)
    {
        // 当前等级只提供公共组归属，单技能冷却本体由 RO 字典唯一持有。
        MSkillCore core = GetSkillCore(skillId);
        if (core == null) return 0f;
        // 单技能冷却优先显示，归零后才显示公共组冷却。
        float remainingSeconds = GetSkillCD(skillId, realtimeSeconds);
        if (remainingSeconds > 0f || core.CooldownGroupId <= 0) return remainingSeconds;
        // 本组原记录引用，查询在帧间也按相同不缩放时钟扣减。
        SkillCDGroupInfo info;
        if (!SkillCDGroupDic.TryGetValue(core.CooldownGroupId, out info)) return 0f;
        return Mathf.Max(0f, info.CurrentCD - Mathf.Max(0f, realtimeSeconds - info.UpdatedRealtime));
    }

    /// <summary>只做本地体验校验，服务器仍独立验证等级、状态、MP 和冷却。</summary>
    /// <param name="skillId">准备释放的旧技能主键。</param>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的秒数。</param>
    /// <param name="currentMp">客户端最近一次已知 MP。</param>
    /// <returns>当前状态与本地资源允许发请求时返回 true。</returns>
    public bool CanStart(int skillId, float realtimeSeconds, int currentMp)
    {
        // 本次技能 ID 对应的已学等级核心。
        MSkillCore core = GetSkillCore(skillId);
        if (core == null || Entity == null || Entity.Machine == null ||
            !CanSkill(skillId) ||
            !core.CanStart(realtimeSeconds, currentMp))
        {
            return false;
        }
        if (GetRemainingCooldown(skillId, realtimeSeconds) > 0f)
        {
            return false;
        }
        return true;
    }

    /// <summary>沿用 RO 新技能 InteruptSkill 决定互断；非技能状态继续使用既有状态矩阵。</summary>
    /// <param name="skillId">当前准备释放的新技能主表 ID。</param>
    /// <returns>状态允许施法，或新技能允许打断当前活动技能时为 true。</returns>
    public bool CanSkill(int skillId)
    {
        if (Entity == null || Entity.Machine == null)
        {
            return false;
        }
        if (Entity.Machine.CurrentState != this)
        {
            return Entity.Machine.CanTransfer(StateDefine.kStateSkill);
        }
        if (_currentSkill == null || !_currentSkill.IsActive || _currentSkill.IsSkillStateFinished)
        {
            return true;
        }
        return SkillCastingRulesCatalog.CanInterrupt(skillId, _currentSkill.SkillId);
    }

    /// <summary>技能事件转移使用同一 RO 互断规则，避免输入校验通过但状态矩阵拒绝自转移。</summary>
    /// <param name="eventArg">强类型技能开始载荷，非技能载荷不能切入。</param>
    /// <param name="last">之前的状态枚举，技能权限读取实体真实当前状态。</param>
    /// <returns>新技能允许切入时为 true。</returns>
    protected override bool OnGetEvent(IEventArg eventArg, StateDefine last)
    {
        // 当前事件的临时施法信息，不能在回调结束后继续持有。
        SkillStartedEventArgs skillEvent = eventArg as SkillStartedEventArgs;
        return skillEvent != null && CanSkill(skillEvent.SkillId);
    }

    /// <summary>建立本地预测冷却；有真实动画配置时同步触发技能动作状态。</summary>
    /// <param name="skillId">要释放的已学技能 ID。</param>
    /// <param name="skillUuid">客户端生成并由服务器原样回传的非零施法 UUID。</param>
    /// <param name="realtimeSeconds">本次输入发生的本机秒数。</param>
    /// <param name="currentMp">客户端最近一次已知 MP。</param>
    /// <returns>成功登记预测时返回 true；缺少表现资源仍可请求服务器。</returns>
    public bool BeginPrediction(int skillId, ulong skillUuid,
        float realtimeSeconds, int currentMp)
    {
        if (skillUuid == 0 || !CanStart(skillId, realtimeSeconds, currentMp))
        {
            return false;
        }
        // 本次预测的技能核心，CanStart 成功后必定存在。
        MSkillCore core = GetSkillCore(skillId);
        if (!core.BeginPrediction(skillUuid, realtimeSeconds, currentMp))
        {
            return false;
        }
        _latestSkillUuid = skillUuid;
        if (!core.HasPresentation)
        {
            Debug.LogWarning("技能表现资源缺失，仍发送权威施法请求。技能=" + skillId);
            return true;
        }
        // 事件分发同步完成，分发器随后立即回收该强类型载荷。
        SkillStartedEventArgs eventArgs = SkillStartedEventArgs.Create(
            core.SkillId, core.EffectId, core.Level, skillUuid,
            0, Entity.Position, true);
        Entity.DispatchEvent(MEventType.SkillStarted, eventArgs);
        if (Entity.Machine.CurrentState == this && _currentSkill != null &&
            _currentSkill.IsActive &&
            _currentSkill.SkillUuid == skillUuid)
        {
            return true;
        }
        // 请求已经发送，动画未能切入仍保留最近 UUID 接纳结果，不增加等待门禁。
        return true;
    }

    /// <summary>按施法 UUID 应用权威 CD；拒绝时回收对应的预测动画。</summary>
    /// <param name="skillId">结果回传的旧技能主键。</param>
    /// <param name="skillUuid">服务器原样回传的单次施法 UUID。</param>
    /// <param name="accepted">服务器是否已提交施法。</param>
    /// <param name="remainingCooldown">权威单技能剩余冷却秒数。</param>
    /// <param name="remainingGroupCooldown">权威公共组剩余冷却秒数。</param>
    /// <param name="realtimeSeconds">结果应用时的本机不缩放时间，单位为秒。</param>
    /// <param name="usingTimes">服务器按成功释放计数选择的动作段号，非负整数。</param>
    /// <returns>匹配且属于最新本地请求时返回 true，旧段仅确认实例而不更新界面 MP/CD。</returns>
    public bool ApplyCastResult(int skillId, ulong skillUuid, bool accepted,
        float remainingCooldown,
        float remainingGroupCooldown, float realtimeSeconds, int usingTimes = 0)
    {
        // 结果对应的已学技能核心，切图后可能已清空。
        MSkillCore core = GetSkillCore(skillId);
        if (core == null || skillUuid == 0) return false;
        // 当前或脱手原实例按 UUID 匹配，旧结果只处理自己的表现，不改最新 CD/MP。
        MSkill skill = FindSkill(skillUuid);
        // 核心只对最近一次施法首次接收结果进行校正，无外置待确认列表或按帧过期事务。
        bool appliedCooldown = core.ApplyCastResult(skillUuid, remainingCooldown, realtimeSeconds);
        if (!appliedCooldown && core.IsLatestCast(skillUuid)) return false;
        if (!appliedCooldown && skill == null) return false;
        // 跨技能的旧结果同样不能覆盖实体最新 MP 和公共组冷却。
        bool isLatestResult = appliedCooldown && skillUuid == _latestSkillUuid;
        if (accepted && isLatestResult && core.IsLatestCast(skillUuid))
        {
            // 确认只校正核心供下一段使用的计数，当前实例已捕获其动画与段号。
            core.SelectPresentation(usingTimes, realtimeSeconds);
        }
        if (isLatestResult && core.CooldownGroupId > 0)
        {
            SetGroupCdByType(core.CooldownGroupId, remainingGroupCooldown, realtimeSeconds);
        }
        if (accepted)
        {
            if (skill != null)
            {
                skill.Confirm(skillUuid);
            }
        }
        else if (skill != null)
        {
            skill.Cease(true);
        }
        return isLatestResult;
    }

    /// <summary>收到开始广播后按同一 UUID 确认本地预表现，远端才启动新表现。</summary>
    /// <param name="core">按服务器效果 ID 与施法者职业解析的技能核心。</param>
    /// <param name="skillUuid">请求与广播共同携带的非零单次施法 UUID。</param>
    /// <returns>广播已匹配或启动新表现时返回 true。</returns>
    public bool ApplyServerStart(MSkillCore core, ulong skillUuid)
    {
        if (core == null || skillUuid == 0 || Entity == null ||
            Entity.Machine == null)
        {
            if (core != null)
            {
                core.Release();
            }
            return false;
        }
        // RO 按同一 UUID 查找本地预表现；广播先于结果到达时也只确认，不重播。
        MSkill boundSkill = FindSkill(skillUuid);
        if (boundSkill != null)
        {
            if (skillUuid == _latestSkillUuid)
            {
                // 旧段的开始广播不得覆盖更新一段的本地使用次数。
                MSkillCore boundCore = GetSkillCore(boundSkill.SkillId);
                if (boundCore != null && boundCore.IsLatestCast(skillUuid))
                {
                    boundCore.SelectPresentation(core.UsingTimes, Time.realtimeSinceStartup);
                }
            }
            boundSkill.Confirm(skillUuid);
            core.Release();
            return true;
        }
        // 本地主角的请求已直接启动表现，晚广播找不到原实例时不能把旧段重新作为远端技能播放。
        PlayerEntity localPlayer = Entity as PlayerEntity;
        if (localPlayer != null && localPlayer.IsLocal)
        {
            core.Release();
            return false;
        }
        if (!core.HasPresentation)
        {
            Debug.LogWarning("服务器技能开始通知没有对应真实动画。技能=" + core.SkillId);
            core.Release();
            return false;
        }
        if (!CanSkill(core.SkillId))
        {
            core.Release();
            return false;
        }
        // 工厂新核心的初始持有权交给字典；重复等级会由 SetSkillCore 直接归还。
        // 输入核心可能在同等级去重后立即归还，保存其旧技能主表 ID 用于查找实际安装核心。
        int installedSkillId = core.SkillId;
        // 服务器选择的零起始动作段号，输入核心归还后仍用于选择实际核心的本段表现。
        int usingTimes = core.UsingTimes;
        SetSkillCore(core);
        // 同等级核心可能保留本地运行状态，使用服务器广播已选择的段号而非默认第零段。
        MSkillCore installedCore = GetSkillCore(installedSkillId);
        if (installedCore == null)
        {
            return false;
        }
        installedCore.SelectPresentation(usingTimes, Time.realtimeSinceStartup);
        core = installedCore;
        // 远端表现使用服务器原样广播的施法 UUID。
        SkillStartedEventArgs eventArgs = SkillStartedEventArgs.Create(
            core.SkillId, core.EffectId, core.Level, skillUuid,
            0, Entity.Position, false);
        Entity.DispatchEvent(MEventType.SkillStarted, eventArgs);
        if (_currentSkill != null && _currentSkill.IsActive &&
            _currentSkill.SkillUuid == skillUuid)
        {
            return true;
        }
        return false;
    }

    /// <summary>按 UUID 处理自然结束或打断；自然结束退出动作但保留尚未完成的制作时间轴。</summary>
    /// <param name="skillUuid">开始广播的非零单次施法 UUID。</param>
    /// <param name="interrupted">真表示打断或兜底清理，立即回池；假表示服务器正常结束，允许表现事件和声音收尾。</param>
    /// <returns>找到当前或旧实例并接受该结束通知时返回 true。</returns>
    public bool ApplyServerEnd(ulong skillUuid, bool interrupted = true)
    {
        if (!interrupted)
        {
            // 自然结束按同一 UUID 查找；找不到的过期广播不能结束当前新技能。
            MSkill endedSkill = FindSkill(skillUuid);
            if (endedSkill == null || !endedSkill.IsActive)
            {
                return false;
            }
            endedSkill.MarkServerEnded();
            if (ReferenceEquals(endedSkill, _currentSkill))
            {
                // 动作槽和根位移立即释放；未来票据仍归此旧实例持有，继续由常驻更新推进。
                OnSkillAutoEnd();
                if (Entity != null && Entity.Machine != null && Entity.Machine.CurrentState == this)
                {
                    Entity.Machine.ForceToDefault();
                }
            }
            if (endedSkill.IsPresentationFinished)
            {
                endedSkill.Cease();
            }
            return true;
        }
        if (skillUuid == 0 || _currentSkill == null || !_currentSkill.IsActive ||
            _currentSkill.SkillUuid != skillUuid)
        {
            // 从尾到头查找该 UUID 对应的旧实例，删除匹配实例不移动前面的索引。
            for (int index = _skills.Count - 1; index >= 0; index--)
            {
                if (_skills[index].SkillUuid == skillUuid && skillUuid != 0)
                {
                    _skills[index].Cease(true);
                    return true;
                }
            }
            return false;
        }
        _currentSkill.Cease(true);
        return true;
    }

    /// <summary>按非零施法 UUID 查找当前段或已退出动作槽的旧段。</summary>
    /// <param name="skillUuid">客户端请求与服务器通知共同携带的 UUID。</param>
    /// <returns>匹配的独立表现实例，已回收或未知 UUID 返回空。</returns>
    private MSkill FindSkill(ulong skillUuid)
    {
        if (skillUuid == 0)
        {
            return null;
        }
        if (_currentSkill != null && _currentSkill.IsActive &&
            _currentSkill.SkillUuid == skillUuid)
        {
            return _currentSkill;
        }
        // 按索引查找 UUID 对应的独立旧施法实例。
        for (int index = 0; index < _skills.Count; index++)
        {
            if (_skills[index].SkillUuid == skillUuid)
            {
                return _skills[index];
            }
        }
        return null;
    }

    /// <summary>实体常驻毫秒入口，只桥接一次 RO 秒更新，施法不依赖服务器确认。</summary>
    /// <param name="deltaMilliseconds">实体本帧更新间隔，单位为毫秒；仅转换技能帧时钟，冷却仍沿不缩放时钟。</param>
    public override void Update(int deltaMilliseconds)
    {
        Update(deltaMilliseconds * 0.001f);
    }

    /// <summary>RO 同名常驻更新：推进技能列表并维护冷却，当前动作由状态机更新。</summary>
    /// <param name="fDeltaT">本帧技能逻辑秒数；冷却沿既有不缩放时钟推进，不受动画时间倍率影响。</param>
    public override void Update(float fDeltaT)
    {
        base.Update(fDeltaT);
        // 冷却与权威结果使用本机不缩放秒数，不按实体动画倍率改变倒计时。
        float realtimeSeconds = Time.realtimeSinceStartup;
        // i 从尾到头推进存活技能，Cease 删除自身不影响前项索引。
        for (int i = _skills.Count - 1; i >= 0; i--)
        {
            // 当前存活技能原实例。
            MSkill skill = _skills[i];
            if (!skill.Update(fDeltaT))
            {
                skill.Cease(true);
            }
        }
        if (Entity != null && Entity.IsPlayer)
        {
            // 公共组独立倒序归零移除，单技能相同数字键不会受影响。
            for (int index = SkillCDGroupList.Count - 1; index >= 0; index--)
            {
                // 原组记录引用，帧间查询不会改其值；本帧从上次更新时间一次扣减。
                SkillCDGroupInfo info = SkillCDGroupList[index];
                info.CurrentCD -= Mathf.Max(0f, realtimeSeconds - info.UpdatedRealtime);
                info.UpdatedRealtime = realtimeSeconds;
                if (info.CurrentCD <= 0f)
                {
                    SkillCDGroupList.RemoveAt(index);
                    SkillCDGroupDic.Remove(info.SkillGroupType);
                    MSkillFactory.singleton.ReturnSkillCdGroupInfo(info);
                }
            }
            // 单技能列表与已装配核心共享唯一冷却源，到期后核心查询也立即返回零。
            for (int index = SkillCDList.Count - 1; index >= 0; index--)
            {
                // 原单技能记录引用，退出字典和列表后才允许池复用。
                SkillCDGroupInfo info = SkillCDList[index];
                info.CurrentCD -= Mathf.Max(0f, realtimeSeconds - info.UpdatedRealtime);
                info.UpdatedRealtime = realtimeSeconds;
                if (info.CurrentCD <= 0f)
                {
                    SkillCDList.RemoveAt(index);
                    SkillCDDic.Remove(info.SkillGroupType);
                    MSkillFactory.singleton.ReturnSkillCdGroupInfo(info);
                }
            }
        }
    }

    /// <summary>注册技能开始和完成事件；开始事件先按 UUID 去重，再交给动作状态机。</summary>
    protected override void RegisterActionEvents()
    {
        RegisterEvent(MEventType.SkillStarted, new MEventHandler(OnSkillStartedEvent));
        RegisterFinishEvent(MEventType.SkillFinished);
    }

    /// <summary>在状态转移前拦截重复施法，避免自转移时把当前新技能移入旧实例列表。</summary>
    /// <param name="eventArg">本次技能开始事件的临时载荷，由事件分发器在回调结束后回收。</param>
    private void OnSkillStartedEvent(IEventArg eventArg)
    {
        // 当前事件的强类型施法参数；其他载荷仍交由原动作入口处理。
        SkillStartedEventArgs skillEvent = eventArg as SkillStartedEventArgs;
        if (skillEvent != null && FindSkill(skillEvent.SkillUuid) != null)
        {
            return;
        }
        if (skillEvent == null || !CanSkill(skillEvent.SkillId))
        {
            // 无效或不许可的新技能不能经过基类拒绝回默认路径，避免误停当前施法。
            return;
        }
        _onActionEvent.Invoke(eventArg);
    }

    /// <summary>同步事件切入技能状态后读取载荷并创建本次动画表现。</summary>
    protected override void OnStart()
    {
        // 本次开始事件参数由分发器在当前调用栈结束后回收，不能保存其实例。
        SkillStartedEventArgs eventArgs = _evt as SkillStartedEventArgs;
        if (eventArgs == null)
        {
            Finish();
            return;
        }
        // RO 按当前和旧施法列表去重；同一 UUID 不重复安装动画。
        MSkill existingSkill = FindSkill(eventArgs.SkillUuid);
        if (existingSkill != null)
        {
            if (existingSkill == _currentSkill)
            {
                return;
            }
            Finish();
            return;
        }
        // 新施法接管动作槽时，旧实例继续完成自己的表现，权威通知只按原 UUID 匹配。
        OnSkillAutoEnd();
        if (_currentSkill == null)
        {
            _currentSkill = MSkillFactory.CreateSkill();
        }
        // 技能核心须与事件的技能、等级和效果三重身份一致。
        MSkillCore core = GetSkillCore(eventArgs.SkillId);
        if (core == null || core.EffectId != eventArgs.EffectId ||
            core.Level != eventArgs.Level ||
            !_currentSkill.Fire(core, Entity, eventArgs.SkillUuid,
                eventArgs.TargetUid, eventArgs.CastPoint, eventArgs.IsPrediction))
        {
            MSkillFactory.ReturnSkill(_currentSkill);
            _currentSkill = null;
            Finish();
            return;
        }
        if (Entity.MoveComp != null && !IsActiveMovable)
        {
            // 禁止施法移动的技能开始时清除旧方向和水平速度；MoveCasting 技能保留有效输入。
            Entity.MoveComp.StopHorizontalMovement();
        }
    }

    /// <summary>按 RO 由状态机统一动画入口触发当前技能，不进入通用动作的缺剪辑回退。</summary>
    /// <param name="preState">状态机记录的前一个动画状态名，技能表现由当前施法自行选择。</param>
    /// <returns>当前技能动作的 Animator 状态名。</returns>
    public override string PlayAnim(string preState)
    {
        if (_currentSkill != null)
        {
            _currentSkill.TriggerAnim();
        }
        return StateName;
    }

    /// <summary>RO 当前动作更新：推进主技能，动作窗结束后释放动作槽。</summary>
    /// <param name="deltaTime">距上一帧的非负秒数。</param>
    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        if (_currentSkill == null)
        {
            Stop();
            return;
        }
        if (!_currentSkill.Update(deltaTime))
        {
            _currentSkill.Cease(true);
            Stop();
            return;
        }
        if (_currentSkill.IsEnd())
        {
            Finish();
        }
    }

    /// <summary>技能动作退出后保留未完成的制作表现，按自身完成状态或 RO 实例存活上限回收。</summary>
    /// <param name="selfTrans">是否由技能自身切回技能状态触发。</param>
    protected override void OnStop(bool selfTrans = false)
    {
    }

    /// <summary>动作结束后把仍存活的主技能移交列表，继续自己的时间轴。</summary>
    private void OnSkillAutoEnd()
    {
        if (_currentSkill == null || !_currentSkill.IsActive)
        {
            return;
        }
        // 动画租约在离开动作槽时释放，施法实例继续完成自己的未来事件和声尾。
        _currentSkill.ReleaseAnimation();
        _skills.Add(_currentSkill);
        _currentSkill = null;
    }

    /// <summary>动作退出时移交仍存活的技能，自然收尾不等待服务器回包。</summary>
    /// <param name="selfTrans">是否由技能自身切回技能状态触发。</param>
    protected override void AfterFinish(bool selfTrans = false)
    {
        OnSkillAutoEnd();
    }

    /// <summary>对象池回收时清空技能集合并调用基类清理。</summary>
    public override void Shutdown()
    {
        ClearSkillCores();
        base.Shutdown();
    }
    /// <summary>本技能状态没有额外的实体事件订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>本技能状态没有额外的实体事件注销。</summary>
    protected override void EventUnsubscribe()
    {
        
    }
}
