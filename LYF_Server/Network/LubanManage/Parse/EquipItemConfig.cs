using cfg;

/// <summary>
/// 武器表的统一配置适配对象。
/// </summary>
public sealed class EquipItemConfig : ItemConfigBase
{
    /// <summary>保留原始装备配置，发放装备时从中读取 EquipType 等专用字段。</summary>
    public EquipInfo Config { get; private set; }

    /// <summary>把装备表一行转换为通用物品配置；名称缺失时用描述或ID，背包类型无效时回退装备背包。</summary>
    public EquipItemConfig(EquipInfo config)
        : base(
            config.ItemTypeId,
            NormalizeMainType(config.ItemMainType, ItemMainType.ItemType_Equip),
            config.ItemSubType,
            GetFirstNonEmpty(config.EquipName, config.Des, $"武器 {config.ItemTypeId}"),
            config.Des,
            config.Icon,
            config.Attribute,
            config.NeedLevel,
            config.Source,
            config.SellPrice,
            config.SellCurrencyType,
            config.MaxStackCount,
            NormalizePackType(config.PackType, KnapsackType.RolePackEquip, config.ItemTypeId),
            config.TimeType)
    {
        Config = config;
    }
}
