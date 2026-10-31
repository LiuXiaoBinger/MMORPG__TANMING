using Google.Protobuf;

/// <summary>
/// 队伍协议处理器，对应 RO TeamMgr.lua 的请求分支。
/// 角色身份一律从网关复合会话取得，客户端载荷只作业务参数。
/// </summary>
public sealed class TeamMessageHandler : IGameMessageHandler
{
    /// <summary>声明本处理器消费的队伍协议号集合。</summary>
    /// <param name="source">命令来源，队伍请求全部来自 GateServer。</param>
    /// <param name="commandCode">网络协议号，见 NetDefine 的 11200 到 11205 段。</param>
    /// <returns>属于队伍协议返回 true。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        if (source != GameCommandSource.GateServer)
        {
            return false;
        }
        if (commandCode == NetDefine.CMD_CreateTeamCode)
        {
            return true;
        }
        if (commandCode == NetDefine.CMD_InviteJoinTeamCode)
        {
            return true;
        }
        if (commandCode == NetDefine.CMD_AcceptTeamInvatationCode)
        {
            return true;
        }
        if (commandCode == NetDefine.CMD_LeaveTeamCode)
        {
            return true;
        }
        if (commandCode == NetDefine.CMD_BegInTeamByTeamIdCode)
        {
            return true;
        }
        if (commandCode == NetDefine.CMD_CaptainRequestEnterFBCode)
        {
            return true;
        }
        return false;
    }

    /// <summary>按协议号分派到 TeamManager 的对应操作。</summary>
    /// <param name="context">逻辑队列提供的网关会话与协议包。</param>
    public void Handle(GameMessageHandlerContext context)
    {
        // 上下文缺失或来源不是网关时直接丢弃，避免后续解引用空对象。
        if (context == null || context.Source != GameCommandSource.GateServer)
        {
            return;
        }
        // 传输链路或协议包缺失属于无效网关会话。
        if (context.Transport == null || context.Package == null)
        {
            return;
        }

        // 角色身份从现有会话索引取得，请求不携带可替换的角色ID。
        // role 为网关复合会话解析出的在线角色，是本请求唯一的权威身份，为 null 说明会话已失效。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        if (role == null)
        {
            LogMsg.Info("队伍请求找不到在线角色，协议=" + context.Package.ProtoCode.ToString(),
                LogMsgType.Error);
            return;
        }

        try
        {
            HandleByCode(context.Package.ProtoCode, context.Package.Data, role);
        }
        catch (InvalidProtocolBufferException ex)
        {
            // ex 为 Protobuf 解析异常，只记录不向客户端回错误码，避免畸形包引发额外往返。
            LogMsg.Info("队伍请求协议解析失败: " + ex.Message, LogMsgType.Error);
        }
    }

    /// <summary>按协议号执行具体队伍操作。</summary>
    /// <param name="protoCode">本次请求的协议号。</param>
    /// <param name="data">Protobuf 二进制消息体。</param>
    /// <param name="role">已通过会话校验的在线角色。</param>
    private static void HandleByCode(int protoCode, ByteString data, OnlineRole role)
    {
        if (protoCode == NetDefine.CMD_CreateTeamCode)
        {
            // arg 为创建队伍请求体，只用到 TargetId（组队目标），创建者身份取自 role。
            CreateTeamArg arg = CreateTeamArg.Parser.ParseFrom(data);
            TeamManager.Instance.RequestCreateTeam(role, arg.TargetId);
            return;
        }
        if (protoCode == NetDefine.CMD_InviteJoinTeamCode)
        {
            // arg 为邀请入队请求体，UserId 是被邀请角色ID，邀请人身份取自 role。
            InviteJoinTeamArg arg = InviteJoinTeamArg.Parser.ParseFrom(data);
            // errorCode 为邀请被拒绝的具体原因，Succeed 表示没有业务错误（含正常登记成功）。
            CmdCode errorCode;
            // invited 为邀请是否已登记；为 false 且带失败原因时必须回执，避免客户端静默无反应。
            bool invited = TeamManager.Instance.InviteJoinTeam(role, arg.UserId, out errorCode);
            if (!invited && errorCode != CmdCode.Succeed)
            {
                SendTeamError(role, errorCode);
            }
            return;
        }
        if (protoCode == NetDefine.CMD_AcceptTeamInvatationCode)
        {
            // arg 为同意/拒绝请求体：TeamId 定位队伍、InviterId 区分邀请与申请来源、Agree 是最终选择。
            AcceptTeamInvatationArg arg = AcceptTeamInvatationArg.Parser.ParseFrom(data);
            // errorCode 为入队被拒绝的具体原因，Succeed 表示没有业务错误（含拒绝邀请这类正常结果）。
            CmdCode errorCode;
            // joined 为是否真的入队；为 false 且带失败原因时回执，替代原来的静默失败。
            bool joined = TeamManager.Instance.AcceptTeamInvatation(role, arg.TeamId, arg.InviterId,
                arg.Agree, out errorCode);
            if (!joined && errorCode != CmdCode.Succeed)
            {
                SendTeamError(role, errorCode);
            }
            return;
        }
        if (protoCode == NetDefine.CMD_LeaveTeamCode)
        {
            // arg 为离队请求体，其 RoleId 只用于一致性校验，不能作为离队对象。
            LeaveTeamArg arg = LeaveTeamArg.Parser.ParseFrom(data);
            // 以会话身份为准，载荷中的 role_id 仅用于校验一致性。
            // sessionRoleId 是会话解析出的真实角色ID，也是实际执行离队的对象。
            ulong sessionRoleId = (ulong)role.mainRoleInfo.BaseInfo.RoleId;
            if (arg.RoleId != 0UL && arg.RoleId != sessionRoleId)
            {
                return;
            }
            TeamManager.Instance.LeaveTeam(sessionRoleId);
            return;
        }
        if (protoCode == NetDefine.CMD_BegInTeamByTeamIdCode)
        {
            // arg 为按队伍ID申请入队请求体，只用到目标 TeamId，申请人身份取自 role。
            BegInTeamByTeamIdArg arg = BegInTeamByTeamIdArg.Parser.ParseFrom(data);
            TeamManager.Instance.BegInTeamByTeamId(role, arg.TeamId);
            return;
        }
        if (protoCode == NetDefine.CMD_CaptainRequestEnterFBCode)
        {
            // arg 为队长进副本请求体，DungeonId 为副本配置ID；章节与难度由服务端按配置决定，不从包内读取。
            CaptainRequestEnterFBArg arg = CaptainRequestEnterFBArg.Parser.ParseFrom(data);
            TeamManager.Instance.CaptainRequestEnterFBPtc(role, arg.DungeonId);
        }
    }

    /// <summary>
    /// 用项目既有的通用错误协议把队伍请求的业务失败原因回给请求者。
    /// 客户端 NetErrorMsgMgr 已注册 CMD_ErrCode 并翻译成系统提示，这里只补上服务端回执。
    /// </summary>
    /// <param name="role">本次请求的权威在线角色，也是错误回执的接收人。</param>
    /// <param name="errorCode">失败原因，必须不是 Succeed。</param>
    private static void SendTeamError(OnlineRole role, CmdCode errorCode)
    {
        if (role == null)
        {
            return;
        }
        // 错误回执载荷，沿用项目既有错误包结构，只带命令码。
        ErrMsg errMsg = new ErrMsg();
        errMsg.CmdCode = errorCode;
        role.SendToClient(NetDefine.CMD_ErrCode, errMsg.ToByteString());
    }
}
