using System.Collections.Generic;

/// <summary>共享中心的选服目录。显示信息取自本机配置，可用状态取自区服网关连接。</summary>
internal sealed class OnlineRealmDirectory
{
    /// <summary>允许对外开放的逻辑区，按区号排列，用于选服列表和默认推荐。</summary>
    private readonly SortedList<int, GameServer> configured = new SortedList<int, GameServer>();
    /// <summary>每个区用于建角的地图编号，已创建角色仍从持久存档进入。</summary>
    private readonly Dictionary<int, int> spawnMaps = new Dictionary<int, int>();
    /// <summary>逻辑区到当前网关 TCP 会话号的映射，同时用于同步网络回调与选服读取。</summary>
    private readonly Dictionary<int, int> owners = new Dictionary<int, int>();

    /// <summary>读取共享部署清单，兼容旧单区 XML 和无参数开发入口。</summary>
    /// <param name="realm">用于启动中心服的区服配置，无参数开发模式传空。</param>
    internal OnlineRealmDirectory(RealmTopology realm)
    {
        if (realm == null)
        {
            configured.Add(1, new GameServer { ServerId = 1, ServerName = "1区 五域大陆",
                IpHost = NetDefine.IPHost, Prot = NetDefine.GateServerPort, RunState = 3 });
            spawnMaps.Add(1, 1);
            return;
        }
        if (!realm.UsesRealmCatalog)
        {
            configured.Add(realm.RealmId, realm.PublicGate);
            spawnMaps.Add(realm.RealmId, realm.SpawnMap);
            return;
        }
        for (int index = 0; index < realm.RealmFiles.Count; index++)
        {
            // 各区的对外地址与名称来自管理员文件，网关只上报逻辑区编号。
            RealmTopology entry = new RealmTopology(realm.RealmFiles[index]);
            configured.Add(entry.RealmId, entry.PublicGate);
            spawnMaps.Add(entry.RealmId, entry.SpawnMap);
        }
    }

    /// <summary>登记一条已经可以接纳玩家的网关连接。</summary>
    /// <param name="realmId">网关配置中的逻辑区号。</param><param name="session">中心服分配的当前内网连接编号。</param>
    internal void Open(int realmId, int session)
    {
        lock (owners)
        {
            if (configured.ContainsKey(realmId)) owners[realmId] = session;
        }
    }

    /// <summary>网关下线或失去游戏线路时撤下它仍持有的入口。</summary>
    /// <param name="session">发出撤回或断开事件的连接编号。</param>
    internal void Close(int session)
    {
        lock (owners)
        {
            for (int index = 0; index < configured.Count; index++)
            {
                // 旧会话断开时不能撤下刚刚重连的网关。
                int id = configured.Keys[index];
                int owner;
                if (owners.TryGetValue(id, out owner) && owner == session) owners.Remove(id);
            }
        }
    }

    /// <summary>选角与建角前核实目标区当前可接入。</summary><param name="realmId">玩家选定的区号。</param>
    internal bool IsOpen(int realmId)
    {
        lock (owners) return owners.ContainsKey(realmId);
    }

    /// <summary>获取已通过选区检查的角色出生地图。</summary><param name="realmId">建角使用的逻辑区号。</param>
    internal int SpawnMap(int realmId) { return spawnMaps[realmId]; }

    /// <summary>只返回已登记的网关，账号上次进入的区可用时优先推荐。</summary>
    /// <param name="preferred">账号上次登录的区号。</param>
    internal GameServer Recommend(int preferred)
    {
        lock (owners)
        {
            if (owners.ContainsKey(preferred)) return configured[preferred];
            for (int index = 0; index < configured.Count; index++)
                if (owners.ContainsKey(configured.Keys[index])) return configured.Values[index];
            return null;
        }
    }

    /// <summary>按逻辑区号填写客户端原有的选服列表协议。</summary><param name="result">本次选服列表响应。</param>
    internal void Fill(GetServerListRet result)
    {
        lock (owners)
        {
            for (int index = 0; index < configured.Count; index++)
                if (owners.ContainsKey(configured.Keys[index])) result.GameServers.Add(configured.Values[index]);
        }
    }
}
