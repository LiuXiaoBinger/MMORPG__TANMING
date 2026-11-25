using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>区服网关的上行连接。登录数据沿中心服读取，游戏包按玩家已进入的场景选择 GS。</summary>
internal sealed class RealmGameLink : NetClient
{
    /// <summary>区服部署及 GS 地址。</summary>
    private readonly RealmTopology realm;
    /// <summary>直接连接各个场景宿主的内网链路。</summary>
    private readonly Dictionary<int, Peer> games = new Dictionary<int, Peer>();
    /// <summary>网关持有的已选账号、角色和迁移状态，TCP 和 KCP 共用。</summary>
    private readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
    /// <summary>负责查档的区服中心。</summary>
    private readonly Peer center;
    /// <summary>负责预留场景名额的区服调度。</summary>
    private readonly Peer control;
    /// <summary>本区所有玩家共用的可选市场链路，断开仅影响交易窗口。</summary>
    private readonly Peer trade;
    /// <summary>本次中心服连接已发布的选服状态，最后一台可连 GS 退出时撤回。</summary>
    private bool listed;

    /// <summary>一次玩家会话的场景位置和交接中的数据。</summary>
    private sealed class Player
    {
        /// <summary>通过选服查询的账号。</summary>
        internal int Account;
        /// <summary>中心服在该账号和区服下返回的角色。</summary>
        internal int Role;
        /// <summary>首次开始游戏的中心服结果，首个 GS 接管后交付客户端。</summary>
        internal StartGameRet Login;
        /// <summary>已完成交接的当前线路。</summary>
        internal PublicSceneLine Current;
        /// <summary>已预留、等待接管的目标线路。</summary>
        internal PublicSceneLine Target;
        /// <summary>线路分配或角色交接进行中。</summary>
        internal bool Switching;
        /// <summary>由已确认的副本请求引发的迁移，不占用玩家手动切线的应答。</summary>
        internal bool Gathering;
        /// <summary>登录、建角和开始游戏的中心服在途请求号。</summary>
        internal int LoginRequest;
        /// <summary>客户端刚收到的权威副本实例，零表示可以切公共场景。</summary>
        internal ulong Dungeon;
        /// <summary>本次登录是否打开过交易市场，用于通知断服。</summary>
        internal bool ReadMarket;
    }

    /// <summary>保留真实来包节点，所有分发在同一份会话路由表上进行。</summary>
    private sealed class Peer : NetClient
    {
        /// <summary>网关所属路由器。</summary>
        private readonly RealmGameLink owner;
        /// <summary>业务线程可读的连接可用状态。</summary>
        internal volatile bool Ready;
        /// <summary>接入一个已配置的内部服务。</summary>
        /// <param name="router">持有玩家连接的网关。</param><param name="host">服务地址。</param><param name="port">TCP 端口。</param>
        internal Peer(RealmGameLink router, string host, int port) : base(host, port, ClientType.GateServer)
        {
            owner = router;
            OnConnSucceed = () => owner.SetReady(this, true);
            OnConnFailed = () => owner.SetReady(this, false);
        }
        /// <summary>将内网来包连同真实链路一起交给路由器。</summary><param name="packet">服务回包。</param>
        protected override void HandleCommand(BasePackage packet) { owner.Receive(this, packet); }
    }

    /// <summary>建立所有内部链路，调用 StartRealm 后启用。</summary><param name="config">本区的部署配置。</param>
    internal RealmGameLink(RealmTopology config) : base(config.Host, config.ControlPort, ClientType.GateServer)
    {
        realm = config;
        center = new Peer(this, realm.CenterHost, realm.CenterPort);
        control = new Peer(this, realm.Host, realm.ControlPort);
        if (realm.TradePort > 0) trade = new Peer(this, realm.TradeHost, realm.TradePort);
        // 每个 GS 内网连接可服务任意数量的网关玩家。
        List<SceneNodeRegistration> nodes = new List<SceneNodeRegistration>(realm.Games.Values);
        for (int index = 0; index < nodes.Count; index++)
            games.Add(nodes[index].GameId, new Peer(this, nodes[index].Host, nodes[index].Port));
    }

    /// <summary>注册完上下行协议后再开放全部内部连接。</summary>
    internal void StartRealm()
    {
        center.StartConnect();
        control.StartConnect();
        trade?.StartConnect();
        // 每个宿主独立重连。
        List<Peer> peers = new List<Peer>(games.Values);
        for (int index = 0; index < peers.Count; index++) peers[index].StartConnect();
    }

