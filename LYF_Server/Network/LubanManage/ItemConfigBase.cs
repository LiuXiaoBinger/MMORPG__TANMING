using cfg;

/// <summary>
/// 物品配置的统一基类，屏蔽普通物品表和武器表的字段差异。
/// </summary>
public abstract class ItemConfigBase
{
    public int ItemId { get; private set; }
    public ItemMainType ItemMainType { get; private set; }
    public int ItemSubType { get; private set; }
    public string ItemName { get; private set; }
    public string Description { get; private set; }
    public string Icon { get; private set; }
    public string Attribute { get; private set; }
    public int NeedLevel { get; private set; }
    public string Source { get; private set; }
    public int SellPrice { get; private set; }
    public int SellCurrencyType { get; private set; }
    public bool CanStack { get; private set; }

    protected ItemConfigBase(
        int itemId,
        ItemMainType itemMainType,
        int itemSubType,
        string itemName,
        string description,
        string icon,
        string attribute,
        int needLevel,
        string source,
        int sellPrice,
        int sellCurrencyType,
        bool canStack)
    {
        ItemId = itemId;
        ItemMainType = itemMainType;
        ItemSubType = itemSubType;
        ItemName = itemName;
        Description = description;
        Icon = icon;
        Attribute = attribute;
        NeedLevel = needLevel;
        Source = source;
        SellPrice = sellPrice;
        SellCurrencyType = sellCurrencyType;
        CanStack = canStack;
    }

    protected static ItemMainType NormalizeMainType(int value, ItemMainType fallback)
    {
        return value == (int)ItemMainType.Consumable || value == (int)ItemMainType.Weapon
            ? (ItemMainType)value
            : fallback;
    }

    protected static string GetFirstNonEmpty(string primary, string secondary, string fallback)
    {
        if (!string.IsNullOrEmpty(primary))
        {
            return primary;
        }

        if (!string.IsNullOrEmpty(secondary))
        {
            return secondary;
        }

        return fallback;
    }
}

/// <summary>
/// 普通物品表的统一配置适配对象。
/// </summary>
public sealed class NormalItemConfig : ItemConfigBase
{
    public ItemInfo Config { get; private set; }

    public NormalItemConfig(ItemInfo config)
        : base(
            config.ItemTypeId,
            NormalizeMainType(config.ItemMainType, ItemMainType.Consumable),
            config.ItemSubType,
            GetFirstNonEmpty(config.Name, config.Desc, $"物品 {config.ItemTypeId}"),
            config.Desc,
            config.Icon,
            config.Attribute,
            config.NeedLevel,
            config.Source,
            config.SellPrice,
            config.SellCurrencyType,
            config.CanStack != 0)
    {
        Config = config;
    }
}

/// <summary>
/// 武器表的统一配置适配对象。
/// </summary>
public sealed class WeaponItemConfig : ItemConfigBase
{
    public EquipInfo Config { get; private set; }

    public WeaponItemConfig(EquipInfo config)
        : base(
            config.ItemTypeId,
            NormalizeMainType(config.ItemMainType, ItemMainType.Weapon),
            config.ItemSubType,
            GetFirstNonEmpty(config.EquipName, config.Desc, $"武器 {config.ItemTypeId}"),
            GetFirstNonEmpty(config.Desc, config.Des, string.Empty),
            config.Icon,
            config.Attribute,
            config.NeedLevel,
            config.Source,
            config.SellPrice,
            config.SellCurrencyType,
            config.BCanBeStacked != 0)
    {
        Config = config;
    }
}
