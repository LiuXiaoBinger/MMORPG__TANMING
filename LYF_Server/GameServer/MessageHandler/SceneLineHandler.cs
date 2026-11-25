using System.Collections.Generic;
using Google.Protobuf;

/// <summary>在现有 GS 逻辑线程执行公共场景接管与离场；框架要求实现 IGameMessageHandler。</summary>
public sealed class SceneLineHandler : IGameMessageHandler
{
    /// <summary>本机 GS 的运行时分线处理器。</summary>
    private static SceneLineHandler active;
    /// <summary>正在等待中心服确认脏模块的源角色。</summary>
    private readonly Dictionary<OnlineRole, PublicSceneLine> leaving = new Dictionary<OnlineRole, PublicSceneLine>();
    /// <summary>已断线但仍需等待中心服确认后才能放开再次登录的角色。</summary>
    private readonly List<OnlineRole> loggingOut = new List<OnlineRole>();
    /// <summary>一份公共地图实例只包含实际进入该线的角色，视野遍历限制在本实例内。</summary>
    private readonly Dictionary<long, List<OnlineRole>> scenes = new Dictionary<long, List<OnlineRole>>();
    /// <summary>本节点配置，切场时必须与调度指定的宿主一致。</summary>
    private readonly SceneNodeRegistration node;
    /// <summary>长期持有的调度连接；重连时重新注册节点。</summary>
    private readonly NetClient control;

    /// <summary>为本机物理 GS 建立调度登记连接。</summary>
    /// <param name="realm">所属逻辑区服。</param><param name="gameId">本机的物理编号。</param>
    private SceneLineHandler(RealmTopology realm, int gameId)
    {
        node = realm.Games[gameId];
        control = new NetClient(realm.Host, realm.ControlPort, ClientType.GameServer);
        control.OnConnSucceed = () => control.Send(SceneLineCode.Register, node.ToByteString());
    }

    /// <summary>为启用区服部署的 GS 注册内部切线协议。</summary>
    /// <param name="server">接入网关的监听服务。</param><param name="realm">当前区服。</param><param name="gameId">本机编号。</param>
    internal static void Start(NetServer server, RealmTopology realm, int gameId)
    {
        active = new SceneLineHandler(realm, gameId);
        MasterTeamLink.Instance.RealmNode = active.node;
        // 源节点离场、目标节点接管和单玩家断线都通过相同的可信复合会话处理。
        ushort[] codes = { SceneLineCode.Depart, SceneLineCode.Arrive, SceneLineCode.Disconnect };
        for (int index = 0; index < codes.Length; index++)
        {
            server.RegistCommand(codes[index], ClientMessageManager.Instance);
            ClientMessageManager.Instance.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, codes[index]),
                active, CenterMessageRoute.Internal);
        }
        active.control.StartConnect();
    }

    /// <summary>只有可信网关可提交场景内网命令。</summary><param name="source">真实链路来源。</param><param name="code">协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        if (source != GameCommandSource.GateServer) return false;
        return code == SceneLineCode.Depart || code == SceneLineCode.Arrive || code == SceneLineCode.Disconnect;
    }

    /// <summary>处理已经进入逻辑线程的玩家切场包。</summary><param name="context">包含网关、玩家会话和原始包体。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 可信网关连接号由接收端 Session 写入。
        BasePackage packet = context.Package;
        if (packet.ProtoCode == SceneLineCode.Arrive)
        {
            // 角色身份来自中心服查档或原 GS 交接，只通过内部网关链路传递。
            SceneRoleTransfer transfer = SceneRoleTransfer.Parser.ParseFrom(packet.Data);
            if (transfer.Scene.GameId != node.GameId || transfer.Role.ServerId != node.RealmId) return;
            // 恢复基础数据后，持久模块仍经 CenterServer 的正式 EnterWorld 路径加载。
            OnlineRole arrival = new OnlineRole { GateSessionId = packet.GateSessionId, UnitySessionId = packet.UnitySessionId,
                mainRoleInfo = transfer.Role.Clone(), PublicScene = transfer.Scene };
            RoleManager.Instance.AddOnlineRole(transfer.Role.BaseInfo.RoleId, arrival);
            JoinScene(arrival);
            if (!transfer.ChangingLine) { arrival.SendToClient(SceneLineCode.Arrive, ByteString.Empty); return; }
            arrival.IncomingScene = transfer;
            arrival.GetComponent<RoleSkillComponent>().ImportSceneCooldowns(transfer);
            packet.ProtoCode = NetDefine.CMD_EnterWroldCode;
            packet.Data = new EnterWroldReq { RoleId = transfer.Role.BaseInfo.RoleId }.ToByteString();
            context.Transport._client.Send(packet);
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
        // 玩家发起的切线仅允许在同一张公共地图内进行。
        PublicSceneLine target = PublicSceneLine.Parser.ParseFrom(packet.Data);
        if (role.mainRoleInfo.BaseInfo.InstanceId != 0 || target.MapId != role.mainRoleInfo.BaseInfo.MapId ||
            !role.IsRoleDataReady())
        { role.SendToClient(SceneLineCode.Depart, ByteString.Empty); return; }
        if (role.GetComponent<StateMachineComponent>().GetCurrentState() != StateDefine.kStateDefault ||
            role.ShieldRemaining > 0 || role.mainRoleInfo.BaseInfo.CurrHp <= 0)
        { role.SendToClient(SceneLineCode.Depart, ByteString.Empty); return; }
        if (target.GameId == node.GameId)
        {
            RoleManager.Instance.LeavePublicScene(role);
            LeaveScene(role);
            role.PublicScene = target;
            JoinScene(role);
            FinishArrival(role);
            return;
        }
        leaving[role] = target;
        role.FlushForSceneChange();
    }

    /// <summary>只对在途离场角色检查保存结果，普通逻辑帧不分配临时表。</summary>
    internal static void FlushDepartures()
    {
        if (active == null) return;
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
                Scene = active.leaving[role], ChangingLine = true };
            role.GetComponent<RoleSkillComponent>().ExportSceneCooldowns(transfer);
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
        RoleManager.Instance.BroadcastRoleEnterWorldByRole(role);
        RoleManager.Instance.ResendNearbyRoles(role);
    }

    /// <summary>登记一份公共场景的在线角色；同一宿主可同时持有多个同地图实例。</summary>
    /// <param name="role">刚进入该场景的真实角色。</param>
    private void JoinScene(OnlineRole role)
    {
        // 第一名角色到达时建立该运行期实例的角色集合。
        List<OnlineRole> members;
        if (!scenes.TryGetValue(role.PublicScene.SceneUid, out members))
        {
            members = new List<OnlineRole>();
            scenes.Add(role.PublicScene.SceneUid, members);
        }
        members.Add(role);
    }

    /// <summary>从原公共场景摘除该角色，最后一人离场时释放实例成员集合。</summary><param name="role">离场角色。</param>
    private static void LeaveScene(OnlineRole role)
    {
        // 下线和离场可先后到达，集合只保存仍归属于此场景的角色。
        List<OnlineRole> members;
        if (!active.scenes.TryGetValue(role.PublicScene.SceneUid, out members)) return;
        members.Remove(role);
        if (members.Count == 0) active.scenes.Remove(role.PublicScene.SceneUid);
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