    /// <summary>中心、调度和至少一个场景宿主可用时，对外发布已经开始监听的区服网关。</summary>
    /// <param name="peer">状态改变的内网连接。</param><param name="ready">该连接是否已建立。</param>
    private void SetReady(Peer peer, bool ready)
    {
        lock (players)
        {
            if (peer.Ready == ready) return;
            peer.Ready = ready;
            if (peer == trade)
            {
                // 已在浏览的窗口收到停服状态，其余游戏连接正常工作。
                if (!ready)
                {
                    // 掉线通知按本区仍持有的玩家快照派发。
                    List<int> readers = players.Keys.ToList();
                    for (int i = 0; i < readers.Count; i++)
                        if (players[readers[i]].ReadMarket) MarketUnavailable(readers[i], null);
                }
                return;
            }
            if (!ready) Lost(peer);
            if (!center.Ready)
            {
                listed = false;
                return;
            }
            // 只需要一台已连接的场景宿主，无需等待配置中的全部 GS。
            bool available = control.Ready && games.Values.Any(game => game.Ready);
            if (available == listed) return;
            if (available)
                listed = center.Send(SceneLineCode.RealmOnline, new SceneNodeRegistration { RealmId = realm.RealmId }.ToByteString());
            else
            {
                center.Send(SceneLineCode.RealmOffline, ByteString.Empty);
                listed = false;
            }
        }
    }

    /// <summary>从玩家已认证的会话路由普通请求，分线命令由调度服答复。</summary>
    /// <param name="packet">网关已补入真实玩家会话的消息。</param><param name="protoCode">可选覆盖协议。</param>
    /// <param name="data">可选覆盖载荷。</param>
    public override bool Send(BasePackage packet, int protoCode = -1, ByteString data = null)
    {
        if (protoCode != -1) packet.ProtoCode = protoCode;
        if (data != null) packet.Data = data;
        lock (players)
        {
            // 实际传输层会话号由 TCP 和 KCP 共同维护。
            int id = packet.UnitySessionId;
            // 首次选服才创建该会话的业务状态。
            Player player;
            players.TryGetValue(id, out player);
            if (packet.ProtoCode == NetDefine.CMD_LoginGameServerCode)
            {
                // 区服存在于玩家所选的登录入口中。
                LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(packet.Data);
                if (req.GameServerId != realm.RealmId || player != null) return Error(id);
                player = new Player { Account = req.AccountId };
                players.Add(id, player);
            }
            if (player == null) return Error(id);
            if (packet.ProtoCode == NetDefine.CMD_LoginGameServerCode || packet.ProtoCode == NetDefine.CMD_CreateRoleCode ||
                packet.ProtoCode == NetDefine.CMD_StartGameCode)
            {
                if (player.LoginRequest != 0 || player.Current != null || player.Switching) return Error(id);
                if (packet.ProtoCode == NetDefine.CMD_CreateRoleCode)
                {
                    // 建角必须用已经选择的区服和账号。
                    CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(packet.Data);
                    if (req.AccountId != player.Account || req.GameServerId != realm.RealmId) return Error(id);
                }
                if (packet.ProtoCode == NetDefine.CMD_StartGameCode)
                {
                    if (player.Role <= 0 || StartGameReq.Parser.ParseFrom(packet.Data).RoleId != player.Role) return Error(id);
                }
                if (!center.Ready || !center.Send(packet))
                {
                    if (packet.ProtoCode == NetDefine.CMD_LoginGameServerCode) players.Remove(id);
                    return Error(id);
                }
                player.LoginRequest = packet.ProtoCode;
                return true;
            }
            if (packet.ProtoCode == StallCode.StallSellItemCancel)
            {
                // 客户端只能指定目标挂牌，区号及角色身份取自已登录会话。
                StallSellItemCancelArg cancel = StallSellItemCancelArg.Parser.ParseFrom(packet.Data);
                if (trade != null && trade.Ready && player.Current != null)
                {
                    // 切线期间的角色暂时没有稳定宿主。
                    StallIdentity actor = new StallIdentity { RealmId = realm.RealmId, RoleId = player.Role, GameId = player.Current.GameId, SessionId = id };
                    if (!player.Switching && trade.Send(id, StallCode.StallSellItemCancel, new StallSellItemCancelForward { ListingId = cancel.ListingId, Actor = actor }.ToByteString())) return true;
                }
                SessionMgr.Instance.GetSession(id)?.Send(StallCode.StallSellItemCancelResult, new StallSellItemCancelRes { ListingId = cancel.ListingId, Reason = StallCode.Unavailable }.ToByteString());
                return true;
            }
            if (packet.ProtoCode == StallCode.Browse)
            {
                // 客户端不能自选区服、角色或宿主，身份由当前网关路由填充。
                StallQuery query = StallQuery.Parser.ParseFrom(packet.Data);
                player.ReadMarket = true;
                if (trade == null || !trade.Ready || player.Current == null) return MarketUnavailable(id, query);
                if (player.Switching) return MarketUnavailable(id, query);
                // 当前区号、宿主和真实连接共同标识角色。
                StallIdentity actor = new StallIdentity { RealmId = realm.RealmId, RoleId = player.Role,
                    GameId = player.Current.GameId, SessionId = id };
                if (trade.Send(id, StallCode.Read, new StallReadEnvelope { Actor = actor, Query = query }.ToByteString())) return true;
                return MarketUnavailable(id, query);
            }
            if (player.Current == null) return Error(id);
            if (packet.ProtoCode == SceneLineCode.List || packet.ProtoCode == SceneLineCode.Change)
            {
                // 副本入场、出场通知已由真实宿主提供实例号。
                if (player.Dungeon != 0 || player.Switching)
                    return LineResult(id, packet.ProtoCode, "当前场景无法切线。");
                if (packet.ProtoCode == SceneLineCode.List)
                    return Control(id, player, SceneLineCode.List, 0);
                // 角色和地图一律由网关查，客户端只能选择正整数线号。
                int line = SceneLineRequest.Parser.ParseFrom(packet.Data).LineId;
                if (line <= 0) return LineResult(id, SceneLineCode.Change, "线路号无效。");
                player.Switching = Control(id, player, SceneLineCode.Allocate, line);
                return player.Switching;
            }
            if (player.Switching)
            {
                if (KcpDefine.IsRealtimeProtocol(packet.ProtoCode)) return false;
                return Error(id);
            }
            return games[player.Current.GameId].Send(packet);
        }
    }

