using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Google.Protobuf;

/// <summary>测试专用的游戏分区对端。通过真实 TCP 驱动独立 MS，从实际下行包读取结果，同时调用正式 GS 场景处理。</summary>
internal sealed class MasterTeamProbe : IDisposable
{
    /// <summary>组队冒烟测试共用的分区连接，仅在测试程序内存在。</summary>
    internal static MasterTeamProbe Instance;
    /// <summary>与实际 MasterServer 建立的 TCP 客户端。</summary>
    private readonly TcpClient _socket = new TcpClient();
    /// <summary>从真实队伍通知观察到的各角色最后一次名单，用于断言客户端可见状态。</summary>
    private readonly Dictionary<ulong, TeamInfoNtf> _views = new Dictionary<ulong, TeamInfoNtf>();
    /// <summary>上一条请求发出的业务回包类型，0 表示没有可见动作。</summary>
    private int _result;
    /// <summary>上一条请求给发起人的实际错误码。</summary>
    private CmdCode _error;
    /// <summary>当前请求实际收到的 MS 定向包，用于核对确认通知覆盖所有收件人。</summary>
    public readonly List<MasterTeamRolePacket> Deliveries = new List<MasterTeamRolePacket>();

    /// <summary>连接测试所启动的 MS，保留原生帧头及 CRC 编码。</summary>
    /// <param name="port">测试进程实际监听的队伍服端口。</param>
    internal MasterTeamProbe(int port)
    {
        _socket.NoDelay = true;
        _socket.ReceiveTimeout = 5000;
        _socket.Connect(NetDefine.IPHost, port);
    }

    /// <summary>将一个组队请求发给独立 MS，并读到排在其后的确定性回执。</summary>
    /// <param name="roleId">模拟游戏服认证后的发起角色。</param>
    /// <param name="name">发起人的服务器昵称。</param>
    /// <param name="code">原客户端命令号。</param>
    /// <param name="msg">该命令的强类型 Protobuf 消息。</param>
    private void Request(ulong roleId, string name, ushort code, IMessage msg)
    {
        Deliveries.Clear();
        Write(NetDefine.CMD_MasterTeamRequest, new MasterTeamRolePacket { RoleId = roleId, Nickname = name,
            Packet = new BasePackage { ProtoCode = code, Data = msg.ToByteString() } });
        Drain();
    }

    /// <summary>通过绝不建队的保留测试玩家发一条必有错误回包的请求，确保之前的通知均已处理。</summary>
    private void Drain()
    {
        _error = CmdCode.Succeed;
        _result = 0;
        Write(NetDefine.CMD_MasterTeamRequest, new MasterTeamRolePacket { RoleId = ulong.MaxValue,
            Packet = new BasePackage { ProtoCode = NetDefine.CMD_InviteJoinTeamCode, Data = new InviteJoinTeamArg().ToByteString() } });
        while (true)
        {
            // 下一条来自实际 MasterServer 的业务消息。
            BasePackage packet = Read();
            if (packet.ProtoCode == NetDefine.CMD_MasterTeamDelivery)
            {
                // 角色号从独立 MS 回包读取，没有模拟队伍决策。
                MasterTeamRolePacket delivery = MasterTeamRolePacket.Parser.ParseFrom(packet.Data);
                if (delivery.RoleId == ulong.MaxValue) return;
                Deliveries.Add(delivery);
                _result = delivery.Packet.ProtoCode;
                if (_result == NetDefine.CMD_ErrCode) _error = ErrMsg.Parser.ParseFrom(delivery.Packet.Data).CmdCode;
            }
            else if (packet.ProtoCode == NetDefine.CMD_MasterTeamExit)
            {
                _views.Remove(LeaveTeamArg.Parser.ParseFrom(packet.Data).RoleId);
                _result = packet.ProtoCode;
            }
            else if (packet.ProtoCode == NetDefine.CMD_MasterTeamRoster)
            {
                // 记录已投递的 UI 名单，供原组队回归断言读取。
                TeamInfoNtf roster = TeamInfoNtf.Parser.ParseFrom(packet.Data);
                for (int index = 0; index < roster.Members.Count; index++) _views[roster.Members[index].RoleId] = roster;
                if (_result == 0) _result = packet.ProtoCode;
            }
            else _result = packet.ProtoCode;
            MasterTeamLink.Instance.OnCall(new GameMessageHandlerContext(GameCommand.From(GameCommandSource.MasterServer, null, packet)));
        }
    }

