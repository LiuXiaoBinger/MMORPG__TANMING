using System;

/// <summary>
/// 商城商品的纯展示数据，避免商品控件依赖配置表和商城管理器。
/// </summary>
public sealed class ShopGoodsViewData
{
    /// <summary>商品所属商店编号。</summary>
    public int ShopId { get; }
    /// <summary>商城商品唯一编号。</summary>
    public int GoodsId { get; }
    /// <summary>商品显示名称。</summary>
    public string ProductName { get; }
    /// <summary>商品图标路径。</summary>
    public string ProductIconPath { get; }
    /// <summary>单份商品价格。</summary>
    public int Price { get; }
    /// <summary>结算货币的物品编号。</summary>
    public int CurrencyItemId { get; }
    /// <summary>结算货币名称。</summary>
    public string CurrencyName { get; }
    /// <summary>结算货币图标路径。</summary>
    public string CurrencyIconPath { get; }
    /// <summary>当前剩余可购买次数。</summary>
    public int RemainingPurchaseCount { get; }
    /// <summary>购买弹窗允许选择的最大份数。</summary>
    public int MaxPurchaseCount { get; }
    /// <summary>商品是否不受次数限制。</summary>
    public bool IsUnlimitedPurchase { get; }

    /// <summary>创建一份不可变的商城商品展示数据。</summary>
    public ShopGoodsViewData(int shopId, int goodsId, string productName, string productIconPath,
        int price, int currencyItemId, string currencyName, string currencyIconPath,
        int remainingPurchaseCount, int maxPurchaseCount, bool isUnlimitedPurchase)
    {
        ShopId = shopId;
        GoodsId = goodsId;
        ProductName = productName;
        if (ProductName == null)
        {
            ProductName = string.Empty;
        }
        ProductIconPath = productIconPath;
        if (ProductIconPath == null)
        {
            ProductIconPath = string.Empty;
        }
        Price = price;
        CurrencyItemId = currencyItemId;
        CurrencyName = currencyName;
        if (string.IsNullOrEmpty(CurrencyName))
        {
            CurrencyName = "未知货币";
        }
        CurrencyIconPath = currencyIconPath;
        if (CurrencyIconPath == null)
        {
            CurrencyIconPath = string.Empty;
        }
        RemainingPurchaseCount = remainingPurchaseCount;
        MaxPurchaseCount = maxPurchaseCount;
        IsUnlimitedPurchase = isUnlimitedPurchase;
    }
}
