using SqlSugar;

/// <summary>
/// 装备词条扩展表，按物品实例保存三个词条的配置编号和实际数值。
/// </summary>
[SugarTable("equip_xl_gene", TableDescription = "装备词条表")]
internal class EquipXLGeneTable
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

    /// <summary>第四条生效词条的基因编号。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int GeneID3 { get; set; }

    /// <summary>第四条生效词条的整数点数。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int GeneValue3 { get; set; }

    /// <summary>第五条生效词条的基因编号。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int GeneID4 { get; set; }

    /// <summary>第五条生效词条的整数点数。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int GeneValue4 { get; set; }

    /// <summary>每次洗练或替换后递增，用于识别客户端看到的那次结果。</summary>
    [SugarColumn(DefaultValue = "0")]
    public long WashRevision { get; set; }

    /// <summary>上一次产生候选方案的萃石物品编号。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int WashStone { get; set; }

    /// <summary>待替换的五槽词条快照，按 Protobuf 字节转 Base64 保存。</summary>
    [SugarColumn(IsNullable = true, Length = 256)]
    public string WashCandidate { get; set; } = string.Empty;
}
