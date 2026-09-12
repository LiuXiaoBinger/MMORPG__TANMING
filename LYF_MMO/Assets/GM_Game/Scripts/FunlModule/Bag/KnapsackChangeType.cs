/// <summary>
/// 背包变更的具体类型，用于让界面只刷新受影响的格子。
/// </summary>
public enum KnapsackChangeType
{
    /// <summary>登录或角色切换后的完整快照。</summary>
    Full = 0,
    /// <summary>新增一个物品实例或新增一个堆叠。</summary>
    Added = 1,
    /// <summary>删除一个物品实例。</summary>
    Removed = 2,
    /// <summary>只改变现有物品的堆叠数量。</summary>
    CountChanged = 3,
    /// <summary>物品在同一背包或不同背包之间移动。</summary>
    Moved = 4,
    /// <summary>背包已开启格子数量发生变化。</summary>
    CapacityChanged = 5,
    /// <summary>物品配置或显示字段发生变化。</summary>
    Updated = 6
}
