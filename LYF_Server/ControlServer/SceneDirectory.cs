using System.Collections.Generic;
using Google.Protobuf;

/// <summary>本区的公共场景目录。控制服在同一把锁内修改线路名额与角色位置。</summary>
internal sealed class SceneDirectory : IContainer
{
    /// <summary>本调度进程负责的逻辑区服。</summary>
    private readonly RealmTopology realm;
    /// <summary>物理 GS 编号到当前受信连接的对应关系。</summary>
    private readonly SortedList<int, Session> hosts = new SortedList<int, Session>();
    /// <summary>评分模式下各注册宿主最近一次上报的 CPU 和副本数。</summary>
    private readonly Dictionary<int, SceneHostLoad> loads = new Dictionary<int, SceneHostLoad>();
    /// <summary>上次承载普通扩展线的 GS，下一次从其后的在线物理编号继续轮询。</summary>
    private int lastPollingGs = -1;
    /// <summary>角色进入游戏或切线时在同一区服只占一份权威归属。</summary>
    private readonly Dictionary<int, Admission> roles = new Dictionary<int, Admission>();
    /// <summary>运行期实例编号在每次创建场景时递增。</summary>
    private long nextUid;
    /// <summary>由负载触发的临时场景 UID，仅这些场景在空闲时回收。</summary>
    private readonly HashSet<long> elastic = new HashSet<long>();
    /// <summary>尚待 GS 创建回执的实例，以已发出的 UID 对应常驻配置或新线条目。</summary>
    private readonly Dictionary<long, PublicSceneLine> creating = new Dictionary<long, PublicSceneLine>();
    /// <summary>各实例已分配但尚未完成进图的人数，参与选线占位，成功接管后才计入在线人数。</summary>
    private readonly Dictionary<long, int> reservations = new Dictionary<long, int>();

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
    internal SceneDirectory(RealmTopology config)
    {
        realm = config;
        realm.Scenes.Sort((left, right) => left.LineId.CompareTo(right.LineId));
    }

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
        Session session = (Session)link;
        // 连接号用于与网关持有的角色归属比较。
        int connection = session.SessionID;
        lock (hosts)
        {
            if (packet.ProtoCode == SceneLineCode.ChooseDungeonHost)
            {
                // MS 已分配实例号，控制服对副本和公共新线共用同一份在线节点与选服策略。
                MasterTeamDungeonOrder order = MasterTeamDungeonOrder.Parser.ParseFrom(packet.Data);
                // 没有宿主时保留失败结果，零号 GS 本身是合法节点。
                MasterDungeonHostResult choice = new MasterDungeonHostResult { TeamId = order.TeamId, InstanceId = order.InstanceId };
                if (hosts.Count > 0) { choice.GameId = GetGsLine(); choice.Success = true; }
                link.Send(SceneLineCode.ChooseDungeonHost, choice.ToByteString());
                return;
            }
            if (packet.ProtoCode == SceneLineCode.Register)
            {
                // 只有当前区服配置中的节点才有承载资格。
                SceneNodeRegistration node = SceneNodeRegistration.Parser.ParseFrom(packet.Data);
                if (node.RealmId != realm.RealmId || !realm.Games.ContainsKey(node.GameId)) return;
                if (hosts.ContainsKey(node.GameId)) return;
                hosts.Add(node.GameId, session);
                // 宿主实际建立各条常驻线后才能接收该线的玩家。
                for (int index = 0; index < realm.Scenes.Count; index++)
                    if (realm.Scenes[index].GameId == node.GameId) CreateStaticScene(realm.Scenes[index]);
                return;
            }
            if (packet.ProtoCode == SceneLineCode.CreateStaticScene)
            {
                // 回执必须来自本次请求所选的宿主。
                PublicSceneLine reply = PublicSceneLine.Parser.ParseFrom(packet.Data);
                PublicSceneLine scene;
                if (!creating.TryGetValue(reply.SceneUid, out scene)) return;
                if (hosts[scene.GameId] != session) return;
                creating.Remove(reply.SceneUid);
                scene.SceneUid = reply.SceneUid;
                if (!realm.Scenes.Contains(scene))
                {
                    elastic.Add(scene.SceneUid);
                    realm.Scenes.Add(scene);
                    realm.Scenes.Sort((left, right) => left.LineId.CompareTo(right.LineId));
                    Recycle();
                }
                return;
            }
            if (packet.ProtoCode == SceneLineCode.HostLoad)
            {
                // 只接受登记该物理编号的连接发布的实测值。
                SceneHostLoad report = SceneHostLoad.Parser.ParseFrom(packet.Data);
                Session registered;
                if (hosts.TryGetValue(report.GameId, out registered) && registered == session)
                    loads[report.GameId] = report;
                return;
            }
            // 原始角色号由网关在选角和中心服读取后绑定。
            SceneRouteRequest request = SceneRouteRequest.Parser.ParseFrom(packet.Data);
            if (packet.ProtoCode == SceneLineCode.Saved)
            {
                // 校验原宿主链路和实例，迟到的回执不能释放已迁往另一个 GS 的角色。
                Session registered;
                Admission saved;
                if (!hosts.TryGetValue(request.GameId, out registered) || registered != session) return;
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
            ChangeReservation(role.Pending, -1);
            if (code == SceneLineCode.Commit)
            {
                if (role.Current != null) role.Current.Population--;
                role.Current = role.Pending;
                role.Current.Population++;
                if (role.Current.LineId != 0) Expand(role.Current);
            }
            role.Pending = null;
            if (role.Current == null) roles.Remove(request.RoleId);
            Recycle();
            return null;
        }
        if (role != null && role.Current != null) result.CurrentLine = role.Current.LineId;
        if (code == SceneLineCode.List)
        {
            for (int index = 0; index < realm.Scenes.Count; index++)
            {
                // 创建中的线路和掉线节点不出现在玩家菜单中。
                PublicSceneLine scene = realm.Scenes[index];
                if (scene.MapId == request.MapId && scene.SceneUid != 0) result.Lines.Add(scene.Clone());
            }
            return result;
        }
        // 初次选角没有当前 GS，跨图则使用已经确认的原宿主作为软偏好。
        PublicSceneLine target;
        if (request.SceneUid < 0 && request.RequireGame)
        {
            // 负路由号对应已建好的私有实例。保留出发地图供退本返回，公共线容量不参与进本。
            target = null;
            if (role?.Current != null && hosts.ContainsKey(request.GameId))
                target = new PublicSceneLine { SceneUid = request.SceneUid, GameId = request.GameId, MapId = role.Current.MapId };
        }
        else target = SelectPrioritySceneLine(request, role?.Current);
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
        ChangeReservation(target, 1);
        result.Lines.Add(target.Clone());
        return result;
    }

