/// <summary>
/// 客户端角色商店记录组件，通过次数组件查询和刷新商品限购状态。
/// </summary>
public class RoleShopRecord : RoleComponentBase
{
    /// <summary>创建商店记录组件。</summary>
    public RoleShopRecord(ClientRole owner) : base(owner)
    {
    }

    /// <summary>在服务器确认购买成功后增加客户端已购买次数。</summary>
    public void UseBuyTimes(int goodsId, uint count)
    {
        Owner.GetComponent<RoleCountComponent>().
            IncrCount(RoleCountAction.ShopPurchase, goodsId,count);
    }
    /// <summary>查询指定商品当前剩余购买次数。</summary>
    public long GetBuyLeftTimes(int goodsId, ShopItemPurchaseCondition configPurchaseCondition)
    {
        long tableBuyCount = (int)configPurchaseCondition.Limit;
        long alreadyBuyCount = 0;

        switch ((PurchaseLimitType)configPurchaseCondition.Action)
        {
            case PurchaseLimitType.kPurchaseLimitTypeDay:
            case PurchaseLimitType.kPurchaseLimitTypeWeek:
            case PurchaseLimitType.kPurchaseLimitTypeForever:
                alreadyBuyCount = Owner.GetComponent<RoleCountComponent>().
                    GetCount(RoleCountAction.ShopPurchase, goodsId);
                break;
            case PurchaseLimitType.kPurchaseLimitTypeUnlimited:
                return -1;
            default:
                break;
        }
        
        if (tableBuyCount >= alreadyBuyCount)
        {
            return tableBuyCount - alreadyBuyCount;
        }
        LogMsg.Info("已购买次数超过配置上限，已购买=" + alreadyBuyCount +
            "，配置上限=" + tableBuyCount, LogMsgType.Warn);
        return 0;
    }
}
