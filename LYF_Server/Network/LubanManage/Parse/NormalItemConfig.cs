using cfg;

/// <summary>
/// 普通物品表的统一配置适配对象。
/// </summary>
public sealed class NormalItemConfig : ItemConfigBase
{
    public ItemInfo Config { get; private set; }

    public NormalItemConfig(ItemInfo config)
        : base(
            config.ItemTypeId,
            NormalizeMainType(config.ItemMainType, ItemMainType.ItemType_Consumables),
            config.ItemSubType,
            GetFirstNonEmpty(config.Name, config.Desc, $"物品 {config.ItemTypeId}"),
            config.Desc,
            config.Icon,
            config.Attribute,
            config.NeedLevel,
            config.Source,
            config.SellPrice,
            config.SellCurrencyType,
            config.MaxStackCount,
            NormalizePackType(config.PackType, KnapsackType.RolePackPlain, config.ItemTypeId),
            config.TimeType)
    {
        Config = config;
    }
}
