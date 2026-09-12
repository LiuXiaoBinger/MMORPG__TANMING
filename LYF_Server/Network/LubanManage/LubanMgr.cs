

using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr:Singleton<LubanMgr>
{
    private Dictionary<int, SkillInfo> _skillInfos;
    private Dictionary<int, ItemInfo> _itemInfos;
    private Dictionary<int, EquipInfo> _equipInfos;
    private Dictionary<int, ItemConfigBase> _itemConfigInfos;
    private Dictionary<int, KnapsackInfo> _knapsackInfos;
    private Dictionary<int, Gene> _geneInfos;
    private Dictionary<int, NpcParseInfo> _npcInfos;
    private Dictionary<int, ProfessionInfo> _professionInfos;
    private Dictionary<int, QuestInfo> _questInfos;
    private Dictionary<int, RoleLevelInfo> _roleLevelInfos;
    private Dictionary<int, RoleLevelAttributeInfo> _roleLevelAttributeInfos;
    private Dictionary<int, ShopTable> _shopTables;
    private Dictionary<int, ShopItemInfo> _shopItemInfos;
    private Dictionary<int, CountInfo> _countInfos;
    // 外层按商店主表 ID 分组，内层按商店商品配置 ID 索引。
    private Dictionary<int, Dictionary<int, ShopItemParseInfo>> _shopItemParseInfos;
    public void Init()
    {
        Tables tables =new Tables((string file)=> new Luban.ByteBuf(File.ReadAllBytes(
            $"D:/unitypro/LYFMMORGP/LYF_Server/Network/LubanManage/Tb/{file}.bytes")));

        _skillInfos = tables.TbSkillInfo.DataMap;
        _itemInfos = tables.TbItemInfo.DataMap;
        _equipInfos = tables.TbEquipInfo.DataMap;
        _itemConfigInfos = CreateItemConfigInfos(_itemInfos, _equipInfos);
        _knapsackInfos = tables.TbKnapsackInfo.DataMap;
        _geneInfos = tables.TbGene.DataMap;
        _npcInfos = ParseNpcInfos(tables.TbNpcInfo.DataList);
        _professionInfos = tables.TbProfessionInfo.DataMap;
        _questInfos = tables.TbQuestInfo.DataMap;
        _roleLevelInfos = tables.TbRoleLevelInfo.DataMap;
        _roleLevelAttributeInfos = tables.TbRoleLevelAttributeInfo.DataMap;
        _shopTables = tables.TbShopTable.DataMap;
        _shopItemInfos = tables.TbShopItemInfo.DataMap;
        _countInfos = tables.TbCountInfo.DataMap;
        _shopItemParseInfos = ParseShopItemInfos(tables.TbShopItemInfo.DataList);

        //PrintTableData(tables);
    }

    /// <summary>
    /// 输出所有 Luban 表的数据，便于启动时检查配置是否正确加载。
    /// </summary>
    private static void PrintTableData(Tables tables)
    {
        LogMsg.Info("Luban TbEquipInfo count: " + tables.TbEquipInfo.DataList.Count);
        foreach (EquipInfo info in tables.TbEquipInfo.DataList)
        {
            LogMsg.Info("TbEquipInfo: " + info);
        }

        LogMsg.Info("Luban TbGene count: " + tables.TbGene.DataList.Count);
        foreach (Gene info in tables.TbGene.DataList)
        {
            LogMsg.Info("TbGene: " + info);
        }

        LogMsg.Info("Luban TbItemInfo count: " + tables.TbItemInfo.DataList.Count);
        foreach (ItemInfo info in tables.TbItemInfo.DataList)
        {
            LogMsg.Info("TbItemInfo: " + info);
        }

        LogMsg.Info("Luban TbKnapsackInfo count: " + tables.TbKnapsackInfo.DataList.Count);
        foreach (KnapsackInfo info in tables.TbKnapsackInfo.DataList)
        {
            LogMsg.Info("TbKnapsackInfo: " + info);
        }

        LogMsg.Info("Luban TbNpcInfo count: " + tables.TbNpcInfo.DataList.Count);
        foreach (NpcInfo info in tables.TbNpcInfo.DataList)
        {
            LogMsg.Info("TbNpcInfo: " + info);
        }

        LogMsg.Info("Luban TbProfessionInfo count: " + tables.TbProfessionInfo.DataList.Count);
        foreach (ProfessionInfo info in tables.TbProfessionInfo.DataList)
        {
            LogMsg.Info("TbProfessionInfo: " + info);
        }

        LogMsg.Info("Luban TbQuestInfo count: " + tables.TbQuestInfo.DataList.Count);
        foreach (QuestInfo info in tables.TbQuestInfo.DataList)
        {
            LogMsg.Info("TbQuestInfo: " + info);
        }

        LogMsg.Info("Luban TbRoleLevelInfo count: " + tables.TbRoleLevelInfo.DataList.Count);
        foreach (RoleLevelInfo info in tables.TbRoleLevelInfo.DataList)
        {
            LogMsg.Info("TbRoleLevelInfo: " + info);
        }

        LogMsg.Info("Luban TbRoleLevelAttributeInfo count: " + tables.TbRoleLevelAttributeInfo.DataList.Count);
        foreach (RoleLevelAttributeInfo info in tables.TbRoleLevelAttributeInfo.DataList)
        {
            LogMsg.Info("TbRoleLevelAttributeInfo: " + info);
        }

        LogMsg.Info("Luban TbShopTable count: " + tables.TbShopTable.DataList.Count);
        foreach (ShopTable info in tables.TbShopTable.DataList)
        {
            LogMsg.Info("TbShopTable: " + info);
        }

        LogMsg.Info("Luban TbShopItemInfo count: " + tables.TbShopItemInfo.DataList.Count);
        foreach (ShopItemInfo info in tables.TbShopItemInfo.DataList)
        {
            LogMsg.Info("TbShopItemInfo: " + info);
        }

        LogMsg.Info("Luban TbSkillInfo count: " + tables.TbSkillInfo.DataList.Count);
        foreach (SkillInfo info in tables.TbSkillInfo.DataList)
        {
            LogMsg.Info("TbSkillInfo: " + info);
        }
    }

    /// <summary>
    /// 将 NPC 的字符串坐标和关联商店 ID 转换为运行时结构化数据。
    /// </summary>
    private static Dictionary<int, NpcParseInfo> ParseNpcInfos(List<NpcInfo> npcInfoList)
    {
        Dictionary<int, NpcParseInfo> npcInfos = new Dictionary<int, NpcParseInfo>();
        foreach (NpcInfo npcInfo in npcInfoList)
        {
            NpcParseInfo parseInfo = NpcParseInfo.Create(npcInfo);
            if (parseInfo != null)
            {
                npcInfos[parseInfo.ID] = parseInfo;
            }
        }

        return npcInfos;
    }

    /// <summary>
    /// 将商店商品表转换为包含限购、环境限制和购买消耗的运行时数据。
    /// </summary>
    private static Dictionary<int, Dictionary<int, ShopItemParseInfo>> ParseShopItemInfos(List<ShopItemInfo> shopItemInfoList)
    {
        Dictionary<int, Dictionary<int, ShopItemParseInfo>> shopItemInfos =
            new Dictionary<int, Dictionary<int, ShopItemParseInfo>>();
        foreach (ShopItemInfo shopItemInfo in shopItemInfoList)
        {
            ShopItemParseInfo parseInfo = ShopItemParseInfo.Create(shopItemInfo);
            if (parseInfo != null)
            {
                if (!shopItemInfos.TryGetValue(parseInfo.ShopID, out Dictionary<int, ShopItemParseInfo> itemsById))
                {
                    itemsById = new Dictionary<int, ShopItemParseInfo>();
                    shopItemInfos.Add(parseInfo.ShopID, itemsById);
                }

                itemsById[parseInfo.ShopItemID] = parseInfo;
            }
        }

        return shopItemInfos;
    }


    #region 技能相关

    public Dictionary<int, SkillInfo> GetSkillInfos()
    {
        return _skillInfos;
    }

    public SkillInfo GetSkillInfoById(int id)
    {
        if (_skillInfos!=null&&_skillInfos.ContainsKey(id))
        {
            return _skillInfos[id];
        }
        return null;
    }
    /// <summary>
    /// 通过职业id获取技能信息
    /// </summary>
    /// <param name="jobid"></param>
    /// <returns></returns>
    public Dictionary<int, SkillInfo> GetSkillInfosByJob(int jobid)
    {
        Dictionary<int, SkillInfo> jobSkillInfos = new Dictionary<int, SkillInfo>();
        foreach (var ite in _skillInfos)
        {
            if (ite.Value.JobId == jobid)
            {
                jobSkillInfos.Add(ite.Key, ite.Value);
            }
        }

        return jobSkillInfos;
    }

    #endregion

    #region 物品相关

    /// <summary>
    /// 将普通物品和武器配置汇总为统一的物品配置集合。
    /// 同一个配置 ID 只能属于一个物品大类，否则商城无法仅凭 ItemId 判断商品类型。
    /// </summary>
    private static Dictionary<int, ItemConfigBase> CreateItemConfigInfos(
        Dictionary<int, ItemInfo> itemInfos,
        Dictionary<int, EquipInfo> equipInfos)
    {
        Dictionary<int, ItemConfigBase> itemConfigInfos = new Dictionary<int, ItemConfigBase>();

        if (itemInfos != null)
        {
            foreach (ItemInfo itemInfo in itemInfos.Values)
            {
                AddItemConfig(itemConfigInfos, new NormalItemConfig(itemInfo));
            }
        }

        if (equipInfos != null)
        {
            foreach (EquipInfo equipInfo in equipInfos.Values)
            {
                AddItemConfig(itemConfigInfos, new EquipItemConfig(equipInfo));
            }
        }

        return itemConfigInfos;
    }

    private static void AddItemConfig(Dictionary<int, ItemConfigBase> itemConfigInfos, ItemConfigBase itemConfig)
    {
        if (itemConfigInfos.TryGetValue(itemConfig.ItemId, out ItemConfigBase existingConfig))
        {
            LogMsg.Info(
                $"物品配置 ID 重复：{itemConfig.ItemId}。已使用后加入的配置类型 {itemConfig.ItemMainType} 覆盖 {existingConfig.ItemMainType}。",
                LogMsgType.Warn);
        }

        // 装备表在普通物品表之后加入，因此同 ID 时装备配置优先，避免商城把武器识别成普通物品。
        itemConfigInfos[itemConfig.ItemId] = itemConfig;
    }

    public Dictionary<int, ItemInfo> GetItemInfos()
    {
        return _itemInfos;
    }

    public ItemInfo GetItemInfoById(int id)
    {
        if (_itemInfos != null && _itemInfos.TryGetValue(id, out ItemInfo info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 获取普通物品表和武器表汇总后的统一物品配置。
    /// </summary>
    public ItemConfigBase GetItemConfigById(int id)
    {
        if (_itemConfigInfos != null && _itemConfigInfos.TryGetValue(id, out ItemConfigBase info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 根据物品配置 ID 获取所属背包类别；未找到配置时返回 null，避免与 RolePackPlain 的零值混淆。
    /// </summary>
    public KnapsackType? GetItemPackTypeById(int itemId)
    {
        ItemConfigBase itemConfig = GetItemConfigById(itemId);
        if (itemConfig == null)
        {
            return null;
        }
        return itemConfig.PackType;
    }

    public Dictionary<int, ItemConfigBase> GetItemConfigInfos()
    {
        return _itemConfigInfos;
    }

    #endregion

    #region 背包相关

    /// <summary>
    /// 获取背包类型配置表。
    /// </summary>
    public Dictionary<int, KnapsackInfo> GetKnapsackInfos()
    {
        return _knapsackInfos;
    }

    /// <summary>
    /// 根据背包类型获取背包配置。
    /// </summary>
    public KnapsackInfo GetKnapsackInfoByType(int knapsackType)
    {
        return _knapsackInfos != null && _knapsackInfos.TryGetValue(knapsackType, out KnapsackInfo info)
            ? info
            : null;
    }

    #endregion

    #region 装备相关

    public Dictionary<int, EquipInfo> GetEquipInfos()
    {
        return _equipInfos;
    }

    public EquipInfo GetEquipInfoById(int id)
    {
        if (_equipInfos != null && _equipInfos.TryGetValue(id, out EquipInfo info))
        {
            return info;
        }
        return null;
    }

    #endregion

    #region 基因相关

    public Dictionary<int, Gene> GetGeneInfos()
    {
        return _geneInfos;
    }

    public Gene GetGeneInfoById(int id)
    {
        if (_geneInfos != null && _geneInfos.TryGetValue(id, out Gene info))
        {
            return info;
        }
        return null;
    }

    #endregion

    #region NPC 相关

    public Dictionary<int, NpcParseInfo> GetNpcInfos()
    {
        return _npcInfos;
    }

    public NpcParseInfo GetNpcInfoById(int id)
    {
        if (_npcInfos != null && _npcInfos.TryGetValue(id, out NpcParseInfo info))
        {
            return info;
        }
        return null;
    }

    #endregion

    #region 职业相关

    public Dictionary<int, ProfessionInfo> GetProfessionInfos()
    {
        return _professionInfos;
    }

    public ProfessionInfo GetProfessionInfoById(int id)
    {
        if (_professionInfos != null && _professionInfos.TryGetValue(id, out ProfessionInfo info))
        {
            return info;
        }
        return null;
    }

    #endregion

    #region 任务相关

    public Dictionary<int, QuestInfo> GetQuestInfos()
    {
        return _questInfos;
    }

    public QuestInfo GetQuestInfoById(int id)
    {
        if (_questInfos != null && _questInfos.TryGetValue(id, out QuestInfo info))
        {
            return info;
        }
        return null;
    }

    #endregion

    #region 角色等级相关

    public Dictionary<int, RoleLevelInfo> GetRoleLevelInfos()
    {
        return _roleLevelInfos;
    }

    public RoleLevelInfo GetRoleLevelInfoById(int id)
    {
        if (_roleLevelInfos != null && _roleLevelInfos.TryGetValue(id, out RoleLevelInfo info))
        {
            return info;
        }
        return null;
    }

    public Dictionary<int, RoleLevelAttributeInfo> GetRoleLevelAttributeInfos()
    {
        return _roleLevelAttributeInfos;
    }

    public RoleLevelAttributeInfo GetRoleLevelAttributeInfoById(int id)
    {
        if (_roleLevelAttributeInfos != null && _roleLevelAttributeInfos.TryGetValue(id, out RoleLevelAttributeInfo info))
        {
            return info;
        }
        return null;
    }

    #endregion

    #region 商店相关

    /// <summary>按行为和业务 ID 查找次数配置。</summary>
    public CountInfo GetCountInfo(RoleCountAction action, int key)
    {
        if (_countInfos == null) return null;
        foreach (CountInfo info in _countInfos.Values)
        {
            if (info.Action == (int)action && info.GoodsId == key && info.ActiveRefresh >= 0) return info;
        }
        return null;
    }

    /// <summary>
    /// 获取商店主表。NPC 通过 NpcParseInfo.ShopId 关联此表。
    /// </summary>
    public Dictionary<int, ShopTable> GetShopTables()
    {
        return _shopTables;
    }

    /// <summary>
    /// 根据商店 ID 获取商店主表。
    /// </summary>
    public ShopTable GetShopTableById(int id)
    {
        if (_shopTables != null && _shopTables.TryGetValue(id, out ShopTable info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 获取商店商品明细表。商品归属由 ShopItemInfo.ShopId 判定。
    /// </summary>
    public Dictionary<int, ShopItemInfo> GetShopItemInfos()
    {
        return _shopItemInfos;
    }

    /// <summary>
    /// 根据商品配置 ID 获取商店商品明细。
    /// </summary>
    public ShopItemInfo GetShopItemInfoById(int id)
    {
        if (_shopItemInfos != null && _shopItemInfos.TryGetValue(id, out ShopItemInfo info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 根据商店主表 ID 获取该商店下的全部商品配置。
    /// ShopItemInfo.GoodsId 是商品配置 ID，ShopItemInfo.ShopId 才是所属商店 ID。
    /// </summary>
    public Dictionary<int, ShopItemInfo> GetShopItemInfosByShopId(int shopId)
    {
        Dictionary<int, ShopItemInfo> shopItemInfos = new Dictionary<int, ShopItemInfo>();
        if (shopId <= 0 || _shopItemInfos == null)
        {
            return shopItemInfos;
        }

        foreach (KeyValuePair<int, ShopItemInfo> pair in _shopItemInfos)
        {
            if (pair.Value != null && pair.Value.ShopId == shopId)
            {
                shopItemInfos.Add(pair.Key, pair.Value);
            }
        }

        return shopItemInfos;
    }

    /// <summary>
    /// 获取按商店主表 ID 分组的解析商品配置集合。
    /// </summary>
    public Dictionary<int, Dictionary<int, ShopItemParseInfo>> GetShopItemParseInfos()
    {
        return _shopItemParseInfos;
    }

    /// <summary>
    /// 根据商品配置 ID 获取解析后的商店商品配置。
    /// </summary>
    public ShopItemParseInfo GetShopItemParseInfoById(int id)
    {
        if (_shopItemParseInfos == null)
        {
            return null;
        }

        foreach (Dictionary<int, ShopItemParseInfo> itemsById in _shopItemParseInfos.Values)
        {
            if (itemsById.TryGetValue(id, out ShopItemParseInfo info))
            {
                return info;
            }
        }

        return null;
    }

    /// <summary>
    /// 根据商店主表 ID 获取解析后的全部商品配置。
    /// </summary>
    public Dictionary<int, ShopItemParseInfo> GetShopItemParseInfosByShopId(int shopId)
    {
        if (shopId <= 0 || _shopItemParseInfos == null)
        {
            return new Dictionary<int, ShopItemParseInfo>();
        }

        if (_shopItemParseInfos.TryGetValue(shopId, out Dictionary<int, ShopItemParseInfo> shopItemInfos))
        {
            return shopItemInfos;
        }
        return null;
    }

    #endregion

}
