using System.Collections.Generic;
using Google.Protobuf;

/// <summary>本区的公共场景目录。控制服在同一把锁内修改线路名额与角色位置。</summary>
internal sealed class SceneDirectory : IContainer
{
    /// <summary>本调度进程负责的逻辑区服。</summary>
    private readonly RealmTopology realm;
    /// <summary>物理 GS 编号到当前受信连接的对应关系。</summary>
    private readonly Dictionary<int, int> hosts = new Dictionary<int, int>();
    /// <summary>角色进入游戏或切线时在同一区服只占一份权威归属。</summary>
    private readonly Dictionary<int, Admission> roles = new Dictionary<int, Admission>();
    /// <summary>运行期实例编号在每次创建场景时递增。</summary>
    private long nextUid;
    /// <summary>由负载触发的临时场景 UID，仅这些场景在空闲时回收。</summary>
    private readonly HashSet<long> elastic = new HashSet<long>();

    /// <summary>同一角色由哪个网关玩家会话持有，以及其已进入和待转入的实例。</summary>
    private sealed class Admission
    {
        /// <summary>持有角色的网关内网会话。</summary>
        internal int Gate;
        /// <summary>玩家在该网关内的会话号。</summary>
        internal int Player;
        /// <summary>最近一次确认接管的场景。</summary>
        internal PublicSceneLine Current;
        /// <summary>已预占名额、尚待 GS 确认的目标。</summary>
        internal PublicSceneLine Pending;
    }

    /// <summary>加载当前区服的可部署场景。</summary><param name="config">本进程所属区服。</param>
    internal SceneDirectory(RealmTopology config) { realm = config; }

    /// <summary>目录在构造时就绪。</summary>
    public void OnInit() { }

    /// <summary>该服务使用监听会话接收请求。</summary>
    /// <param name="link">主动连接入口。</param><param name="packet">内网协议。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet) { }

