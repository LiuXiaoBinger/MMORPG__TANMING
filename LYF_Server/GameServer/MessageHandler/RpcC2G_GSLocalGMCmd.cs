using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>RO 游戏内 GM 请求入口，按游戏服分发框架的 IGameMessageHandler 契约消费网关消息。</summary>
internal sealed class RpcC2G_GSLocalGMCmd : IGameMessageHandler
{
    /// <summary>保留通过游戏服环境独立授权的数据库账号 ID。</summary>
    private readonly HashSet<int> accounts = new HashSet<int>();

    /// <summary>注册接收器时读取游戏服启动环境 LYF_GM_ACCOUNTS，多个账号用逗号分隔。</summary>
    internal RpcC2G_GSLocalGMCmd()
    {
        // 账号许可只取自服务端配置，客户端构建类型只影响入口显示。
        string list = Environment.GetEnvironmentVariable("LYF_GM_ACCOUNTS");
        if (string.IsNullOrWhiteSpace(list)) return;
        // 每一项均为数据库账号 ID。
        string[] entries = list.Split(',');
        for (int i = 0; i < entries.Length; i++)
        {
            // 忽略无法匹配数据库账号的配置项。
            if (int.TryParse(entries[i], out int accountId) && accountId > 0) accounts.Add(accountId);
        }
    }

    /// <summary>仅接收由玩家网关进入的单个 GM RPC。</summary>
    /// <param name="source">协议注册表提供的真实链路来源。</param><param name="code">本次协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        return source == GameCommandSource.GateServer && code == NetDefine.RpcC2G_GSLocalGMCmd;
    }

    /// <summary>用真实连接定位角色，执行命令并把文字结果沿原会话返回。</summary>
    /// <param name="context">游戏逻辑线程正在处理的网关包及回传连接。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 请求体只有 RO 的原始命令行，包头中的会话确定操作对象。
        BasePackage packet = context.Package;
        GSLocalGMCmdArg request = GSLocalGMCmdArg.Parser.ParseFrom(packet.Data);
        // 获取此物理网关连接上的登录角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
        // 本次 RPC 的全部分支都返回面板可读的结果。
        GSLocalGMCmdRes result = new GSLocalGMCmdRes();
        if (role == null || role.State != OnlineRoleState.Online || !role.IsRoleDataReady()) result.ResultMsg = "角色尚未进入游戏";
        // 角色标识来自中心服读取的数据库记录，兼容已配置的整账号 GM 授权。
        else if (!role.mainRoleInfo.IsGm && !accounts.Contains(role.mainRoleInfo.AccountId))
            result.ResultMsg = "当前角色未获游戏服 GM 授权";
        else
        {
            result.ResultMsg = CMDManager.Instance.RunGMCommond(role, request.Args);
            LogMsg.Info("[GM] role=" + role.GetID() + " cmd=" + request.Args + " result=" + result.ResultMsg);
        }
        context.Transport.Send(packet, NetDefine.RpcC2G_GSLocalGMCmd, result.ToByteString());
    }
}
