using System.Collections.Generic;
using Google.Protobuf.Collections;

public class ShopCondition
{
    private ShopStatic _host_shop;

    public ShopCondition(ShopStatic shop)
    {
        _host_shop =  shop;
    }
    public CmdCode CheckBuy(OnlineRole role, BuyShopItemReq request, CostType realCostType)
    {   
        CmdCode error_info;
        // 交易合法性
        error_info  = CheckLegalityBeforeBuy(role, request);
        /*if (code != KKSG::ERR_SUCCESS)
        {
            error_info.set_errorno(code);
            return error_info;
        }*/

        // 购买限制
        error_info = CheckRoleQualificationBeforeBuy(role, request);
        if (error_info != CmdCode.Succeed)
        {
            return error_info;
        }

        

        // 钱币和背包容量充足
        error_info = CheckCostAndBagCapcity(role, request, realCostType);
        return error_info;
        //todo
        return CmdCode.Succeed;
    }

    private CmdCode CheckCostAndBagCapcity(OnlineRole role, BuyShopItemReq 
        request, CostType realCostType)
    {
        CmdCode code = CmdCode.Succeed;
        if (role == null)
        {
            return CmdCode.RoleNotExist;
        }
        List<ItemDesc> things_to_buy = new List<ItemDesc>();
        List<ItemDesc> total_cost = new List<ItemDesc>();
        List<ItemDesc>total_cost2 = new List<ItemDesc>();
        RepeatedField < BuyShopItem > items= request.Items;
        for (int i = 0; i < request.Items.Count; ++i)
        {
            var shop_conf = LubanMgr.Instance.GetShopItemParseInfoById((int)items[i].GoodsId);
            if (shop_conf == null)
            {
                LogMsg.Info($"config not found , shop table id is : [{items[i].GoodsId}]");
                continue;
            }
            things_to_buy.Add(new ItemDesc(shop_conf.ItemID, (int)items[i].BuyCount));
            var const_info = _host_shop.GetCommoditCostInfo((int)items[i].GoodsId);
            //todo 交易可能有组合这个先默认就只有一种代币购买 以后有需求在加
           
            total_cost.Add(new ItemDesc(const_info.buy_cost.money_type,
                const_info.buy_cost.cost_money));
            //判断💰够不够
            code = ItemTransition.TryTakeItem(role, total_cost);
            if (code != CmdCode.Succeed) return code;
            
            // 背包容量
            //需要将 物品
            Dictionary<KnapsackType, List < ItemDesc >> thingsByKnapsackType = ItemTransition.ClassifyItemsByBagType(things_to_buy);
            foreach (var item in thingsByKnapsackType)
            {
                ItemZhuDongGetResult result =  ItemTransition.TryZhuDongGetItem(role, item.Value);
                if (result.ErrorCode != CmdCode.Succeed)
                {
                    return result.ErrorCode;
                }
            }
            
        }
        return CmdCode.Succeed;
    }
  
    private CmdCode CheckRoleQualificationBeforeBuy(OnlineRole role, BuyShopItemReq request)
    {
        RepeatedField < BuyShopItem > items= request.Items;
        for (int i= 0; i<request.Items.Count; ++i)
        {
            ShopItemParseInfo config = LubanMgr.Instance.GetShopItemParseInfoById((int)items[i].GoodsId);
            if (config == null)
            {
                return CmdCode.ShopItemNotFound;
            }
            //查询限购次数
            long left_times = role.GetComponent<RoleShopRecord>()
                .GetBuyLeftTimes( (int)items[i].GoodsId,config.PurchaseCondition);
                
            if (left_times >= 0 && items[i].BuyCount > left_times)
            {
                LogMsg.Info($"item : {items[i].GoodsId} over buy limit count [{items[i].BuyCount}:{left_times}]");
                return CmdCode.ShopItemNotFound;
            }
        }
        return CmdCode.Succeed;
    }

    private CmdCode CheckLegalityBeforeBuy(OnlineRole role, BuyShopItemReq request)
    {
        return CmdCode.Succeed;
    }
}