    /// <summary>指定线和集结宿主须有空位。跨图优先原宿主，其次按线号找警戒以下的线，再选人数最少的线。</summary>
    /// <param name="request">网关传入的目标地图及手动选线或集结条件。</param>
    /// <param name="current">控制服已确认的原场景，首次登录为空。</param>
    private PublicSceneLine SelectPrioritySceneLine(SceneRouteRequest request, PublicSceneLine current)
    {
        // 配有扩线规则的公共地图使用 RO 的警戒值；旧固定线路仍以各自容量接纳。
        RealmTopology.MapPolicy policy;
        realm.MapPolicies.TryGetValue(request.MapId, out policy);
        // RO 的登录调用传入人数 0，已在线的一人通过传送点转入时计 1。
        int incoming = 0;
        if (current != null) incoming = 1;
        // 保留第一个低于警戒的候选，继续向后检查是否存在原宿主。
        PublicSceneLine belowWarning = null;
        // 全线都到警戒时用占用最少的实例，同人数保留先遇到的较小线号。
        PublicSceneLine least = null;
        int leastOccupied = int.MaxValue;
        for (int index = 0; index < realm.Scenes.Count; index++)
        {
            // 目录按线号排序，预留人数参与本次选择，避免同时进图挤到同一空位。
            PublicSceneLine scene = realm.Scenes[index];
            if (scene.MapId != request.MapId || scene.SceneUid == 0) continue;
            if (request.RequireGame && scene.GameId != request.GameId) continue;
            if (request.LineId != 0 && scene.LineId != request.LineId) continue;
            // 当前占用包含等待 GS 接管的角色。
            int pending;
            reservations.TryGetValue(scene.SceneUid, out pending);
            int occupied = scene.Population + pending;
            if (request.LineId != 0 || request.RequireGame)
            {
                if (occupied < scene.Capacity) return scene;
                continue;
            }
            if (current != null && scene.GameId == current.GameId && occupied < scene.Capacity) return scene;
            if (policy == null && occupied >= scene.Capacity) continue;
            if (belowWarning == null && (policy == null || occupied < policy.Warning - incoming))
                belowWarning = scene;
            if (occupied < leastOccupied)
            {
                least = scene;
                leastOccupied = occupied;
            }
        }
        return belowWarning ?? least;
    }

