
/// <summary>
/// 背包物品变更原因的监听/记录器：持有一条聚合 ChangeReason，供事务设置与读取主原因。
/// </summary>
public class BagListener
{
    // 默认原因使用参考服务器的 NONE 值，避免未设置原因时出现空引用。
    /// <summary>本次物品变更的聚合原因对象。</summary>
    private ChangeReason _item_change_reason = new ChangeReason();

    /// <summary>设置本次变更原因。</summary>
    /// <param name="r">新的变更原因。</param>
    public void SetItemChangeReason(ChangeReason r)
    {
        _item_change_reason = r;
    }

    /// <summary>获取完整聚合原因。</summary>
    public ChangeReason GetTotalReason()
    {
        return _item_change_reason;
    }
    /// <summary>获取主物品变更原因。</summary>
    public ItemChangeReason GetItemChangeReason()
    {
        return _item_change_reason.MainReason;
    }
}
