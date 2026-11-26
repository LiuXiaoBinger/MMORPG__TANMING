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

    /// <summary>使用服务器保存的角色坐标校验门与目的地。</summary>
    /// <param name="role">当前网关会话绑定的角色。</param><param name="destination">调度选定的目的地图。</param>
    internal static bool CanEnter(OnlineRole role, int destination)
    {
        // 副本地图和非配对目的地都没有对应的城际传送门。
        RoleBaseInfo info = role.mainRoleInfo.BaseInfo;
        PublicCityPortal portal = PublicCityPortal.Find(info.MapId);
        if (portal == null || info.InstanceId != 0 || portal.Destination.MapId != destination) return false;
        // 服务端半径比客户端触发圈宽一米，容纳一次移动同步的时间差。
        Vector3 position = info.Pos.ToVector3();
        return portal.Contains(position.X, position.Y, position.Z, 3.5f);
    }

    /// <summary>跨城落在目的城出生点，同地图换线保留原坐标。</summary>
    /// <param name="info">已接管的角色位置或交接数据副本。</param><param name="destination">目的公共地图。</param>
    internal static void PlaceArrival(RoleBaseInfo info, int destination)
    {
        if (info.MapId == destination) return;
        info.MapId = destination;
        info.Pos = PublicCityPortal.Find(destination).ArrivalPosition;
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
        PublicCityPortal portal = PublicCityPortal.Find(role.mainRoleInfo.BaseInfo.MapId);
        if (portal == null || !role.IsRoleDataReady() || role.mainRoleInfo.BaseInfo.CurrHp <= 0)
            result.Reason = "当前无法传送。";
        else if (!CanEnter(role, portal.Destination.MapId))
            result.Reason = "请走入传送门。";
        else
        {
            RoleManager.Instance.LeavePublicScene(role);
            PlaceArrival(role.mainRoleInfo.BaseInfo, portal.Destination.MapId);
            result.Lines.Add(new PublicSceneLine { MapId = portal.Destination.MapId });
        }
        role.SendToClient(SceneLineCode.Portal, result.ToByteString());
        if (result.Lines.Count == 0) return;
        RoleManager.Instance.BroadcastRoleEnterWorldByRole(role);
        RoleManager.Instance.ResendNearbyRoles(role);
    }
}
