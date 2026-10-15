using System.Collections.Generic;
using Google.Protobuf.Collections;

/// <summary>
/// 购买前置条件校验：依次做合法性、限购资格、货币与背包容量校验，校验失败即返回对应错误码。
/// </summary>
public class ShopCondition
{
    // 所属商店对象，用于读取商品价格等静态配置。
    private ShopStatic _host_shop;

    /// <summary>绑定所属商店。</summary>
    public ShopCondition(ShopStatic shop)
    {
        _host_shop =  shop;
    }

    /// <summary>
    /// 购买前综合校验：合法性 -> 限购资格 -> 货币与背包容量。
    /// </summary>
    /// <param name="role">购买角色。</param>
    /// <param name="request">客户端购买请求。</param>
    /// <param name="realCostType">实际计费类型，预留扩展。</param>
    /// <returns>任一环节失败返回对应错误码，全部通过返回 Succeed。</returns>
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
// 注意：以下 return 位于上一条 return 之后，为不可达代码（历史移植残留），不参与实际执行。
        //todo
        return CmdCode.Succeed;
    }

    /// <summary>逐商品校验货币是否足够、背包是否有空间，并试扣货币。</summary>
    private CmdCode CheckCostAndBagCapcity(OnlineRole role, BuyShopItemReq
        request, CostType realCostType)
    {
        CmdCode code = CmdCode.Succeed;
        if (role == null)
        {
            return CmdCode.RoleNotExist;
        }
        // 本次要发放的物品清单（按商品累计）。
        List<ItemDesc> things_to_buy = new List<ItemDesc>();
        // 本次要扣除的货币清单。
        List<ItemDesc> total_cost = new List<ItemDesc>();
        // 历史残留：未使用的第二份货币清单。
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

    /// <summary>逐商品校验配置存在性与剩余限购次数。</summary>
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

    /// <summary>交易合法性校验入口，当前恒通过。</summary>
    private CmdCode CheckLegalityBeforeBuy(OnlineRole role, BuyShopItemReq request)
    {
        return CmdCode.Succeed;
    }
}
