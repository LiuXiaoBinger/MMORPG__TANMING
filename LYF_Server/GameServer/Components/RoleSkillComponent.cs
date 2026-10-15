using System.Collections.Generic;

/// <summary>对应 RO 角色技能扩展；中心服负责学习等级，释放、动作和冷却统一继承 RO 技能组件。</summary>
public sealed class RoleSkillComponent : ROSkillComponent
{
    /// <summary>中心服装载的稳定技能 ID 到学习等级；零级表示尚未学习。</summary>
    private Dictionary<int, int> skill_map_ = new Dictionary<int, int>();
    /// <summary>当前在线生命周期是否已成功接收完整中心服学习快照。</summary>
    private bool skills_loaded_;

    /// <summary>为现有在线角色容器创建唯一技能释放组件，不另建场景宿主。</summary>
    /// <param name="owner">持有权威角色快照及网关会话的在线角色。</param>
    public RoleSkillComponent(OnlineRole owner) : base(owner) { }

    /// <summary>完整中心服学习快照已装载后允许释放校验。</summary>
    public override bool SkillsLoaded { get { return skills_loaded_; } }

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

    /// <summary>先由基类结束活动技能和冷却缓存，再释放角色学习快照。</summary>
    public override void Dispose()
    {
        base.Dispose();
        skill_map_.Clear();
        skills_loaded_ = false;
    }
}
