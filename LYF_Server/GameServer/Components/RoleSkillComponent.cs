using System.Collections.Generic;
using cfg;

/// <summary>
/// 对应 RO 角色技能扩展；中心服负责学习等级，释放、动作和冷却统一继承 RO 技能组件。
/// 本类不再自行监听 kActionSkill 事件：技能动作态由基类作为状态机状态统一接管，
/// 正式链路是 singing 派发 kActionSkill → 状态机切到 kStateSkill → 基类 OnStart 一次性提交。
/// </summary>
public sealed class RoleSkillComponent : ROSkillComponent
{

    #region 构造与初始化
    /// <summary>为现有在线角色容器创建唯一技能释放组件，不另建场景宿主。</summary>
    /// <param name="owner">持有权威角色快照及网关会话的在线角色。</param>
    public RoleSkillComponent(OnlineRole owner) : base(owner) { }

    /// <summary>由基类解除状态机驱动事件监听并结束活动技能，最后释放角色学习快照。</summary>
    public override void Dispose()
    {
        base.Dispose();
        skill_map_.Clear();
        skills_loaded_ = false;
    }
    #endregion

    /// <summary>中心服装载的稳定技能 ID 到学习等级；零级表示尚未学习。</summary>
    private Dictionary<int, int> skill_map_ = new Dictionary<int, int>();

    /// <summary>当前在线生命周期是否已成功接收完整中心服学习快照。</summary>
    private bool skills_loaded_;

    /// <summary>当前未确认的已升级技能编号，每个编号保存最新学习等级。</summary>
    private readonly HashSet<int> modifiedSkills = new HashSet<int>();

    /// <summary>最近一次包含全部变更的角色保存版本，新升级发生后重置为零。</summary>
    private long skillSnapshotVersion;

    /// <summary>完整中心服学习快照已装载后允许释放校验。</summary>
    public override bool SkillsLoaded { get { return skills_loaded_; } }

    /// <summary>读取当前正式学习等级的效果，用于普攻和其他技能触发的学习被动。</summary>
    /// <param name="skillId">提供被动的技能主表编号。</param>
    /// <returns>权威等级行，未学习时为空。</returns>
    internal SkillEffectInfo FindLearnedEffect(int skillId)
    {
        // 等级只从角色技能组件查询，零级代表尚未习得。
        int level = GetLearnedLevel(skillId);
        if (level <= 0) return null;
        return LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(skillId, level);
    }

    /// <summary>读下一级成长行，验证并在当前逻辑帧同时扣铜币、提升等级。</summary>
    /// <param name="request">真实客户端会话点击时显示的技能和学习等级。</param>
    /// <returns>供本角色刷新显示的权威升级结果。</returns>
    internal SkillUpgradeRet Upgrade(SkillUpgradeReq request)
    {
        // 仅本角色持有的学习等级和虚拟背包能作为升级依据。
        RoleBaseInfo role = Owner.mainRoleInfo.BaseInfo;
        // 升级不创建虚拟货币的另一套余额。
        RoItemComponent items = Owner.GetComponent<RoItemComponent>();
        // 持久化保存的稳定技能编号。
        int skillId = request.SkillId;
        // 铜币在现有虚拟背包中的物品编号。
        int copperId = (int)ItemEnum.ZENY;
        // 失败同样回现有等级与铜币，便于面板用真实数值刷新。
        SkillUpgradeRet result = new SkillUpgradeRet
        {
            RoleId = role.RoleId,
            SkillId = skillId,
            Level = GetLearnedLevel(skillId),
            Copper = items.GetVirtualItemCount(copperId),
            CmdCode = CmdCode.SkillNotLearned
        };

        // 只有当前等级已经配置真实成长数值的技能走新流程。
        SkillEffectInfo current = FindLearnedEffect(skillId);
        if (current == null || current.RequireRoleLevel <= 0) return result;
        if (result.Level != request.CurrentLevel)
        {
            result.CmdCode = CmdCode.SkillUpgradeLevelChanged;
            return result;
        }
        // 单次最多提升一个等级，价格与角色门槛都取目标行。
        SkillEffectInfo next = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(skillId, result.Level + 1);
        if (next == null || next.RequireRoleLevel <= 0)
        {
            result.CmdCode = CmdCode.SkillUpgradeMaxLevel;
            return result;
        }
        if (role.Level < next.RequireRoleLevel)
        {
            result.CmdCode = CmdCode.SkillUpgradeRoleLevel;
            return result;
        }
        if (next.UpgradeCopper > 0 && !items.TakeVirtualItem(copperId, next.UpgradeCopper))
        {
            result.CmdCode = CmdCode.NotEnoughCurrency;
            return result;
        }

        // 扣除成功后才能变更学习等级，现有存档打包时两个组件共用一个版本。
        skill_map_[skillId] = next.Level;
        modifiedSkills.Add(skillId);
        skillSnapshotVersion = 0;
        result.Level = next.Level;
        result.Copper = items.GetVirtualItemCount(copperId);
        result.CmdCode = CmdCode.Succeed;
        return result;
    }

