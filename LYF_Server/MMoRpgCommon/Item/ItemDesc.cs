

using System;

/// <summary>
/// 一次物品增删操作的完整描述，统一携带数量、绑定、价格和变化原因。
/// </summary>
public  class ItemDesc
{
    /// <summary>道具配置 ID。</summary>
    public int ItemID { get; set; }

    /// <summary>道具数量；扣除时使用正数表示扣除数量。</summary>
    public long ItemCount { get; set; }

    /// <summary>道具实例唯一 ID，堆叠道具可为 0。</summary>
    public ulong ItemUID { get; set; }

    /// <summary>发放道具时采用的绑定标记。</summary>
    public ItemBind BindOptionForGive { get; set; }

    /// <summary>扣除道具时允许匹配的绑定标记。</summary>
    public ItemBind BindOptionForTake { get; set; }

    /// <summary>本次物品的总价，通常为单价乘以数量。</summary>
    public long TotalPrice { get; set; }

    /// <summary>道具单价。</summary>
    public long Price { get; set; }

    /// <summary>购买或扣除使用的货币类型。</summary>
    public int MoneyType { get; set; }

    /// <summary>道具创建时间，使用 Unix 时间戳或服务端约定的时间值。</summary>
    public ulong CreateTime { get; set; }

    /// <summary>业务侧道具标记。</summary>
    public Int32 ItemSign { get; set; }

    /// <summary>道具附加数据。</summary>
    public ItemExtraData ExtraData { get; set; }

    /// <summary>本次物品变化原因。</summary>
    public ItemChangeReason ChangeReason { get; set; }

    public ItemDesc()
    {
        BindOptionForGive = ItemBind.kItemBindNo;
        BindOptionForTake = ItemBind.kItemBindIs;
        ExtraData = new ItemExtraData();
        ChangeReason = ItemChangeReason.ITEM_REASON_NONE;
    }

    public ItemDesc(
        int itemId,
        long itemCount,
        ItemBind bindOptionForGive = ItemBind.kItemBindNo,
        ItemBind bindOptionForTake = ItemBind.kItemBindIs)
        : this()
    {
        ItemID = itemId;
        ItemCount = itemCount;
        BindOptionForGive = bindOptionForGive;
        BindOptionForTake = bindOptionForTake;
    }

    public ItemDesc(int itemId, int itemCount, int totalPrice,  ItemBind bindOptionForGive = ItemBind.kItemBindNo,
        ItemBind bindOptionForTake = ItemBind.kItemBindIs)
    {
        ItemID = itemId;
        ItemCount = itemCount;
        TotalPrice =  totalPrice;
        Price = TotalPrice / ItemCount;
        BindOptionForGive = bindOptionForGive;
        BindOptionForTake = bindOptionForTake;
    }


    /// <summary>
    /// 输出便于日志检索的道具摘要。
    /// </summary>
    public  string ToString()
    {
        return string.Format(
            "道具id:{0},道具数量:{1},道具唯一id:{2},变化原因:{3}",
            ItemID,
            ItemCount,
            ItemUID,
            (int)ChangeReason);
    }
    
    
    /// <summary>
    /// 判断物品 ID 是否位于公共虚拟物品编号段。
    /// 这里只校验公共编号边界，具体物品是否存在以及所属背包由业务层读取 Luban 配置确认。
    /// </summary>
    /// <param name="itemId">物品配置 ID。</param>
    /// <returns>大于 0 且小于虚拟物品上界时返回 true。</returns>
    public static bool IsVirtualItem(int itemId)
    {
        return itemId > (int)ItemEnum.DEAFULT_CONFIG_ID && itemId < (int)ItemEnum.VIRTUAL_ITEM_MAX;
    }

    /// <summary>
    /// 判断物品 ID 是否符合虚拟物品公共编号规则。
    /// 不再逐项硬编码枚举成员，新增配置只要位于约定编号段即可被公共层识别。
    /// </summary>
    /// <param name="itemId">物品配置 ID。</param>
    /// <returns>符合虚拟物品编号范围返回 true，否则返回 false。</returns>
    public static bool IsLegalVirtualItem(int itemId)
    {
        return IsVirtualItem(itemId);
    }
}
