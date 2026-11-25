using System;

/// <summary>在角色逻辑线程执行 RO 摆摊材料与银两操作，只接受中心库已准备的凭证。</summary>
internal static class StallAssets
{
    /// <summary>按实际背包和虚拟钱包准备本次写入；余额或容量不足时不扣款。</summary>
    /// <param name = "role">角色当前宿主的在线对象。</param><param name = "deal">已确定数量、价格和所有者的交易。</param><param name = "escrow">新寄售时给中心库的材料快照。</param>
    internal static string Apply(OnlineRole role, StallDeal deal, out RoleItemInfo escrow)
    {
        escrow = null;
        // 已上线角色的权威物品容器。
        RoItemComponent bag = role.GetComponent<RoItemComponent>();
        // 沿用普通物品变更通知和存档。
        ChangeReason reason = new ChangeReason();
        if (deal.Arg.Action == StallAction.StallSellItem)
        {
            // 按材料表的实际包类型查找，兼容旧普通包材料与材料专包。
            ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(deal.Arg.ItemId);
            // 客户端选择的格位可能已经变更，以实例 UID 为准。
            ItemBase item = bag.GetContainer(config.PackType).GetItemByUid(deal.Arg.ItemUid);
            if (item == null || item.GetItemID() != deal.Arg.ItemId || item.GetItemCount() < deal.Count)
                return "材料数量不足。";
            if (item.GetItemSign() != 0 || item.GetExpireTime() != DateTime.MinValue)
                return "绑定或限时材料不能上架。";
            // 部分上架的原购成本按真实件数拆分，尾差留在原堆。
            long held = (long)((decimal)item.GetTotalPrice() * deal.Count / item.GetItemCount());
            escrow = new RoleItemInfo
            {
                ItemUid = item.GetItemUID(),
                RoleId = (int)role.GetID(),
                ItemTypeId = item.GetItemID(),
                BagType = item.GetBagType(),
                BagIndex = item.GetBagIndex(),
                Count = deal.Count,
                TotalPrice = held,
                MoneyType = item.GetMoneyType(),
                CreateTimeUtcTicks = item.GetCreateTime().ToUniversalTime().Ticks
            };
            if (!bag.DeleteItem(item.GetItemUID(), deal.Count, reason, UpdateType.eUT_Normal))
                return "材料已发生变化。";
            item.SetTotalPrice(item.GetTotalPrice() - held);
            return "";
        }

        if (deal.Arg.Action == StallAction.StallDrawMoney)
        {
            if (!bag.GiveVirtualItem(StallCode.Silver, deal.Amount))
                return "银两暂时无法入账。";
            return "";
        }

        // 先确认余额，材料入包会整体检查容量，只有全部装入后才扣除本笔银两。
        bool buy = deal.Arg.Action == StallAction.StallItemBuy;
        if (buy && bag.GetVirtualItemCount(StallCode.Silver) < deal.Amount)
            return "银两不足。";
        // 取回保留原成本，购买则使用买家的新取得成本。
        long cost = deal.HeldCost;
        // 与成本相对应的货币配置编号。
        int currency = deal.MoneyType;
        if (buy)
        {
            cost = deal.Amount;
            currency = StallCode.Silver;
        }

        // 容器先整体核对容量，返回用于补足整数成本尾差的最后一堆。
        ItemBase received = bag.AddItem(deal.Offer.ItemId, deal.Count, reason, ItemSign.IsNoSign, currency, cost / deal.Count, UpdateType.eUT_Normal, new IdipParam());
        if (received == null)
            return "材料背包空间不足。";
        received.SetTotalPrice(received.GetTotalPrice() + cost % deal.Count);
        if (buy && !bag.TakeVirtualItem(StallCode.Silver, deal.Amount))
            throw new InvalidOperationException("同一逻辑帧的钱包金额发生变化");
        return "";
    }
}
