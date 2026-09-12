/// <summary>
/// 背包变更的不可变快照，不直接把 ItemBase 可变对象暴露给界面。
/// </summary>
public sealed class KnapsackChangedEventArgs
{
    /// <summary>本次变更的类型。</summary>
    public KnapsackChangeType ChangeType { get; }
    /// <summary>变更后物品所属的背包类型。</summary>
    public KnapsackType BagType { get; }
    /// <summary>物品变更前所属的背包类型，非移动时与 BagType 相同。</summary>
    public KnapsackType PreviousBagType { get; }
    /// <summary>物品实例唯一编号。</summary>
    public long ItemUid { get; }
    /// <summary>变更后物品所在格位。</summary>
    public int BagIndex { get; }
    /// <summary>物品变更前所在格位，非移动时与 BagIndex 相同。</summary>
    public int PreviousBagIndex { get; }
    /// <summary>物品配置编号。</summary>
    public int ItemTypeId { get; }
    /// <summary>变更后的堆叠数量，删除时为零。</summary>
    public int Count { get; }
    /// <summary>当前背包已开启格子数量，容量变更时有效。</summary>
    public int OpenedSlotCount { get; }
    /// <summary>用于生成展示数据的图标路径。</summary>
    public string IconPath { get; }

    /// <summary>创建一个背包变更快照。</summary>
    public KnapsackChangedEventArgs(KnapsackChangeType changeType,
        KnapsackType bagType, long itemUid, int bagIndex, int itemTypeId,
        int count, int openedSlotCount, string iconPath,
        KnapsackType previousBagType, int previousBagIndex)
    {
        ChangeType = changeType;
        BagType = bagType;
        PreviousBagType = previousBagType;
        ItemUid = itemUid;
        BagIndex = bagIndex;
        PreviousBagIndex = previousBagIndex;
        ItemTypeId = itemTypeId;
        Count = count;
        OpenedSlotCount = openedSlotCount;
        IconPath = iconPath;
    }
}
