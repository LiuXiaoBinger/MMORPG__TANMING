using System;
using System.Collections.Generic;
using System.Globalization;
using cfg;

/// <summary>
/// 解析后的商店商品运行时配置，避免业务层直接依赖 Luban 原始表字段。
/// </summary>
public sealed class ShopItemParseInfo
{
    /// <summary>
    /// 商店商品配置 ID，对应 ShopItemInfo.GoodsId。
    /// </summary>
    public int ShopItemID { get; private set; }

    /// <summary>
    /// 所属商店主表 ID，对应 ShopItemInfo.ShopId。
    /// </summary>
    public int ShopID { get; private set; }

    /// <summary>
    /// 发放的道具配置 ID，对应 ShopItemInfo.ItemId。
    /// </summary>
    public int ItemID { get; private set; }

    /// <summary>
    /// 商品发放后是否绑定。
    /// </summary>
    public int IsBind { get; private set; }

    /// <summary>
    /// 商品限购条件。
    /// </summary>
    public ShopItemPurchaseCondition PurchaseCondition { get; private set; }

    /// <summary>
    /// 商品可购买环境限制。
    /// </summary>
    public ShopItemEnvironmentCondition EnvironmentCondition { get; private set; }

    /// <summary>
    /// 一次购买的道具数量、折扣、价格变化和货币消耗。
    /// </summary>
    public ShopItemBuyCost Cost { get; private set; }

    /// <summary>
    /// 是否不限购。
    /// </summary>
    public bool IsUnlimitedPurchase()
    {
        return PurchaseCondition != null && PurchaseCondition.
            IsUnlimitedPurchase;
    }

    /// <summary>
    /// 将 Luban 商品配置转换为运行时结构。
    /// </summary>
    public static ShopItemParseInfo Create(ShopItemInfo shopItemInfo)
    {
        if (shopItemInfo == null)
        {
            return null;
        }

        CountInfo countInfo = LubanMgr.Instance.GetCountInfo(RoleCountAction.ShopPurchase, shopItemInfo.GoodsId);
        return new ShopItemParseInfo
        {
            ShopItemID = shopItemInfo.GoodsId,
            ShopID = shopItemInfo.ShopId,
            ItemID = shopItemInfo.ItemId,
            IsBind = shopItemInfo.BindAfterPurchase ,
            PurchaseCondition = new ShopItemPurchaseCondition(countInfo),
            EnvironmentCondition = new ShopItemEnvironmentCondition(
                shopItemInfo.IsUnlocked != 0,
                shopItemInfo.JobLimit,
                shopItemInfo.BaseLevelLimit,
                shopItemInfo.QuestLimit,
                shopItemInfo.StartTime,
                shopItemInfo.EndTime),
            Cost = CreateCost(shopItemInfo)
        };
    }

    private static ShopItemBuyCost CreateCost(ShopItemInfo shopItemInfo)
    {
        ShopItemBuyCost cost = new ShopItemBuyCost();
        cost.Init();
        cost.buy_count = shopItemInfo.ItemPerMount;
        cost.discount = shopItemInfo.Discount;
        cost.price_change = ParsePriceChange(shopItemInfo.PriceChange);

        if (shopItemInfo.CurrencyItemId > 0 && shopItemInfo.Price > 0)
        {
            cost.buy_cost = new ItemCostMoney
            {
                money_type = shopItemInfo.CurrencyItemId,
                cost_money = shopItemInfo.Price
            };
        }

        return cost;
    }

    private static int[] ParsePriceChange(string priceChangeText)
    {
        int[] priceChanges = new int[4];
        if (string.IsNullOrWhiteSpace(priceChangeText))
        {
            return priceChanges;
        }

        string[] values = priceChangeText.Split(new[] { '_', ',', ':', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        int count = Math.Min(values.Length, priceChanges.Length);
        for (int index = 0; index < count; index++)
        {
            int.TryParse(values[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out priceChanges[index]);
        }

        return priceChanges;
    }
}


/// <summary>
/// 商店商品的展示和购买环境限制。
/// </summary>
public sealed class ShopItemEnvironmentCondition
{
    /// <summary>
    /// 商品是否默认解锁。
    /// </summary>
    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// 职业限制，0 表示不限职业。
    /// </summary>
    public int JobLimit { get; private set; }

    /// <summary>
    /// 最低角色等级限制，0 表示不限等级。
    /// </summary>
    public int BaseLevelLimit { get; private set; }

    /// <summary>
    /// 前置任务限制，0 表示不限任务。
    /// </summary>
    public int QuestLimit { get; private set; }

    /// <summary>
    /// 商品可购买开始时间；空字符串表示不限制。
    /// </summary>
    public string StartTime { get; private set; }

    /// <summary>
    /// 商品可购买结束时间；空字符串表示不限制。
    /// </summary>
    public string EndTime { get; private set; }

    public ShopItemEnvironmentCondition(
        bool isUnlocked,
        int jobLimit,
        int baseLevelLimit,
        int questLimit,
        string startTime,
        string endTime)
    {
        IsUnlocked = isUnlocked;
        JobLimit = jobLimit;
        BaseLevelLimit = baseLevelLimit;
        QuestLimit = questLimit;
        StartTime = startTime ?? string.Empty;
        EndTime = endTime ?? string.Empty;
    }
}


/// <summary>
/// 商店商品的限购条件，由 CountInfo 配置解析而来。
/// </summary>
public sealed class ShopItemPurchaseCondition
{
    /// <summary>
    /// 限购类型：0 不限购，1 每日限购，2 永久限购。
    /// </summary>
    public RoleCountAction Action { get; private set; }

    /// <summary>
    /// 对应限购类型允许购买的最大次数；不限购时为 0。
    /// </summary>
    public int Limit { get; private set; }

    /// <summary>
    /// 是否不限制购买次数。
    /// </summary>
    public bool IsUnlimitedPurchase
    {
        get { return Limit <= 0; }
    }

    public ShopItemPurchaseCondition(CountInfo config)
    {
        if (config == null)
        {
            Action = RoleCountAction.None;
            Limit = 0;
            return;
        }
        Action = (RoleCountAction)config.Action;
        Limit = config.Limit;
    }
}
