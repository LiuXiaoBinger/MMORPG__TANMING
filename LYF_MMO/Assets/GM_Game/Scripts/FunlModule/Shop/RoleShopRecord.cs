/// <summary>
/// 客户端角色商店记录组件，通过次数组件查询和刷新商品限购状态。
/// </summary>
public class RoleShopRecord : RoleComponentBase
{
    /// <summary>将商品购买记录绑定到 owner 角色，实际次数保存在 RoleCountComponent 中。</summary>
    public RoleShopRecord(PlayerEntity owner) : base(owner)
    {
    }

    /// <summary>按 goodsId 商品编号增加已购买数量 count；最终由次数组件检查上限。</summary>
    public void UseBuyTimes(int goodsId, uint count)
    {
        Owner.GetComponent<RoleCountComponent>().
            IncrCount(RoleCountAction.ShopPurchase, goodsId,count);
    }
    /// <summary>用 goodsId 读取已购数量，再减去 configPurchaseCondition 的上限；不限购返回 -1，超限返回 0。</summary>
    public long GetBuyLeftTimes(int goodsId, ShopItemPurchaseCondition configPurchaseCondition)
    {
        // 本条限购配置允许购买的总数量。
        long tableBuyCount = (int)configPurchaseCondition.Limit;
        // 该角色已记录的商品购买数量，初始按未购买处理。
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
