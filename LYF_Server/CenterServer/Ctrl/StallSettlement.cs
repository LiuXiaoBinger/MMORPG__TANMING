using System;
using System.Collections.Generic;
using Google.Protobuf;
using SqlSugar;

/// <summary>中心库持久化摆摊操作。交易凭证先占用挂牌，角色背包快照与最终货款、余量在同一事务提交。</summary>
internal static class StallSettlement
{
    /// <summary>同一中心进程内按事务串行准备和完成市场业务，普通同角色写入仍走原存档队列。</summary>
    internal static readonly object Sync = new object ();
    /// <summary>订阅本区提交结果的交易服连接。</summary>
    private static readonly Dictionary<int, BaseLink> markets = new Dictionary<int, BaseLink>();
    /// <summary>记录当前装载本区市场的交易服。</summary><param name = "realm">逻辑区号。</param><param name = "link">交易服连接。</param>
    internal static void Subscribe(int realm, BaseLink link)
    {
        lock (Sync)
            markets[realm] = link;
    }

    /// <summary>将中心已提交的挂单版本推给交易服，即使发起角色已经离线也更新货架。</summary><param name = "deal">已持久化的结果。</param>
    internal static void Publish(StallDeal deal)
    {
        if (deal == null)
            return;
        lock (Sync)
        {
            // 本区最近登记的市场负责编排展示与回执。
            BaseLink market;
            if (markets.TryGetValue(deal.Actor.RealmId, out market))
                market.Send(StallCode.Settled, deal.ToByteString());
        }
    }

