using SqlSugar;

/// <summary>
/// 装备扩展属性表。
/// 业务读写只使用装备部位和强化等级；特技属于实例玩法数据，已独立存放在
/// EquipSpecialSkillTable 的 equip_special_skill 表。
/// 末尾四个旧特技列仅为保留历史数据而继续保留映射：SqlSugar CodeFirst 会删除实体中不存在的
/// 物理列（本轮实测确认），因此不能在数据确认迁移完成前从模型中移除。
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

    /// <summary>旧装备第一个特技槽的历史值，仅供迁移读取，业务代码不再读写；新数据写入 equip_special_skill。</summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int SpecialSkillId0 { get; set; }

    /// <summary>旧装备第二个特技槽的历史值，仅供迁移读取，业务代码不再读写。</summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int SpecialSkillId1 { get; set; }

    /// <summary>旧装备第三个特技槽的历史值，仅供迁移读取，业务代码不再读写。</summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int SpecialSkillId2 { get; set; }

    /// <summary>旧装备启用特技槽的历史值，仅供迁移读取，业务代码不再读写。</summary>
    [SugarColumn(DefaultValue = "0", IsOnlyIgnoreInsert = true)]
    public int ActiveSpecialSkillIndex { get; set; }
}
