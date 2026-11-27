using System.Collections.Generic;
using System.Diagnostics;
using Google.Protobuf;

/// <summary>在 GS 逻辑线程执行公共实例创建与玩家交接。网络接收和逻辑分派分别要求 IContainer、IGameMessageHandler。</summary>
public sealed class SceneLineHandler : IGameMessageHandler, IContainer
{
    /// <summary>本机 GS 的运行时分线处理器。</summary>
    private static SceneLineHandler active;
    /// <summary>正在等待中心服确认脏模块的源角色。</summary>
    private readonly Dictionary<OnlineRole, SceneDeparture> leaving = new Dictionary<OnlineRole, SceneDeparture>();
    /// <summary>已断线但仍需等待中心服确认后才能放开再次登录的角色。</summary>
    private readonly List<OnlineRole> loggingOut = new List<OnlineRole>();
    /// <summary>一份公共地图实例只包含实际进入该线的角色，视野遍历限制在本实例内。</summary>
    private readonly Dictionary<long, List<OnlineRole>> scenes = new Dictionary<long, List<OnlineRole>>();
    /// <summary>本节点配置，切场时必须与调度指定的宿主一致。</summary>
    private readonly SceneNodeRegistration node;
    /// <summary>长期持有的调度连接；重连时重新注册节点。</summary>
    private readonly NetClient control;
    /// <summary>当前区启用市场时向其报告入场和离场角色。</summary>
    private readonly StallRoleLink trade;
    /// <summary>评分模式才持有本 GS 的进程句柄，采样累计处理器时间。</summary>
    private readonly Process process;
    /// <summary>两次 CPU 采样间的单调计时器；轮询模式不启动。</summary>
    private readonly Stopwatch sampleClock;
    /// <summary>上一采样点已使用的处理器毫秒数，各核累加。</summary>
    private double previousCpuMs;
    /// <summary>内网连接线程更新，逻辑帧只向已登记的控制服上报负载。</summary>
    private volatile bool registered;

    /// <summary>为本机物理 GS 建立调度登记连接。</summary>
    /// <param name="realm">所属逻辑区服。</param><param name="gameId">本机的物理编号。</param>
    private SceneLineHandler(RealmTopology realm, int gameId)
    {
        node = realm.Games[gameId];
        if (realm.TradePort > 0) trade = new StallRoleLink(realm, gameId);
        control = new NetClient(realm.Host, realm.ControlPort, ClientType.GameServer);
        if (realm.ChooseGsType == 2)
        {
            process = Process.GetCurrentProcess();
            previousCpuMs = process.TotalProcessorTime.TotalMilliseconds;
            sampleClock = Stopwatch.StartNew();
        }
        control.OnConnSucceed = () =>
        {
            // 新控制连接会为本机重发常驻场景，在同一逻辑队列内先清空上次登记的实例。
            GameGlobal.Instance.EnqueueGameCommand(GameCommandSource.ControlServer, control,
                new BasePackage { ProtoCode = SceneLineCode.Register });
            registered = control.Send(SceneLineCode.Register, node.ToByteString());
        };
        control.OnConnFailed = () => registered = false;
    }

