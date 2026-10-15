
/// <summary>
/// 背包变更监听器：记录一次物品变更链路携带的原因对象，供 Transition 统一读取主原因与子原因。
/// </summary>
public class BagListener
{
    // 默认原因使用参考服务器的 NONE 值，避免未设置原因时出现空引用。
    private ChangeReason _item_change_reason = new ChangeReason();

    /// <summary>设置本次变更链路的完整原因对象。</summary>
    public void SetItemChangeReason(ChangeReason r)
    {
        _item_change_reason = r;
    }

    /// <summary>获取完整原因对象，用于在结算时回查全部子原因。</summary>
    public ChangeReason GetTotalReason()
    {
        return _item_change_reason;
    }

    /// <summary>获取本次变更的主原因枚举。</summary>
    public ItemChangeReason GetItemChangeReason()
    {
        return _item_change_reason.MainReason;
    }
}
