using SqlSugar;
using System;

/// <summary>
/// 角色技能表，保存角色对技能的学习和成长状态。
/// 技能名称、伤害、冷却等静态数据由 Luban 的 SkillInfo 提供。
/// </summary>
[SugarTable("role_skill", TableDescription = "角色技能表")]
internal class RoleSkillTable
{
    /// <summary>
    /// 数据库主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 是否已解锁：0 未解锁，1 已解锁。
    /// </summary>
    [SugarColumn(DefaultValue = "1", IsOnlyIgnoreInsert = true)]
    public byte IsUnlocked { get; set; }
    
    /// <summary>
    /// 所属角色ID，对应 RoleTable.Id。
    /// </summary>
    public int RoleID { get; set; }

    /// <summary>
    /// 技能配置ID，对应 Luban SkillInfo.Id。
    /// </summary>
    public int SkillID { get; set; }

    /// <summary>
    /// 当前技能等级。
    /// </summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int SkillLevel { get; set; }

    /// <summary>
    /// 技能熟练度或升级经验。
    /// </summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int SkillExp { get; set; }

   

    /// <summary>
    /// 技能栏位置，未装备时为 0。
    /// </summary>
    //[SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
   // public int SlotIndex { get; set; }
    /// <summary>
    /// 绑定键盘按键
    /// </summary>
    public string Bindkey { get; set; }
    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime UpdateDate { get; set; }
}
