/// <summary>
/// 游戏世界中的物品基础数据。
/// </summary>
public class ItemBase
{
    /// <summary>
    /// 角色范围内的物品 ID。
    /// </summary>
    public int ItemID { get; set; }

    /// <summary>
    /// 物品所属角色 ID。
    /// </summary>
    public int RoleID { get; set; }

    /// <summary>
    /// 物品配置 ID。
    /// </summary>
    public int ItemTypeID { get; set; }

    /// <summary>
    /// 背包类型。
    /// </summary>
    public int BagType { get; set; }

    /// <summary>
    /// 物品所在背包位置。
    /// </summary>
    public int BagIndex { get; set; }

    /// <summary>
    /// 物品数量。
    /// </summary>
    public int Count { get; set; }
}
