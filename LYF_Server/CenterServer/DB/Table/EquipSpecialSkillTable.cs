using SqlSugar;

/// <summary>
/// 装备特技实例表。特技属于打在具体装备实例上的玩法数据，按物品实例保存三个特技族槽位
/// 和唯一启用槽位；装备配置表（EquipInfo）只保留装备基础属性，不再携带初始特技。
/// 主键与装备表一致，采用 RoleID + ItemUID 复合键，同一件装备最多一行。
/// </summary>
[SugarTable("equip_special_skill", TableDescription = "装备特技表")]
internal class EquipSpecialSkillTable
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
    /// 第一个特技槽的特技族编号，对应 EquipSpecialSkillInfo.SpecialSkillId；0 表示空槽。
    /// 新建装备三个槽位都为空，只有后续打特技玩法写入后才非 0。
    /// </summary>
    public int SpecialSkillId0 { get; set; } = 0;

    /// <summary>
    /// 第二个特技槽的特技族编号；0 表示空槽。
    /// </summary>
    public int SpecialSkillId1 { get; set; } = 0;

    /// <summary>
    /// 第三个特技槽的特技族编号；0 表示空槽。
    /// </summary>
    public int SpecialSkillId2 { get; set; } = 0;

    /// <summary>
    /// 唯一启用的特技槽下标，取值 0 到 2，默认 0；该槽为空时不产生任何特技效果。
    /// </summary>
    public int ActiveSpecialSkillIndex { get; set; } = 0;

    /// <summary>
    /// 已开启的特技孔数量，取值 0 到 3；新装备为 0，按 0/1/2 顺序逐个开启。
    /// 该字段与六个宝石孔无关，只决定哪几个特技槽可以安装和启用。
    /// </summary>
    public int OpenedSpecialSkillCount { get; set; } = 0;

    /// <summary>
    /// 把旧 equip 表四个特技列的历史数值迁移到本表。方法可重复执行：
    /// 第一段按本表自身槽位把已开孔数抬到“最高非空槽下标 + 1”，覆盖旧 equip 列已被清空的新表历史数据；
    /// 第二段只为“确实存在特技、本表还没有记录”的旧装备插入实例行；
    /// 第三段按旧槽位再抬高一次已开孔数，只增不减。
    /// 因此重复迁移既不会减少已开孔数，也不会用旧值覆盖新表里更完整的记录。
    /// </summary>
    /// <param name="db">已由 CodeFirst 建好本表的 SqlSugar 客户端。</param>
    /// <returns>本次实际插入的行数；第二次运行返回 0。</returns>
    public static int MigrateFromEquip(SqlSugarClient db)
    {
        // 没有客户端时没有任何可迁移的数据来源。
        if (db == null)
        {
            return 0;
        }

        // 第一段与旧 equip 表无关：按本表自身槽位把已开孔数抬到“最高非空槽下标 + 1”。
        // 只增不减，因此已经开过孔的实例不会被降级；新表历史行即使旧 equip 列全为 0 也能得到正确孔数。
        string raiseOwnSlotSql =
            "UPDATE `equip_special_skill` SET `OpenedSpecialSkillCount` = GREATEST(" +
            "`OpenedSpecialSkillCount`, " +
            "(CASE WHEN `SpecialSkillId2` <> 0 THEN 3 " +
            "WHEN `SpecialSkillId1` <> 0 THEN 2 " +
            "WHEN `SpecialSkillId0` <> 0 THEN 1 ELSE 0 END))";
        db.Ado.ExecuteCommand(raiseOwnSlotSql);

        // 旧 equip 表已不存在旧特技列（全新库）时，除上面的槽位归一外无需再做迁移。
        if (!HasLegacyEquipColumns(db))
        {
            return 0;
        }

        // 只迁移至少有一个特技槽非 0 的旧装备行，普通装备不产生空记录。
        // NOT EXISTS 保证同一实例已有新表记录时跳过，实现幂等且不覆盖新数据。
        // 已开孔数按最高非空槽下标 + 1 推导：第三槽非空为 3，否则第二槽非空为 2，否则为 1。
        string insertSql =
            "INSERT INTO `equip_special_skill` (`ItemUID`,`RoleID`,`SpecialSkillId0`,`SpecialSkillId1`," +
            "`SpecialSkillId2`,`ActiveSpecialSkillIndex`,`OpenedSpecialSkillCount`) " +
            "SELECT `ItemUID`,`RoleID`,`SpecialSkillId0`,`SpecialSkillId1`,`SpecialSkillId2`," +
            "`ActiveSpecialSkillIndex`, " +
            "(CASE WHEN `legacyEquip`.`SpecialSkillId2` <> 0 THEN 3 " +
            "WHEN `legacyEquip`.`SpecialSkillId1` <> 0 THEN 2 ELSE 1 END) " +
            "FROM `equip` AS `legacyEquip` " +
            "WHERE (`legacyEquip`.`SpecialSkillId0` <> 0 OR `legacyEquip`.`SpecialSkillId1` <> 0 OR " +
            "`legacyEquip`.`SpecialSkillId2` <> 0) " +
            "AND NOT EXISTS (SELECT 1 FROM `equip_special_skill` AS `currentSpecial` " +
            "WHERE `currentSpecial`.`ItemUID` = `legacyEquip`.`ItemUID` " +
            "AND `currentSpecial`.`RoleID` = `legacyEquip`.`RoleID`)";
        int inserted = db.Ado.ExecuteCommand(insertSql);

        // 第二段：已有新表记录的旧装备保留新值，但已开孔数不得低于旧槽位推导值。
        // GREATEST 保证单调，空槽装备不参与，因此不会把新装备的 0 孔错误抬高。
        string raiseSql =
            "UPDATE `equip_special_skill` AS `currentSpecial` " +
            "INNER JOIN `equip` AS `legacyEquip` ON `legacyEquip`.`ItemUID` = `currentSpecial`.`ItemUID` " +
            "AND `legacyEquip`.`RoleID` = `currentSpecial`.`RoleID` " +
            "SET `currentSpecial`.`OpenedSpecialSkillCount` = GREATEST(" +
            "`currentSpecial`.`OpenedSpecialSkillCount`, " +
            "(CASE WHEN `legacyEquip`.`SpecialSkillId2` <> 0 THEN 3 " +
            "WHEN `legacyEquip`.`SpecialSkillId1` <> 0 THEN 2 " +
            "WHEN `legacyEquip`.`SpecialSkillId0` <> 0 THEN 1 ELSE 0 END))";
        db.Ado.ExecuteCommand(raiseSql);
        return inserted;
    }

    /// <summary>
    /// 判断当前库的 equip 表是否仍保留旧特技列（SpecialSkillId0）。
    /// 全新数据库由 CodeFirst 直接建出新模型，没有这些列，迁移应直接跳过。
    /// </summary>
    /// <param name="db">当前连接的 SqlSugar 客户端。</param>
    /// <returns>旧特技列存在返回 true。</returns>
    private static bool HasLegacyEquipColumns(SqlSugarClient db)
    {
        // 通过 information_schema 查询当前库的列，不依赖 SqlSugar 版本提供的维护接口。
        string columnSql = "SELECT COUNT(*) FROM information_schema.COLUMNS " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'equip' " +
            "AND COLUMN_NAME = 'SpecialSkillId0'";
        return db.Ado.GetInt(columnSql) > 0;
    }
}
