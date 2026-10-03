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

    /// <summary>仍等待服务器结果的技能 ID；常驻组件按索引检查预测到期。</summary>
    private readonly List<int> _pendingSkillIds = new List<int>();

    /// <summary>公共冷却组的本机截止时间，单位为不受时间缩放影响的秒数。</summary>
    private readonly Dictionary<int, float> _groupCooldownEnds =
        new Dictionary<int, float>();

    /// <summary>实体当前动作槽借出的单次表现；空值表示槽位未占用。</summary>
    private MSkill _currentSkill;

    /// <summary>已退出动作状态但仍在施放的实例，按施法 UUID 等待服务器确认和结束。</summary>
    private readonly List<MSkill> _skills = new List<MSkill>();

    /// <summary>本实体最后一次本地施法 UUID，用于防止旧段回包覆盖更新的 MP。</summary>
    private ulong _latestSkillUuid;

    /// <summary>构造函数，初始化技能状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MSkillComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateSkill; }

    /// <summary>组件类型为技能快捷组件，不进入普通组件集合。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Skill; } }

    /// <summary>技能动作期间禁止水平移动；跳跃的垂直积分与落地检测由移动组件继续执行。</summary>
    public override bool IsActiveMovable { get { return false; } }

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
            _pendingSkillIds.Remove(skillId);
            // index 从最后一项递减，回收被移除技能的旧施法后仍能检查前面的原实例。
            for (int index = _skills.Count - 1; index >= 0; index--)
            {
                if (_skills[index].SkillId == skillId)
                {
                    MSkillFactory.ReturnSkill(_skills[index]);
                    _skills.RemoveAt(index);
                }
            }
            if (_currentSkill != null && _currentSkill.SkillId == skillId)
            {
                MSkillFactory.ReturnSkill(_currentSkill);
                _currentSkill = null;
                if (Entity != null && Entity.Machine != null)
                {
                    Entity.Machine.ForceToDefault();
                }
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
        // previous 是同技能的原字典核心，携带已有 CD 与待确认 UUID，替换后解除其字典持有。
        MSkillCore previous;
        if (_cores.TryGetValue(core.SkillId, out previous) && previous != null)
        {
            if (previous.Level == core.Level && previous.EffectId == core.EffectId)
            {
                _learnedSkillIds.Add(core.SkillId);
                core.Release();
                return;
            }
            // 等级升级时携带旧 CD 与待确认施法，使后续权威回包仍可按 UUID 匹配。
            core.AdoptRuntimeState(previous);
        }
        _cores[core.SkillId] = core;
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
            MSkillFactory.ReturnSkill(_currentSkill);
            _currentSkill = null;
        }
        // index 为旧施法列表的零起始下标，清空列表前依次归还全部实例。
        for (int index = 0; index < _skills.Count; index++)
        {
            MSkillFactory.ReturnSkill(_skills[index]);
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
        _groupCooldownEnds.Clear();
        _pendingSkillIds.Clear();
    }

    /// <summary>按 RO 规则先读单技能 CD，其结束后再读服务器同步的组 CD。</summary>
    /// <param name="skillId">旧技能主表 ID。</param>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的秒数。</param>
    /// <returns>非负剩余秒数。</returns>
    public float GetRemainingCooldown(int skillId, float realtimeSeconds)
    {
        // 已学技能核心负责保存当前单技能截止时间。
        MSkillCore core = GetSkillCore(skillId);
        if (core == null)
        {
            return 0f;
        }
        // 单技能 CD 优先于公共组 CD 显示。
        float remainingSeconds = core.GetRemainingCooldown(realtimeSeconds);
        if (remainingSeconds > 0f || core.CooldownGroupId <= 0)
        {
            return remainingSeconds;
        }
        // 组 CD 只由服务器结果写入；没有权威截止时间时显示零。
        float groupEndRealtime;
        if (!_groupCooldownEnds.TryGetValue(core.CooldownGroupId,
            out groupEndRealtime))
        {
            return 0f;
        }
        return Mathf.Max(0f, groupEndRealtime - realtimeSeconds);
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
            !Entity.Machine.CanTransfer(StateDefine.kStateSkill) ||
            !core.CanStart(realtimeSeconds, currentMp))
        {
            return false;
        }
        if (core.CooldownGroupId > 0)
        {
            // 本组已知的 CD 截止时间，缺失时不阻断施法。
            float groupEnd;
            if (_groupCooldownEnds.TryGetValue(core.CooldownGroupId,
                out groupEnd) && realtimeSeconds < groupEnd)
            {
                return false;
            }
        }
        return true;
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
        if (!_pendingSkillIds.Contains(skillId))
        {
            _pendingSkillIds.Add(skillId);
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
        // 请求已经发送，动画状态未能切入也必须保留 UUID 以接纳 11170 权威结果。
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
        if (core == null || !core.ApplyCastResult(skillUuid,
            remainingCooldown, realtimeSeconds))
        {
            return false;
        }
        if (!core.HasPendingPrediction)
        {
            _pendingSkillIds.Remove(skillId);
        }
        // 不同技能间的旧段结果同样不能覆盖最后发起请求的 MP 和公共冷却快照。
        bool isLatestResult = skillUuid == _latestSkillUuid;
        if (accepted && isLatestResult && core.IsLatestCast(skillUuid))
        {
            // 确认只校正核心供下一段使用的计数，当前实例已捕获其动画与段号。
            core.SelectPresentation(usingTimes, realtimeSeconds);
        }
        if (isLatestResult && core.CooldownGroupId > 0)
        {
            _groupCooldownEnds[core.CooldownGroupId] =
                realtimeSeconds + Mathf.Max(0f, remainingGroupCooldown);
        }
        // 结果按施法 UUID 查找当前或已结束的动作，不按技能 ID 猜配连击段。
        MSkill skill = FindSkill(skillUuid);
        if (accepted)
        {
            if (skill != null)
            {
                skill.Confirm(skillUuid);
            }
        }
        else if (skill != null)
        {
            if (skill == _currentSkill)
            {
                _currentSkill = null;
                MSkillFactory.ReturnSkill(skill);
                if (Entity != null && Entity.Machine != null)
                {
                    Entity.Machine.ForceToDefault();
                }
            }
            else
            {
                _skills.Remove(skill);
                MSkillFactory.ReturnSkill(skill);
            }
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
        if (!core.HasPresentation)
        {
            Debug.LogWarning("服务器技能开始通知没有对应真实动画。技能=" + core.SkillId);
            core.Release();
            return false;
        }
        if (!Entity.Machine.CanTransfer(StateDefine.kStateSkill))
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

    /// <summary>按施法 UUID 结束动画表现，重复或过期结束通知无效。</summary>
    /// <param name="skillUuid">开始广播的非零单次施法 UUID。</param>
    /// <returns>当前表现与 UUID 匹配且完成清理时返回 true。</returns>
    public bool ApplyServerEnd(ulong skillUuid)
    {
        if (skillUuid == 0 || _currentSkill == null || !_currentSkill.IsActive ||
            _currentSkill.SkillUuid != skillUuid)
        {
            // 从尾到头查找该 UUID 对应的旧实例，删除匹配实例不移动前面的索引。
            for (int index = _skills.Count - 1; index >= 0; index--)
            {
                if (_skills[index].SkillUuid == skillUuid && skillUuid != 0)
                {
                    MSkillFactory.ReturnSkill(_skills[index]);
                    _skills.RemoveAt(index);
                    return true;
                }
            }
            return false;
        }
        MSkillFactory.ReturnSkill(_currentSkill);
        _currentSkill = null;
        if (Entity != null && Entity.Machine != null &&
            Entity.Machine.CurrentState == this)
        {
            Entity.Machine.ForceToDefault();
        }
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

    /// <summary>常驻组件逐帧回收未获服务器确认的预测，包含动画已结束的施法。</summary>
    /// <param name="deltaMilliseconds">实体本帧更新间隔，单位为毫秒；期限使用不缩放时钟。</param>
    public override void Update(int deltaMilliseconds)
    {
        // 技能动作状态只在当前状态更新；确认期限必须在动作退出后继续检查。
        float realtimeSeconds = Time.realtimeSinceStartup;
        // 从尾到头检查有未确认施法的技能 ID，删除失效核心时保持索引有效。
        for (int index = _pendingSkillIds.Count - 1; index >= 0; index--)
        {
            // 待确认技能对应的旧主表 ID，核心升级后仍从当前字典读取。
            int skillId = _pendingSkillIds[index];
            // 当前技能等级对应的核心，技能快照升级后可能已替换实例。
            MSkillCore core = GetSkillCore(skillId);
            if (core == null)
            {
                _pendingSkillIds.RemoveAt(index);
                continue;
            }
            // 已超过动画时长加宽限期的施法 UUID，零表示尚未到期。
            ulong expiredSkillUuid = core.ExpirePrediction(realtimeSeconds);
            if (expiredSkillUuid == 0)
            {
                continue;
            }
            if (!core.HasPendingPrediction)
            {
                _pendingSkillIds.RemoveAt(index);
            }
            // 超时的旧段已经归还动画槽，只回收该实例，不影响正在播放的新段。
            MSkill expiredSkill = FindSkill(expiredSkillUuid);
            if (expiredSkill != null && expiredSkill != _currentSkill)
            {
                _skills.Remove(expiredSkill);
                MSkillFactory.ReturnSkill(expiredSkill);
            }
            if (_currentSkill != null && _currentSkill.IsActive &&
                _currentSkill.IsPredicted &&
                _currentSkill.SkillUuid == expiredSkillUuid)
            {
                MSkillFactory.ReturnSkill(_currentSkill);
                _currentSkill = null;
                if (Entity != null && Entity.Machine != null &&
                    Entity.Machine.CurrentState == this)
                {
                    Entity.Machine.ForceToDefault();
                }
            }
        }
        if (_currentSkill != null &&
            _currentSkill.IsServerEndOverdue(realtimeSeconds))
        {
            // 服务器结束通知丢失时按 RO 的动画时长加十秒释放已确认实例。
            ApplyServerEnd(_currentSkill.SkillUuid);
        }
        // RO 的旧施法列表在动作状态退出后继续逐帧运行，各实例各自等待结束通知。
        for (int index = _skills.Count - 1; index >= 0; index--)
        {
            _skills[index].Update(deltaMilliseconds * 0.001f);
        }
        // 从尾到头清理丢失服务器结束通知的旧实例，保证删除后遍历仍有效。
        for (int index = _skills.Count - 1; index >= 0; index--)
        {
            if (_skills[index].IsServerEndOverdue(realtimeSeconds))
            {
                MSkillFactory.ReturnSkill(_skills[index]);
                _skills.RemoveAt(index);
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
        // 新施法接管动作槽时，旧实例继续按自己的 UUID 等待服务器结束通知。
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
        if (Entity.MoveComp != null)
        {
            // 施法开始即清除上一帧的方向和水平速度，避免在下一次移动更新前继续滑行。
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

    /// <summary>逐帧推进动画；剪辑结束后回默认动作，仍等服务器结束通知回收施法 UUID。</summary>
    /// <param name="deltaTime">距上一帧的非负秒数。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (_currentSkill != null)
        {
            _currentSkill.Update(deltaTime);
        }
        if (_currentSkill == null || !_currentSkill.IsActive ||
            _currentSkill.IsSkillStateFinished)
        {
            Finish();
        }
    }

    /// <summary>技能动作退出后仍保留施法实例，等待同一 UUID 的结束通知或超时回收。</summary>
    /// <param name="selfTrans">是否由技能自身切回技能状态触发。</param>
    protected override void OnStop(bool selfTrans = false)
    {
    }

    /// <summary>将当前施法移交旧实例列表，供迟到的结果与结束通知按 UUID 匹配。</summary>
    private void OnSkillAutoEnd()
    {
        if (_currentSkill == null || !_currentSkill.IsActive)
        {
            return;
        }
        // 动画租约在离开动作槽时释放，施法实例仍保留其服务器生命周期。
        _currentSkill.ReleaseAnimation();
        _skills.Add(_currentSkill);
        _currentSkill = null;
    }

    /// <summary>任意施法退出动作槽后保留实例，等待同一 UUID 的结束通知。</summary>
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