    /// <summary>将数据库托管记录转换成客户端可展示的挂单。</summary><param name = "r">中心库权威行。</param>
    internal static StallOffer Offer(TradeListingTable r)
    {
        return new StallOffer
        {
            ListingId = r.ListingId,
            SellerId = r.SellerRoleId,
            ItemId = r.ItemTypeId,
            Price = r.UnitPrice,
            CurrencyId = r.CurrencyItemId,
            Quantity = r.HeldQty,
            Status = r.Status,
            Income = r.Income.GetValueOrDefault(),
            Version = r.ListingVersion,
            Sold = r.InitialQty - r.HeldQty - r.ReturnedQty,
            ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(r.ExpiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds()
        };
    }

    /// <summary>准备或恢复摆摊操作，凭证绑定具体角色、货物与成交单价。</summary>
    /// <param name = "db">单独的中心库连接。</param><param name = "deal">来自交易服的角色与参数。</param><param name = "code">准备、恢复或放弃操作。</param>
    internal static StallDeal Handle(SqlSugarClient db, StallDeal deal, int code)
    {
        lock (Sync)
        {
            // 恢复使用角色归属，普通重发使用玩家原请求号。
            string key = deal.Actor.RealmId + "/" + deal.Actor.RoleId + "/" + deal.Arg.RequestId;
            // 已有凭证可来自重发或角色重新上线。
            StallReceiptTable stored;
            if (code == StallCode.Resume)
                stored = db.Queryable<StallReceiptTable>().Where(x => x.ServerId == deal.Actor.RealmId && x.RoleId == deal.Actor.RoleId && x.State == 0).First();
            else
                stored = db.Queryable<StallReceiptTable>().Where(x => x.RequestKey == key).First();
            if (stored != null)
            {
                // 只把已存凭证的投递地址切换到当前角色连接。
                StallDeal saved = StallDeal.Parser.ParseFrom(stored.Payload);
                saved.Actor = deal.Actor;
                if (deal.GateLink != 0)
                    saved.GateLink = deal.GateLink;
                // 历史回执仍确认原操作，市场货架以数据库的最新版本为准。
                if (saved.Finished && saved.Offer != null)
                {
                    // 转发原操作结果时同步最新挂牌展示值。
                    TradeListingTable current = db.Queryable<TradeListingTable>().Where(x => x.ListingId == saved.Offer.ListingId).First();
                    if (current != null)
                        saved.Offer = Offer(current);
                }

                if (stored.State == 0 && code != StallCode.Abort)
                {
                    // 恢复后让最终回执使用角色的新宿主和已知网关。
                    Store(db, stored, saved, 0);
                }

                if (code == StallCode.Abort && stored.State == 0 && deal.Ticket == stored.Ticket)
                {
                    saved.Reason = deal.Reason;
                    saved.Finished = true;
                    db.Ado.BeginTran();
                    try
                    {
                        db.Updateable<TradeListingTable>().SetColumns(x => new TradeListingTable { PendingTicket = null }).Where(x => x.PendingTicket == saved.Ticket).ExecuteCommand();
                        Store(db, stored, saved, 2);
                        db.Ado.CommitTran();
                    }
                    catch
                    {
                        db.Ado.RollbackTran();
                        throw;
                    }
                }

                return saved;
            }

            if (code != StallCode.Prepare)
                return null;
            // 每个角色最多一笔尚未应用到真实背包的交易。
            if (!Guid.TryParseExact(deal.Arg.RequestId, "N", out _))
                return Reject(deal, "操作号无效。");
            if (db.Queryable<StallReceiptTable>().Any(x => x.RoleId == deal.Actor.RoleId && x.ServerId == deal.Actor.RealmId && x.State == 0))
                return Reject(deal, "上一笔交易正在结算，请稍后再试。");
            deal.Ticket = UIDHelper.CreateUID(UIDType.UID_Shop);
            // 所有占用和凭证同时持久化；拒绝时不更改任何货物或资产。
            db.Ado.BeginTran();
            try
            {
                // 准备结果直接用作玩家可见的拒绝原因。
                string reason = Prepare(db, deal);
                if (reason.Length != 0)
                    Reject(deal, reason);
                // 流水与挂牌占用同事务保存，进程恢复后仍可辨识同一笔确认。
                StallReceiptTable receipt = new StallReceiptTable
                {
                    RequestKey = key,
                    RoleId = deal.Actor.RoleId,
                    ServerId = deal.Actor.RealmId,
                    Ticket = deal.Ticket,
                    Payload = deal.ToByteArray()
                };
                if (deal.Finished)
                    receipt.State = 1;
                if (deal.Reason.Length > 0)
                    receipt.State = 2;
                db.Insertable(receipt).ExecuteCommand();
                db.Ado.CommitTran();
            }
            catch
            {
                db.Ado.RollbackTran();
                throw;
            }

            return deal;
        }
    }

    /// <summary>把已判定的业务错误作为终态返回给原操作人。</summary><param name = "deal">操作凭证。</param><param name = "reason">可展示的错误。</param>
    private static StallDeal Reject(StallDeal deal, string reason)
    {
        deal.Finished = true;
        deal.Reason = reason;
        return deal;
    }

    /// <summary>核对货架条件、数量和所有权；资金、物品实物由 GS 再核对。</summary><param name = "db">当前事务。</param><param name = "deal">待填充的不可变报价。</param>
    private static string Prepare(SqlSugarClient db, StallDeal deal)
    {
        // 请求中仅使用玩家可选择的业务字段。
        StallDealArg args = deal.Arg;
        if (args.Action == StallAction.StallSellItem)
        {
            // 首期仅允许材料包内的有效配置，单笔价款符合虚拟钱包接口范围。
            ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(args.ItemId);
            if (config == null || config.ItemMainType != ItemMainType.ItemType_Material)
                return "仅可上架材料背包中的未绑定材料。";
            if (args.ItemUid <= 0 || args.Quantity <= 0 || args.Price <= 0)
                return "数量或单价无效。";
            if (args.Price > int.MaxValue / args.Quantity)
                return "总价超过允许范围。";
            if (db.Queryable<TradeListingTable>().Where(r => r.ServerId == deal.Actor.RealmId && r.MarketType == 0 && r.SellerRoleId == deal.Actor.RoleId).Where(r => r.HeldQty > 0 || r.Income > 0).Count() >= 8)
                return "八个摊位已满。";
            deal.Count = args.Quantity;
            deal.Offer = new StallOffer
            {
                ListingId = deal.Ticket,
                SellerId = deal.Actor.RoleId,
                ItemId = args.ItemId,
                Price = args.Price,
                CurrencyId = StallCode.Silver
            };
            return "";
        }

        // 所有后续操作都针对同一逻辑区的真实挂牌。
        TradeListingTable listing = db.Queryable<TradeListingTable>().Where(r => r.ListingId == args.ListingId && r.ServerId == deal.Actor.RealmId && r.MarketType == 0).First();
        if (listing == null || listing.PendingTicket.HasValue)
            return "该挂单正在结算或已失效。";
        if (args.Action != StallAction.StallItemBuy && listing.SellerRoleId != deal.Actor.RoleId)
            return "只能操作自己的挂牌。";
        deal.Offer = Offer(listing);
        switch (args.Action)
        {
            case StallAction.StallItemBuy:
                if (listing.SellerRoleId == deal.Actor.RoleId)
                    return "不能购买自己的商品。";
                if (listing.Status != 1 || listing.ExpiresAt <= DateTime.UtcNow || listing.UnitPrice != args.Price)
                    return "价格或在售状态已改变，请刷新。";
                if (args.Quantity <= 0 || args.Quantity > listing.HeldQty || listing.UnitPrice <= 0)
                    return "在售数量不足。";
                if (listing.CurrencyItemId != StallCode.Silver || listing.UnitPrice > int.MaxValue / args.Quantity)
                    return "商品计价无效。";
                deal.Count = args.Quantity;
                deal.Amount = listing.UnitPrice * deal.Count;
                break;
            case StallAction.StallDrawMoney:
                deal.Amount = listing.Income.GetValueOrDefault();
                if (deal.Amount <= 0)
                    return "没有可领取的银两。";
                // 钱包单次入账参数最大为 Int32，改价后累积超过上限的货款可分次领取。
                deal.Amount = Math.Min(deal.Amount, int.MaxValue);
                break;
            case StallAction.StallTakeBack:
            case StallAction.StallReSellItem:
                if (listing.Status == 1 && listing.ExpiresAt > DateTime.UtcNow)
                    return "请先下架物品。";
                if (listing.HeldQty <= 0)
                    return "没有可取回的余货。";
                deal.Count = listing.HeldQty;
                if (args.Action == StallAction.StallReSellItem)
                {
                    if (args.Price <= 0 || args.Price > int.MaxValue / listing.HeldQty)
                        return "重新上架价格无效。";
                    listing.UnitPrice = args.Price;
                    listing.ExpiresAt = DateTime.UtcNow.AddDays(1);
                    listing.Status = 1;
                    listing.ClosedAt = null;
                    listing.ListingVersion++;
                    db.Updateable(listing).ExecuteCommand();
                    deal.Offer = Offer(listing);
                    deal.Finished = true;
                    return "";
                }

                break;
            default:
                return "未知摆摊操作。";
        }

        if (deal.Count > 0)
        {
            if (listing.ItemPayload == null || listing.ItemPayload.Length == 0)
                return "该挂单缺少可交付的物品。";
            // 原购成本按比例随货物转移，最后一份取得剩余尾差。
            deal.MoneyType = listing.CostCurrency.GetValueOrDefault();
            deal.HeldCost = (long)((decimal)listing.HeldAcquisitionCost * deal.Count / listing.HeldQty);
        }

        listing.PendingTicket = deal.Ticket;
        db.Updateable(listing).UpdateColumns(x => x.PendingTicket).ExecuteCommand();
        return "";
    }

    /// <summary>按角色在途快照取已授权的资产凭证。</summary><param name = "db">中心主线程连接。</param><param name = "req">GS 的唯一在途快照。</param>
    internal static StallDeal Load(SqlSugarClient db, SaveRoleDataReq req)
    {
        if (req.StallTicket == 0)
            return null;
        // 只接受同一角色的已准备或已提交凭证。
        StallReceiptTable ticket = db.Queryable<StallReceiptTable>().Where(x => x.Ticket == req.StallTicket && x.RoleId == req.RoleId).First();
        if (ticket == null || ticket.State == 2)
            throw new InvalidOperationException("找不到有效交易凭证");
        return StallDeal.Parser.ParseFrom(ticket.Payload);
    }

    /// <summary>在角色物品事务内更新托管、收入及幂等回执。</summary><param name = "db">角色快照当前事务。</param><param name = "deal">已准备的凭证。</param><param name = "escrow">GS 实际从材料堆中取出的快照。</param>
    internal static void Commit(SqlSugarClient db, StallDeal deal, RoleItemInfo escrow)
    {
        if (deal == null)
            return;
        if (deal.Finished)
            throw new InvalidOperationException("重复的交易不能用新角色版本再次提交");
        // 已准备的业务操作只有持有挂牌的凭证才能完成。
        TradeListingTable row;
        if (deal.Arg.Action == StallAction.StallSellItem)
        {
            if (escrow == null || escrow.ItemUid != deal.Arg.ItemUid || escrow.Count != deal.Count)
                throw new InvalidOperationException("寄存快照不匹配");
            row = new TradeListingTable
            {
                ListingId = deal.Ticket,
                ServerId = deal.Actor.RealmId,
                SellerRoleId = deal.Actor.RoleId,
                ItemTypeId = deal.Arg.ItemId,
                CurrencyItemId = StallCode.Silver,
                UnitPrice = deal.Arg.Price,
                InitialQty = deal.Count,
                HeldQty = deal.Count,
                Status = 1,
                ListingVersion = 1,
                FeeBps = 800,
                ListedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                EscrowItemUid = escrow.ItemUid,
                ItemPayload = escrow.ToByteArray(),
                PayloadVersion = 1,
                CostCurrency = escrow.MoneyType,
                HeldAcquisitionCost = escrow.TotalPrice
            };
            db.Insertable(row).ExecuteCommand();
        }
        else
        {
            row = db.Queryable<TradeListingTable>().Where(x => x.ListingId == deal.Offer.ListingId && x.PendingTicket == deal.Ticket).First();
            if (row == null)
                throw new InvalidOperationException("挂牌占用与角色快照不一致");
            if (deal.Arg.Action == StallAction.StallDrawMoney)
                row.Income = row.Income.GetValueOrDefault() - deal.Amount;
            else
            {
                row.HeldQty -= deal.Count;
                row.HeldAcquisitionCost -= deal.HeldCost;
                if (deal.Arg.Action == StallAction.StallTakeBack)
                {
                    row.ReturnedQty += deal.Count;
                    row.Status = 3;
                }
                else
                {
                    // 上一笔不足一银两的税额随挂单保留，拆单后总税与整单一致。
                    long taxUnits = deal.Amount * row.FeeBps + row.TaxRemainder.GetValueOrDefault();
                    row.Income = row.Income.GetValueOrDefault() + deal.Amount - taxUnits / 10000;
                    row.TaxRemainder = (int)(taxUnits % 10000);
                    if (row.HeldQty == 0)
                        row.Status = 2;
                }

                if (row.HeldQty == 0)
                    row.ClosedAt = DateTime.UtcNow;
            }

            row.PendingTicket = null;
            row.ListingVersion++;
            db.Updateable(row).ExecuteCommand();
        }

        deal.Finished = true;
        deal.Offer = Offer(row);
        // 成交回执随角色存档一起更新。
        StallReceiptTable receipt = db.Queryable<StallReceiptTable>().Where(x => x.Ticket == deal.Ticket).First();
        Store(db, receipt, deal, 1);
    }

    /// <summary>保存权威终态与原始业务参数，支持重复操作返回同一结果。</summary><param name = "db">当前事务。</param><param name = "receipt">原回执行。</param><param name = "deal">完成结果。</param><param name = "state">一成功，二拒绝。</param>
    private static void Store(SqlSugarClient db, StallReceiptTable receipt, StallDeal deal, int state)
    {
        receipt.State = state;
        receipt.Payload = deal.ToByteArray();
        db.Updateable(receipt).ExecuteCommand();
    }
}
