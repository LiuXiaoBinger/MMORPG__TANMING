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
    /// <summary>技能完整冷却时间。</summary>
    public float Cooldown { get; }
    /// <summary>技能绑定的快捷键。</summary>
    public string BindKey { get; }
    /// <summary>控制器保存的剩余冷却时间。</summary>
    public float RemainingCooldown { get; }

    /// <summary>创建技能展示数据。</summary>
    public SkillViewData(int skillId, string name, string description, string iconPath,
        int level, float cooldown, string bindKey, float remainingCooldown)
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
    }
}
