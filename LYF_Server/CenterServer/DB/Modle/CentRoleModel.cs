

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using SqlSugar;

/// <summary>
/// 中心服角色与背包数据访问层。负责进入世界时查询并回包角色技能、分类型背包物品、
/// 装备/装备词条扩展、角色通用次数与已开格子容量，以及背包容量同步与商城购买占位处理。
/// 由 CenterRoleCtrl 在协议回调中调用并直接访问 MySQL；异步排队的角色快照保存另由 CRoleWriteTask 执行。
/// </summary>
public class CentRoleModel
{

    #region 字段与构造
    // 背包 UI 至少需要 9x9 个格子；旧角色没有记录时使用该兼容容量。
    private const int DefaultOpenedKnapsackGridCount = 81;

    /// <summary>共享数据库客户端，由 CenterApp 注入，全进程复用。</summary>
    private SqlSugarClient _db = null;

    /// <summary>
    /// 构造角色数据访问层。
    /// </summary>
    /// <param name="db">已初始化的 SqlSugar 客户端。</param>
    public CentRoleModel(SqlSugarClient db)
    {
        _db = db;
    }
    #endregion

    #region 背包格数同步与物品登记
    /// <summary>
    /// 将 GameServer 已验证的背包格子总数持久化。
    /// roleKnapsackcount 是 byte 字段，因此此处再次校验范围，避免异常数据写入数据库。
    /// </summary>
    public OpenKnapsackGridRet SyncKnapsackGridCount(SyncKnapsackGridCountReq req)
    {
        OpenKnapsackGridRet ret = new OpenKnapsackGridRet();
        // 请求缺失或角色 ID、已开格子数不合法时按参数错误返回。
        if (req == null || req.RoleId <= 0 || req.OpenedGridCount <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }
        // 已开格子数超出 byte 范围或背包类型不在持久化范围内时按参数错误返回。
        if (req.OpenedGridCount > byte.MaxValue || !IsPersistedKnapsackType((KnapsackType)req.KnapsackType))
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        ret.RoleId = req.RoleId;
        ret.KnapsackType = req.KnapsackType;
        ret.CurrentOpenedGridCount = req.OpenedGridCount;

        RoleKnapsackTable knapsackTable = _db.Queryable<RoleKnapsackTable>()
            .Where(v => v.RoleId == req.RoleId && v.roleKnapsack == (byte)req.KnapsackType)
            .First();
        if (knapsackTable == null)
        {
            // 兼容旧角色单条总背包记录：新分类首次同步时补建对应类型记录。
            knapsackTable = new RoleKnapsackTable
            {
                RoleId = req.RoleId,
                roleKnapsack = (byte)req.KnapsackType,
                roleKnapsackcount = (byte)req.OpenedGridCount,
                CreateDate = System.DateTime.Now,
                UpdateDate = System.DateTime.Now
            };
            ret.CmdCode = CmdCode.ServerError;
            if (_db.Insertable(knapsackTable).ExecuteCommand() > 0)
            {
                ret.CmdCode = CmdCode.Succeed;
            }
            return ret;
        }

        knapsackTable.roleKnapsackcount = (byte)req.OpenedGridCount;
        knapsackTable.UpdateDate = System.DateTime.Now;
        ret.CmdCode = CmdCode.ServerError;
        if (_db.Updateable(knapsackTable).ExecuteCommand() > 0)
        {
            ret.CmdCode = CmdCode.Succeed;
        }
        return ret;
    }

