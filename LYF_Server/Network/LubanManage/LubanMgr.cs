

using System;
using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr:Singleton<LubanMgr>
{
    private Dictionary<int, SkillInfo> _skillInfos;
    /// <summary>按稳定效果 ID 索引的技能等级效果表，启动后只读。</summary>
    private Dictionary<int, SkillEffectInfo> _skillEffectInfos;
    /// <summary>技能 ID 到学习等级再到效果的双层索引，启动时检查组合键唯一。</summary>
    private Dictionary<int, Dictionary<int, SkillEffectInfo>> _skillEffectsByLevel;
    /// <summary>按表现配置 ID 索引的技能动画资源表，供客户端选择变体。</summary>
    private Dictionary<int, SkillPresentationInfo> _skillPresentationInfos;
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
    // 按表现配置 ID 索引的角色表现表，初始化后供运行时查询模型、动画和碰撞参数。
    private Dictionary<int, PresentTable> _presentTables;
    private Dictionary<int, ShopTable> _shopTables;
    private Dictionary<int, ShopItemInfo> _shopItemInfos;
    private Dictionary<int, CountInfo> _countInfos;
    // 外层按商店主表 ID 分组，内层按商店商品配置 ID 索引。
    private Dictionary<int, Dictionary<int, ShopItemParseInfo>> _shopItemParseInfos;

    /// <summary>
    /// 从 Luban 二进制配置文件加载并缓存运行时所需的全部配置表。
    /// </summary>
    public void Init()
    {
        // 服务部署时优先从输出目录读取全部表数据；旧 Unity 开发目录仍可回退读取。
        string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "LubanManage", "Tb");
        if (!File.Exists(Path.Combine(dataDirectory, "tbskilleffectinfo.bytes")))
        {
            dataDirectory = "D:/unitypro/LYFMMORGP/LYF_Server/Network/LubanManage/Tb";
        }
        // file 是 Luban 生成器传入的表名，不接受外部业务请求作为文件路径。
        Tables tables = new Tables((string file) => new Luban.ByteBuf(
            File.ReadAllBytes(Path.Combine(dataDirectory, file + ".bytes"))));

        _skillInfos = tables.TbSkillInfo.DataMap;
        _skillEffectInfos = tables.TbSkillEffectInfo.DataMap;
        _skillEffectsByLevel = BuildSkillEffectsByLevel(tables.TbSkillEffectInfo.DataList);
        _skillPresentationInfos = tables.TbSkillPresentationInfo.DataMap;
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
        // 直接复用 Luban 生成表的索引，避免启动后重复复制配置数据。
        _presentTables = tables.TbPresentTable.DataMap;
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

    /// <summary>建立等级效果索引并验证服务器权威技能参数。</summary>
    /// <param name="effectList">Luban 装载的全部技能等级效果。</param>
    /// <returns>按技能 ID 与正整数等级查找效果的索引。</returns>
    private Dictionary<int, Dictionary<int, SkillEffectInfo>> BuildSkillEffectsByLevel(
        List<SkillEffectInfo> effectList)
    {
        // 外层索引的每个元素对应旧 SkillInfo 中一个稳定技能 ID。
        Dictionary<int, Dictionary<int, SkillEffectInfo>> effectsBySkill =
            new Dictionary<int, Dictionary<int, SkillEffectInfo>>();
        for (int index = 0; index < effectList.Count; index++)
        {
            // 当前行描述一个技能在一个学习等级的唯一权威数值。
            SkillEffectInfo effect = effectList[index];
            // 效果行必须存在，且效果 ID 与学习等级为正整数。
            if (effect == null || effect.Id <= 0 || effect.Level <= 0)
            {
                throw new InvalidDataException("技能等级效果含无效服务端参数，效果 ID=" + effect.Id);
            }
            // 技能 ID 必须命中旧技能表索引，且目标模式不能为负。
            if (effect.SkillId <= 0 || !_skillInfos.ContainsKey(effect.SkillId) ||
                effect.TargetMode < 0)
            {
                throw new InvalidDataException("技能等级效果含无效服务端参数，效果 ID=" + effect.Id);
            }
            // 目标模式不得超过 3，冷却与分组冷却组 ID 不得为负。
            if (effect.TargetMode > 3 || effect.CooldownMs < 0 || effect.CooldownGroupId < 0)
            {
                throw new InvalidDataException("技能等级效果含无效服务端参数，效果 ID=" + effect.Id);
            }
            // 分组冷却、施法时长与恢复时长均不得为负。
            if (effect.GroupCooldownMs < 0 || effect.CastMs < 0 || effect.RecoveryMs < 0)
            {
                throw new InvalidDataException("技能等级效果含无效服务端参数，效果 ID=" + effect.Id);
            }
            // MP 消耗不得为负，且施法时长叠加恢复时长不得溢出 int 上限。
            if (effect.MpCost < 0 || effect.CastMs > int.MaxValue - effect.RecoveryMs)
            {
                throw new InvalidDataException("技能等级效果含无效服务端参数，效果 ID=" + effect.Id);
            }

            // 目标模式为无目标（0）时才需要校验命中数值是否误配。
            bool isNoTargetMode = effect.TargetMode == 0;
            // 射程类数值（最小射程/最大射程/半径）是否有任意一项非零。
            bool hasRangeValues = effect.MinRangeCm != 0 || effect.MaxRangeCm != 0 ||
                effect.RadiusCm != 0;
            // 目标数量与命中节奏数值（目标数/命中次数/命中间隔）是否有任意一项非零。
            bool hasTargetHitValues = effect.MaxTargets != 0 || effect.HitCount != 0 ||
                effect.HitIntervalMs != 0;
            // 伤害数值（万分比伤害/固定伤害）是否有任意一项非零。
            bool hasDamageValues = effect.DamagePermyriad != 0 || effect.FlatDamage != 0;
            // 上述命中数值任一非零即视为无目标技能误配。
            bool hasAnyHitValue = hasRangeValues || hasTargetHitValues || hasDamageValues;
            if (isNoTargetMode && hasAnyHitValue)
            {
                throw new InvalidDataException("无目标表现技能不能配置本期未实现的命中数值，效果 ID=" + effect.Id);
            }

            // 一个技能 ID 下每个学习等级只允许映射到一条效果配置。
            Dictionary<int, SkillEffectInfo> effectsByLevel;
            if (!effectsBySkill.TryGetValue(effect.SkillId, out effectsByLevel))
            {
                effectsByLevel = new Dictionary<int, SkillEffectInfo>();
                effectsBySkill.Add(effect.SkillId, effectsByLevel);
            }
            if (effectsByLevel.ContainsKey(effect.Level))
            {
                throw new InvalidDataException("技能等级效果重复，技能=" + effect.SkillId +
                    "，等级=" + effect.Level);
            }
            effectsByLevel.Add(effect.Level, effect);
        }

        return effectsBySkill;
    }

    /// <summary>按技能 ID 和角色已学等级读取唯一权威效果。</summary>
    /// <param name="skillId">旧 SkillInfo.Id 中的稳定技能 ID。</param>
    /// <param name="level">角色数据库保存的正整数学习等级。</param>
    /// <returns>匹配的技能等级效果，缺失时返回空。</returns>
    public SkillEffectInfo GetSkillEffectInfoBySkillAndLevel(int skillId, int level)
    {
        // 命中的内层索引只包含同一技能的等级效果。
        Dictionary<int, SkillEffectInfo> effectsByLevel;
        if (_skillEffectsByLevel != null &&
            _skillEffectsByLevel.TryGetValue(skillId, out effectsByLevel))
        {
            // 当前等级的效果实例由生成表持有，不应在业务层修改。
            SkillEffectInfo effect;
            if (effectsByLevel.TryGetValue(level, out effect))
            {
                return effect;
            }
        }
        return null;
    }

    /// <summary>返回效果 ID 到效果配置的只读使用索引，调用方不得修改集合。</summary>
    /// <returns>服务端启动时加载的技能效果集合。</returns>
    public Dictionary<int, SkillEffectInfo> GetSkillEffectInfos()
    {
        return _skillEffectInfos;
    }

    /// <summary>返回表现配置 ID 到动画变体的索引，调用方不得修改集合。</summary>
    /// <returns>服务端启动时加载的技能表现集合。</returns>
    public Dictionary<int, SkillPresentationInfo> GetSkillPresentationInfos()
    {
        return _skillPresentationInfos;
    }

    /// <summary>读取旧技能定义表，旧 CD/MP 字段仅供既有 UI 兼容。</summary>
    /// <returns>稳定技能 ID 到技能定义的索引，调用方不得修改集合。</returns>
    public Dictionary<int, SkillInfo> GetSkillInfos()
    {
        return _skillInfos;
    }

    /// <summary>按稳定技能 ID 查找旧技能定义，用于职业和类型校验。</summary>
    /// <param name="id">SkillInfo.Id 中的稳定技能 ID。</param>
    /// <returns>匹配的技能定义，未配置时为空。</returns>
    public SkillInfo GetSkillInfoById(int id)
    {
        if (_skillInfos!=null&&_skillInfos.ContainsKey(id))
        {
            return _skillInfos[id];
        }
        return null;
    }
    /// <summary>
    /// 按职业 ID 获取该职业的旧技能定义快照。
    /// </summary>
    /// <param name="jobid">角色职业配置 ID。</param>
    /// <returns>技能 ID 到定义的独立字典，调用方可筛选但不得修改原表。</returns>
    public Dictionary<int, SkillInfo> GetSkillInfosByJob(int jobid)
    {
        // 返回值是独立字典，避免调用方修改服务器加载的原始技能索引。
        Dictionary<int, SkillInfo> jobSkillInfos = new Dictionary<int, SkillInfo>();
        if (_skillInfos == null)
        {
            return jobSkillInfos;
        }
        // 字典先复制为可按整数索引遍历的快照，保留原 SkillInfo 引用。
        List<KeyValuePair<int, SkillInfo>> skillSnapshot =
            new List<KeyValuePair<int, SkillInfo>>(_skillInfos);
        for (int index = 0; index < skillSnapshot.Count; index++)
        {
            // 一条技能定义的职业 ID 决定是否包含在返回快照中。
            KeyValuePair<int, SkillInfo> skillEntry = skillSnapshot[index];
            if (skillEntry.Value.JobId == jobid)
            {
                jobSkillInfos.Add(skillEntry.Key, skillEntry.Value);
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

    #region 表现相关

    /// <summary>
    /// 获取全部角色表现配置，返回值为表现配置 ID 到配置对象的索引集合。
    /// </summary>
    /// <returns>角色表现配置字典；未初始化时返回 null。</returns>
    public Dictionary<int, PresentTable> GetPresentTables()
    {
        return _presentTables;
    }

    /// <summary>
    /// 根据表现配置 ID 获取角色表现配置。
    /// </summary>
    /// <param name="id">表现配置唯一 ID，来源于 PresentTable.Id。</param>
    /// <returns>匹配的表现配置；未初始化或 ID 不存在时返回 null。</returns>
    public PresentTable GetPresentTableById(int id)
    {
        // 保存字典查找到的表现配置，未命中时保持空引用并返回 null。
        if (_presentTables != null && _presentTables.TryGetValue(id, out PresentTable info))
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