    /// <summary>由网络框架分派内网节点的注册和场景调度。</summary>
    /// <param name="link">真实的 GS 或网关 TCP 连接。</param><param name="packet">玩家会话及路由命令。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 运行中的内网连接号用来识别登记和后续断开。
        int connection = ((Session)link).SessionID;
        lock (hosts)
        {
            if (packet.ProtoCode == SceneLineCode.Register)
            {
                // 只有当前区服配置中的节点才有承载资格。
                SceneNodeRegistration node = SceneNodeRegistration.Parser.ParseFrom(packet.Data);
                if (node.RealmId != realm.RealmId || !realm.Games.ContainsKey(node.GameId)) return;
                if (hosts.ContainsKey(node.GameId)) return;
                hosts.Add(node.GameId, connection);
                // 同一 GS 可承载多张地图以及同一地图的多条线。
                for (int index = 0; index < realm.Scenes.Count; index++)
                    if (realm.Scenes[index].GameId == node.GameId) realm.Scenes[index].SceneUid = ++nextUid;
                return;
            }
            // 原始角色号由网关在选角和中心服读取后绑定。
            SceneRouteRequest request = SceneRouteRequest.Parser.ParseFrom(packet.Data);
            if (packet.ProtoCode == SceneLineCode.Saved)
            {
                // 校验原宿主链路和实例，迟到的回执不能释放已迁往另一个 GS 的角色。
                int registered;
                Admission saved;
                if (!hosts.TryGetValue(request.GameId, out registered) || registered != connection) return;
                if (!roles.TryGetValue(request.RoleId, out saved)) return;
                bool current = saved.Current != null && saved.Current.SceneUid == request.SceneUid;
                bool pending = saved.Pending != null && saved.Pending.SceneUid == request.SceneUid;
                if (current || pending) Release(request.RoleId, saved);
                return;
            }
            // 查询或名额变更的返回结构。
            SceneLineResult result = Route(connection, packet.UnitySessionId, packet.ProtoCode, request);
            if (result != null) link.Send(packet.UnitySessionId, packet.ProtoCode, result.ToByteString());
        }
    }

    /// <summary>按网关会话、角色和当前地图读取或变更线路占用。</summary>
    /// <param name="gate">控制服内的网关连接号。</param><param name="player">客户端会话。</param>
    /// <param name="code">内部路由操作。</param><param name="request">角色与场景选择。</param>
    private SceneLineResult Route(int gate, int player, int code, SceneRouteRequest request)
    {
        // 返回给网关的选线或列表结果；提交和释放无需回包。
        SceneLineResult result = new SceneLineResult();
        // 区服唯一角色归属，防止另一连接抢占同一角色。
        Admission role;
        roles.TryGetValue(request.RoleId, out role);
        if (role != null && (role.Gate != gate || role.Player != player))
        {
            result.Reason = "角色已在另一个会话中。";
            return result;
        }
        if (code == SceneLineCode.Disconnect)
        {
            // 已进入的角色由 GS 在存档完成后释放，避免重登录从另一个 GS 读出旧背包。
            if (role != null && role.Current == null) Release(request.RoleId, role);
            return null;
        }
        if (code == SceneLineCode.Commit || code == SceneLineCode.Cancel)
        {
            if (role == null || role.Pending == null) return null;
            if (code == SceneLineCode.Commit)
            {
                if (role.Current != null) role.Current.Population--;
                role.Current = role.Pending;
            }
            else role.Pending.Population--;
            Recycle();
            role.Pending = null;
            if (role.Current == null) roles.Remove(request.RoleId);
            return null;
        }
        if (role != null && role.Current != null) result.CurrentLine = role.Current.LineId;
        // 自动进线优先配置顺序，已满或未登记的宿主不会收到分配。
        PublicSceneLine target = null;
        for (int index = 0; index < realm.Scenes.Count; index++)
        {
            // 该目录项是一个已配置的独立场景。
            PublicSceneLine scene = realm.Scenes[index];
            if (scene.MapId != request.MapId || scene.SceneUid == 0) continue;
            if (request.RequireGame && scene.GameId != request.GameId) continue;
            if (code == SceneLineCode.List) result.Lines.Add(scene.Clone());
            if (request.LineId != 0 && scene.LineId != request.LineId) continue;
            if (scene.Population < scene.Capacity && target == null) target = scene;
        }
        if (code == SceneLineCode.List) return result;
        if (target == null) result.Reason = "目标线路未开放或人数已满。";
        else if (role != null && role.Current == target) result.Reason = "已经在这条线路。";
        else if (role != null && role.Pending != null) result.Reason = "正在切换场景。";
        if (result.Reason.Length != 0) return result;
        if (role == null)
        {
            role = new Admission { Gate = gate, Player = player };
            roles.Add(request.RoleId, role);
        }
        role.Pending = target;
        target.Population++;
        result.Lines.Add(target.Clone());
        Expand(target.MapId);
        return result;
    }

    /// <summary>所有已开放线均达警戒时，将新实例放到当前承载人数最少的可用节点。</summary>
    /// <param name="map">刚分配到角色的地图。</param>
    private void Expand(int map)
    {
        // 未配置扩展阈值的地图只运行指定常驻线路。
        RealmTopology.MapPolicy policy;
        if (!realm.MapPolicies.TryGetValue(map, out policy)) return;
        // 只统计本次已通过登记的在线宿主。
        Dictionary<int, int> loads = new Dictionary<int, int>();
        List<int> nodes = new List<int>(hosts.Keys);
        nodes.Sort();
        for (int index = 0; index < nodes.Count; index++) loads.Add(nodes[index], 0);
        // 玩家可见线号允许复用已经回收的空号，场景 UID 每次重建均递增。
        HashSet<int> numbers = new HashSet<int>();
        int capacity = 0;
        for (int index = 0; index < realm.Scenes.Count; index++)
        {
            // 汇总整台 GS 的角色占用，同时核查本地图是否还有宽松线路。
            PublicSceneLine scene = realm.Scenes[index];
            if (scene.SceneUid != 0) loads[scene.GameId] += scene.Population;
            if (scene.MapId != map) continue;
            numbers.Add(scene.LineId);
            if (scene.SceneUid == 0) continue;
            if (scene.Population < policy.Warning) return;
            capacity = scene.Capacity;
        }
        if (numbers.Count >= policy.MaxLines || capacity == 0) return;
        // 同负载时使用较小的物理编号保证选择可复现。
        int host = nodes[0];
        for (int index = 1; index < nodes.Count; index++)
            if (loads[nodes[index]] < loads[host]) host = nodes[index];
        int line = 1;
        while (numbers.Contains(line)) line++;
        PublicSceneLine opened = new PublicSceneLine { MapId = map, LineId = line, GameId = host,
            Capacity = capacity, SceneUid = ++nextUid };
        realm.Scenes.Add(opened);
        elastic.Add(opened.SceneUid);
    }

    /// <summary>回收无人的弹性场景，若其他线仍拥挤则保留空线供下一名玩家进入。</summary>
    private void Recycle()
    {
        if (elastic.Count == 0) return;
        for (int index = realm.Scenes.Count - 1; index >= 0; index--)
        {
            // 固定线路永远保留，只有未占用的临时线路可释放。
            PublicSceneLine scene = realm.Scenes[index];
            if (scene.Population != 0 || !elastic.Contains(scene.SceneUid)) continue;
            // 仍需一条空线缓冲时保留当前实例。
            int warning = realm.MapPolicies[scene.MapId].Warning;
            bool otherAvailable = realm.Scenes.Exists(other =>
            {
                if (other == scene || other.SceneUid == 0) return false;
                return other.MapId == scene.MapId && other.Population < warning;
            });
            if (!otherAvailable) continue;
            elastic.Remove(scene.SceneUid);
            realm.Scenes.RemoveAt(index);
        }
    }

    /// <summary>回收指定角色的已进入名额与切线预留名额。</summary>
    /// <param name="id">持久角色编号。</param><param name="role">本次连接登记的归属。</param>
    private void Release(int id, Admission role)
    {
        if (role.Current != null) role.Current.Population--;
        if (role.Pending != null) role.Pending.Population--;
        roles.Remove(id);
        Recycle();
    }

    /// <summary>移除断开的宿主实例以及断开网关所持有的角色登记。</summary>
    /// <param name="connection">控制服监听端分配的连接编号。</param>
    internal void Disconnected(int connection)
    {
        lock (hosts)
        {
            // 一条内网连接只登记一个物理节点。
            int lost = -1;
            // 遍历快照以支持移除已断开的节点。
            List<int> ids = new List<int>(hosts.Keys);
            for (int index = 0; index < ids.Count; index++)
                if (hosts[ids[index]] == connection) { lost = ids[index]; hosts.Remove(lost); break; }
            // 网关断开时释放整个会话集；GS 断开时使该节点的场景不可再分配。
            List<int> players = new List<int>(roles.Keys);
            for (int index = 0; index < players.Count; index++)
            {
                // 正在交接的角色由网关确认或取消；宿主失效时该场景不能接纳新角色。
                Admission role = roles[players[index]];
                bool lostCurrent = role.Current != null && role.Current.GameId == lost;
                bool lostPending = role.Pending != null && role.Pending.GameId == lost;
                bool lostLogin = role.Gate == connection && role.Current == null;
                if (lostCurrent || lostPending || lostLogin)
                    Release(players[index], role);
            }
            for (int index = realm.Scenes.Count - 1; index >= 0; index--)
            {
                // 常驻线等待原宿主重新登记，弹性实例随本次宿主断开而回收。
                PublicSceneLine scene = realm.Scenes[index];
                if (scene.GameId != lost) continue;
                if (elastic.Remove(scene.SceneUid)) realm.Scenes.RemoveAt(index);
                else scene.SceneUid = 0;
            }
        }
    }
}
