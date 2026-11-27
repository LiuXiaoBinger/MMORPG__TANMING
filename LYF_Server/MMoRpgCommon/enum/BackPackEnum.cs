

/// <summary>
/// 角色背包类别；六类可扩容背包相互独立，穿戴栏与虚拟物品栏不参与扩容聚合。
/// </summary>
public enum KnapsackType
{
    RolePackPlain,//角色普通背包，不聚合其它背包
    RolePackEquip,//角色武器背包
    RolePackConsume,//角色消耗品背包
    RolePackMaterial,//角色材料
    RoleCurrtEquipPack,//角色已经装备的装备包裹
    RoleVirtualItemPack, //角色虚拟物品
    RolePackManual = 6, //整本秘籍和对应的秘籍碎片，各堆保留自己的格位
    RolePackGem = 7 //用于装备镶嵌的属性及元素宝石
}
