using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>本区市场目录和角色路由，网络框架以 IContainer 交付内网消息。</summary>
internal sealed class StallManager : IContainer
{
    /// <summary>当前市场的区号、所属 GS 和中心地址。</summary>
    private readonly RealmTopology realm;
    /// <summary>负责分批恢复目录的中心连接。</summary>
    private readonly NetClient center;
    /// <summary>物品配置到其所有寄存挂单，恢复完成后按价格和主键排序。</summary>
    private readonly SortedDictionary<int, List<StallOffer>> goods = new SortedDictionary<int, List<StallOffer>>();
    /// <summary>卖家角色到寄存记录，含已撤或过期待取回的货物。</summary>
    private readonly Dictionary<int, List<StallOffer>> sellers = new Dictionary<int, List<StallOffer>>();
    /// <summary>物理 GS 编号到该宿主的真实内网连接。</summary>
    private readonly Dictionary<int, int> hosts = new Dictionary<int, int>();
    /// <summary>GS 内网连接号到仍在该节点的角色与玩家会话。</summary>
    private readonly Dictionary<int, Dictionary<int, int>> roles = new Dictionary<int, Dictionary<int, int>>();
    /// <summary>打开本人摊位的在线卖家和原网关，成交时刷新其提取按钮。</summary>
    private readonly Dictionary<int, (StallIdentity Actor, int Gate)> watching = new Dictionary<int, (StallIdentity, int)>();
    /// <summary>完整目录装载完毕后允许受理查询。</summary>
    private bool ready;
    /// <summary>分批装载使用与 Gate 和 GS 相同的标准重连客户端。</summary><param name="realm">本区共享配置。</param>
    internal StallManager(RealmTopology realm)
    {
        this.realm = realm;
        center = new NetClient(realm.CenterHost, realm.CenterPort, ClientType.TradeServer);
        center.RegistCommand(StallCode.Load, this);
        center.RegistCommand(StallCode.Settled,this);
        center.RegistCommand(StallCode.StallSellItemCancelCommit, this);
        center.OnConnSucceed = () => { lock (goods) { Reset(); Next(0); } };
        center.OnConnFailed = () => { lock (goods) Reset(); };
    }
    /// <summary>连接存档中心并开始装载。</summary>
    internal void Load() { center.StartConnect(); }
    /// <summary>上次连接的目录不能在装载中对外发布。</summary>
    private void Reset() { ready = false; goods.Clear(); sellers.Clear(); }
    /// <summary>申请后续一批记录。</summary><param name="after">上批末尾主键，首批为零。</param>
    private void Next(long after) { center.Send(StallCode.Load, new StallLoadPage { RealmId = realm.RealmId, AfterId = after }.ToByteString()); }
    /// <summary>内网处理器在构造时完成绑定。</summary>
    public void OnInit() { }
    /// <summary>一批记录进入商品和卖家两个索引；最后一批收到后才允许查询。</summary>
    /// <param name="link">存档中心连接。</param><param name="packet">已提交挂单分页。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet)
    {
        if (packet.ProtoCode == StallCode.Settled)
        {
            // 准备好的指令送给当前宿主，终态按版本更新共享货架。
            StallDeal deal=StallDeal.Parser.ParseFrom(packet.Data);
            lock(goods)
            {
                if (deal.Finished)
                {
                    if (deal.Offer != null && deal.Reason.Length == 0)
                    {
                        Upsert(deal.Offer);
                        if (deal.Arg.Action == StallAction.StallItemBuy)
                        {
                            NotifySeller(deal.Offer.SellerId);
                            // 买方只收到其购买结果；待领余额仅在卖家自己的页面展示。
                            deal.Offer = PublicOffer(deal.Offer);
                        }
                    }
                    if (Online(deal.Actor)) SessionMgr.Instance.GetSession(deal.GateLink)?.Send(deal.Actor.SessionId,StallCode.Settled,deal.ToByteString());
                }
                else if(Online(deal.Actor))
                    SessionMgr.Instance.GetSession(hosts[deal.Actor.GameId])?.Send(StallCode.Apply,deal.ToByteString());
            }
            return;
        }
        if (packet.ProtoCode == StallCode.StallSellItemCancelCommit)
        {
            // 中心库已处理的结果，内存货架仅在落库成功后撤下该行。
            StallSellItemCancelForward cancel = StallSellItemCancelForward.Parser.ParseFrom(packet.Data);
            lock (goods)
            {
                // 卖家索引和公开货架引用同一个挂牌对象。
                StallOffer row = FindOwnOffer(cancel.Actor.RoleId, cancel.ListingId);
                if (row != null && cancel.Reason.Length == 0) row.Status = 3;
                if (Online(cancel.Actor)) ReplyCancel(SessionMgr.Instance.GetSession(cancel.GateLink), cancel);
            }
            return;
        }
        // 每批最多 128 条，引用同一条挂单避免复制托管数据。
        StallLoadPage page = StallLoadPage.Parser.ParseFrom(packet.Data);
        lock (goods)
        {
            for (int i = 0; i < page.Offers.Count; i++)
            {
                // 记录按主键递增返回，卖家索引保留稳定顺序。
                StallOffer row = page.Offers[i];
                Upsert(row);
            }
            if (page.More) { Next(page.AfterId); return; }
            // 同价按主键排序，单品页始终可使用挂牌号恢复位置。
            List<List<StallOffer>> groups = goods.Values.ToList();
            for (int i = 0; i < groups.Count; i++) groups[i].Sort((a,b) => {
                // 先比较单价，只有同价时才比较挂牌号。
                int byPrice = a.Price.CompareTo(b.Price);
                if (byPrice != 0) return byPrice;
                return a.ListingId.CompareTo(b.ListingId);
            });
            ready = true;
            // 中心服迟于角色路由恢复时，补查已登记角色的未完成凭证。
            List<int> nodes=hosts.Keys.ToList();
            for(int n=0;n<nodes.Count;n++)
            {
                Dictionary<int,int> online=roles[hosts[nodes[n]]];
                List<int> ids=online.Keys.ToList();
                for(int i=0;i<ids.Count;i++)
                    center.Send(StallCode.Resume,new StallDeal{Actor=new StallIdentity{RealmId=realm.RealmId,GameId=nodes[n],RoleId=ids[i],SessionId=online[ids[i]]},Arg=new StallDealArg()}.ToByteString());
            }
        }
    }
    /// <summary>交易服只接受已配置节点的登记以及与角色归属相符的网关查询。</summary>
    /// <param name="link">内网会话。</param><param name="packet">登记、离场或浏览协议。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 入站 TCP 连接号属于当前交易服。
        int connection = ((Session)link).SessionID;
        lock (goods)
        {
            if(packet.ProtoCode==StallCode.Deal || packet.ProtoCode==StallCode.Abort)
            {
                // 网关只提供已认证身份和业务参数，中心路由号由本进程赋值。
                StallDeal d=StallDeal.Parser.ParseFrom(packet.Data);
                if(packet.ProtoCode==StallCode.Abort)
                {
                    if(BoundHost(d.Actor,connection))center.Send(StallCode.Abort,d.ToByteString());
                    return;
                }
                d.GateLink=connection;
                if(ready && Online(d.Actor) && center.Send(StallCode.Prepare,d.ToByteString()))return;
                d.Reason=StallCode.Unavailable;d.Finished=true;
                link.Send(d.Actor.SessionId,StallCode.Settled,d.ToByteString());return;
            }
            if (packet.ProtoCode == StallCode.StallSellItemCancel)
            {
                // 实际来源连接由交易服填入，中心回执才能送回原网关。
                StallSellItemCancelForward cancel = StallSellItemCancelForward.Parser.ParseFrom(packet.Data);
                cancel.GateLink = connection;
                if (!ready || !Online(cancel.Actor)) cancel.Reason = StallCode.Unavailable;
                else if (FindOwnOffer(cancel.Actor.RoleId, cancel.ListingId) == null) cancel.Reason = "该挂单无法撤下，请刷新摊位。";
                else if (center.Send(StallCode.StallSellItemCancelCommit, cancel.ToByteString())) return;
                else cancel.Reason = StallCode.Unavailable;
                ReplyCancel(link, cancel);
                return;
            }
            if (packet.ProtoCode != StallCode.Read)
            {
                // GS 首包用空角色登记节点，之后只登记这条链路上的角色。
                StallIdentity actor = StallIdentity.Parser.ParseFrom(packet.Data);
                if (actor.RealmId != realm.RealmId || !realm.Games.ContainsKey(actor.GameId)) return;
                if (actor.RoleId == 0 && packet.ProtoCode == StallCode.Bind)
                {
                    hosts[actor.GameId] = connection;
                    roles[connection] = new Dictionary<int, int>();
                    return;
                }
                if (!BoundHost(actor, connection)) return;
                if (packet.ProtoCode == StallCode.Bind)
                {
                    roles[connection][actor.RoleId] = actor.SessionId;
                    center.Send(StallCode.Resume,new StallDeal{Actor=actor,Arg=new StallDealArg()}.ToByteString());
                }
                else
                {
                    roles[connection].Remove(actor.RoleId);
                    if (watching.TryGetValue(actor.RoleId, out var route) && route.Actor.Equals(actor))
                        watching.Remove(actor.RoleId);
                }
                return;
            }
            // 收到的参数只包含网关确认的角色和客户端选择的浏览页。
            StallReadEnvelope request = StallReadEnvelope.Parser.ParseFrom(packet.Data);
            // 原查询随结果返回，便于窗口识别切页后的迟到结果。
            StallResult result = new StallResult { Query = request.Query };
            if (!ready || !Online(request.Actor)) result.Reason = StallCode.Unavailable;
            else
            {
                if (request.Query.View == StallView.StallOwn) watching[request.Actor.RoleId] = (request.Actor, connection);
                else if (request.Query.View != StallView.StallGetPreSellItemInfo) watching.Remove(request.Actor.RoleId);
                Read(request.Query, request.Actor.RoleId, result);
            }
            link.Send(packet.UnitySessionId, StallCode.Result, result.ToByteString());
        }
    }
    /// <summary>卖家停在个人摊位时，主动显示买家刚产生的可提取货款。</summary><param name="role">收款卖家角色号。</param>
    private void NotifySeller(int role)
    {
        if (!watching.TryGetValue(role, out var route) || !Online(route.Actor)) return;
        // 沿用个人货架结果协议，只发送到登记时的网关和会话。
        StallResult result = new StallResult { Query = new StallQuery { View = StallView.StallOwn } };
        Read(result.Query, role, result);
        SessionMgr.Instance.GetSession(route.Gate)?.Send(route.Actor.SessionId, StallCode.Result, result.ToByteString());
    }

    /// <summary>构造可公开的挂牌视图，保留价格与货量。</summary><param name="row">包含卖家待领取余额的内部挂牌。</param>
    private static StallOffer PublicOffer(StallOffer row)
    {
        StallOffer visible = row.Clone();
        visible.Income = 0;
        return visible;
    }

    /// <summary>初次加载和交易回执共用按版本替换的索引。</summary><param name="offer">中心库提交的挂牌。</param>
    private void Upsert(StallOffer offer)
    {
        // 旧版本的重放不覆盖后来的购买或领取。
        StallOffer old = FindOwnOffer(offer.SellerId,offer.ListingId);
        if(old!=null && old.Version>=offer.Version)return;
        if(!sellers.ContainsKey(offer.SellerId))sellers.Add(offer.SellerId,new List<StallOffer>());
        if (old != null)
        {
            // 旧挂单可能只在卖家的托管列表内，公开市场无需保留对应商品。
            if (goods.TryGetValue(old.ItemId, out List<StallOffer> previous)) previous.Remove(old);
            sellers[old.SellerId].Remove(old);
        }
        if(offer.Quantity==0 && offer.Income==0)return;
        sellers[offer.SellerId].Add(offer);
        if (ready) sellers[offer.SellerId].Sort((left,right)=>left.ListingId.CompareTo(right.ListingId));
        if (StallCode.IsWorkshopOnly(offer.ItemId)) return;
        if (!goods.ContainsKey(offer.ItemId)) goods.Add(offer.ItemId, new List<StallOffer>());
        goods[offer.ItemId].Add(offer);
        if (ready) goods[offer.ItemId].Sort((left, right) =>
        {
            // 先比较单价，同价时按挂牌号保持稳定次序。
            int price = left.Price.CompareTo(right.Price);
            if (price != 0) return price;
            return left.ListingId.CompareTo(right.ListingId);
        });
    }

    /// <summary>在当前卖家的寄存记录中匹配指定挂牌号。</summary>
    /// <param name="role">已登录卖家编号。</param><param name="listing">要操作的挂牌号。</param>
    private StallOffer FindOwnOffer(int role, long listing)
    {
        // 只从当前角色自己的寄存记录中查找。
        List<StallOffer> rows;
        if (!sellers.TryGetValue(role, out rows)) return null;
        return rows.Find(row => row.ListingId == listing);
    }
    /// <summary>经原网关将撤单结果送给对应玩家。</summary>
    /// <param name="gate">原网关连接，断开后允许为空。</param><param name="cancel">中心回执或本地业务错误。</param>
    private static void ReplyCancel(BaseLink gate, StallSellItemCancelForward cancel)
    {
        gate?.Send(cancel.Actor.SessionId, StallCode.StallSellItemCancelResult,
            new StallSellItemCancelRes { ListingId = cancel.ListingId, Reason = cancel.Reason }.ToByteString());
    }
    /// <summary>物理编号只接受当前登记连接的角色更新。</summary><param name="actor">GS 身份。</param><param name="connection">来包连接。</param>
    private bool BoundHost(StallIdentity actor, int connection)
    {
        // 当前 GS 最后一次成功登记的连接号。
        int registered;
        return hosts.TryGetValue(actor.GameId, out registered) && registered == connection;
    }
    /// <summary>比对权威区号、宿主和角色会话。</summary><param name="actor">网关已从当前路由填充的身份。</param>
    private bool Online(StallIdentity actor)
    {
        if (actor == null || actor.RealmId != realm.RealmId) return false;
        // GS 的当前连接号。
        int host;
        if (!hosts.TryGetValue(actor.GameId, out host)) return false;
        // GS 保存的网关玩家会话号。
        int session;
        return roles[host].TryGetValue(actor.RoleId, out session) && session == actor.SessionId;
    }
    /// <summary>连接失效后解除该宿主的全部角色，不影响重新登记的新连接。</summary><param name="connection">断开的交易服内网会话号。</param>
    internal void Disconnected(int connection)
    {
        lock (goods)
        {
            // 宿主或网关断开后丢弃对应的个人订阅，重新登录会重新登记。
            List<int> readers = watching.Keys.ToList();
            for (int i = 0; i < readers.Count; i++)
            {
                var route = watching[readers[i]];
                if (route.Gate == connection || BoundHost(route.Actor, connection)) watching.Remove(readers[i]);
            }
            if (!roles.Remove(connection)) return;
            // 只在 GS 连接断开时扫描配置数量级的节点。
            List<int> nodes = hosts.Keys.ToList();
            for (int i = 0; i < nodes.Count; i++) if (hosts[nodes[i]] == connection) hosts.Remove(nodes[i]);
        }
    }
    /// <summary>汇总本区同种商品的有效数量，从已排序挂牌中取得最低单价。</summary>
    /// <param name="itemId">商品配置号。</param><param name="now">本次查询的 UTC 秒。</param>
    private StallQuote GetStallItemSellBasePrice(int itemId, long now)
    {
        // 没有有效挂单时数量为零。
        StallQuote quote = new StallQuote { ItemId = itemId };
        // 同类挂牌按单价和挂牌号升序排列。
        List<StallOffer> rows;
        if (!goods.TryGetValue(itemId, out rows)) return quote;
        for (int index = 0; index < rows.Count; index++)
        {
            // 已到期、已撤和售罄的物品不参与报价。
            StallOffer row = rows[index];
            if (row.Status != 1 || row.ExpiresAt <= now) continue;
            if (quote.Quantity == 0) { quote.Price = row.Price; quote.CurrencyId = row.CurrencyId; }
            quote.Quantity += row.Quantity;
        }
        return quote;
    }
    /// <summary>使用服务器 UTC 截止时间过滤公开行情，个人列表保留期满待取回记录。</summary>
    /// <param name="query">只读视图及游标。</param><param name="role">已查证的卖家号。</param><param name="result">当前请求的响应。</param>
    private void Read(StallQuery query, int role, StallResult result)
    {
        // 同一个响应中的所有行使用同一 UTC 秒作比较。
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (query.View == StallView.StallGetPreSellItemInfo)
        {
            // 本项目以本区实时最低挂牌价供卖家参考，无在售量时不返回虚构报价。
            StallQuote quote = GetStallItemSellBasePrice(query.ItemId, now);
            if (quote.Quantity > 0)
            {
                result.Quotes.Add(quote);
                // 预售面板按当前最低单价列出前三笔可买的真实挂单。
                List<StallOffer> similar = goods[query.ItemId];
                for (int i = 0; i < similar.Count && result.Offers.Count < 3; i++)
                {
                    StallOffer row = similar[i];
                    if (row.Status == 1 && row.ExpiresAt > now) result.Offers.Add(PublicOffer(row));
                }
            }
            return;
        }
        if (query.View == StallView.StallSummary)
        {
            // 每页最多返回 96 个不同的物品配置。
            List<int> ids = goods.Keys.Where(id => id > query.AfterId).ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                // 目录与卖家询价使用同一份当前有效报价。
                StallQuote quote = GetStallItemSellBasePrice(ids[i], now);
                if (quote.Quantity == 0) continue;
                if (result.Quotes.Count == 96) { result.NextId = result.Quotes[95].ItemId; break; }
                result.Quotes.Add(quote);
            }
            return;
        }
        // 单品按价格与主键排序，自己的挂牌按主键排序。
        List<StallOffer> offers;
        if (query.View == StallView.StallOwn) { if (!sellers.TryGetValue(role, out offers)) return; }
        else if (query.View == StallView.StallOffers) { if (!goods.TryGetValue(query.ItemId, out offers)) return; }
        else { result.Reason = "无效的市场页面。"; return; }
        // 仍保留的游标即使刚刚过期也能定位，不会跳回首页。
        int start = 0;
        if (query.AfterId != 0)
        {
            start = offers.FindIndex(row => row.ListingId == query.AfterId) + 1;
            if (start == 0) return;
        }
        for (int i = start; i < offers.Count; i++)
        {
            // 展示层只需每页三条，不暴露可写的托管快照。
            StallOffer row = offers[i];
            if (query.View == StallView.StallOffers && (row.Status != 1 || row.ExpiresAt <= now)) continue;
            // 新角色最多八个托管位，历史超量的卖家仍可翻页逐笔处理。
            int pageSize = 3;
            if (query.View == StallView.StallOwn) pageSize = 8;
            if (result.Offers.Count == pageSize)
            {
                result.NextId = result.Offers[pageSize - 1].ListingId;
                break;
            }
            if (row.Status == 1 && row.ExpiresAt <= now) { row = row.Clone(); row.Status = 4; }
            if (query.View == StallView.StallOffers) row = PublicOffer(row);
            result.Offers.Add(row);
        }
    }
}
