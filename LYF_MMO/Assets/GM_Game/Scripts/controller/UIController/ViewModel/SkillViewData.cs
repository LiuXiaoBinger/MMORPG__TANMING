/// <summary>
/// 技能窗口使用的展示数据，不让控件直接读取 Luban 或角色全局状态。
/// </summary>
public sealed class SkillViewData
{
    /// <summary>技能配置编号。</summary>
    public int SkillId { get; }
    /// <summary>技能显示名称。</summary>
    public string Name { get; }
    /// <summary>技能说明文本。</summary>
    public string Description { get; }
    /// <summary>技能图标路径。</summary>
    public string IconPath { get; }
    /// <summary>角色当前技能等级。</summary>
    public int Level { get; }
    /// <summary>技能完整冷却时长，单位为秒。</summary>
    public float Cooldown { get; }
    /// <summary>技能绑定的快捷键。</summary>
    public string BindKey { get; }
    /// <summary>创建这份展示数据时的剩余冷却秒数，不是持续自动更新的计时器。</summary>
    public float RemainingCooldown { get; }
    /// <summary>技能分类，对应技能表 Type：1 普攻、2 主动技能、3 滑行、4 轻功。</summary>
    public int SkillType { get; }
    /// <summary>节点与详情使用的两字定位标签，例如“突进”“绝学”。</summary>
    public string Tag { get; }
    /// <summary>释放一次消耗的法力值。</summary>
    public int MpCost { get; }
    /// <summary>技能有效距离，单位为米。</summary>
    public int Range { get; }

    /// <summary>升到下一级消耗的铜币，绝学不走货币升级。</summary>
    public int UpgradeCost { get; }

    /// <summary>升到下一级所需的角色等级，0 表示由界面按当前技能等级推算。</summary>
    public int NextRoleLevel { get; }

    /// <summary>创建技能展示数据。</summary>
    /// <param name="skillId">技能配置编号。</param>
    /// <param name="name">技能名称，null 转为空串。</param>
    /// <param name="description">技能说明正文。</param>
    /// <param name="iconPath">技能图标资源路径。</param>
    /// <param name="level">已学等级，0 表示尚未学会。</param>
    /// <param name="cooldown">完整冷却秒数。</param>
    /// <param name="bindKey">技能绑定的快捷键字符。</param>
    /// <param name="remainingCooldown">本次刷新时剩余的冷却秒数。</param>
    /// <param name="skillType">技能分类，取自技能表 Type。</param>
    /// <param name="tag">两字定位标签。</param>
    /// <param name="mpCost">单次消耗法力值。</param>
    /// <param name="range">有效距离，单位为米。</param>
    /// <param name="upgradeCost">升到下一级消耗的铜币，默认 40。</param>
    /// <param name="nextRoleLevel">下一级所需角色等级，默认 0 由界面推算。</param>
    public SkillViewData(int skillId, string name, string description, string iconPath,
        int level, float cooldown, string bindKey, float remainingCooldown,
        int skillType, string tag, int mpCost, int range,
        int upgradeCost = 40, int nextRoleLevel = 0)
    {
        SkillId = skillId;
        Name = name;
        if (Name == null)
        {
            Name = string.Empty;
        }
        Description = description;
        if (Description == null)
        {
            Description = string.Empty;
        }
        IconPath = iconPath;
        if (IconPath == null)
        {
            IconPath = string.Empty;
        }
        Level = level;
        Cooldown = cooldown;
        BindKey = bindKey;
        if (BindKey == null)
        {
            BindKey = string.Empty;
        }
        RemainingCooldown = remainingCooldown;
        SkillType = skillType;
        Tag = tag;
        if (Tag == null)
        {
            Tag = string.Empty;
        }
        MpCost = mpCost;
        Range = range;
        UpgradeCost = upgradeCost;
        NextRoleLevel = nextRoleLevel;
    }
}
