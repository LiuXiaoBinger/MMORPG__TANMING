


using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using SqlSugar;

public class CentRoleModel
{
    // 背包 UI 至少需要 9x9 个格子；旧角色没有记录时使用该兼容容量。
    private const int DefaultOpenedKnapsackGridCount = 81;
    

    private SqlSugarClient _db = null;
    public CentRoleModel(SqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>处理中心服商城购买请求。</summary>
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

    /// <summary>
    /// 将 GameServer 已验证的背包格子总数持久化。
    /// roleKnapsackcount 是 byte 字段，因此此处再次校验范围，避免异常数据写入数据库。
    /// </summary>
    public OpenKnapsackGridRet SyncKnapsackGridCount(SyncKnapsackGridCountReq req)
    {
        OpenKnapsackGridRet ret = new OpenKnapsackGridRet();
        if (req == null || req.RoleId <= 0 || req.OpenedGridCount <= 0 || req.OpenedGridCount > byte.MaxValue ||
            !IsPersistedKnapsackType((KnapsackType)req.KnapsackType))
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
    /// 查询角色技能信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public RoleSkillInfoRet RoleSkillInfo(EnterWroldReq req)
    {
        RoleSkillInfoRet ret = new RoleSkillInfoRet();
        ret.CmdCode = CmdCode.Succeed;
        List< RoleSkillTable> roleSkillList = _db.Queryable<RoleSkillTable>().Where(v => v.RoleID == req.RoleId).ToList();
        if (roleSkillList != null && roleSkillList.Count > 0)
        {
            for (int i = 0; i < roleSkillList.Count; i++)
            {
                RoleSkillInfo roleSkillInfo = new RoleSkillInfo()
                {
                    SkillId =  roleSkillList[i].SkillID,
                    Level =  roleSkillList[i].SkillLevel,
                    BindKey = roleSkillList[i].Bindkey
                };
                ret.RoleSkillInfoList.Add(roleSkillInfo);
            }
        }
        else
        {
            ret.CmdCode = CmdCode.RoleNotExist;
        }
        
        return ret;
    }

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

    /// <summary>
    /// 获取主角背包信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
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
        int legacyFallback = GetLegacyFallback(knapsackTables);
        EnsureMissingKnapsackRows(req.RoleId, knapsackTables, legacyFallback);

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
        Dictionary<long, EquipXLGeneTable> geneByItemUid = _db.Queryable<EquipXLGeneTable>()
            .Where(v => v.RoleID == req.RoleId)
            .ToList()
            .ToDictionary(v => v.ItemUID);

        List<ItemTable> itemSnapshot = new List<ItemTable>(itemList);
        for (int itemIndex = 0; itemIndex < itemSnapshot.Count; itemIndex++)
        {
            ItemTable item = itemSnapshot[itemIndex];
            KnapsackType packType = (KnapsackType)item.BagType;
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

        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == req.RoleId).First();
        if (role != null)
        {
            ret.RoleKanpsackInfo.SaveVersion = role.ItemSaveVersion;
        }

        // 次数与背包物品一起返回，GameServer 登录时一次性装载所有角色组件数据。
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

    /// <summary>将数据库刷新时间转换为协议 UTC ticks。</summary>
    private static long GetUtcTicks(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return 0L;
        }
        return value.ToUniversalTime().Ticks;
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

    private static bool IsPersistedKnapsackType(KnapsackType type)
    {
        return type == KnapsackType.RolePackPlain || type == KnapsackType.RolePackEquip ||
            type == KnapsackType.RolePackConsume || type == KnapsackType.RolePackMaterial;
    }

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
}
