
/// <summary>
/// 保存一次背包操作的原因，供发放、扣除等操作读取；本类不主动监听事件。
/// </summary>
public class BagListener
{
    // 默认原因使用参考服务器的 NONE 值，避免未设置原因时出现空引用。
    /// <summary>本次操作的完整原因记录，初始主原因为 NONE。</summary>
    private ChangeReason _item_change_reason = new ChangeReason();

    /// <summary>设置本次变更原因。</summary>
    /// <param name="r">新的变更原因。</param>
    public void SetItemChangeReason(ChangeReason r)
    {
        _item_change_reason = r;
    }

    /// <summary>返回完整原因记录，包含主原因及附加信息。</summary>
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
