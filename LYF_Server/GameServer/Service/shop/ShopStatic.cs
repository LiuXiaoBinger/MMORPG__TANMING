//商店类型通过商店类型保存在shopmgr中

using System.Collections.Generic;
using cfg;

public class ShopStatic
{
    private  ShopType type;
    //商店的基础数据
    private ShopTable  shopDate;
    //商店的购买条件
    private ShopCondition _shopCondition;
    private Dictionary<int, ShopItemParseInfo> _shop_items; 
    ShopStatic(ShopTable  static_shops)
    {
        shopDate = static_shops;
       

        //商店的商品
        LoadShopItemInfos();
        _shopCondition = new ShopCondition(this);
    }

    public ShopItemBuyCost GetCommoditCostInfo(int GoodsId)
    {
        _shop_items.TryGetValue(GoodsId, out ShopItemParseInfo shopItemInfo);
        if (shopItemInfo!=null)
        {
            return shopItemInfo.Cost;
            
        } 
        ShopItemBuyCost default_cost =new ShopItemBuyCost();
        return default_cost;
    }
    private void LoadShopItemInfos()
    {
        _shop_items = new Dictionary<int, ShopItemParseInfo>();
        _shop_items = LubanMgr.Instance.GetShopItemParseInfosByShopId(shopDate.Id);
    }
    
    public static ShopStatic CreateStaticShop(ShopTable shopTable)
    {
        ShopStatic shopStatic = new ShopStatic(shopTable);
        
        return shopStatic;
    }
    
    public ShopType GetType() 
    {
        return type;
    }

    ShopItemParseInfo GetShopItemInfo(int id)
    {
        if (_shop_items.ContainsKey(id))
        {
            return _shop_items[id];
        }
        return null;
    }
    //卖
    //public CmdCode SellItems(OnlineRole role, const KKSG::SellShopItemArg &args);
    //public CmdCode SellAfterCheck(OnlineRole role, const std::vector<ItemDesc> &items);
    // 买
    public CmdCode BuyItems(OnlineRole role, BuyShopItemReq request)
    {
        CmdCode code = CmdCode.Succeed;
        CostType real_cost_type = CostType.kCostDefault;
        code = _shopCondition.CheckBuy(role, request,real_cost_type);
        if (code != CmdCode.Succeed) { return code; }
        
        BagGiveItemTransition give_trans = new BagGiveItemTransition(role);
        give_trans.SetReason(ItemChangeReason.ITEM_REASON_BUY_ITEM);
        BagTakeItemTransition take_trans= new BagTakeItemTransition(role);
        take_trans.SetReason(ItemChangeReason.ITEM_REASON_BUY_ITEM);
        for (int index = 0; index < request.Items.Count; index++)
        {
            if (request.Items[index].BuyCount <= 0)
            {
                continue;
            }

            ShopItemParseInfo shopItemInfo = GetShopItemInfo((int)request.Items[index].GoodsId);
            if (shopItemInfo == null) continue;

            //一个商品的消耗数量
            var cost_info = shopItemInfo.Cost;
            //所消耗的钱
            int cost_money = cost_info.buy_cost.cost_money * (int)request.Items[index].BuyCount;
            if (cost_money < 0)
            {
                continue;
            }
            //todo 折扣
            //给予item给到背包 
            code = give_trans.GiveItem(new ItemDesc(shopItemInfo.ItemID, 
                (int)request.Items[index].BuyCount,cost_money, (ItemBind)(int)shopItemInfo.IsBind));
            
            //先给后面在扣除兑换的钱或者物品
            take_trans.TakeItem(new ItemDesc(cost_info.buy_cost.money_type, cost_money));

            if (!shopItemInfo.IsUnlimitedPurchase())
            {
                role.GetComponent<RoleShopRecord>().UseBuyTimes(shopItemInfo.ShopItemID, request.Items[index].BuyCount);
            }
            
        }
        take_trans.NotifyClient();
        give_trans.NotifyClient();
        return code;
    }
}
