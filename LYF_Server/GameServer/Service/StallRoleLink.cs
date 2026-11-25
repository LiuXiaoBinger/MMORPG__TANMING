using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>将本 GS 当前角色及网关会话报告给本区交易服，重连后重新登记仍在线的角色。</summary>
internal sealed class StallRoleLink
{
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
        client.OnConnSucceed = Register;
        client.OnConnFailed = () => { lock (actors) connected = false; };
        client.StartConnect();
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