    /// <summary>写入和生产链路一致的完整网络帧。</summary>
    /// <param name="code">外层服务间协议号。</param>
    /// <param name="msg">要序列化的服务间消息。</param>
    private void Write(ushort code, IMessage msg)
    {
        // 原有协议帧包含长度、压缩位和校验值。
        byte[] frame = NetUtils.Instance.MakeData(new BasePackage { ProtoCode = code, Data = msg.ToByteString() }.ToByteArray());
        _socket.GetStream().Write(frame, 0, frame.Length);
    }

    /// <summary>读满一帧并使用正式网络解析器校验负载。</summary>
    private BasePackage Read()
    {
        // 首先只读两字节小端长度。
        byte[] header = new byte[2];
        ReadAll(header, 0, 2);
        // 帧长不含头部两个字节。
        int length = BitConverter.ToUInt16(header, 0);
        // CRC 解析器接受完整帧。
        byte[] frame = new byte[length + 2];
        Buffer.BlockCopy(header, 0, frame, 0, 2);
        ReadAll(frame, 2, length);
        return BasePackage.Parser.ParseFrom(NetUtils.Instance.ParseData(frame, length));
    }

    /// <summary>从 TCP 流累计读取，允许单帧跨多次接收。</summary>
    /// <param name="buffer">目标缓冲区。</param><param name="offset">可写起点。</param><param name="length">需读字节数。</param>
    private void ReadAll(byte[] buffer, int offset, int length)
    {
        while (length > 0)
        {
            // 当前可用的实际字节数。
            int n = _socket.GetStream().Read(buffer, offset, length);
            if (n == 0) throw new InvalidOperationException("队伍服提前断开");
            offset += n;
            length -= n;
        }
    }

    /// <summary>查询某个玩家最后实际收到的队伍号。</summary><param name="id">角色号。</param>
    public ulong GetTeamId(ulong id)
    {
        // 最近一次 MS 下发的队伍视图。
        TeamInfoNtf info;
        if (_views.TryGetValue(id, out info)) return info.TeamId;
        return 0;
    }

    /// <summary>读回包中记录的队长。</summary><param name="id">队伍号。</param>
    public ulong GetCaptainId(ulong id)
    {
        // 复制已观测的名单供索引遍历，不执行队长选择算法。
        List<TeamInfoNtf> values = new List<TeamInfoNtf>(_views.Values);
        for (int i = 0; i < values.Count; i++) if (values[i].TeamId == id) return values[i].CaptainId;
        return 0;
    }

    /// <summary>从 MS 最新发布的名单读取人数。</summary><param name="id">队伍号。</param>
    public int GetMemberCount(ulong id)
    {
        // 当前有报告的队伍视图。
        List<TeamInfoNtf> values = new List<TeamInfoNtf>(_views.Values);
        for (int i = 0; i < values.Count; i++) if (values[i].TeamId == id) return values[i].Members.Count;
        return 0;
    }

    /// <summary>从实际 GS 场景数据读取角色所在实例。</summary><param name="id">角色号。</param>
    public ulong GetInstanceId(ulong id)
    {
        // 测试已在正式 RoleManager 注册的在线角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRole((int)id);
        return (ulong)role.mainRoleInfo.BaseInfo.InstanceId;
    }

    /// <summary>通过原始建队协议返回 MS 实际分配的队伍号。</summary><param name="role">发起人。</param><param name="target">客户端队伍目标。</param>
    public ulong RequestCreateTeam(OnlineRole role, int target)
    {
        Request(role.GetID(), role.mainRoleInfo.BaseInfo.Nickname, NetDefine.CMD_CreateTeamCode, new CreateTeamArg { TargetId = target });
        return GetTeamId(role.GetID());
    }

    /// <summary>向目标发送真实邀请。</summary><param name="role">邀请人。</param><param name="id">被邀请人。</param>
    public bool InviteJoinTeam(OnlineRole role, ulong id)
    {
        // 此用例只关心是否出现邀请，错误码由专门的回归用例断言。
        CmdCode ignored;
        return InviteJoinTeam(role, id, out ignored);
    }

