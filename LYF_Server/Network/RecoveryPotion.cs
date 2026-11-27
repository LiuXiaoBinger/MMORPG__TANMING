/// <summary>背包与游戏服按物品表中的单瓶恢复点数识别药品。</summary>
public static class RecoveryPotion
{
    /// <summary>客户端用药请求及游戏服回执共用的协议号。</summary>
    public const ushort UseCommand = 11163;

    /// <summary>读取药品恢复的资源和点数，两种资源的配置互斥。</summary>
    /// <param name="item">选中堆叠的物品配置。</param><param name="mana">真表示法力药位，假表示生命药位。</param>
    /// <param name="amount">成功时为饮用一瓶的恢复点数。</param>
    public static bool TryRead(cfg.ItemInfo item, out bool mana, out int amount)
    {
        mana = false;
        amount = 0;
        if (item == null) return false;
        if (item.RestoreHp > 0 && item.RestoreMp == 0) amount = item.RestoreHp;
        if (item.RestoreMp > 0 && item.RestoreHp == 0)
        {
            mana = true;
            amount = item.RestoreMp;
        }
        return amount > 0;
    }
}
