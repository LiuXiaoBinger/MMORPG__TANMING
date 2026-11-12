using System.Threading;
using Google.Protobuf;

/// <summary>游戏服到队伍服的 TCP 连接。网络框架和游戏线程调度分别要求实现现有的 IContainer、IGameMessageHandler。</summary>
public sealed class MasterTeamLink : IContainer, IGameMessageHandler
{
    /// <summary>本 GS 的唯一队伍服连接，复用网络库的连接及重连行为。</summary>
    public static readonly MasterTeamLink Instance = new MasterTeamLink();
    /// <summary>已注册回包处理器的队伍服客户端。</summary>
    private NetClient _client;
    /// <summary>连接是否实际建立过，用于只在真实断链时清除已失效的队伍展示与场景。</summary>
    private int _connected;

    /// <summary>队伍服连接由游戏服入口统一初始化。</summary>
    private MasterTeamLink() { }

    /// <summary>连接指定的队伍服，并把内网回包接入现有游戏逻辑线程。</summary>
    /// <param name="host">队伍服监听地址。</param>
    /// <param name="port">队伍服 TCP 端口。</param>
    public void Connect(string host, int port)
    {
        _client = new NetClient(host, port, ClientType.GameServer);
        // 网络回包与本地断线事件都按游戏线程的命令顺序处理。
        ushort[] results = { NetDefine.CMD_MasterTeamDelivery, NetDefine.CMD_MasterTeamRoster,
            NetDefine.CMD_MasterTeamEnter, NetDefine.CMD_MasterTeamExit, NetDefine.CMD_MasterTeamLost };
        // 断线事件仅在本机产生，不向远端注册该网络协议。
        for (int index = 0; index < results.Length; index++)
        {
            // 本次注册的队伍服命令。
            ushort code = results[index];
            ClientMessageManager.Instance.Register(new GameMessageHandlerKey(GameCommandSource.MasterServer, code),
                this, CenterMessageRoute.Internal);
            if (code != NetDefine.CMD_MasterTeamLost) _client.RegistCommand(code, this);
        }
        // 聊天回包共用同一条 GS→MS 连接，因此队伍聊天读取的就是本分区权威名单。
        _client.RegistCommand(NetDefine.RpcC2M_SendChatMsg, this);
        _client.RegistCommand(NetDefine.RpcC2M_UploadChatAudio, this);
        _client.RegistCommand(NetDefine.RpcC2M_DownloadChatAudio, this);
        _client.RegistCommand(NetDefine.PtcM2T_ChatBroadCastM2T, this);
        _client.RegistCommand(NetDefine.PtcM2G_ChatBroadCastM2Gs, this);
        _client.OnConnSucceed = () => Interlocked.Exchange(ref _connected, 1);
        _client.OnConnFailed = OnDisconnected;
        _client.StartConnect();
    }

    /// <summary>把经网关会话认证的角色及原始请求交给 MS 决策。</summary>
    /// <param name="role">游戏服根据网关与玩家会话确定的在线角色。</param>
    /// <param name="request">原客户端组队协议及载荷。</param>
    public void Forward(OnlineRole role, BasePackage request)
    {
        // 只拦截玩家再次发起进本；内部晚加入/原成员同步仍由既有入场流程处理。
        if (request.ProtoCode == NetDefine.CMD_CaptainRequestEnterFBCode && role.mainRoleInfo.BaseInfo.InstanceId != 0)
        {
            role.SendToClient(NetDefine.CMD_ErrCode, new ErrMsg { CmdCode = CmdCode.DungeonAlreadyEntered }.ToByteString());
            return;
        }
        // 昵称取自服务器角色数据，供邀请或申请提示使用。
        MasterTeamRolePacket packet = new MasterTeamRolePacket
        {
            RoleId = role.GetID(), Nickname = role.mainRoleInfo.BaseInfo.Nickname, Packet = request
        };
        // 队长已离场但队员仍在旧副本时，不能借成员同意把该队员拉入另一场景。
        if (request.ProtoCode == NetDefine.PtcC2M_MemberReplyEnterFBPtc && role.mainRoleInfo.BaseInfo.InstanceId != 0)
        {
            // 只把本次同意改为拒绝，沿原MS取消链结束全队等待；原拒绝不受影响。
            MemberReplyEnterFBData reply = MemberReplyEnterFBData.Parser.ParseFrom(request.Data);
            if (reply.Type == ReplyType.Agree)
            {
                role.SendToClient(NetDefine.CMD_ErrCode, new ErrMsg { CmdCode = CmdCode.DungeonAlreadyEntered }.ToByteString());
                packet.Packet = request.Clone();
                packet.Packet.Data = new MemberReplyEnterFBData { Type = ReplyType.Refuse }.ToByteString();
            }
        }
        if (Volatile.Read(ref _connected) == 0 || !_client.Send(NetDefine.CMD_MasterTeamRequest, packet.ToByteString()))
        {
            role.SendToClient(NetDefine.CMD_ErrCode, new ErrMsg { CmdCode = CmdCode.ServerError }.ToByteString());
        }
    }

