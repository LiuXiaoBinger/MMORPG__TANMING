using SqlSugar;

/// <summary>
/// 装备扩展属性表。
/// 业务读写装备部位、强化等级和六个宝石孔；特技属于实例玩法数据，已独立存放在
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
    /// 装备强化等级，全量存档重建行时写回实例原值，新装备默认为零。
    /// </summary>
    [SugarColumn(DefaultValue = "0")]
    public int StrengthenLevel { get; set; }

    /// <summary>金装已升的十级档数，零对应原有的60级。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int GoldLevelSteps { get; set; }

    /// <summary>金装已进的阶数，零对应初始一阶。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int GoldTierSteps { get; set; }

    /// <summary>按配置槽顺序存放六个实例整数；NULL 表示旧版按固定模板结算的装备。</summary>
    [SugarColumn(IsNullable = true, Length = 96)]
    public string BaseRolls { get; set; }

    /// <summary>该装备第一个孔的宝石物品编号，零表示空孔。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int Gem0 { get; set; }

    /// <summary>该装备第二个孔的宝石物品编号，零表示空孔。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int Gem1 { get; set; }

    /// <summary>该装备第三个孔的宝石物品编号，零表示空孔。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int Gem2 { get; set; }

    /// <summary>该装备第四个孔的宝石物品编号，零表示空孔。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int Gem3 { get; set; }

    /// <summary>该装备第五个孔的宝石物品编号，零表示空孔。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int Gem4 { get; set; }

    /// <summary>该装备第六个孔的宝石物品编号，零表示空孔。</summary>
    [SugarColumn(DefaultValue = "0")]
    public int Gem5 { get; set; }

    /// <summary>将存档协议中的六个孔写入装备行，兼容没有宝石字段的旧装备。</summary>
    /// <param name="info">同一装备的有序孔位快照。</param>
    public void ReadGemSlots(RoleEquipItemInfo info)
    {
        Gem0 = ReadGem(info, 0);
        Gem1 = ReadGem(info, 1);
        Gem2 = ReadGem(info, 2);
        Gem3 = ReadGem(info, 3);
        Gem4 = ReadGem(info, 4);
        Gem5 = ReadGem(info, 5);
    }

    /// <summary>按固定顺序构造重登时交给游戏服的六个宝石编号。</summary>
    /// <param name="info">由当前数据库行恢复的装备协议。</param>
    public void WriteGemSlots(RoleEquipItemInfo info)
    {
        info.GemItemIds.Add(new[] { Gem0, Gem1, Gem2, Gem3, Gem4, Gem5 });
    }

    /// <summary>核对同版本重发的六个孔，与新旧快照的尾部空孔兼容。</summary>
    /// <param name="info">需要与数据库比较的装备快照。</param>
    public bool HasSameGemSlots(RoleEquipItemInfo info)
    {
        if (Gem0 != ReadGem(info, 0) || Gem1 != ReadGem(info, 1)) return false;
        if (Gem2 != ReadGem(info, 2) || Gem3 != ReadGem(info, 3)) return false;
        return Gem4 == ReadGem(info, 4) && Gem5 == ReadGem(info, 5);
    }

    /// <summary>读取指定孔位；旧档或省略的尾部孔位按空孔处理。</summary>
    /// <param name="info">需要读取的装备快照。</param>
    /// <param name="index">零开始的孔位顺序。</param>
    private static int ReadGem(RoleEquipItemInfo info, int index)
    {
        if (index < info.GemItemIds.Count) return info.GemItemIds[index];
        return 0;
    }

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
