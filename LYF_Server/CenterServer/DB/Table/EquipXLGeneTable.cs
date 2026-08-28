using SqlSugar;

/// <summary>
/// 装备词条表，保存装备随机生成的三个词条ID。
/// </summary>
[SugarTable("equip_xl_gene", TableDescription = "装备词条表")]
internal class EquipXLGeneTable
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
    /// 第一个装备词条配置ID。
    /// </summary>
    public int GeneID0 { get; set; } = 0;

    /// <summary>
    /// 第一个装备词条数值。
    /// </summary>
    public int GeneValue0 { get; set; } = 0;

    /// <summary>
    /// 第二个装备词条配置ID。
    /// </summary>
    public int GeneID1 { get; set; } = 0;

    /// <summary>
    /// 第二个装备词条数值。
    /// </summary>
    public int GeneValue1 { get; set; } = 0;

    /// <summary>
    /// 第三个装备词条配置ID。
    /// </summary>
    public int GeneID2 { get; set; } = 0;

    /// <summary>
    /// 第三个装备词条数值。
    /// </summary>
    public int GeneValue2 { get; set; } = 0;
}
