/// <summary>NPC 商店中同一条货物的陈列、限购与结算数据，由控制器从配置和角色背包读取。</summary>
public sealed class ShopGoodsViewData
{
    /// <summary>购买请求定位的商店编号。</summary>
    public int ShopId { get; }
    /// <summary>限购记录和购买请求共用的商品编号。</summary>
    public int GoodsId { get; }
    /// <summary>货架与详情中展示的物品名称。</summary>
    public string ProductName { get; }
    /// <summary>商品物品在 UISprites 下的贴图或图集切片地址。</summary>
    public string ProductIconPath { get; }
    /// <summary>每份商品扣除的货币数量。</summary>
    public int Price { get; }
    /// <summary>服务器扣取的虚拟物品编号。</summary>
    public int CurrencyItemId { get; }
    /// <summary>结算货币的显示名。</summary>
    public string CurrencyName { get; }
    /// <summary>与结算货币对应的背包图标地址。</summary>
    public string CurrencyIconPath { get; }
    /// <summary>角色当前剩余限购份数，不限购时为 int.MaxValue。</summary>
    public int RemainingPurchaseCount { get; }
    /// <summary>本次可选的最大购买份数，包含余额与限购约束。</summary>
    public int MaxPurchaseCount { get; }
    /// <summary>商品是否不限次数。</summary>
    public bool IsUnlimitedPurchase { get; }
    /// <summary>从物品配置读取的用途或装备说明。</summary>
    public string Description { get; set; }
    /// <summary>商品是否在买入后绑定到角色。</summary>
    public bool BindAfterPurchase { get; set; }
    /// <summary>装备类商品进入装备页，其余商货进入杂货页。</summary>
    public bool IsEquipment { get; set; }
    /// <summary>角色包内已持有的该物品数量，含绑定物品。</summary>
    public int OwnedCount { get; set; }
    /// <summary>可用于结算的虚拟物品余额，与购买校验使用相同的背包查询。</summary>
    public long Balance { get; set; }
    /// <summary>按当前商品的限购周期展示剩余份数。</summary>
    public string LimitText { get; set; }

    /// <summary>保存同一商品的展示标识与可购买范围。</summary>
    /// <param name="shopId">所属商店编号。</param>
    /// <param name="goodsId">商品配置主键。</param>
    /// <param name="productName">实际发放的物品名称。</param>
    /// <param name="productIconPath">商品图标地址。</param>
    /// <param name="price">单份商品的货币价格。</param>
    /// <param name="currencyItemId">扣款使用的虚拟物品编号。</param>
    /// <param name="currencyName">结算货币名称。</param>
    /// <param name="currencyIconPath">支付货币的显示图标。</param>
    /// <param name="remainingPurchaseCount">限购记录允许的剩余份数。</param>
    /// <param name="maxPurchaseCount">本次购买数量上限。</param>
    /// <param name="isUnlimitedPurchase">商品是否不限购买次数。</param>
    public ShopGoodsViewData(int shopId, int goodsId, string productName, string productIconPath,
        int price, int currencyItemId, string currencyName, string currencyIconPath,
        int remainingPurchaseCount, int maxPurchaseCount, bool isUnlimitedPurchase)
    {
        ShopId = shopId;
        GoodsId = goodsId;
        ProductName = productName ?? string.Empty;
        ProductIconPath = productIconPath ?? string.Empty;
        Price = price;
        CurrencyItemId = currencyItemId;
        CurrencyName = currencyName;
        if (string.IsNullOrEmpty(CurrencyName)) CurrencyName = "未知货币";
        CurrencyIconPath = currencyIconPath ?? string.Empty;
        RemainingPurchaseCount = remainingPurchaseCount;
        MaxPurchaseCount = maxPurchaseCount;
        IsUnlimitedPurchase = isUnlimitedPurchase;
    }
}