    /// <summary>为启用区服部署的 GS 注册内部切线协议。</summary>
    /// <param name="server">接入网关的监听服务。</param><param name="realm">当前区服。</param><param name="gameId">本机编号。</param>
    internal static void Start(NetServer server, RealmTopology realm, int gameId)
    {
        active = new SceneLineHandler(realm, gameId);
        RedisManager.Instance.Start(realm);
        MasterTeamLink.Instance.RealmNode = active.node;
        // 源节点离场、目标节点接管和单玩家断线都通过相同的可信复合会话处理。
        ushort[] codes = { SceneLineCode.Depart, SceneLineCode.Arrive, SceneLineCode.Disconnect };
        for (int index = 0; index < codes.Length; index++)
        {
            server.RegistCommand(codes[index], ClientMessageManager.Instance);
            ClientMessageManager.Instance.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, codes[index]),
                active, CenterMessageRoute.Internal);
        }
        // 只在 GS 主动建立的控制链路上接收场景命令，网关侧没有这两个入口。
        ushort[] sceneCodes = { SceneLineCode.Register, SceneLineCode.CreateStaticScene, SceneLineCode.CloseStaticScene };
        for (int index = 0; index < sceneCodes.Length; index++)
        {
            // Register 在本机建立连接时入队。
            ushort code = sceneCodes[index];
            ClientMessageManager.Instance.Register(new GameMessageHandlerKey(GameCommandSource.ControlServer, code),
                active, CenterMessageRoute.Internal);
            if (code != SceneLineCode.Register) active.control.RegistCommand(code, active);
        }
        active.control.StartConnect();
    }

    /// <summary>网络容器的初始化入口，绑定操作由 Start 完成。</summary>
    public void OnInit() { }

    /// <summary>本容器只用于主动连接，玩家请求由网关的既有入口转交。</summary>
    /// <param name="link">监听端连接。</param><param name="packet">监听端收到的封包。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet) { }

    /// <summary>把控制服的实例命令交给现有逻辑线程，与玩家进出场串行处理。</summary>
    /// <param name="link">到本区控制服的连接。</param><param name="packet">实例命令。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet)
    {
        GameGlobal.Instance.EnqueueGameCommand(GameCommandSource.ControlServer, link, packet);
    }

    /// <summary>按连接来源分别接收建图与玩家交接命令。</summary><param name="source">真实链路来源。</param><param name="code">协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        if (source == GameCommandSource.ControlServer)
            return code == SceneLineCode.Register || code == SceneLineCode.CreateStaticScene || code == SceneLineCode.CloseStaticScene;
        if (source != GameCommandSource.GateServer) return false;
        return code == SceneLineCode.Depart || code == SceneLineCode.Arrive || code == SceneLineCode.Disconnect;
    }

    /// <summary>处理已经进入逻辑线程的玩家切场包。</summary><param name="context">包含网关、玩家会话和原始包体。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 可信网关连接号由接收端 Session 写入。
        BasePackage packet = context.Package;
        if (context.Source == GameCommandSource.ControlServer)
        {
            if (packet.ProtoCode == SceneLineCode.Register) { scenes.Clear(); return; }
            // 实例在逻辑线程上创建或回收，确认时其独立视野名单已经可用。
            PublicSceneLine scene = PublicSceneLine.Parser.ParseFrom(packet.Data);
            if (packet.ProtoCode == SceneLineCode.CloseStaticScene) scenes.Remove(scene.SceneUid);
            else
            {
                if (!scenes.ContainsKey(scene.SceneUid)) scenes.Add(scene.SceneUid, new List<OnlineRole>());
                control.Send(SceneLineCode.CreateStaticScene, packet.Data);
            }
            return;
        }
        if (packet.ProtoCode == SceneLineCode.Arrive)
        {
            // 角色身份来自中心服查档或原 GS 交接，只通过内部网关链路传递。
            SceneRoleTransfer transfer = SceneRoleTransfer.Parser.ParseFrom(packet.Data);
            if (transfer.Scene.GameId != node.GameId || transfer.Role.ServerId != node.RealmId) return;
            // 角色基础数据来自网关交接，跨 GS 的持久模块优先读取同版缓存。
            OnlineRole arrival = new OnlineRole { GateSessionId = packet.GateSessionId, UnitySessionId = packet.UnitySessionId,
                mainRoleInfo = transfer.Role.Clone(), PublicScene = transfer.Scene };
            RoleManager.Instance.AddOnlineRole(transfer.Role.BaseInfo.RoleId, arrival);
            JoinScene(arrival);
            if (!transfer.ChangingLine) { arrival.SendToClient(SceneLineCode.Arrive, ByteString.Empty); return; }
            arrival.IncomingScene = transfer;
            arrival.GetComponent<RoleSkillComponent>().ImportSceneCooldowns(transfer);
            RedisManager.Instance.Load(arrival, context.Transport._client);
            return;
        }
        // 本 GS 只能操作真实会话当前绑定的角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
        if (role == null) return;
        if (packet.ProtoCode == SceneLineCode.Disconnect)
        {
            leaving.Remove(role);
            RoleManager.Instance.BroadcastRoleLeave(role);
            RoleManager.Instance.RemoveOnlineRole((int)role.GetID());
            return;
        }
        // 同地图沿用切线流程，跨城时必须站在通往目标地图的实际传送门。
        SceneDeparture departure = SceneDeparture.Parser.ParseFrom(packet.Data);
        // 线路由调度服分配；门号单独来自本次玩家在网关发起的交通操作。
        PublicSceneLine target = departure.Target;
        PublicCityPortal portal = null;
        if (departure.Portal != null)
        {
            portal = PublicCityPortal.Find(role.mainRoleInfo.BaseInfo.MapId, departure.Portal.DoorId);
            if (!CityPortalHandler.CanEnter(role, portal) || portal.Destination.MapId != target.MapId)
            { role.SendToClient(SceneLineCode.Depart, ByteString.Empty); return; }
        }
        else if (target.MapId != role.mainRoleInfo.BaseInfo.MapId)
        { role.SendToClient(SceneLineCode.Depart, ByteString.Empty); return; }
        if (role.mainRoleInfo.BaseInfo.InstanceId != 0 || !role.IsRoleDataReady())
        { role.SendToClient(SceneLineCode.Depart, ByteString.Empty); return; }
        // 玩家手动换公共线路沿用战斗限制，服务端已批准的副本入场及退本直接交接。
        if (target.LineId != 0 && role.PublicScene.LineId != 0)
        {
            if (role.GetComponent<StateMachineComponent>().GetCurrentState() != StateDefine.kStateDefault ||
                role.ShieldRemaining > 0 || role.mainRoleInfo.BaseInfo.CurrHp <= 0)
            { role.SendToClient(SceneLineCode.Depart, ByteString.Empty); return; }
        }
        if (target.GameId == node.GameId)
        {
            RoleManager.Instance.LeavePublicScene(role);
            LeaveScene(role);
            CityPortalHandler.PlaceArrival(role.mainRoleInfo.BaseInfo, portal);
            role.PublicScene = target;
            JoinScene(role);
            FinishArrival(role);
            return;
        }
        leaving[role] = departure;
        role.FlushForSceneChange();
    }

    /// <summary>只对在途离场角色检查保存结果，普通逻辑帧不分配临时表。</summary>
    internal static void FlushDepartures()
    {
        if (active == null) return;
        active.ReportHostLoad();
        active.trade?.Update();
        for (int index = active.loggingOut.Count - 1; index >= 0; index--)
        {
            // 正常离线已经走原保存流程，中心服确认后向调度归还角色名额。
            OnlineRole closed = active.loggingOut[index];
            if (!closed.CanDispose()) continue;
            if (active.control.Send(SceneLineCode.Saved, new SceneRouteRequest { RoleId = (int)closed.GetID(),
                GameId = active.node.GameId, SceneUid = closed.PublicScene.SceneUid }.ToByteString()))
                active.loggingOut.RemoveAt(index);
        }
        if (active.leaving.Count == 0) return;
        // 交接成功后会移除角色，故迭代引用快照。
        List<OnlineRole> roles = new List<OnlineRole>(active.leaving.Keys);
        for (int index = 0; index < roles.Count; index++)
        {
            // 已由本机接收并停止玩家上行的角色。
            OnlineRole role = roles[index];
            if (role.State != OnlineRoleState.Online) { active.leaving.Remove(role); continue; }
            if (!role.CanDispose()) { role.FlushForSceneChange(); continue; }
            // 此时背包、技能与次数版本均已获得现有中心服写入确认。
            SceneRoleTransfer transfer = new SceneRoleTransfer { Role = role.mainRoleInfo.Clone(),
                Scene = active.leaving[role].Target, ChangingLine = true, SavedVersion = role.GetSaveVersion() };
            // 跨宿主也使用离场时核准的门；普通换线保留原坐标。
            ScenePortalRequest source = active.leaving[role].Portal;
            if (source != null) CityPortalHandler.PlaceArrival(transfer.Role.BaseInfo,
                PublicCityPortal.Find(transfer.Role.BaseInfo.MapId, source.DoorId));
            role.GetComponent<RoleSkillComponent>().ExportSceneCooldowns(transfer);
            role.GetComponent<RoItemComponent>().ExportSceneGridCounts(transfer);
            RoleManager.Instance.LeavePublicScene(role);
            LeaveScene(role);
            role.SendToClient(SceneLineCode.Depart, transfer.ToByteString());
            active.leaving.Remove(role);
            RoleManager.Instance.DetachForSceneChange(role);
        }
    }

    /// <summary>角色接管成功后先切换网关路由，再重建新场景的双向视野。</summary>
    /// <param name="role">已加载自身数据的场景角色。</param>
    internal static void FinishArrival(OnlineRole role)
    {
        role.IncomingScene = null;
        role.SendToClient(SceneLineCode.Arrive, ByteString.Empty);
        if (role.PublicScene.LineId == 0)
        {
            // 网关已先切到本机，入本通知及初始怪物快照现在才会到达正确的客户端。
            if (!TeamManager.Instance.EnterPreparedMember(role, (ulong)-role.PublicScene.SceneUid))
            {
                // 交接完成前实例已回收，沿正常退本路由返回原公共地图。
                role.SendToClient(NetDefine.CMD_ErrCode, new ErrMsg { CmdCode = CmdCode.ServerError }.ToByteString());
                role.SendToClient(NetDefine.CMD_TeamEnterDungeonCode, new TeamEnterDungeonNtf { SceneId = TeamManager.MainCitySceneId,
                    MapId = role.PublicScene.MapId, Pos = role.mainRoleInfo.BaseInfo.Pos }.ToByteString());
            }
            return;
        }
        RoleManager.Instance.BroadcastRoleEnterWorldByRole(role);
        RoleManager.Instance.ResendNearbyRoles(role);
    }

    /// <summary>评分模式每五秒在逻辑线程报告进程 CPU 与活跃副本数。</summary>
    private void ReportHostLoad()
    {
        if (sampleClock == null || !registered || sampleClock.ElapsedMilliseconds < 5000) return;
        // 以实际经过的墙钟时间为分母；使用一个完整核心记为 10000，沿用 RO 的单核口径。
        double cpuMs = process.TotalProcessorTime.TotalMilliseconds;
        int cpuRate = (int)((cpuMs - previousCpuMs) * 10000 / sampleClock.Elapsed.TotalMilliseconds);
        previousCpuMs = cpuMs;
        sampleClock.Restart();
        control.Send(SceneLineCode.HostLoad, new SceneHostLoad { GameId = node.GameId, CpuRate = cpuRate,
            DungeonCount = TeamManager.Instance.ActiveDungeonCount }.ToByteString());
    }

    /// <summary>把角色加入调度服已经确认开放的 GS 实例。</summary>
    /// <param name="role">刚进入该场景的真实角色。</param>
    private void JoinScene(OnlineRole role)
    {
        if (role.PublicScene.LineId == 0) return;
        scenes[role.PublicScene.SceneUid].Add(role);
        trade?.SetRole(role, true);
    }

    /// <summary>从原公共场景摘除角色，空线保持可进入，直到控制服通知回收。</summary><param name="role">离场角色。</param>
    private static void LeaveScene(OnlineRole role)
    {
        // 下线和离场可先后到达，集合只保存仍归属于此场景的角色。
        List<OnlineRole> members;
        if (!active.scenes.TryGetValue(role.PublicScene.SceneUid, out members)) return;
        members.Remove(role);
        active.trade?.SetRole(role, false);
    }

    /// <summary>复制本公共实例的角色引用供 AOI 遍历，独立副本沿用原有名单过滤。</summary>
    /// <param name="role">广播来源或需要补发视野的角色。</param>
    internal static List<OnlineRole> Observers(OnlineRole role)
    {
        if (active == null || role == null || role.PublicScene == null) return null;
        if (role.mainRoleInfo.BaseInfo.InstanceId != 0) return null;
        // 该 GS 内属于相同场景唯一编号的玩家。
        List<OnlineRole> members;
        if (active.scenes.TryGetValue(role.PublicScene.SceneUid, out members)) return new List<OnlineRole>(members);
        return new List<OnlineRole>();
    }

    /// <summary>玩家下线时先从公共视野移除，存档完成后释放控制服登记。</summary><param name="role">实际离线角色。</param>
    internal static void TrackLogout(OnlineRole role)
    {
        if (active == null || role.PublicScene == null) return;
        LeaveScene(role);
        if (!active.loggingOut.Contains(role)) active.loggingOut.Add(role);
    }
}
