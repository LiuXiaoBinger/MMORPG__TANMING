using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>将本 GS 当前角色及网关会话报告给本区交易服，重连后重新登记仍在线的角色。</summary>
internal sealed class StallRoleLink : IContainer
{
    /// <summary>网络线程交来的已准备操作，只在 GS 逻辑帧变动背包。</summary>
    private readonly System.Collections.Concurrent.ConcurrentQueue<StallDeal> incoming = new System.Collections.Concurrent.ConcurrentQueue<StallDeal>();
    /// <summary>同一角色等待上一次普通存档确认时保留本笔操作。</summary>
    private readonly Dictionary<int,StallDeal> pending = new Dictionary<int,StallDeal>();
    /// <summary>同一内网连接上只发布本 GS 的角色。</summary>
    private readonly NetClient client;
    /// <summary>区号与物理节点号，作为本 GS 的空角色登记包。</summary>
    private readonly StallIdentity node;
    /// <summary>游戏逻辑线程更新，重连回调读取的本机在线角色快照。</summary>
    private readonly Dictionary<int, StallIdentity> actors = new Dictionary<int, StallIdentity>();
    /// <summary>只有成功登记本连接后才立即发送增量。</summary>
    private bool connected;
    /// <summary>建立市场连接，不影响游戏服与中心服的原连接。</summary>
    /// <param name="realm">当前区服地址。</param><param name="gameId">本机物理 GS 编号。</param>
    internal StallRoleLink(RealmTopology realm, int gameId)
    {
        node = new StallIdentity { RealmId = realm.RealmId, GameId = gameId };
        client = new NetClient(realm.TradeHost, realm.TradePort, ClientType.GameServer);
        client.RegistCommand(StallCode.Apply,this);
        client.OnConnSucceed = Register;
        client.OnConnFailed = () => { lock (actors) connected = false; };
        client.StartConnect();
    }
    /// <summary>网络框架 IContainer 的初始化入口。</summary>
    public void OnInit() { }
    /// <summary>此连接仅由 GS 主动连出。</summary><param name="link">框架连接。</param><param name="packet">入站包。</param>
    public void OnServerCommand(BaseLink link,BasePackage packet) { }
    /// <summary>网络线程只解析交易凭证后入队。</summary><param name="link">交易服连接。</param><param name="packet">已准备的凭证。</param>
    public void OnClientCommand(BaseLink link,BasePackage packet) { incoming.Enqueue(StallDeal.Parser.ParseFrom(packet.Data)); }

    /// <summary>在角色主循环中核对当前会话并执行一次，普通存档未确认时延至下一帧。</summary>
    internal void Update()
    {
        // 网络回包可能重复，中心保证一个角色同一时间最多一单。
        StallDeal incomingDeal;
        while(incoming.TryDequeue(out incomingDeal)) pending[incomingDeal.Actor.RoleId]=incomingDeal;
        if(pending.Count==0)return;
        // 固定本帧需要处理的角色编号。
        List<int> ids=pending.Keys.ToList();
        for(int i=0;i<ids.Count;i++)
        {
            // 每名角色仅允许一笔已准备的操作。
            StallDeal deal=pending[ids[i]];
            // 宿主和会话必须仍对应在线角色。
            OnlineRole role=RoleManager.Instance.GetOnlineRole(ids[i]);
            pending.Remove(ids[i]);
            // 旧会话迟到时交给新宿主恢复，不能用旧会话终止新宿主的资产写入。
            if(role==null || role.UnitySessionId!=deal.Actor.SessionId || role.State!=OnlineRoleState.Online)continue;
            if (deal.Ticket < role.LastStallTicket) continue;
            if(role.LastStallTicket==deal.Ticket)
            {
                if(role.StallError.Length>0){deal.Reason=role.StallError;client.Send(StallCode.Abort,deal.ToByteString());}
                continue;
            }
            if(!role.IsRoleDataReady() || role.StallSaveBusy){pending[ids[i]]=deal;continue;}
            // 新寄售时记录从原背包取出的实际物品。
            RoleItemInfo escrow;
            // 空串表示本帧的资产修改成功，可以提交。
            string error=StallAssets.Apply(role,deal,out escrow);
            role.LastStallTicket=deal.Ticket;role.StallError=error;
            if(error.Length>0){deal.Reason=error;client.Send(StallCode.Abort,deal.ToByteString());continue;}
            role.SaveStallAssets(deal.Ticket,escrow);
        }
    }

    /// <summary>交易服恢复后先登记宿主，再恢复所有角色路由。</summary>
    private void Register()
    {
        lock (actors)
        {
            connected = client.Send(StallCode.Bind, node.ToByteString());
            // 注册期间沿用同一把锁，离场更新不会被旧快照覆盖。
            List<StallIdentity> online = actors.Values.ToList();
            for (int i = 0; i < online.Count; i++) client.Send(StallCode.Bind, online[i].ToByteString());
        }
    }
    /// <summary>在实际宿主接管或释放时更新交易路由。</summary><param name="role">已由 GS 确认的角色。</param><param name="online">入场时登记、离场时摘除。</param>
    internal void SetRole(OnlineRole role, bool online)
    {
        // 同区单网关使用真实会话号区分同一角色的不同登录。
        StallIdentity actor = new StallIdentity { RealmId = node.RealmId, GameId = node.GameId,
            RoleId = (int)role.GetID(), SessionId = role.UnitySessionId };
        lock (actors)
        {
            // 登记与断开可能跨重连发生，摘除只匹配实际离场的会话。
            ushort code = StallCode.Bind;
            if (online) actors[actor.RoleId] = actor;
            else
            {
                // 该角色最近一次登记的会话。
                StallIdentity previous;
                if (!actors.TryGetValue(actor.RoleId, out previous) || previous.SessionId != actor.SessionId) return;
                actors.Remove(actor.RoleId);
                code = StallCode.Unbind;
            }
            if (connected) client.Send(code, actor.ToByteString());
        }
    }
}
