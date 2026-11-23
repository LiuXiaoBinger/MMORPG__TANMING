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
    DungeonPurchase = 3,
    /// <summary>每本秘笈的领悟等级。</summary>
    ManualLevel = 4,
    /// <summary>每本秘笈的进阶次数。</summary>
    ManualRank = 5,
    /// <summary>每本秘笈的明悟次数。</summary>
    ManualInsight = 6,
    /// <summary>角色正在使用的出战方案编号。</summary>
    ManualScheme = 7,
    /// <summary>各出战方案的槽位书籍编号。</summary>
    ManualSlot = 8
}
