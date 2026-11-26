/// <summary>RO 数字命令按物品编号和有符号数量发放或扣除，复用角色的正式背包。</summary>
internal sealed class SimpleItemUpdateProcessor : CMDProcessor
{
    /// <summary>数字首词在 CMDManager 中映射到此注册名。</summary>
    internal override string Name => "simpleitemupdate";

    /// <param name="role">接收或交出物品的角色。</param><param name="args">物品配置 ID、带正负号的数量。</param>
    internal override string Process(OnlineRole role, string[] args)
    {
        // 数字快捷写法中首词就是物品编号。
        int id;
        // 正数增加，负数扣除；最小整数不能转为背包接受的正数数量。
        int count;
        if (args.Length != 2 || !int.TryParse(args[0], out id) || !int.TryParse(args[1], out count))
            return "用法：<物品ID> <正负数量>";
        if (count == 0 || count == int.MinValue) return "物品数量无效";
        return ChangeItem(role, id, System.Math.Abs(count), count < 0, false);
    }

    /// <summary>校验正式物品配置，复用背包的容量、堆叠、扣除与增量同步。</summary>
    /// <param name="role">本次操作的物品所有者。</param><param name="id">物品或货币配置 ID。</param>
    /// <param name="count">绝对数量，大于零。</param><param name="take">为真时扣除，否则发放。</param>
    /// <param name="moneyOnly">货币命令只接受虚拟背包的货币行。</param>
    internal static string ChangeItem(OnlineRole role, int id, int count, bool take, bool moneyOnly)
    {
        // 配置决定落入的实际背包及物品类型。
        ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(id);
        if (config == null) return "物品配置不存在：" + id;
        if (count <= 0) return "数量必须为正整数";
        if (moneyOnly && (config.ItemMainType != ItemMainType.ItemType_Money || config.PackType != KnapsackType.RoleVirtualItemPack))
            return "该编号不是货币：" + id;
        // 当前角色持有的组件负责实际写入、物品通知及存档脏标记。
        RoItemComponent bag = role.GetComponent<RoItemComponent>();
        if (take)
        {
            // 事务先核对总量，失败时保留所有堆叠。
            BagTakeItemTransition transaction = new BagTakeItemTransition(role);
            transaction.SetReason(ItemChangeReason.ITEM_REASON_GM_CMD);
            if (config.PackType == KnapsackType.RoleVirtualItemPack)
            {
                if (transaction.TakeVirtualItem(id, count) != BagTransitionResult.Success) return "货币余额不足：" + id;
            }
            else if (transaction.TakeItem(new ItemDesc { ItemID = id, ItemCount = count }) != CmdCode.Succeed)
                return "可扣除数量不足：" + id;
            transaction.NotifyClient();
            return "已扣除 " + id + " × " + count;
        }
        // GM 免费发放，以专用变更原因进入正式容器，由实际添加结果决定回执。
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_GM_CMD);
        if (bag.AddItem(id, count, reason, ItemSign.IsNoSign, 0, 0, UpdateType.eUT_Update_All, default(IdipParam)) == null)
            return "发放失败，请检查背包容量";
        bag.NotifyItemChanges();
        return "已添加 " + id + " × " + count;
    }
}