    /// <summary>从邀请通知或错误回包读取结果。</summary><param name="role">邀请人。</param><param name="id">目标角色。</param><param name="err">实际业务错误。</param>
    public bool InviteJoinTeam(OnlineRole role, ulong id, out CmdCode err)
    {
        Request(role.GetID(), role.mainRoleInfo.BaseInfo.Nickname, NetDefine.CMD_InviteJoinTeamCode, new InviteJoinTeamArg { UserId = id });
        err = _error;
        return _result == NetDefine.CMD_TeamInvatationCode;
    }

    /// <summary>向队长提交原始入队申请。</summary><param name="role">申请人。</param><param name="team">目标队伍。</param>
    public bool BegInTeamByTeamId(OnlineRole role, ulong team)
    {
        Request(role.GetID(), role.mainRoleInfo.BaseInfo.Nickname, NetDefine.CMD_BegInTeamByTeamIdCode, new BegInTeamByTeamIdArg { TeamId = team });
        return _result == NetDefine.CMD_TeamInvatationCode;
    }

    /// <summary>提交同意或拒绝，不关心详细错误码。</summary><param name="role">答复人。</param><param name="team">队伍号。</param><param name="inviter">发起人。</param><param name="agree">是否同意。</param>
    public bool AcceptTeamInvatation(OnlineRole role, ulong team, ulong inviter, bool agree)
    {
        // 需要错误码的用例使用下方重载。
        CmdCode ignored;
        return AcceptTeamInvatation(role, team, inviter, agree, out ignored);
    }

    /// <summary>从真实名单变更或错误包确定答复结果。</summary><param name="role">答复人。</param><param name="team">队伍号。</param><param name="inviter">发起者。</param><param name="agree">是否同意。</param><param name="err">实际错误码。</param>
    public bool AcceptTeamInvatation(OnlineRole role, ulong team, ulong inviter, bool agree, out CmdCode err)
    {
        Request(role.GetID(), role.mainRoleInfo.BaseInfo.Nickname, NetDefine.CMD_AcceptTeamInvatationCode,
            new AcceptTeamInvatationArg { TeamId = team, InviterId = inviter, Agree = agree });
        err = _error;
        return _result == NetDefine.CMD_MasterTeamRoster;
    }

    /// <summary>发送退队请求。</summary><param name="id">本人的角色号。</param>
    public bool LeaveTeam(ulong id)
    {
        Request(id, string.Empty, NetDefine.CMD_LeaveTeamCode, new LeaveTeamArg { RoleId = id });
        return _result == NetDefine.CMD_MasterTeamExit;
    }

    /// <summary>模拟 GS 下线事件，分别确认本地场景清理与 MS 成员变更。</summary><param name="id">下线者。</param>
    public void OnRoleOffline(ulong id)
    {
        TeamManager.Instance.OnRoleOffline(id);
        Write(NetDefine.CMD_MasterTeamOffline, new LeaveTeamArg { RoleId = id });
        _views.Remove(id);
        Drain();
    }

    /// <summary>提交真实进本请求，以是否收到 MS 的进场指令判断结果。</summary><param name="role">发起人。</param><param name="dungeon">目标副本。</param>
    public bool CaptainRequestEnterFBPtc(OnlineRole role, int dungeon)
    {
        Request(role.GetID(), role.mainRoleInfo.BaseInfo.Nickname, NetDefine.CMD_CaptainRequestEnterFBCode,
            new CaptainRequestEnterFBArg { DungeonId = dungeon });
        return _result == NetDefine.CMD_MasterTeamEnter;
    }

    /// <summary>通过真实 TCP 提交测试成员的明确答复，不在探针里自动替全员同意。</summary>
    /// <param name="role">本次实际答复人。</param><param name="reply">同意或拒绝。</param><returns>是否收到 GS 入场授权。</returns>
    public bool MemberReplyEnterFBPtc(OnlineRole role, ReplyType reply)
    {
        Request(role.GetID(), role.mainRoleInfo.BaseInfo.Nickname, NetDefine.PtcC2M_MemberReplyEnterFBPtc,
            new MemberReplyEnterFBData { Type = reply });
        return _result == NetDefine.CMD_MasterTeamEnter;
    }

    /// <summary>等待真实 MS 的十秒确认时限结束并读取其取消通知。</summary>
    public void WaitForConfirmationTimeout()
    {
        Deliveries.Clear();
        System.Threading.Thread.Sleep(11000);
        Drain();
    }

    /// <summary>关闭本次测试的实际服务端 TCP 会话。</summary>
    public void Dispose() { _socket.Close(); }
}
