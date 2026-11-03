/// <summary>次数配置的业务行为类型。</summary>
public enum RoleCountAction
{
    /// <summary>未绑定具体业务行为。</summary>
    None = 0,
    /// <summary>记录商城商品购买次数。</summary>
    ShopPurchase = 1,
    /// <summary>记录进入副本的次数。</summary>
    DungeonEnter = 2,
    /// <summary>记录购买副本次数的行为。</summary>
    DungeonPurchase = 3
}
