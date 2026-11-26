using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

/// <summary>在 GS 逻辑线程处理公共地图 NPC 的视野和对话；IGameMessageHandler 是现有消息分派所需的接口。</summary>
public sealed class SceneNpcPatrolHandler : IGameMessageHandler
{
    /// <summary>在首个巡逻请求时读取各场景的正式路线。</summary>
    private static readonly XmlDocument routes = new XmlDocument();
    /// <summary>当前有观察者的公共场景，区服可在同一台 GS 开多条线。</summary>
    private static readonly List<SceneNpcPatrolRoom> rooms = new List<SceneNpcPatrolRoom>();
    /// <summary>权威巡逻专用单调时钟，系统时间调校不会使巡卫跳位。</summary>
    private static readonly Stopwatch clock = Stopwatch.StartNew();

    /// <summary>区服启动时优先读取所用区服 XML 同目录的巡逻配置，独立启动使用随包文件。</summary>
    /// <param name="args">GS 既有启动参数，第一项为实际区服文件路径。</param>
    internal static void LoadRoutes(string[] args)
    {
        // 独立启动和缺少覆盖文件的隔离部署使用编译时随包复制的同一份配置。
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "公共地图巡逻.xml");
        if (args.Length > 0)
        {
            // 维护者可以直接编辑部署目录中的实际路点，重启后生效。
            string overridePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])), "公共地图巡逻.xml");
            if (File.Exists(overridePath)) path = overridePath;
        }
        routes.Load(path);
    }

    /// <summary>只处理网关转来的公共 NPC 请求。</summary><param name="source">接收链路。</param><param name="code">协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        return source == GameCommandSource.GateServer && code == SceneLineCode.NpcPatrol;
    }

    /// <summary>根据角色实际所在线路订阅巡卫；客户端不能指定用另一条线的 NPC。</summary>
    /// <param name="context">已进入 GS 逻辑线程的当前复合会话。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 角色身份、地图和实例均取自本机在线实体。
        BasePackage packet = context.Package;
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
        if (role == null || !role.IsRoleDataReady()) return;
        RoleBaseInfo info = role.mainRoleInfo.BaseInfo;
        if (info.InstanceId != 0) return;
        SceneNpcPatrolRequest request = SceneNpcPatrolRequest.Parser.ParseFrom(packet.Data);
        // 独立启动时按逻辑区和地图区分，集群采用调度服签发的真实场景号。
        long uid = -1L - ((long)role.mainRoleInfo.ServerId << 32) - info.MapId;
        if (role.PublicScene != null) uid = role.PublicScene.SceneUid;
        double now = clock.Elapsed.TotalSeconds;
        SceneNpcPatrolRoom room = null;
        for (int i = 0; i < rooms.Count; i++) if (rooms[i].Uid == uid) { room = rooms[i]; break; }
        if (request.NpcId != 0)
        {
            if (room != null && request.SceneUid == uid && room.Members.Contains(role)) room.Talk(role, request, now);
            return;
        }
        if (room == null)
        {
            // 室内等没有移动 NPC 的地图不建立巡逻实例。
            XmlNodeList entries = routes.SelectNodes("/patrols/route[@map='" + info.MapId.ToString(CultureInfo.InvariantCulture) + "']");
            if (entries.Count == 0) return;
            room = new SceneNpcPatrolRoom(uid, info.MapId, entries, now);
            rooms.Add(room);
        }
        if (!room.Members.Contains(role)) room.Members.Add(role);
        room.Send(now, role);
    }

    /// <summary>在游戏逻辑帧推进有玩家订阅的线路并回收空房间。</summary>
    internal static void Update()
    {
        // 同帧各条线路使用相同的采样时间。
        double now = clock.Elapsed.TotalSeconds;
        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            rooms[i].Update(now);
            if (rooms[i].Members.Count == 0) rooms.RemoveAt(i);
        }
    }

    /// <summary>角色换线、换图和退出时，立即撤销旧订阅和正在进行的交谈。</summary><param name="role">离开的会话角色。</param>
    internal static void Leave(OnlineRole role)
    {
        for (int i = 0; i < rooms.Count; i++) rooms[i].Leave(role, clock.Elapsed.TotalSeconds);
    }
}
