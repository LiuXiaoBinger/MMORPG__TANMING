/// <summary>背包单个格子的纯展示数据。</summary>
public sealed class KnapsackSlotViewData
{
    /// <summary>物品实例唯一编号。</summary>
    public long ItemUid { get; }

    /// <summary>物品配置编号。</summary>
    public int ItemTypeId { get; }

    /// <summary>物品堆叠数量。</summary>
    public int Count { get; }

    /// <summary>当前背包页面中的显示格子位置。</summary>
    public int SlotIndex { get; }

    /// <summary>物品图标资源路径。</summary>
    public string IconPath { get; }

    /// <summary>创建背包格子展示数据。</summary>
    public KnapsackSlotViewData(long itemUid, int itemTypeId, int count,
        int slotIndex, string iconPath)
    {
        ItemUid = itemUid;
        ItemTypeId = itemTypeId;
        Count = count;
        SlotIndex = slotIndex;
        IconPath = iconPath;
    }
}
