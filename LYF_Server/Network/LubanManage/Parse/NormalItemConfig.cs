using cfg;

/// <summary>
/// 普通物品表的统一配置适配对象。
/// </summary>
public sealed class NormalItemConfig : ItemConfigBase
{
    /// <summary>保留原始普通物品配置，供通用字段之外的业务读取。</summary>
    public ItemInfo Config { get; private set; }

    /// <summary>把普通物品表一行转换为通用配置；名称缺失时用描述或物品ID，背包类型无效时回退普通背包。</summary>
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
