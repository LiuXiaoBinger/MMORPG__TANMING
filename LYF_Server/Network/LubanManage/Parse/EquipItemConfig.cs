using cfg;

/// <summary>
/// 武器表的统一配置适配对象。
/// </summary>
public sealed class EquipItemConfig : ItemConfigBase
{
    public EquipInfo Config { get; private set; }

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
