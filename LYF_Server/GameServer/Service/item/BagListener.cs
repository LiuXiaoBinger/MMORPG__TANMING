
public class BagListener
{
    // 默认原因使用参考服务器的 NONE 值，避免未设置原因时出现空引用。
    private ChangeReason _item_change_reason = new ChangeReason();
    
    
    public void SetItemChangeReason(ChangeReason r)
    {
        _item_change_reason = r;
    }

    public ChangeReason GetTotalReason()
    {
        return _item_change_reason;
    }
    public ItemChangeReason GetItemChangeReason() 
    {
        return _item_change_reason.MainReason;
    }
}
