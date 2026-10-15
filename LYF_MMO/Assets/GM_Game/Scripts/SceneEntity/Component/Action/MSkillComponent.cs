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
    private MSkill _skill;

    /// <summary>技能动作被外部停止的通知位；BeenStop读取一次后复位，归池时清除。</summary>
    private bool stop;

    /// <summary>已脱离主动作槽或独立运行的存活技能，保留各自的时间轴与 UUID。</summary>
    private readonly List<MSkill> _skills = new List<MSkill>();

    /// <summary>本实体最后一次本地施法 UUID，用于防止旧段回包覆盖更新的 MP。</summary>
    private ulong _latestSkillUuid;

    /// <summary>组件类型为技能快捷组件，不进入普通组件集合。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Skill; } }

    /// <summary>按当前技能 RO MoveCasting 控制普通水平移动；主动起跳仍禁止，已有重力和落地继续。</summary>
    public override bool IsActiveMovable
    {
        get
        {
            return _skill != null && _skill.IsActive &&
                SkillCastingRulesCatalog.IsMoveCasting(_skill.SkillId);
        }
    }

    /// <summary>技能状态对应的动画机状态名。</summary>
    public override string StateName { get { return MAnimator.STATE_SKILL; } }

    /// <summary>当前动作槽中的施法实例，空值表示没有动作；旧施法保存在列表。</summary>
    public MSkill CurrentSkill { get { return _skill; } }

    /// <summary>RO同名停止通知；一次读取消费本次外部停止，不影响技能CD或表现实例。</summary>
    public bool BeenStop
    {
        get
        {
            // 保存待消费的停止结果，下一次读取恢复为未停止。
            bool t = stop;
            stop = false;
            return t;
        }
    }

    /// <summary>当前主动作尚未收到服务器确认时为预表现；退出主槽的旧技能不占该状态。</summary>
    public override bool IsPreperformance
    {
        get { return _skill != null && _skill.IsPredicted; }
    }

    /// <summary>构造函数，初始化技能状态定义。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    public MSkillComponent(ROObject owner) : base(owner) { _selfState = StateDefine.kStateSkill; }

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

    /// <summary>RO同名UUID去重查询，当前主技能和独立存活列表共用一次施法身份。</summary>
    /// <param name="skillUuid">请求和服务器广播共用的非零施法UUID。</param>
    /// <returns>该施法仍有存活表现实例时返回真。</returns>
    public bool IsAlreadyCasting(ulong skillUuid)
    {
        return GetSkill(skillUuid) != null;
    }

    /// <summary>RO同名查询主动作是否施法中；旧列表的尾迹不继续占主动作槽。</summary>
    /// <returns>当前主动作持有活动技能时返回真。</returns>
    public bool IsCasting()
    {
        return _skill != null && _skill.IsActive;
    }

    /// <summary>RO同名获取最近主技能；没有主技能时取独立列表的第一项。</summary>
    /// <returns>仍存活的主技能或列表首项，没有施法时为空。</returns>
    public MSkill GetLastSkill()
    {
        if (_skill != null)
        {
            return _skill;
        }
        if (_skills.Count > 0)
        {
            return _skills[0];
        }
        return null;
    }

    /// <summary>RO同名停止全部表现；普通停止保留单技能和组CD，明确清场才清CD。</summary>
    /// <param name="clearCD">真表示同时归还所有冷却记录，假表示仅结束表现实例。</param>
    public void StopAllSkill(bool clearCD = false)
    {
        EndSkill(true);
        // 从尾到头结束原实例，Cease会把自己从同一列表移除。
        for (int i = _skills.Count - 1; i >= 0; i--)
        {
            _skills[i].Cease(true);
        }
        if (clearCD)
        {
            ClearGroupCd();
        }
    }

    /// <summary>RO同名结束当前主技能；自然动作结束仍由OnSkillAutoEnd交给独立列表。</summary>
    /// <param name="cleanUp">是否立即清理本次未触发表现，停止及清场传真。</param>
    private void EndSkill(bool cleanUp = false)
    {
        if (_skill != null)
        {
            _skill.Cease(cleanUp);
        }
    }

    /// <summary>RO同名装配服务器已学列表，只持有每个技能当前等级；保留原CD及旧施法持有权。</summary>
    /// <param name="skills">11080已学技能项或控制器保存的同角色列表，零级项只用于显示。</param>
    public void OnAttachSkill(IList<RoleSkillInfo> skills)
    {
        // 本次有效已学主表ID集合，用来删除本轮回包已失效的技能。
        HashSet<int> skillIds = new HashSet<int>();
        // i为服务器列表的零起始下标，每个技能只装配当前学习等级。
        for (int i = 0; i < skills.Count; i++)
        {
            // 当前协议技能项，等级大于零才进入施法核心。
            RoleSkillInfo info = skills[i];
            if (info.Level <= 0)
            {
                continue;
            }
            if (CheckAndAttackSkillCore(info.SkillId, info.Level) != null)
            {
                skillIds.Add(info.SkillId);
            }
        }
        RetainSkillCores(skillIds);
    }

    /// <summary>RO同名按主表与当前学习等级装配核心，职业沿角色真实基础数据解析。</summary>
    /// <param name="skillId">服务器已学技能的主表ID。</param>
    /// <param name="lv">服务器同步的当前正数技能等级。</param>
    public void AttachSkillInfo(int skillId, int lv)
    {
        // 角色基础信息提供本技能当前职业，不从动作资源推测职业。
        RoleEntity role = Entity as RoleEntity;
        if (role == null || role.BaseInfo == null || lv <= 0)
        {
            return;
        }
        // 同等级重复同步直接沿用已装配核心，保留施法身份和CD。
        if (HasSkillCore(skillId, lv))
        {
            return;
        }
        // 工厂初始持有权交给SetSkillCore，旧实例独立持有原等级核心直到回池。
        MSkillCore core = MSkillFactory.CreateCore(skillId, lv, role.BaseInfo.JobId);
        if (core != null)
        {
            SetSkillCore(core);
        }
    }

    /// <summary>RO同名获取指定等级核心，未装配时先按当前角色职业装配再返回。</summary>
    /// <param name="skillId">需要装配的技能主表ID。</param>
    /// <param name="skillLv">调用方提供的当前正数学习等级。</param>
    /// <returns>该等级可由当前职业配置装配时返回核心，否则为空。</returns>
    public MSkillCore CheckAndAttackSkillCore(int skillId, int skillLv)
    {
        AttachSkillInfo(skillId, skillLv);
        return GetSkill(skillId, skillLv);
    }

    /// <summary>RO同名按效果主键查询是否已有核心，不将效果ID当主表ID。</summary>
    /// <param name="id">SkillEffectInfo.Id效果主键。</param>
    /// <returns>当前装配等级的效果主键相符时为真。</returns>
    public bool HasSkillCore(uint id)
    {
        return GetSkill(id) != null;
    }

    /// <summary>RO同名检查技能的当前装配等级；本项目只保留服务器当前学习等级。</summary>
    /// <param name="id">技能主表ID。</param>
    /// <param name="lv">正数技能等级。</param>
    /// <returns>当前已装配等级相符时为真。</returns>
    public bool HasSkillCore(int id, int lv)
    {
        return GetSkill(id, lv) != null;
    }

    /// <summary>RO同名按主表ID及等级获取已装配核心，不降低或替换服务器当前等级。</summary>
    /// <param name="skill_id">技能主表ID。</param>
    /// <param name="lv">当前学习等级，从1开始。</param>
    /// <returns>已装配且等级一致时返回核心，否则为空。</returns>
    public MSkillCore GetSkill(int skill_id, int lv)
    {
        // 原主表索引的当前等级核心；历史施法自行持有其旧等级核心。
        MSkillCore core = GetSkillCore(skill_id);
        if (core != null && core.Level == lv)
        {
            return core;
        }
        return null;
    }

    /// <summary>RO同名按效果主键获取已装配核心；未知效果不会被查询操作登记为已学技能。</summary>
    /// <param name="id">SkillEffectInfo.Id效果主键，零表示无效。</param>
    /// <returns>效果存在且与实体当前装配等级一致时返回核心，否则为空。</returns>
    public MSkillCore GetSkill(uint id)
    {
        // 正式效果配置字典，仅用于把效果ID映射到已有主表核心。
        Dictionary<int, cfg.SkillEffectInfo> effects = LubanMgr.Instance.GetSkillEffectInfos();
        // 查询效果行的主表归属，不为查询临时创建核心或改变学习状态。
        cfg.SkillEffectInfo effect;
        if (id == 0 || effects == null || !effects.TryGetValue((int)id, out effect))
        {
            return null;
        }
        // 已装配核心必须使用同一个效果主键，旧等级效果不会返回新等级核心。
        MSkillCore core = GetSkillCore(effect.SkillId);
        if (core != null && core.EffectId == (int)id)
        {
            return core;
        }
        return null;
    }

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
            if (_skill != null && _skill.SkillId == skillId)
            {
                _skill.Cease(true);
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
        StopAllSkill();
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
        if (!ReferenceEquals(_skill, skill)) return;
        _skill = null;
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

    /// <summary>本地起跳至落地期间禁止技能和普攻；服务器仍独立验证等级、状态、MP 和冷却。</summary>
    /// <param name="skillId">准备释放的旧技能主键。</param>
    /// <param name="realtimeSeconds">本机不受时间缩放影响的秒数。</param>
    /// <param name="currentMp">客户端最近一次已知 MP。</param>
    /// <returns>当前状态与本地资源允许发请求时返回 true。</returns>
    public bool CanStart(int skillId, float realtimeSeconds, int currentMp)
    {
        // 本次技能 ID 对应的已学等级核心。
        MSkillCore core = GetSkillCore(skillId);
        // 起跳同步置位、落地清除的移动状态，同时拦截发包前校验和直接预测入口。
        // 宿主缺失或正处于起跳状态时拒绝发请求；均为纯读取，短路语义与原表达式一致。
        bool hostBlocked = Entity == null || Entity.Machine == null || Entity.IsJump;
        if (core == null || hostBlocked)
        {
            return false;
        }
        // 核心与宿主有效后才依次校验技能可用性与核心自身启动条件。
        if (!CanSkill(skillId) || !core.CanStart(realtimeSeconds, currentMp))
        {
            return false;
        }
        if (GetRemainingCooldown(skillId, realtimeSeconds) > 0f)
        {
            return false;
        }
        return true;
    }

    /// <summary>滑步按当前技能CanSlideCancel决定取消，普通技能沿RO InteruptSkill互断；非技能状态使用原矩阵。</summary>
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
        if (_skill == null || !_skill.IsActive || _skill.IsSkillStateFinished)
        {
            return true;
        }
        // 新技能主表类别3表示滑步，保持普通技能请求和CD流程，仅区别取消许可。
        cfg.SkillInfo newSkill = LubanMgr.Instance.GetSkillInfoById(skillId);
        if (newSkill != null && newSkill.Type == 3)
        {
            // 正占动作槽的原技能配置；1允许被滑步取消，0需等待原动作结束。
            cfg.SkillInfo currentSkill = LubanMgr.Instance.GetSkillInfoById(_skill.SkillId);
            return currentSkill != null && currentSkill.CanSlideCancel == 1;
        }
        return SkillCastingRulesCatalog.CanInterrupt(skillId, _skill.SkillId);
    }

    /// <summary>技能事件转移使用同一 RO 互断规则，避免输入校验通过但状态矩阵拒绝自转移。</summary>
    /// <param name="eventArg">强类型技能开始载荷，非技能载荷不能切入。</param>
    /// <param name="last">之前的状态枚举，技能权限读取实体真实当前状态。</param>
    /// <returns>新技能允许切入时为 true。</returns>
    protected override bool OnGetEvent(IEventArg eventArg, StateDefine last)
    {
        // 当前事件的临时施法信息，不能在回调结束后继续持有。
        SkillStartedEventArgs skillEvent = eventArg as SkillStartedEventArgs;
        return OnAttack(skillEvent);
    }

    /// <summary>RO同名技能事件准入，先找到已装配的当前等级核心，再使用本项目原互断规则。</summary>
    /// <param name="skill_event">当前同步派发的强类型开始事件，结束后立即回收。</param>
    /// <returns>效果与学习等级匹配、且动作允许切入时返回真。</returns>
    private bool OnAttack(SkillStartedEventArgs skill_event)
    {
        if (skill_event == null)
        {
            return false;
        }
        // 事件记录的当前等级核心；服务端广播先装配后进入动作事件。
        MSkillCore core = GetSkill(skill_event.SkillId, skill_event.Level);
        return core != null && core.EffectId == skill_event.EffectId && CanSkill(core.SkillId);
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
        // 当前技能实例是否活动且 UUID 与本次请求一致。
        bool skillActive = _skill != null &&
            _skill.IsActive &&
            _skill.SkillUuid == skillUuid;
        if (Entity.Machine.CurrentState == this && skillActive)
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
        MSkill skill = GetSkill(skillUuid);
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

    /// <summary>RO同名核心同步入口，把本项目权威结果中的单技能和组CD统一换算为剩余秒数。</summary>
    /// <param name="data">11170施法结果，包含服务器生成时间、冷却截止及权威段号。</param>
    /// <param name="realtimeSeconds">接收时本机不缩放时间，单位秒，仅用于本地后续倒计时。</param>
    /// <returns>该结果属于最新本地请求且首次应用时为真，供控制器同步MP和提示。</returns>
    public bool OnSkillCoreSync(SkillCastRet data, float realtimeSeconds)
    {
        // 服务器给出的单技能剩余秒数，避免把设备墙上时钟用于权威截止比较。
        float cd = GetServerRemainingSeconds(data.CooldownEndUnixMs, data.ServerTimeUnixMs);
        // 公共组剩余秒数，保持独立字典及最新UUID校正规则。
        float groupCD = GetServerRemainingSeconds(data.GroupCooldownEndUnixMs, data.ServerTimeUnixMs);
        return ApplyCastResult(data.SkillId, data.SkillUuid, data.CmdCode == CmdCode.Succeed,
            cd, groupCD, realtimeSeconds, data.UsingTimes);
    }

    /// <summary>把同一服务器回包的截止与生成时间相减，得到非负剩余冷却秒数。</summary>
    /// <param name="endUnixMs">服务器冷却截止Unix毫秒。</param>
    /// <param name="serverUnixMs">服务器生成该消息的Unix毫秒。</param>
    /// <returns>截止晚于生成时间时的秒数，否则为零。</returns>
    private static float GetServerRemainingSeconds(long endUnixMs, long serverUnixMs)
    {
        if (endUnixMs <= serverUnixMs || serverUnixMs <= 0)
        {
            return 0f;
        }
        return (endUnixMs - serverUnixMs) * 0.001f;
    }

    /// <summary>RO同名服务器开始入口，按真实职业和效果装配，再确认预表现或启动远端动作。</summary>
    /// <param name="data">服务器开始广播，本项目协议提供技能、效果、UUID和段号。</param>
    /// <returns>广播确认了已有实例或成功创建远端表现时为真。</returns>
    public bool OnSkillStart(SkillCastStartNtf data)
    {
        // 施法者真实角色基础数据，职业决定表现表中的模型及默认武器动作。
        RoleEntity role = Entity as RoleEntity;
        if (role == null || role.BaseInfo == null)
        {
            return false;
        }
        // 按广播效果借出的核心，由ApplyServerStart消费初始持有权。
        MSkillCore core = MSkillFactory.CreateCoreByEffect(data.SkillId, data.EffectId,
            role.BaseInfo.JobId);
        if (core != null)
        {
            core.SelectPresentation(data.UsingTimes, Time.realtimeSinceStartup);
        }
        return ApplyServerStart(core, data.SkillUuid);
    }

    /// <summary>RO同名服务器结束入口，按UUID结束原实例；自然结束保留时间轴事件和声音尾段。</summary>
    /// <param name="data">服务器结束广播，Interrupted为真表示立即清理该次施法。</param>
    /// <returns>匹配当前主技能或独立列表的原实例时为真。</returns>
    public bool OnSkillEnd(SkillCastEndNtf data)
    {
        return ApplyServerEnd(data.SkillUuid, data.Interrupted);
    }

    /// <summary>收到开始广播后按同一 UUID 确认本地预表现，远端才启动新表现。</summary>
    /// <param name="core">按服务器效果 ID 与施法者职业解析的技能核心。</param>
    /// <param name="skillUuid">请求与广播共同携带的非零单次施法 UUID。</param>
    /// <returns>广播已匹配或启动新表现时返回 true。</returns>
    public bool ApplyServerStart(MSkillCore core, ulong skillUuid)
    {
        // 技能核心缺失或单次施法 UUID 无效。
        bool coreOrUuidInvalid = core == null || skillUuid == 0;
        // 宿主实体或状态机缺失；前一项为真时短路，不再触碰 Entity.Machine。
        bool hostInvalid = coreOrUuidInvalid || Entity == null || Entity.Machine == null;
        if (hostInvalid)
        {
            if (core != null)
            {
                core.Release();
            }
            return false;
        }
        // RO 按同一 UUID 查找本地预表现；广播先于结果到达时也只确认，不重播。
        MSkill boundSkill = GetSkill(skillUuid);
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
        if (_skill != null && _skill.IsActive &&
            _skill.SkillUuid == skillUuid)
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
            MSkill endedSkill = GetSkill(skillUuid);
            if (endedSkill == null || !endedSkill.IsActive)
            {
                return false;
            }
            endedSkill.MarkServerEnded();
            if (ReferenceEquals(endedSkill, _skill))
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
        // 当前技能实例是否缺失或已结束活动态。
        bool noActiveSkill = _skill == null || !_skill.IsActive;
        if (skillUuid == 0 || noActiveSkill ||
            _skill.SkillUuid != skillUuid)
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
        _skill.Cease(true);
        return true;
    }

    /// <summary>按非零施法 UUID 查找当前段或已退出动作槽的旧段。</summary>
    /// <param name="skillUuid">客户端请求与服务器通知共同携带的 UUID。</param>
    /// <returns>匹配的独立表现实例，已回收或未知 UUID 返回空。</returns>
    public MSkill GetSkill(ulong skillUuid)
    {
        if (skillUuid == 0)
        {
            return null;
        }
        if (_skill != null && _skill.IsActive &&
            _skill.SkillUuid == skillUuid)
        {
            return _skill;
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
        if (skillEvent != null && IsAlreadyCasting(skillEvent.SkillUuid))
        {
            return;
        }
        if (!OnAttack(skillEvent))
        {
            // 无效或不许可的新技能不能经过基类拒绝回默认路径，避免误停当前施法。
            return;
        }
        _onActionEvent.Invoke(eventArg);
    }

    /// <summary>同步事件切入技能状态后读取载荷并创建本次动画表现。</summary>
    protected override void OnStart()
    {
        base.OnStart();
        // 本次开始事件参数由分发器在当前调用栈结束后回收，不能保存其实例。
        SkillStartedEventArgs eventArgs = _evt as SkillStartedEventArgs;
        if (eventArgs == null)
        {
            Finish();
            return;
        }
        // RO开始入口只解析当前等级核心，技能载体借出及Fire由FireSkill承担。
        MSkillCore core = GetSkill(eventArgs.SkillId, eventArgs.Level);
        FireSkill(core, eventArgs);
    }

    /// <summary>RO同名主技能启动入口，按UUID去重、移交旧实例，再借出并启动本次技能载体。</summary>
    /// <param name="newOne">开始事件对应的当前已装配等级核心。</param>
    /// <param name="args">同步事件中的施法UUID、释放点、目标与预测标志，禁止跨帧保存。</param>
    private void FireSkill(MSkillCore newOne, SkillStartedEventArgs args)
    {
        // 当前或独立列表中已经运行的同一UUID实例。
        MSkill existingSkill = GetSkill(args.SkillUuid);
        if (existingSkill != null)
        {
            if (existingSkill == _skill)
            {
                return;
            }
            Finish();
            return;
        }
        // 新施法接管动作槽时，旧实例继续完成自己的表现，权威通知只按原 UUID 匹配。
        OnSkillAutoEnd();
        if (_skill == null)
        {
            _skill = MSkillFactory.CreateSkill();
        }
        // 技能核心须与事件的技能、等级和效果三重身份一致；任一不符即视为播放失败。
        bool identityMismatch = newOne == null || newOne.EffectId != args.EffectId ||
            newOne.Level != args.Level;
        // 身份通过后才播放技能；Fire 为带副作用调用，身份不符时按短路不执行。
        bool fireFailed = identityMismatch || !_skill.Fire(newOne, Entity, args.SkillUuid,
            args.TargetUid, args.CastPoint, args.IsPrediction);
        if (fireFailed)
        {
            MSkillFactory.ReturnSkill(_skill);
            _skill = null;
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
        if (_skill != null)
        {
            _skill.TriggerAnim();
        }
        return StateName;
    }

    /// <summary>RO 当前动作更新：推进主技能，动作窗结束后释放动作槽。</summary>
    /// <param name="deltaTime">距上一帧的非负秒数。</param>
    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        if (_skill == null)
        {
            Stop();
            return;
        }
        if (!_skill.Update(deltaTime))
        {
            _skill.Cease(true);
            Stop();
            return;
        }
        if (_skill.IsEnd())
        {
            Finish();
        }
    }

    /// <summary>RO同名正常动作结束钩子，恢复默认动作已有的后摇标记和战斗待机表现。</summary>
    protected override void OnFinished()
    {
        base.OnFinished();
        Entity.IsHouyao = true;
        Entity.BattleIdle = true;
    }

    /// <summary>RO同名外部停止钩子，只记录可消费通知；尾段仍由AfterFinish移交并沿原生命周期回收。</summary>
    /// <param name="selfTrans">是否由技能自身切回技能状态触发。</param>
    protected override void OnStop(bool selfTrans = false)
    {
        base.OnStop(selfTrans);
        stop = true;
    }

    /// <summary>动作结束后把仍存活的主技能移交列表，继续自己的时间轴。</summary>
    private void OnSkillAutoEnd()
    {
        if (_skill == null || !_skill.IsActive)
        {
            return;
        }
        // 动画租约在离开动作槽时释放，施法实例继续完成自己的未来事件和声尾。
        _skill.ReleaseAnimation();
        _skills.Add(_skill);
        _skill = null;
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
        stop = false;
        base.Shutdown();
    }

    /// <summary>RO同名宿主摘除入口，立即结束仍存活技能并解除全部核心与冷却持有。</summary>
    public override void OnDetachFromHost()
    {
        ClearSkillCores();
        stop = false;
        base.OnDetachFromHost();
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