    /// <summary>按真实来源校验各阶段回执，普通 GS 来包沿原业务处理器下发。</summary>
    /// <param name="peer">实际返回消息的连接。</param><param name="packet">带玩家会话的服务回包。</param>
    private void Receive(Peer peer, BasePackage packet)
    {
        lock (players)
        {
            // 没有会话的迟到回包不能触发角色再入场。
            int id = packet.UnitySessionId;
            // 当前客户端仍然占用的路由记录。
            Player player;
            if (!players.TryGetValue(id, out player)) return;
            if (peer == trade)
            {
                if (packet.ProtoCode == StallCode.Result || packet.ProtoCode == StallCode.StallSellItemCancelResult) SessionMgr.Instance.GetSession(id)?.Send(packet);
                return;
            }
            if (peer == center) { CenterReply(id, player, packet); return; }
            if (peer == control) { ControlReply(id, player, packet); return; }
            if (packet.ProtoCode == SceneLineCode.Arrive && player.Target != null && games[player.Target.GameId] == peer)
            {
                // 目标宿主已经建立角色，普通上行自此只送往新 GS。
                player.Current = player.Target;
                player.Target = null;
                player.Switching = false;
                Control(id, player, SceneLineCode.Commit, 0);
                if (player.Login != null)
                {
                    packet.ProtoCode = NetDefine.CMD_StartGameCode;
                    packet.Data = player.Login.ToByteString();
                    player.Login = null;
                    base.HandleCommand(packet);
                }
                else
                {
                    if (!player.Gathering) LineResult(id, SceneLineCode.Change, string.Empty, player.Current.LineId);
                    LineResult(id, SceneLineCode.State, string.Empty, player.Current.LineId);
                }
                player.Gathering = false;
                return;
            }
            if (player.Current == null || games[player.Current.GameId] != peer) return;
            if (packet.ProtoCode == SceneLineCode.Gather)
            {
                // 已确认的队伍需要全员到同一 GS，再由该机创建唯一的战斗世界。
                SceneRouteRequest gather = SceneRouteRequest.Parser.ParseFrom(packet.Data);
                if (gather.RoleId != player.Role || player.Switching || player.Dungeon != 0) return;
                player.Switching = Control(id, player, SceneLineCode.Allocate, 0, gather.GameId);
                player.Gathering = player.Switching;
                return;
            }
            if (packet.ProtoCode == SceneLineCode.Depart && player.Target != null)
            {
                // 原宿主等待落库后才交出这份角色数据。
                SceneRoleTransfer transfer = SceneRoleTransfer.Parser.ParseFrom(packet.Data);
                if (transfer.Role == null) { Cancel(id, player, "当前状态无法切线。"); return; }
                transfer.Scene = player.Target;
                if (!games[player.Target.GameId].Send(id, SceneLineCode.Arrive, transfer.ToByteString()))
                    Recover(id);
                return;
            }
            if (packet.ProtoCode == NetDefine.CMD_TeamEnterDungeonCode)
                player.Dungeon = TeamEnterDungeonNtf.Parser.ParseFrom(packet.Data).InstanceId;
            base.HandleCommand(packet);
            if (packet.ProtoCode == NetDefine.CMD_TeamEnterDungeonCode || packet.ProtoCode == NetDefine.CMD_RoleKnapsackInfoCode)
            {
                // 区服集群客户端在角色装载或副本变更后才展示可用线路。
                int visible = 0;
                if (player.Dungeon == 0) visible = player.Current.LineId;
                LineResult(id, SceneLineCode.State, string.Empty, visible);
            }
        }
    }

