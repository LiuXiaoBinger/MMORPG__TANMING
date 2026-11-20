using Google.Protobuf;

/// <summary>在游戏逻辑线程接收已有玩家会话的技能提升请求。</summary>
public sealed class SkillUpgradeHandler : IGameMessageHandler
{
    /// <summary>只接收从网关转发的升级指令。</summary>
    /// <param name="source">转发该命令的服务类型。</param>
    /// <param name="commandCode">入站协议号。</param>
    /// <returns>是此类处理的升级命令时返回真。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_SkillUpgradeCode;
    }

    /// <summary>从当前网关会话定位实际角色，再由在线技能组件处理一次升级。</summary>
    /// <param name="context">游戏主循环派发的当前网关命令。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 客户端仅提供技能及本次点击前的等级，不直接指定角色、费用或下一级。
        SkillUpgradeReq request = SkillUpgradeReq.Parser.ParseFrom(context.Package.Data);
        // 同 GameServer 现有施法请求，使用复合会话反查已登录的唯一角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        // 会话未准备好时仍可向原请求返回明确失败。
        SkillUpgradeRet result = new SkillUpgradeRet { CmdCode = CmdCode.InvalidSession, SkillId = request.SkillId };
        if (role != null && role.State == OnlineRoleState.Online && role.IsRoleDataReady())
        {
            result = role.GetComponent<RoleSkillComponent>().Upgrade(request);
        }
        context.Transport.Send(context.Package, NetDefine.CMD_SkillUpgradeCode, result.ToByteString());
    }
}
