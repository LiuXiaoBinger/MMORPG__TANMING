

using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr:Singleton<LubanMgr>
{
    private Dictionary<int, SkillInfo> _skillInfos;
    private Dictionary<int, ItemInfo> _itemInfos;
    private Dictionary<int, EquipInfo> _equipInfos;
    private Dictionary<int, ItemConfigBase> _itemConfigInfos;
    private Dictionary<int, Gene> _geneInfos;
    private Dictionary<int, NpcParseInfo> _npcInfos;
    private Dictionary<int, ProfessionInfo> _professionInfos;
    private Dictionary<int, QuestInfo> _questInfos;
    private Dictionary<int, RoleLevelInfo> _roleLevelInfos;
    private Dictionary<int, RoleLevelAttributeInfo> _roleLevelAttributeInfos;
    private Dictionary<int, ShopInfo> _shopInfos;
    private Dictionary<int, WalletInfo> _walletInfos;
    public void Init()
    {
        Tables tables =new Tables((string file)=> new Luban.ByteBuf(File.ReadAllBytes(
            $"D:/unitypro/LYFMMORGP/LYF_Server/Network/LubanManage/Tb/{file}.bytes")));

        /*List<SkillInfo> lst = tables.TbSkillInfo.DataList;
        for (int i = 0; i < lst.Count; i++)
        {
            LogMsg.Info("Skill::"+lst[i].ToString());
        }*/
        _skillInfos = tables.TbSkillInfo.DataMap;
        _itemInfos = tables.TbItemInfo.DataMap;
        _equipInfos = tables.TbEquipInfo.DataMap;
        _itemConfigInfos = CreateItemConfigInfos(_itemInfos, _equipInfos);
        _geneInfos = tables.TbGene.DataMap;
        _npcInfos = ParseNpcInfos(tables.TbNpcInfo.DataList);
        _professionInfos = tables.TbProfessionInfo.DataMap;
        _questInfos = tables.TbQuestInfo.DataMap;
        _roleLevelInfos = tables.TbRoleLevelInfo.DataMap;
        _roleLevelAttributeInfos = tables.TbRoleLevelAttributeInfo.DataMap;
        _shopInfos = tables.TbShopInfo.DataMap;
        _walletInfos = tables.TbWalletInfo.DataMap;

        PrintTableData(tables);
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

        LogMsg.Info("Luban TbShopInfo count: " + tables.TbShopInfo.DataList.Count);
        foreach (ShopInfo info in tables.TbShopInfo.DataList)
        {
            LogMsg.Info("TbShopInfo: " + info);
        }

        LogMsg.Info("Luban TbWalletInfo count: " + tables.TbWalletInfo.DataList.Count);
        foreach (WalletInfo info in tables.TbWalletInfo.DataList)
        {
            LogMsg.Info("TbWalletInfo: " + info);
        }

        LogMsg.Info("Luban TbSkillInfo count: " + tables.TbSkillInfo.DataList.Count);
        foreach (SkillInfo info in tables.TbSkillInfo.DataList)
        {
            LogMsg.Info("TbSkillInfo: " + info);
        }
    }

    /// <summary>
    /// 将 NPC 的字符串坐标和商品限购配置转换为运行时结构化数据。
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
                AddItemConfig(itemConfigInfos, new WeaponItemConfig(equipInfo));
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
        return _itemInfos != null && _itemInfos.TryGetValue(id, out ItemInfo info) ? info : null;
    }

    /// <summary>
    /// 获取普通物品表和武器表汇总后的统一物品配置。
    /// </summary>
    public ItemConfigBase GetItemConfigById(int id)
    {
        return _itemConfigInfos != null && _itemConfigInfos.TryGetValue(id, out ItemConfigBase info)
            ? info
            : null;
    }

    public Dictionary<int, ItemConfigBase> GetItemConfigInfos()
    {
        return _itemConfigInfos;
    }

    #endregion

    #region 装备相关

    public Dictionary<int, EquipInfo> GetEquipInfos()
    {
        return _equipInfos;
    }

    public EquipInfo GetEquipInfoById(int id)
    {
        return _equipInfos != null && _equipInfos.TryGetValue(id, out EquipInfo info) ? info : null;
    }

    #endregion

    #region 基因相关

    public Dictionary<int, Gene> GetGeneInfos()
    {
        return _geneInfos;
    }

    public Gene GetGeneInfoById(int id)
    {
        return _geneInfos != null && _geneInfos.TryGetValue(id, out Gene info) ? info : null;
    }

    #endregion

    #region NPC 相关

    public Dictionary<int, NpcParseInfo> GetNpcInfos()
    {
        return _npcInfos;
    }

    public NpcParseInfo GetNpcInfoById(int id)
    {
        return _npcInfos != null && _npcInfos.TryGetValue(id, out NpcParseInfo info) ? info : null;
    }

    #endregion

    #region 职业相关

    public Dictionary<int, ProfessionInfo> GetProfessionInfos()
    {
        return _professionInfos;
    }

    public ProfessionInfo GetProfessionInfoById(int id)
    {
        return _professionInfos != null && _professionInfos.TryGetValue(id, out ProfessionInfo info) ? info : null;
    }

    #endregion

    #region 任务相关

    public Dictionary<int, QuestInfo> GetQuestInfos()
    {
        return _questInfos;
    }

    public QuestInfo GetQuestInfoById(int id)
    {
        return _questInfos != null && _questInfos.TryGetValue(id, out QuestInfo info) ? info : null;
    }

    #endregion

    #region 角色等级相关

    public Dictionary<int, RoleLevelInfo> GetRoleLevelInfos()
    {
        return _roleLevelInfos;
    }

    public RoleLevelInfo GetRoleLevelInfoById(int id)
    {
        return _roleLevelInfos != null && _roleLevelInfos.TryGetValue(id, out RoleLevelInfo info) ? info : null;
    }

    public Dictionary<int, RoleLevelAttributeInfo> GetRoleLevelAttributeInfos()
    {
        return _roleLevelAttributeInfos;
    }

    public RoleLevelAttributeInfo GetRoleLevelAttributeInfoById(int id)
    {
        return _roleLevelAttributeInfos != null && _roleLevelAttributeInfos.TryGetValue(id, out RoleLevelAttributeInfo info) ? info : null;
    }

    #endregion

    #region 商店相关

    public Dictionary<int, ShopInfo> GetShopInfos()
    {
        return _shopInfos;
    }

    public ShopInfo GetShopInfoById(int id)
    {
        return _shopInfos != null && _shopInfos.TryGetValue(id, out ShopInfo info) ? info : null;
    }

    #endregion

    #region 货币相关

    public Dictionary<int, WalletInfo> GetWalletInfos()
    {
        return _walletInfos;
    }

    public WalletInfo GetWalletInfoById(int id)
    {
        return _walletInfos != null && _walletInfos.TryGetValue(id, out WalletInfo info) ? info : null;
    }

    #endregion
}