    /// <summary>沿本分区连接发送需要保留玩家返回地址的 MS 业务包。</summary>
    /// <param name="packet">已由 GS 核对会话的服务请求。</param><returns>消息是否提交给连接。</returns>
    public bool SendToMaster(BasePackage packet)
    {
        return Volatile.Read(ref _connected) != 0 && _client.Send(packet);
    }

    /// <summary>GS 下线时让 MS 删除该角色的组队关系。</summary>
    /// <param name="roleId">已从原会话下线的角色号。</param>
    public void Offline(ulong roleId)
    {
        if (Volatile.Read(ref _connected) != 0)
        {
            _client.Send(NetDefine.CMD_MasterTeamOffline, new LeaveTeamArg { RoleId = roleId }.ToByteString());
        }
    }

    /// <summary>实际连接中断后，在 GS 逻辑线程清理旧分区的临时队伍与副本。</summary>
    private void OnDisconnected()
    {
        if (Interlocked.Exchange(ref _connected, 0) == 0) return;
        GameGlobal.Instance.EnqueueGameCommand(GameCommandSource.MasterServer, _client,
            new BasePackage { ProtoCode = NetDefine.CMD_MasterTeamLost });
    }

    /// <summary>注册工作由 Connect 统一执行。</summary>
    public void OnInit() { }

    /// <summary>此连接仅主动连接 MS，不接受下游请求。</summary>
    /// <param name="serverBase">网络框架预留的监听会话。</param>
    /// <param name="basePackage">网络框架预留的消息。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage) { }

    /// <summary>网络线程只投递包，实际修改角色状态发生在游戏线程。</summary>
    /// <param name="serverBase">与队伍服建立的 TCP 连接。</param>
    /// <param name="basePackage">队伍服发出的定向通知、名单或副本指令。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {
        GameGlobal.Instance.EnqueueGameCommand(GameCommandSource.MasterServer, serverBase, basePackage);
    }

    /// <summary>只接收明确注册的队伍服来源及回包。</summary>
    /// <param name="source">游戏命令的真实网络来源。</param>
    /// <param name="code">要核对的内部协议号。</param>
    /// <returns>是否可交给本连接处理。</returns>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        if (source != GameCommandSource.MasterServer) return false;
        switch (code)
        {
            case NetDefine.CMD_MasterTeamDelivery:
            case NetDefine.CMD_MasterTeamRoster:
            case NetDefine.CMD_MasterTeamEnter:
            case NetDefine.CMD_MasterTeamExit:
            case NetDefine.CMD_MasterTeamLost:
                return true;
            default:
                return false;
        }
    }

    /// <summary>在游戏线程交付社交通知或执行副本场景操作。</summary>
    /// <param name="context">已按 MS 来源和命令号分派的消息。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 不改变客户端包的编码，服务间控制消息直接按各自定义解析。
        ByteString data = context.Package.Data;
        switch (context.Package.ProtoCode)
        {
            case NetDefine.CMD_MasterTeamDelivery:
                // 指定单一游戏角色的错误或邀请。
                MasterTeamRolePacket delivery = MasterTeamRolePacket.Parser.ParseFrom(data);
                // 角色可能已退出，投递沿用现有在线角色索引。
                OnlineRole target = RoleManager.Instance.GetOnlineRole((int)delivery.RoleId);
                if (target != null) target.SendToClient(delivery.Packet.ProtoCode, delivery.Packet.Data);
                break;
            case NetDefine.CMD_MasterTeamRoster:
                TeamManager.Instance.PublishRoster(TeamInfoNtf.Parser.ParseFrom(data));
                break;
            case NetDefine.CMD_MasterTeamEnter:
                TeamManager.Instance.EnterDungeon(MasterTeamDungeonOrder.Parser.ParseFrom(data));
                break;
            case NetDefine.CMD_MasterTeamExit:
                TeamManager.Instance.LeaveDungeon(LeaveTeamArg.Parser.ParseFrom(data).RoleId);
                break;
            case NetDefine.CMD_MasterTeamLost:
                TeamManager.Instance.OnMasterLost();
                break;
        }
    }
}
