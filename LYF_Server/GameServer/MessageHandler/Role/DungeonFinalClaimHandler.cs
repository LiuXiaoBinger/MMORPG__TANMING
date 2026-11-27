using System;
using Google.Protobuf;

/// <summary>接入游戏服原有命令处理器契约，只接收网关转发的本人领箱请求。</summary>
public sealed class DungeonFinalClaimHandler : IGameMessageHandler
{
    /// <summary>限定正式网关来源和终绝周箱命令。</summary><param name="source">发包服务的身份。</param>
    /// <param name="commandCode">请求命令编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_DungeonFinalClaim;
    }

    /// <summary>在游戏服原有串行命令队列中核对角色、结算并回复当前会话。</summary><param name="context">已路由的网关请求。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 只使用经过登录绑定的角色和服务器时间。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(context.Package.GateSessionId, context.Package.UnitySessionId);
        if (role == null) return;
        DungeonFinalClaimReq request = DungeonFinalClaimReq.Parser.ParseFrom(context.Package.Data);
        DungeonFinalClaimRet result = DungeonFinalRewards.Claim(role, request.Week, DateTime.UtcNow);
        role.SendToClient(NetDefine.CMD_DungeonFinalClaimResult, result.ToByteString());
    }
}
