


using System.Collections.Generic;
using System.Linq;
using SqlSugar;

public class CentRoleModel
{
    

    private SqlSugarClient _db = null;
    public CentRoleModel(SqlSugarClient db)
    {
        _db = db;
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

    /// <summary>
    /// 获取主角背包信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public RoleKanpsackInfoRet RoleKanpaskInfo(EnterWroldReq req)
    {
        RoleKanpsackInfoRet ret = new RoleKanpsackInfoRet();
        if (req == null || req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

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

        Dictionary<int, EquipTable> equipByItemId = _db.Queryable<EquipTable>()
            .Where(v => v.RoleID == req.RoleId)
            .ToList()
            .ToDictionary(v => v.ItemID);
        Dictionary<int, EquipXLGeneTable> geneByItemId = _db.Queryable<EquipXLGeneTable>()
            .Where(v => v.RoleID == req.RoleId)
            .ToList()
            .ToDictionary(v => v.ItemID);

        foreach (ItemTable item in itemList)
        {
            KnapsackType packType = (KnapsackType)item.BagType;
            RoleItemInfo itemInfo = new RoleItemInfo
            {
                ItemId = item.ItemID,
                Count = item.count,
                RoleId = item.RoleID,
                ItemTypeId = item.ItemTypeID,
                BagType = (int)packType,
                BagIndex = item.BagIndex
            };

            EquipTable equip;
            if (equipByItemId.TryGetValue(item.ItemID, out equip))
            {
                itemInfo.EquipInfo = new RoleEquipItemInfo
                {
                    ItemId = equip.ItemID,
                    RoleId = equip.RoleID,
                    StrengthenLevel = equip.StrengthenLevel,
                    EquipType = equip.EquipType
                };
            }

            EquipXLGeneTable gene;
            if (geneByItemId.TryGetValue(item.ItemID, out gene))
            {
                itemInfo.EquipGeneInfo = new RoleEquipGeneInfo
                {
                    ItemId = gene.ItemID,
                    RoleId = gene.RoleID,
                    GeneId0 = gene.GeneID0,
                    GeneId1 = gene.GeneID1,
                    GeneId2 = gene.GeneID2,
                    GeneValue0 = gene.GeneValue0,
                    GeneValue1 = gene.GeneValue1,
                    GeneValue2 = gene.GeneValue2
                };
            }

            AddItemToPack(ret, packType, itemInfo);
        }

        AddPackCount(ret, KnapsackType.RolePackAll, ret.RolePackAll.Count);
        AddPackCount(ret, KnapsackType.RolePackEquip, ret.RolePackEquip.Count);
        AddPackCount(ret, KnapsackType.RolePackConsume, ret.RolePackConsume.Count);
        AddPackCount(ret, KnapsackType.RolePackMaterial, ret.RolePackMaterial.Count);
        AddPackCount(ret, KnapsackType.RoleCurrtEquipPack, ret.RoleCurrtEquipPack.Count);

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 将物品放入对应背包；总背包只包含装备、消耗品和材料。
    /// </summary>
    private static void AddItemToPack(RoleKanpsackInfoRet ret, KnapsackType packType, RoleItemInfo itemInfo)
    {
        switch (packType)
        {
            case KnapsackType.RolePackEquip:
                ret.RolePackEquip.Add(itemInfo);
                ret.RolePackAll.Add(itemInfo);
                break;
            case KnapsackType.RolePackConsume:
                ret.RolePackConsume.Add(itemInfo);
                ret.RolePackAll.Add(itemInfo);
                break;
            case KnapsackType.RolePackMaterial:
                ret.RolePackMaterial.Add(itemInfo);
                ret.RolePackAll.Add(itemInfo);
                break;
            case KnapsackType.RoleCurrtEquipPack:
                ret.RoleCurrtEquipPack.Add(itemInfo);
                break;
        }
    }

    private static void AddPackCount(RoleKanpsackInfoRet ret, KnapsackType packType, int count)
    {
        ret.KanpsackTypeCountLst.Add(new Kanpsacktypecount
        {
            Type = (int)packType,
            Count = count
        });
    }
}
