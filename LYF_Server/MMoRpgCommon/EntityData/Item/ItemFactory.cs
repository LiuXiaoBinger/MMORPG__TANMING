/// <summary>
/// 物品类别。数值应与 Luban 中的物品类别定义保持一致。
/// </summary>
public enum ItemCategory
{
    Unknown = 0,
    NormalItem = 1,
    Equip = 2
}

/// <summary>
/// 物品类别查询接口。
/// </summary>
public interface IItemCategoryProvider
{
    ItemCategory GetCategory(int itemTypeID);
}

/// <summary>
/// Luban 物品类别查询占位实现。
/// TODO：增加 Luban 物品表后，在 GetCategory 中按 itemTypeID 查询实际类别。
/// </summary>
public sealed class LubanItemCategoryProvider : IItemCategoryProvider
{
    public ItemCategory GetCategory(int itemTypeID)
    {
        // 伪代码：var config = LubanMgr.Instance.GetItemInfo(itemTypeID);
        // return (ItemCategory)config.Category;
        return ItemCategory.NormalItem;
    }
}

/// <summary>
/// 根据物品配置类别创建运行时物品对象。
/// </summary>
public static class ItemFactory
{
    private static IItemCategoryProvider _categoryProvider = new LubanItemCategoryProvider();

    /// <summary>
    /// 替换物品类别查询实现，例如接入正式 Luban 表。
    /// </summary>
    public static void SetCategoryProvider(IItemCategoryProvider provider)
    {
        if (provider == null)
        {
            throw new System.ArgumentNullException(nameof(provider));
        }

        _categoryProvider = provider;
    }

    /// <summary>
    /// 根据物品配置 ID 创建物品对象。
    /// </summary>
    public static ItemBase Create(int itemID, int roleID, int itemTypeID,
        int bagType, int bagIndex, int count)
    {
        ItemCategory category = _categoryProvider.GetCategory(itemTypeID);
        ItemBase item;

        switch (category)
        {
            case ItemCategory.Equip:
                item = new EquipBase();
                break;
            case ItemCategory.NormalItem:
            case ItemCategory.Unknown:
            default:
                item = new ItemBase();
                break;
        }

        item.ItemID = itemID;
        item.RoleID = roleID;
        item.ItemTypeID = itemTypeID;
        item.BagType = bagType;
        item.BagIndex = bagIndex;
        item.Count = count;
        return item;
    }
}
