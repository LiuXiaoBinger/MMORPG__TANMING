using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>可存放背包物品并独立扩容的分类，顺序与背包页签一致。</summary>
public static class InventoryKnapsacks
{
    /// <summary>六个实体背包共用的只读分类列表，供客户端和服务器建包、校验及枚举库存。</summary>
    public static readonly ReadOnlyCollection<KnapsackType> Types = new List<KnapsackType>
    {
        KnapsackType.RolePackPlain,
        KnapsackType.RolePackEquip,
        KnapsackType.RolePackConsume,
        KnapsackType.RolePackMaterial,
        KnapsackType.RolePackManual,
        KnapsackType.RolePackGem
    }.AsReadOnly();
}