    /// <summary>更新正在交接的占位，最后一个预留结束后移除计数。</summary>
    /// <param name="scene">待接管的目标实例。</param><param name="change">预留为 1，提交或取消为 -1。</param>
    private void ChangeReservation(PublicSceneLine scene, int change)
    {
        // 私有副本由队伍名单准入，不预占任何公共线路的名额。
        if (scene.LineId == 0) return;
        // 本实例已签发且尚未完成的入场数。
        int count;
        reservations.TryGetValue(scene.SceneUid, out count);
        count += change;
        if (count == 0) reservations.Remove(scene.SceneUid);
        else reservations[scene.SceneUid] = count;
    }

    /// <summary>一次真实入场后，检查同图所有已开线路。每张地图同时只向一个宿主申请一条新线。</summary>
    /// <param name="arrived">刚由 GS 接管玩家的线路。</param>
    private void Expand(PublicSceneLine arrived)
    {
        // 未配置扩线的地图、未达警戒的入场均不需要扫描目录。
        RealmTopology.MapPolicy policy;
        if (!realm.MapPolicies.TryGetValue(arrived.MapId, out policy)) return;
        if (arrived.Population < policy.Warning) return;
        // 常驻线也要等创建回执，刚启动的 GS 正在开图时不重复扩线。
        List<PublicSceneLine> inFlight = new List<PublicSceneLine>(creating.Values);
        if (inFlight.Exists(scene => scene.MapId == arrived.MapId)) return;
        // 为离线宿主的常驻线保留原编号；弹性线优先使用已回收的空号。
        HashSet<int> numbers = new HashSet<int>();
        for (int index = 0; index < realm.Scenes.Count; index++)
        {
            // 只统计同一张地图，其他地图的人数不参与触发。
            PublicSceneLine scene = realm.Scenes[index];
            if (scene.MapId != arrived.MapId) continue;
            if (scene.SceneUid != 0 && scene.Population < policy.Warning) return;
            numbers.Add(scene.LineId);
        }
        if (numbers.Count >= policy.MaxLines) return;
        // 本地图最小可用的玩家可见线号。
        int line = 1;
        while (numbers.Contains(line)) line++;
        CreateStaticScene(new PublicSceneLine { MapId = arrived.MapId, LineId = line,
            Capacity = arrived.Capacity, GameId = GetGsLine() });
    }

