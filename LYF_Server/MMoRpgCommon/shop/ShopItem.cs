//商店购买消耗 - 类型 数量

using System.Collections.Generic;

/// <summary>商店购买消耗：货币类型与数量。</summary>
public struct ItemCostMoney
{
    public int money_type; // 消耗货币id
    public int cost_money; // 一份商品对应的货币数量

};

/// <summary>一次购买的消耗信息。</summary>
public struct ShopItemBuyCost
{
    public int buy_count;        // 一次购买获得数量
    public int discount;         // 配置中的折扣值；当前 ShopStatic 结算未使用
    public int[] price_change;   // 价格变动，默认null
    /// <summary>主消耗货币。</summary>
    public ItemCostMoney buy_cost;
    //public List<ItemCostMoney> buy_cost;   // 购买消耗货币集合
    /// <summary>附加消耗货币集合（预留）。</summary>
    public List<ItemCostMoney> buy_cost2;  // 购买消耗货币集合2

    /// <summary>初始化价格变化数组并清零购买份数与折扣；主、附加货币字段在此方法中不赋值</summary>
    public void Init()
    {
        buy_count = 0;
        discount = 0;
        price_change = new int[4]; // 固定四项价格变化数据，初值均为 0
        //buy_cost = new List<ItemCostMoney>();
        //buy_cost2 = new List<ItemCostMoney>();
    }
}