using Google.Protobuf;
using System.Numerics;

/// <summary>两城传送的服务端判定。单服由消息框架要求的 IGameMessageHandler 接收，集群复用同一位置校验。</summary>
public sealed class CityPortalHandler : IGameMessageHandler
{
    /// <summary>单服只接受从玩家网关进入的传送请求。</summary>
    /// <param name="source">包的真实链路来源。</param><param name="code">协议号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        return source == GameCommandSource.GateServer && code == SceneLineCode.Portal;
    }

    /// <summary>按实际选择的门号与 GS 保存的脚底位置判定是否可以启程。</summary>
    /// <param name="role">当前网关会话绑定的角色。</param><param name="portal">本图中选中的入口。</param>
    internal static bool CanEnter(OnlineRole role, PublicCityPortal portal)
    {
        // 只有公共世界中的角色能使用这里的线路与交通设施。
        RoleBaseInfo info = role.mainRoleInfo.BaseInfo;
        if (portal == null || info.InstanceId != 0) return false;
        // 普通门以踩圈为准；服务入口沿用客户端十米的交谈范围，均留一次移动同步余量。
        float radius = 3.5f;
        if (portal.UseService) radius = 11f;
        // 位置取自服务器角色，不接受客户端指定落点。
        Vector3 position = info.Pos.ToVector3();
        return portal.Contains(position.X, position.Y, position.Z, radius);
    }

    /// <summary>在角色交接数据中应用真实出发门的对端落点。</summary>
    /// <param name="info">正在离场的角色数据或交接副本。</param><param name="portal">出发门，普通换线为空。</param>
    internal static void PlaceArrival(RoleBaseInfo info, PublicCityPortal portal)
    {
        if (portal == null) return;
        info.Pos = portal.Destination.ArrivalPosition;
        info.MapId = portal.Destination.MapId;
    }

    /// <summary>无独立调度服时直接在本机完成离场、落点和两侧视野更新。</summary>
    /// <param name="context">在游戏逻辑线程处理的真实会话。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 玩家身份从网关会话解析，传送包不携带角色、地图或坐标。
        BasePackage packet = context.Package;
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
        if (role == null || role.PublicScene != null) return;
        // 将结果沿原玩家会话返回。
        SceneLineResult result = new SceneLineResult();
        // 门号只在该角色当前的权威地图中解释。
        int doorId = ScenePortalRequest.Parser.ParseFrom(packet.Data).DoorId;
        PublicCityPortal portal = PublicCityPortal.Find(role.mainRoleInfo.BaseInfo.MapId, doorId);
        if (portal == null || !role.IsRoleDataReady() || role.mainRoleInfo.BaseInfo.CurrHp <= 0)
            result.Reason = "当前无法传送。";
        else if (!CanEnter(role, portal))
            result.Reason = "请走入传送门。";
        else
        {
            RoleManager.Instance.LeavePublicScene(role);
            PlaceArrival(role.mainRoleInfo.BaseInfo, portal);
            result.Lines.Add(new PublicSceneLine { MapId = portal.Destination.MapId });
            result.ArrivalPosition = role.mainRoleInfo.BaseInfo.Pos;
            // 单 GS 模式由本机直接向中心服保存已确认的目的地。
            RoleBaseInfo location = new RoleBaseInfo { RoleId = role.mainRoleInfo.BaseInfo.RoleId,
                MapId = portal.Destination.MapId, Pos = result.ArrivalPosition };
            context.Transport._client.Send(packet, SceneLineCode.SavePublicArrival, location.ToByteString());
        }
        role.SendToClient(SceneLineCode.Portal, result.ToByteString());
        if (result.Lines.Count == 0) return;
        RoleManager.Instance.BroadcastRoleEnterWorldByRole(role);
        RoleManager.Instance.ResendNearbyRoles(role);
    }
}