    /// <summary>标记当前尚有未确认的技能成长需要持久化。</summary>
    /// <returns>已经有技能升级等待存档确认时返回真。</returns>
    public override bool NeedSave() { return modifiedSkills.Count > 0; }

    /// <summary>把本批全部变化的技能等级写进背包同一次角色快照。</summary>
    /// <param name="fieldManager">当前用于汇集全部角色组件数据的存档组包器。</param>
    /// <param name="saveVersion">当前角色存档版本，供中心服确认与重放核对。</param>
    /// <returns>本次实际追加了技能等级时返回真。</returns>
    public override bool Save(RoleDataFieldManager fieldManager, long saveVersion)
    {
        if (!NeedSave()) return false;
        // 用可索引快照遍历已升级技能，本次只写当前最终等级。
        List<int> skills = new List<int>(modifiedSkills);
        for (int index = 0; index < skills.Count; index++)
        {
            // 编号在升级时已经与同一个角色技能字典关联。
            int skillId = skills[index];
            fieldManager.SaveSkillLevel(skillId, skill_map_[skillId]);
        }
        skillSnapshotVersion = saveVersion;
        return true;
    }

    /// <summary>中心服确认同一事务后清掉此次已保存的技能等级标记。</summary>
    /// <param name="saveVersion">已落库的角色快照版本。</param>
    public override void OnSaveAcknowledged(long saveVersion)
    {
        if (saveVersion != skillSnapshotVersion) return;
        modifiedSkills.Clear();
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
        // 临时索引完整验证后才替换，保持原中心服学习装载语义。
        Dictionary<int, int> levels = new Dictionary<int, int>();
        // 当前技能行下标，范围为零到中心服列表数量减一。
        for (int index = 0; index < response.RoleSkillInfoList.Count; index++)
        {
            // 一行对应角色对一个稳定技能 ID 的当前学习等级。
            RoleSkillInfo skill = response.RoleSkillInfoList[index];
            // 技能行缺失、技能 ID 不合法或等级为负时视为装载失败。
            if (skill == null || skill.SkillId <= 0 || skill.Level < 0)
            {
                return false;
            }
            // 同一技能 ID 在本次列表中重复出现时视为装载失败。
            if (levels.ContainsKey(skill.SkillId))
            {
                return false;
            }
            levels.Add(skill.SkillId, skill.Level);
        }
        skill_map_ = levels;
        skills_loaded_ = true;
        return true;
    }

    /// <summary>查询中心服已确认的等级，客户端请求不提供等级或效果 ID。</summary>
    /// <param name="skill_id">释放组件查询的稳定技能主表 ID。</param>
    /// <returns>已学正整数等级，未记录或零级为零。</returns>
    protected override int GetLearnedLevel(int skill_id)
    {
        // 当前技能对应的权威学习等级，字典缺项时保持未学习。
        int level;
        if (skill_map_.TryGetValue(skill_id, out level))
        {
            return level;
        }
        return 0;
    }
}
