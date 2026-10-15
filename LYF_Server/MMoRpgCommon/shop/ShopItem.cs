//商店购买消耗 - 类型 数量

using System.Collections.Generic;

/// <summary>商店购买消耗：货币类型与数量。</summary>
public struct ItemCostMoney
{
    public int money_type; // 消耗货币id
    public int cost_money; // 消耗货币number

};

/// <summary>一次购买的消耗信息。</summary>
public struct ShopItemBuyCost
{
    public int buy_count;        // 一次购买获得数量
    public int discount;         // 折扣
    public int[] price_change;   // 价格变动，默认null
    /// <summary>主消耗货币。</summary>
    public ItemCostMoney buy_cost;
    //public List<ItemCostMoney> buy_cost;   // 购买消耗货币集合
    /// <summary>附加消耗货币集合（预留）。</summary>
    public List<ItemCostMoney> buy_cost2;  // 购买消耗货币集合2

    /// <summary>手动初始化，代替构造函数</summary>
    public void Init()
    {
        buy_count = 0;
        discount = 0;
        price_change = new int[4]; // 在这里new数组，4个int默认为0
        //buy_cost = new List<ItemCostMoney>();
        //buy_cost2 = new List<ItemCostMoney>();
    }
}