    /// <summary>从账号查询结果绑定角色，读到持久地图后才开始申请线路。</summary>
    /// <param name="id">玩家会话。</param><param name="player">该会话持有的角色。</param><param name="packet">中心服查询结果。</param>
    private void CenterReply(int id, Player player, BasePackage packet)
    {
        if (player.LoginRequest != packet.ProtoCode) return;
        player.LoginRequest = 0;
        if (packet.ProtoCode == NetDefine.CMD_LoginGameServerCode)
        {
            // 中心服只返回此账号在所选逻辑区服的角色。
            LoginGameServerRet result = LoginGameServerRet.Parser.ParseFrom(packet.Data);
            if (result.CmdCode != CmdCode.Succeed) players.Remove(id);
            else if (result.CreateRoleInfo != null) player.Role = result.CreateRoleInfo.RoleId;
        }
        if (packet.ProtoCode == NetDefine.CMD_CreateRoleCode)
        {
            // 建角成功后同一连接可以直接开始游戏。
            CreateRoleRet result = CreateRoleRet.Parser.ParseFrom(packet.Data);
            if (result.CmdCode == CmdCode.Succeed) player.Role = result.RoleId;
        }
        if (packet.ProtoCode == NetDefine.CMD_StartGameCode)
        {
            // 读出档案里的地图与归属，用实际地图选择进线。
            StartGameRet result = StartGameRet.Parser.ParseFrom(packet.Data);
            if (result.CmdCode == CmdCode.Succeed)
            {
                if (result.MainRoleInfo.AccountId != player.Account || result.MainRoleInfo.ServerId != realm.RealmId)
                { Error(id); return; }
                player.Login = result;
                player.Switching = Control(id, player, SceneLineCode.Allocate, 0);
                if (!player.Switching) player.Login = null;
                return;
            }
        }
        base.HandleCommand(packet);
    }

    /// <summary>将目录回复交给客户端或启动游戏服之间的一次角色交接。</summary>
    /// <param name="id">玩家会话。</param><param name="player">线路申请人。</param><param name="packet">目录回包。</param>
    private void ControlReply(int id, Player player, BasePackage packet)
    {
        if (packet.ProtoCode == SceneLineCode.List)
        {
            SessionMgr.Instance.GetSession(id)?.Send(packet);
            return;
        }
        if (packet.ProtoCode != SceneLineCode.Allocate || !player.Switching) return;
        // 调度已在同一个临界区内检查容量和预占名额。
        SceneLineResult result = SceneLineResult.Parser.ParseFrom(packet.Data);
        if (result.Lines.Count == 0) { Cancel(id, player, result.Reason); return; }
        player.Target = result.Lines[0];
        // 目标 GS 连接可能在调度回包之前断开。
        Peer target = games[player.Target.GameId];
        if (!target.Ready) { Cancel(id, player, "目标游戏服已断开。"); return; }
        if (player.Login != null)
        {
            if (!target.Send(id, SceneLineCode.Arrive, new SceneRoleTransfer { Role = player.Login.MainRoleInfo,
                Scene = player.Target }.ToByteString())) Cancel(id, player, "目标游戏服不可用。");
        }
        else if (!games[player.Current.GameId].Send(id, SceneLineCode.Depart, player.Target.ToByteString()))
            Cancel(id, player, "原游戏服不可用。");
    }