    /// <summary>
    /// 将物品放入对应的独立背包；普通背包不聚合其它背包的物品。
    /// </summary>
    private static void AddItemToPack(RoleKanpsackInfo ret, KnapsackType packType, RoleItemInfo itemInfo)
    {
        switch (packType)
        {
            case KnapsackType.RolePackPlain:
                ret.RolePackPlain.Add(itemInfo);
                break;
            case KnapsackType.RolePackEquip:
                ret.RolePackEquip.Add(itemInfo);
                break;
            case KnapsackType.RolePackConsume:
                ret.RolePackConsume.Add(itemInfo);
                break;
            case KnapsackType.RolePackMaterial:
                ret.RolePackMaterial.Add(itemInfo);
                break;
            case KnapsackType.RoleCurrtEquipPack:
                ret.RoleCurrtEquipPack.Add(itemInfo);
                break;
            case KnapsackType.RoleVirtualItemPack:
                // 虚拟物品单独返回，不进入普通背包，也不参与普通背包格子展示。
                ret.RoleVirtualItemPack.Add(itemInfo);
                break;
        }
    }

    /// <summary>
    /// 写入对应包裹的已开格子容量。该 Count 不是物品数量。
    /// </summary>
    private static void AddPackCount(RoleKanpsackInfo ret, KnapsackType packType, int openedGridCount)
    {
        ret.KanpsackTypeCountLst.Add(new Kanpsacktypecount
        {
            Type = (int)packType,
            Count = openedGridCount
        });
    }
    #endregion

    #region 查询与判定
    /// <summary>查询角色商城购买次数，在进入世界时同步给 GameServer。</summary>
    public RoleShopPurchaseInfoRet GetRoleShopPurchaseInfo(EnterWroldReq req)
    {
        RoleShopPurchaseInfoRet ret = new RoleShopPurchaseInfoRet
        {
            CmdCode = CmdCode.Succeed
        };
        if (req == null || req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        try
        {
            List<RoleShopPurchaseTable> rows = _db.Queryable<RoleShopPurchaseTable>()
                .Where(v => v.RoleID == req.RoleId)
                .ToList();
            if (rows == null)
            {
                ret.CmdCode = CmdCode.ServerError;
                return ret;
            }

            List<RoleShopPurchaseTable> rowSnapshot = new List<RoleShopPurchaseTable>(rows);
            for (int index = 0; index < rowSnapshot.Count; index++)
            {
                RoleShopPurchaseTable row = rowSnapshot[index];
                ret.PurchaseInfoList.Add(new RoleShopPurchaseInfo
                {
                    RoleId = row.RoleID,
                    GoodsId = row.ShopID,
                    DailyPurchaseDate = row.DailyPurchaseDate.ToString("O"),
                    DailyPurchaseCount = row.DailyPurchaseCount,
                    TotalPurchaseCount = row.TotalPurchaseCount,
                    CreateDate = row.CreateDate.ToString("O"),
                    UpdateDate = row.UpdateDate.ToString("O")
                });
            }
        }
        catch (Exception ex)
        {
            // 商城记录是附加登录数据，查询失败时返回错误但不影响技能和背包回包。
            LogMsg.Info("查询角色商城购买记录失败: " + ex, LogMsgType.Error);
            ret.CmdCode = CmdCode.ServerError;
            ret.PurchaseInfoList.Clear();
        }

        return ret;
    }

    /// <summary>将数据库刷新时间转换为协议 UTC ticks。</summary>
    private static long GetUtcTicks(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return 0L;
        }
        return value.ToUniversalTime().Ticks;
    }

    /// <summary>判断该背包是否保存可扩容容量；穿戴栏和虚拟物品栏不在其中。</summary>
    /// <param name="type">待检查的背包分类。</param>
    private static bool IsPersistedKnapsackType(KnapsackType type)
    {
        // 是否为普通包裹或装备包裹类型。
        bool isPlainOrEquip = type == KnapsackType.RolePackPlain || type == KnapsackType.RolePackEquip;
        // 是否为消耗品包裹或材料包裹类型。
        bool isConsumeOrMaterial = type == KnapsackType.RolePackConsume || type == KnapsackType.RolePackMaterial;
        return isPlainOrEquip || isConsumeOrMaterial;
    }

