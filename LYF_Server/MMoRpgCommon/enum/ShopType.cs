/// <summary>
/// 商店类型。
/// 商店类型来自 ShopTable.ShopType；购买协议只传递 ShopTable.Id。
/// </summary>
public enum ShopType
{
    /// <summary>
    /// NPC 商店，商品通过 ShopItemInfo.ShopId 关联商店。
    /// </summary>
    Npc = 1,

    /// <summary>
    /// 全局商城，暂未接入商品查询与购买事务。
    /// </summary>
    Mall = 2,

    /// <summary>
    /// 活动商店，暂未接入商品查询与购买事务。
    /// </summary>
    Activity = 3,
}

public enum PurchaseLimitType
{
    kPurchaseLimitTypeUnlimited = 0, // 不限购
    kPurchaseLimitTypeDay       = 1, // 每日
    kPurchaseLimitTypeWeek      = 2, // 每周
    kPurchaseLimitTypeForever   = 3, // 永久
};
/**
 * @brief  商店购买最终消耗类型
*/
public enum CostType
{
    kCostDefault = 0,           //默认消耗
    kCostSecond = 1,            //第二种消耗货币
};