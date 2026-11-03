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
    /// <summary>返回给定物品配置ID对应的类别，供工厂选择普通物品或装备对象。</summary>
    ItemCategory GetCategory(int itemTypeID);
}

/// <summary>
/// Luban 物品类别查询占位实现。
/// TODO：增加 Luban 物品表后，在 GetCategory 中按 itemTypeID 查询实际类别。
/// </summary>
public sealed class LubanItemCategoryProvider : IItemCategoryProvider
{
    /// <summary>占位查询，当前不读取配置表，固定返回 NormalItem。</summary>
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
    // 当前类别来源；默认占位实现把所有配置都当普通物品，替换后才可按类别创建装备。
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
    /// <param name="itemUID">存档已有的正整数实例UID；此处不生成新UID。</param>
    /// <param name="roleID">实例归属角色。</param>
    /// <param name="itemID">物品配置ID。</param>
    /// <param name="bagType">存档记录的背包类型。</param>
    /// <param name="bagIndex">存档记录的格子下标。</param>
    /// <param name="count">恢复后的堆叠数量，允许为 0。</param>
    public static ItemBase Create(long itemUID, int roleID, int itemID,
        int bagType, int bagIndex, int count)
    {
        if (itemUID <= 0 || itemID <= 0 || count < 0)
        {
            throw new System.ArgumentOutOfRangeException("物品实例 UID、配置 ID 和数量必须合法。");
        }

        ItemCategory category = _categoryProvider.GetCategory(itemID);
        ItemBase item;

        switch (category)
        {
            case ItemCategory.Equip:
                item = EquipBase.CreateEquipForRestore();
                break;
            case ItemCategory.NormalItem:
            case ItemCategory.Unknown:
            default:
                item = ItemBase.CreateForRestore();
                break;
        }

        item.SetItemUID(itemUID);
        item.SetItemID(itemID);
        item.SetItemCount(count);
        item.SetLocation(roleID, bagType, bagIndex);
        return item;
    }
}