    /// <summary>返回需要分别保存容量的普通、装备、消耗品和材料背包。</summary>
    private static KnapsackType[] GetPersistedKnapsackTypes()
    {
        return new[]
        {
            KnapsackType.RolePackPlain,
            KnapsackType.RolePackEquip,
            KnapsackType.RolePackConsume,
            KnapsackType.RolePackMaterial
        };
    }

    /// <summary>旧普通背包记录是分类容量补齐时的最低值。</summary>
    private static int GetLegacyFallback(List<RoleKnapsackTable> rows)
    {
        RoleKnapsackTable oldAll = null;
        if (rows != null)
        {
            oldAll = rows.FirstOrDefault(v => v.roleKnapsack == (byte)KnapsackType.RolePackPlain);
        }
        if (oldAll == null)
        {
            return DefaultOpenedKnapsackGridCount;
        }
        return System.Math.Max(DefaultOpenedKnapsackGridCount, (int)oldAll.roleKnapsackcount);
    }

    /// <summary>已有分类保留自身与旧普通背包中的较大容量，缺失分类继承旧普通背包。</summary>
    private static int GetMigratedGridCount(RoleKnapsackTable row, int legacyFallback)
    {
        if (row == null)
        {
            return legacyFallback;
        }
        return System.Math.Max(legacyFallback,
            System.Math.Max(DefaultOpenedKnapsackGridCount, (int)row.roleKnapsackcount));
    }

    /// <summary>中心服商城入口当前仅返回功能未就绪，不扣货币、不发物品，也不改购买次数。</summary>
    public BuyShopItemRes ProcessShopPurchase(BuyShopItemReq request)
    {
        return new BuyShopItemRes
        {
            Error = new ErrorInfo
            {
                Code = CmdCode.FeatureNotReady,
                Message = "商城购买事务尚未接入"
            }
        };
    }
    #endregion

    #region 登录数据组装
    /// <summary>
    /// 查询指定持久角色的技能列表，并在成功或失败回包中保留该角色 ID。
    /// </summary>
    /// <param name="req">GameServer 已校验会话后转发的进入世界请求，携带持久角色 ID。</param>
    /// <returns>包含原角色 ID、查询结果码及技能等级明细的回包。</returns>
    public RoleSkillInfoRet RoleSkillInfo(EnterWroldReq req)
    {
        // 角色 ID 必须来自中心服处理的请求，供 GameServer 抵御复合会话复用后的迟到回包。
        RoleSkillInfoRet ret = new RoleSkillInfoRet { RoleId = req.RoleId };
        ret.CmdCode = CmdCode.Succeed;
        // 本次角色查询结果的每一行对应已持久化的学习等级和快捷键。
        List< RoleSkillTable> roleSkillList = _db.Queryable<RoleSkillTable>().Where(v => v.RoleID == req.RoleId).ToList();
        if (roleSkillList != null && roleSkillList.Count > 0)
        {
            // 持久技能行的索引，逐行保留等级与绑定，仅规范本次 F/2 功能对应两项。
            for (int i = 0; i < roleSkillList.Count; i++)
            {
                // 单条角色技能的 ID/等级来自数据库，不使用客户端提供的效果或数值。
                RoleSkillInfo roleSkillInfo = new RoleSkillInfo()
                {
                    SkillId =  roleSkillList[i].SkillID,
                    Level =  roleSkillList[i].SkillLevel,
                    BindKey = roleSkillList[i].Bindkey
                };
                // F 固定使用已接入的滑行，旧 F 斩天剑移至 2；只规范回包，不覆盖数据库键位。
                if (roleSkillInfo.SkillId == 10080)
                {
                    roleSkillInfo.BindKey = "F";
                    if (roleSkillInfo.Level <= 0)
                    {
                        roleSkillInfo.Level = 1;
                    }
                }
                else if (roleSkillInfo.SkillId == 10060 &&
                    string.Equals(roleSkillInfo.BindKey, "F", StringComparison.OrdinalIgnoreCase))
                {
                    roleSkillInfo.BindKey = "2";
                }
                ret.RoleSkillInfoList.Add(roleSkillInfo);
            }
            AddCombinationSkill(ret);
        }
        else
        {
            ret.CmdCode = CmdCode.RoleNotExist;
        }
        
        return ret;
    }

