using SqlSugar;

/// <summary>
/// 装备扩展属性表。
/// </summary>
[SugarTable("equip", TableDescription = "装备表")]
internal class EquipTable
{
    /// <summary>
    /// 物品实例唯一ID，与 RoleID 组成复合主键，对应 ItemTable.ItemUID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long ItemUID { get; set; }

    /// <summary>
    /// 所属角色ID，与 ItemUID 组成复合主键，对应 RoleTable.Id。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleID { get; set; }

    /// <summary>
    /// 装备部位类别，对应 EquipType 中的衣服、武器、饰品等位置。
    /// </summary>
    public int EquipType { get; set; }

    /// <summary>
    /// 装备强化等级，默认值为0。
    /// </summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int StrengthenLevel { get; set; }

    /// <summary>装备实例第一个特技配置族编号，0 表示空槽；旧表升级时默认补 0。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int SpecialSkillId0 { get; set; }
    /// <summary>装备实例第二个特技配置族编号，0 表示空槽；旧表升级时默认补 0。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int SpecialSkillId1 { get; set; }
    /// <summary>装备实例第三个特技配置族编号，0 表示空槽；旧表升级时默认补 0。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int SpecialSkillId2 { get; set; }
    /// <summary>唯一启用的特技槽位，下标为 0 到 2；旧记录默认使用第一个槽位。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int ActiveSpecialSkillIndex { get; set; }
}
