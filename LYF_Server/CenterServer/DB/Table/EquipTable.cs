using SqlSugar;

/// <summary>
/// 装备扩展属性表。
/// </summary>
[SugarTable("equip", TableDescription = "装备表")]
internal class EquipTable
{
    /// <summary>
    /// 物品ID，与 RoleID 组成复合主键，对应 ItemTable.ItemID。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int ItemID { get; set; }

    /// <summary>
    /// 所属角色ID，与 ItemID 组成复合主键，对应 RoleTable.Id。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleID { get; set; }

    /// <summary>
    /// 装备类别，由业务层定义，例如武器、防具或饰品。
    /// </summary>
    public int EquipType { get; set; }

    /// <summary>
    /// 装备强化等级，默认值为0。
    /// </summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int StrengthenLevel { get; set; }
}