    /// <summary>发送实例创建命令，目录项只在指定 GS 回送同一 UID 后发布。</summary>
    /// <param name="scene">固定线路模板或尚未加入目录的弹性线路。</param>
    private void CreateStaticScene(PublicSceneLine scene)
    {
        // 发给 GS 的副本携带新 UID，待回执的常驻配置仍保持未开放。
        PublicSceneLine command = scene.Clone();
        command.SceneUid = ++nextUid;
        if (hosts[scene.GameId].Send(SceneLineCode.CreateStaticScene, command.ToByteString()))
            creating.Add(command.SceneUid, scene);
    }

    /// <summary>在本区已注册的普通 GS 中轮询，或按 RO 负载评分选择新场景宿主。</summary>
    private int GetGsLine()
    {
        if (realm.ChooseGsType == 1)
        {
            // 物理编号升序轮转，两个城市共用同一个游标。
            int next = 0;
            while (next < hosts.Count && hosts.Keys[next] <= lastPollingGs) next++;
            if (next == hosts.Count) next = 0;
            lastPollingGs = hosts.Keys[next];
            return lastPollingGs;
        }
        // 优先 CPU 低于 60% 的节点；全部达到警戒时在高负载节点中取最低分。
        int bestHost = -1;
        bool bestHigh = true;
        long bestScore = long.MaxValue;
        for (int index = 0; index < hosts.Count; index++)
        {
            // 只评价仍登记的 GS，刚登记时 CPU 与副本数按 RO 初值零计算。
            int host = hosts.Keys[index];
            SceneHostLoad load;
            loads.TryGetValue(host, out load);
            int cpu = load?.CpuRate ?? 0;
            bool high = cpu >= 6000;
            long score = (cpu / 100) * 30L + (load?.DungeonCount ?? 0) * 40L;
            for (int sceneIndex = 0; sceneIndex < realm.Scenes.Count; sceneIndex++)
            {
                // 已登记的城市及其室内公共地图每实例计 10，其他野外实例计 20。
                PublicSceneLine scene = realm.Scenes[sceneIndex];
                if (scene.SceneUid == 0 || scene.GameId != host) continue;
                if (PublicCityPortal.Find(scene.MapId) != null) score += 10;
                else score += 20;
            }
            if (high && !bestHigh) continue;
            if (high == bestHigh && score >= bestScore) continue;
            bestHost = host;
            bestHigh = high;
            bestScore = score;
        }
        return bestHost;
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
            if (reservations.ContainsKey(scene.SceneUid)) continue;
            // 仍需一条空线缓冲时保留当前实例。
            int warning = realm.MapPolicies[scene.MapId].Warning;
            bool otherAvailable = realm.Scenes.Exists(other =>
            {
                if (other == scene || other.SceneUid == 0) return false;
                return other.MapId == scene.MapId && other.Population < warning;
            });
            if (!otherAvailable) continue;
            // 宿主仍在线时释放对应的 GS 实例及其空的视野名单。
            Session host;
            if (hosts.TryGetValue(scene.GameId, out host))
                host.Send(SceneLineCode.CloseStaticScene, scene.ToByteString());
            elastic.Remove(scene.SceneUid);
            realm.Scenes.RemoveAt(index);
        }
    }

    /// <summary>回收指定角色的已进入名额与切线预留名额。</summary>
    /// <param name="id">持久角色编号。</param><param name="role">本次连接登记的归属。</param>
    private void Release(int id, Admission role)
    {
        if (role.Current != null) role.Current.Population--;
        if (role.Pending != null) ChangeReservation(role.Pending, -1);
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
                if (hosts[ids[index]].SessionID == connection) { lost = ids[index]; hosts.Remove(lost); break; }
            if (lost >= 0)
            {
                loads.Remove(lost);
                // 撤销断开宿主尚未回复的创建，其他在线宿主下次入场时可继续扩线。
                List<long> requests = new List<long>(creating.Keys);
                for (int index = 0; index < requests.Count; index++)
                    if (creating[requests[index]].GameId == lost) creating.Remove(requests[index]);
            }
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
