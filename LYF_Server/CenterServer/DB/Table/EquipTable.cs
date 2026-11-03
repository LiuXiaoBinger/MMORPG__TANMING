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
}