    /// <summary>魔剑士的职业编号，与 Luban 职业表及技能表 JobId 一致。</summary>
    private const int MageSwordsmanJobId = 1;

    /// <summary>已有 E 和 2 的角色获得本次组合示例，保留数据库原技能和已有快捷键。</summary>
    /// <param name="response">本角色从数据库组装出的技能列表，仅补充派生组合技能。</param>
    private void AddCombinationSkill(RoleSkillInfoRet response)
    {
        // 职业取自角色表真实 JobID，只有魔剑士获得本次两个新技能，其他职业键位完全不动。
        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == response.RoleId).First();
        // 职业编号 1 为魔剑士，与 Luban SkillInfo 的 JobId 一致。
        bool isMageSwordsman = role != null && role.JobID == MageSwordsmanJobId;
        ApplyNewSkillKeys(response, isMageSwordsman);
    }

    /// <summary>
    /// 按职业补发刀刃风暴与苍蓝屏障，并规范三、四、五号键的归属。
    /// 与数据库和网络连接无关，便于用最小用例覆盖新建角色、老角色和五号键被占用三种输入。
    /// </summary>
    /// <param name="response">登录回包里的技能列表，就地补发新技能并改写快捷键。</param>
    /// <param name="isMageSwordsman">当前角色是否为魔剑士；非魔剑士只补组合技能，键位保持原样。</param>
    private static void ApplyNewSkillKeys(RoleSkillInfoRet response, bool isMageSwordsman)
    {
        // 两个组成技能均已学会时才提供组合入口。
        bool hasFirst = false;
        // 斩天剑学习标记，与清风剑法共同决定示例是否可用。
        bool hasSecond = false;
        // 已存在的组合技能记录，只在缺键时补回退键，不改写玩家已有的绑定。
        RoleSkillInfo combination = null;
        // 已存在的刀刃风暴记录，登录时为主动技能建等级 0 行，未提升会无法释放。
        RoleSkillInfo bladeStorm = null;
        // 已存在的苍蓝屏障记录，同样可能停在等级 0 且没有任何快捷键。
        RoleSkillInfo barrier = null;
        // 占用三号键的其它技能，键位让给刀刃风暴后只清空它的快捷键，技能本身仍留在列表。
        RoleSkillInfo thirdHolder = null;
        // 占用四号键的技能，仅用于判断屏障能否默认绑定，不用于抢占。
        RoleSkillInfo fourthHolder = null;
        // 占用五号键的技能，组合技能回退到五号键前必须先确认它空闲。
        RoleSkillInfo fifthHolder = null;
        // 原技能列表的零起始索引，新技能在遍历完成后追加，一次扫描完成全部占用判定。
        for (int index = 0; index < response.RoleSkillInfoList.Count; index++)
        {
            // 当前角色持有的技能记录，不改写其原对象字段。
            RoleSkillInfo skill = response.RoleSkillInfoList[index];
            if (skill.SkillId == 10040 && skill.Level > 0) hasFirst = true;
            if (skill.SkillId == 10060 && skill.Level > 0) hasSecond = true;
            if (skill.SkillId == 10100) combination = skill;
            if (skill.SkillId == 10110) bladeStorm = skill;
            if (skill.SkillId == 10120) barrier = skill;
            if (skill.BindKey == "3") thirdHolder = skill;
            if (skill.BindKey == "4") fourthHolder = skill;
            if (skill.BindKey == "5") fifthHolder = skill;
        }
        // 组合学习条件与键位无关，只要 E 与 2 都学会就给组合技能记录。
        if (hasFirst && hasSecond && combination == null)
        {
            // 新技能以一级参数执行 E→2，不对已有角色数据库做迁移。
            combination = new RoleSkillInfo { SkillId = 10100, Level = 1 };
            response.RoleSkillInfoList.Add(combination);
        }
        // 非魔剑士到此为止，保留其原有键位与技能列表。
        if (!isMageSwordsman) return;
        // 三号键固定属于刀刃风暴：占用它的其它技能只清空快捷键，仍保留在技能列表里。
        if (thirdHolder != null && thirdHolder.SkillId != 10110) thirdHolder.BindKey = string.Empty;
        // 魔剑士一律获得一级刀刃风暴并绑定数字 3，不要求先学旧 E/2。
        if (bladeStorm == null)
        {
            bladeStorm = new RoleSkillInfo { SkillId = 10110, Level = 1 };
            response.RoleSkillInfoList.Add(bladeStorm);
        }
        // 登录时为每个主动技能建等级 0 行，不提升到 1 时正式施法入口会直接拒绝。
        else if (bladeStorm.Level <= 0) bladeStorm.Level = 1;
        bladeStorm.BindKey = "3";
        // 苍蓝屏障同样补齐记录，新建角色只有等级 0 行时在此提升为可释放的一级。
        if (barrier == null)
        {
            barrier = new RoleSkillInfo { SkillId = 10120, Level = 1 };
            response.RoleSkillInfoList.Add(barrier);
        }
        else if (barrier.Level <= 0) barrier.Level = 1;
        // 四号键空闲且屏障自身没有自定义键时才默认绑定，保留玩家已有的四号键设置。
        if (fourthHolder == null && string.IsNullOrEmpty(barrier.BindKey)) barrier.BindKey = "4";
        // 组合技能始终以 E 与 2 都学会为学习条件：登录时会为所有主动技能建等级 0 行，
        // 缺少该判定就会让尚未学清风剑法与斩天剑的新角色提前拿到组合技能。
        if (hasFirst && hasSecond && combination != null)
        {
            // 已学会的两个基础技能使该记录可以执行，等级 0 提升为可释放的一级。
            if (combination.Level <= 0) combination.Level = 1;
            // 空字符串表示从未绑定；三号键已让位给刀刃风暴，两种情况都需要回退。
            bool needsFallback = string.IsNullOrEmpty(combination.BindKey);
            if (string.Equals(combination.BindKey, "3", StringComparison.Ordinal)) needsFallback = true;
            // 五号键已被其它技能占用时优先保留玩家的个性键，不抢占。
            if (needsFallback && fifthHolder == null) combination.BindKey = "5";
        }
    }

    /// <summary>
    /// 获取主角背包信息：分类型物品列表、装备/词条扩展、已开格子容量、角色通用次数与保存版本。
    /// 旧角色缺失的分类记录会在读取背包时补齐，容量取旧普通背包与默认 81 格的较大值。
    /// </summary>
    /// <param name="req">进入世界请求，携带持久角色 ID。</param>
    /// <returns>完整背包与次数信息回包，失败时仅携带错误码。</returns>
    public RoleKanpsackInfoRet RoleKanpaskInfo(EnterWroldReq req)
    {
        RoleKanpsackInfoRet ret = new RoleKanpsackInfoRet();
        ret.RoleKanpsackInfo = new RoleKanpsackInfo();
        if (req == null || req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        List<RoleKnapsackTable> knapsackTables = _db.Queryable<RoleKnapsackTable>()
            .Where(v => v.RoleId == req.RoleId)
            .ToList();
        if (knapsackTables == null)
        {
            knapsackTables = new List<RoleKnapsackTable>();
        }
        // 旧普通背包容量作为新增分类的最低容量，且至少保留 81 格。
        int legacyFallback = GetLegacyFallback(knapsackTables);
        EnsureMissingKnapsackRows(req.RoleId, knapsackTables, legacyFallback);

        // 当前角色的物品主记录，先按分类再按格子排列。
        List<ItemTable> itemList = _db.Queryable<ItemTable>()
            .Where(v => v.RoleID == req.RoleId)
            .OrderBy(v => v.BagType)
            .OrderBy(v => v.BagIndex)
            .ToList();
        if (itemList == null)
        {
            ret.CmdCode = CmdCode.ServerError;
            return ret;
        }

        // 三张表都用 ItemUID 标识同一个物品实例，不能用物品配置 ID 关联。
        Dictionary<long, EquipTable> equipByItemUid = _db.Queryable<EquipTable>()
            .Where(v => v.RoleID == req.RoleId)
            .ToList()
            .ToDictionary(v => v.ItemUID);
        // 通过物品实例编号拼接词条，避免同配置的不同装备互相混淆。
        Dictionary<long, EquipXLGeneTable> geneByItemUid = _db.Queryable<EquipXLGeneTable>()
            .Where(v => v.RoleID == req.RoleId)
            .ToList()
            .ToDictionary(v => v.ItemUID);
        // 特技实例单独成表，按物品实例编号与装备行对齐；没有记录的装备表示三个槽位都是空槽。
        Dictionary<long, EquipSpecialSkillTable> specialSkillByItemUid = _db.Queryable<EquipSpecialSkillTable>()
            .Where(v => v.RoleID == req.RoleId)
            .ToList()
            .ToDictionary(v => v.ItemUID);

        // 读取结果的索引列表，每个物品再附上装备和词条扩展。
        List<ItemTable> itemSnapshot = new List<ItemTable>(itemList);
        for (int itemIndex = 0; itemIndex < itemSnapshot.Count; itemIndex++)
        {
            ItemTable item = itemSnapshot[itemIndex];
            KnapsackType packType = (KnapsackType)item.BagType;
            // 协议中零表示没有到期时间，其余用 UTC 的 100 纳秒刻度。
            long expireTimeUtcTicks = 0L;
            if (item.ExpireDate.HasValue)
            {
                expireTimeUtcTicks = item.ExpireDate.Value.ToUniversalTime().Ticks;
            }
            RoleItemInfo itemInfo = new RoleItemInfo
            {
                ItemUid = item.ItemUID,
                Count = item.count,
                RoleId = item.RoleID,
                ItemTypeId = item.ItemTypeID,
                BagType = (int)packType,
                BagIndex = item.BagIndex,
                ItemSign = item.ItemSign,
                MoneyType = item.MoneyType,
                TotalPrice = item.TotalPrice,
                CreateTimeUtcTicks = item.CreateDate.ToUniversalTime().Ticks,
                ExpireTimeUtcTicks = expireTimeUtcTicks
            };

            EquipTable equip;
            if (equipByItemUid.TryGetValue(item.ItemUID, out equip))
            {
                itemInfo.EquipInfo = new RoleEquipItemInfo
                {
                    ItemUid = equip.ItemUID,
                    RoleId = equip.RoleID,
                    StrengthenLevel = equip.StrengthenLevel,
                    EquipType = equip.EquipType
                };
                // 三个特技槽和启用槽来自独立特技表；没有记录时保持协议默认值 0，表示槽位全空。
                EquipSpecialSkillTable specialSkill;
                if (specialSkillByItemUid.TryGetValue(equip.ItemUID, out specialSkill))
                {
                    itemInfo.EquipInfo.SpecialSkillId0 = specialSkill.SpecialSkillId0;
                    itemInfo.EquipInfo.SpecialSkillId1 = specialSkill.SpecialSkillId1;
                    itemInfo.EquipInfo.SpecialSkillId2 = specialSkill.SpecialSkillId2;
                    itemInfo.EquipInfo.ActiveSpecialSkillIndex = specialSkill.ActiveSpecialSkillIndex;
                    // 已开孔数必须随装备下发，否则客户端无法区分“未开孔”和“已开孔空槽”。
                    itemInfo.EquipInfo.OpenedSpecialSkillCount = specialSkill.OpenedSpecialSkillCount;
                }
            }

            EquipXLGeneTable gene;
            if (geneByItemUid.TryGetValue(item.ItemUID, out gene))
            {
                itemInfo.EquipGeneInfo = new RoleEquipGeneInfo
                {
                    ItemUid = gene.ItemUID,
                    RoleId = gene.RoleID,
                    GeneId0 = gene.GeneID0,
                    GeneId1 = gene.GeneID1,
                    GeneId2 = gene.GeneID2,
                    GeneValue0 = gene.GeneValue0,
                    GeneValue1 = gene.GeneValue1,
                    GeneValue2 = gene.GeneValue2
                };
            }

            AddItemToPack(ret.RoleKanpsackInfo, packType, itemInfo);
        }

        KnapsackType[] persistedTypes = GetPersistedKnapsackTypes();
        for (int typeIndex = 0; typeIndex < persistedTypes.Length; typeIndex++)
        {
            KnapsackType packType = persistedTypes[typeIndex];
            RoleKnapsackTable row = null;
            if (knapsackTables != null)
            {
                row = knapsackTables.FirstOrDefault(v => v.roleKnapsack == (byte)packType);
            }
            // 独立分类记录和旧总包容量取较大值，保证老数据升级后容量单调不减。
            int openedGridCount = GetMigratedGridCount(row, legacyFallback);
            AddPackCount(ret.RoleKanpsackInfo, packType, openedGridCount);
        }
        // 穿戴栏是装备状态而非可扩容背包，仍返回最小容量供旧客户端布局兼容。
        AddPackCount(ret.RoleKanpsackInfo, KnapsackType.RoleCurrtEquipPack, DefaultOpenedKnapsackGridCount);

        // 角色主表保存已落库版本，游戏服从该版本继续产生后续保存请求。
        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == req.RoleId).First();
        if (role != null)
        {
            ret.RoleKanpsackInfo.SaveVersion = role.ItemSaveVersion;
        }

        // 次数与背包物品一起返回，GameServer 登录时一次性装载所有角色组件数据。
        // 商城、副本等计数随背包一起恢复，避免在线角色各组件使用不同批次存档。
        List<RoleCountTable> roleCounts = _db.Queryable<RoleCountTable>()
            .Where(v => v.RoleId == req.RoleId).ToList();
        if (roleCounts != null)
        {
            List<RoleCountTable> countSnapshot = new List<RoleCountTable>(roleCounts);
            for (int countIndex = 0; countIndex < countSnapshot.Count; countIndex++)
            {
                RoleCountTable count = countSnapshot[countIndex];
                ret.RoleKanpsackInfo.RoleCountInfoList.Add(new RoleCountInfo
                {
                    Action = count.Action,
                    CountKey = count.CountKey,
                    Count = count.Count,
                    LastRefreshTime = GetUtcTicks(count.LastRefreshTime)
                });
            }
        }

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>查询旧角色时补齐四类可扩容背包，穿戴栏不创建容量记录。</summary>
    private void EnsureMissingKnapsackRows(int roleId, List<RoleKnapsackTable> rows, int legacyFallback)
    {
        List<RoleKnapsackTable> missingRows = new List<RoleKnapsackTable>();
        System.DateTime now = System.DateTime.Now;
        KnapsackType[] persistedTypes = GetPersistedKnapsackTypes();
        for (int typeIndex = 0; typeIndex < persistedTypes.Length; typeIndex++)
        {
            KnapsackType type = persistedTypes[typeIndex];
            if (rows != null && rows.Any(v => v.roleKnapsack == (byte)type))
            {
                continue;
            }

            RoleKnapsackTable row = new RoleKnapsackTable
            {
                RoleId = roleId,
                roleKnapsack = (byte)type,
                roleKnapsackcount = (byte)legacyFallback,
                CreateDate = now,
                UpdateDate = now
            };
            missingRows.Add(row);
            rows.Add(row);
        }

        if (missingRows.Count > 0)
        {
            _db.Insertable(missingRows).ExecuteCommand();
        }
    }
    #endregion
}