    /// <summary>通知调度服处理已核对角色身份的路由操作。</summary>
    /// <param name="id">玩家会话。</param><param name="player">已选角色。</param>
    /// <param name="code">调度命令。</param><param name="line">申请的线路，零表示自动。</param>
    /// <param name="host">组队集结要求的目标 GS，负数表示自动分配。</param>
    private bool Control(int id, Player player, int code, int line, int host = -1)
    {
        // 初次选用存档中的地图，以后依据当前宿主确认的场景。
        int map = 0;
        if (player.Login != null) map = player.Login.MainRoleInfo.BaseInfo.MapId;
        else if (player.Current != null) map = player.Current.MapId;
        if (control.Ready && control.Send(id, code, new SceneRouteRequest { RoleId = player.Role,
            MapId = map, LineId = line, RequireGame = host >= 0, GameId = host }.ToByteString())) return true;
        return Error(id);
    }

    /// <summary>放弃未交接的目标，已在线的角色继续留在原线。</summary>
    /// <param name="id">玩家会话。</param><param name="player">当前路由。</param><param name="reason">玩家可读原因。</param>
    private void Cancel(int id, Player player, string reason)
    {
        if (player.Target != null) Control(id, player, SceneLineCode.Cancel, 0);
        player.Target = null;
        player.Switching = false;
        player.Gathering = false;
        if (player.Login != null) { player.Login = null; Error(id); }
        else LineResult(id, SceneLineCode.Change, reason);
    }

    /// <summary>原节点已交出、目标不可接管时终止本次游戏会话，由普通登录重新读取已确认存档。</summary>
    /// <param name="id">受影响玩家的网关会话。</param>
    private void Recover(int id)
    {
        LineResult(id, SceneLineCode.Change, "场景连接中断，请重新进入游戏。");
        SessionMgr.Instance.GetSession(id)?.Disconnect();
    }

    /// <summary>路由不可用时沿已有通用错误协议返回。</summary><param name="id">目标玩家会话。</param>
    private static bool Error(int id)
    {
        SessionMgr.Instance.GetSession(id)?.Send(NetDefine.CMD_ErrCode, new ErrMsg { CmdCode = CmdCode.ServerError }.ToByteString());
        return false;
    }

    /// <summary>发送可直接展示的线路状态。</summary><param name="id">玩家会话。</param>
    /// <param name="code">列表或切线操作。</param><param name="reason">失败原因。</param><param name="line">实际线路。</param>
    private static bool LineResult(int id, int code, string reason, int line = 0)
    {
        SessionMgr.Instance.GetSession(id)?.Send(code, new SceneLineResult { CurrentLine = line, Reason = reason }.ToByteString());
        return reason.Length == 0;
    }

    /// <summary>玩家断开后回收当前宿主角色与调度名额。</summary><param name="id">玩家在网关的会话号。</param>
    internal void PlayerLeft(int id)
    {
        lock (players)
        {
            // 同一次断线可能先后从 TCP 与 KCP 报告。
            Player player;
            if (!players.TryGetValue(id, out player)) return;
            players.Remove(id);
            if (player.Role <= 0) return;
            if (player.Current != null) games[player.Current.GameId].Send(id, SceneLineCode.Disconnect, ByteString.Empty);
            if (player.Target != null) games[player.Target.GameId].Send(id, SceneLineCode.Disconnect, ByteString.Empty);
            Control(id, player, SceneLineCode.Disconnect, 0);
        }
    }

    /// <summary>将市场离线回应给当前玩家，不阻塞场景和背包的上行。</summary>
    /// <param name="id">真实客户端会话。</param><param name="query">正在读取的页面，主动通知时为空。</param>
    private static bool MarketUnavailable(int id, StallQuery query)
    {
        SessionMgr.Instance.GetSession(id)?.Send(StallCode.Result, new StallResult { Query = query, Reason = StallCode.Unavailable }.ToByteString());
        return true;
    }

    /// <summary>内部连接失效时终止受影响的会话，避免将旧角色的消息直接路由到另一台 GS。</summary>
    /// <param name="peer">已确认断开的连接。</param>
    private void Lost(Peer peer)
    {
        lock (players)
        {
            // Disconnect 会回调 PlayerLeft，因此先复制会话编号。
            List<int> ids = new List<int>(players.Keys);
            for (int index = 0; index < ids.Count; index++)
            {
                // 该玩家当前或待进入的宿主是否受此次断线影响。
                Player player = players[ids[index]];
                bool affected = peer == control || (peer == center && player.LoginRequest != 0);
                if (player.Current != null && games[player.Current.GameId] == peer) affected = true;
                if (player.Target != null && games[player.Target.GameId] == peer) affected = true;
                if (affected) SessionMgr.Instance.GetSession(ids[index])?.Disconnect();
            }
        }
    }
}
