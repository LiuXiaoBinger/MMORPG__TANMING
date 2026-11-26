using System.Collections.Generic;
using Google.Protobuf;

/// <summary>按 GS 的 IGameMessageHandler 接口接入邮箱 RPC，核对会话身份并在角色线程发放附件。</summary>
public sealed class MailMessageHandler : IGameMessageHandler
{
    /// <summary>注册三个客户端请求和 MS 的领取、投递及通知回包。</summary>
    /// <param name="server">接入网关的监听器。</param><param name="messages">游戏线程的消息分发器。</param>
    public static void Register(NetServer server, ClientMessageManager messages)
    {
        // 所有邮箱消息共享同一个无状态接入器。
        var handler = new MailMessageHandler();
        // 玩家允许发起的三个业务操作。
        ushort[] requests = { MailCode.GetMailList, MailCode.GetOneMail, MailCode.MailOp };
        for (int i = 0; i < requests.Length; i++)
        {
            server.RegistCommand(requests[i], messages);
            messages.Register(new GameMessageHandlerKey(GameCommandSource.GateServer, requests[i]), handler, CenterMessageRoute.Internal);
        }
        // 只有来自 MS 的回包才可携带奖励。
        ushort[] replies = { MailCode.Delivery, MailCode.GiveMailPrize, MailCode.UpdateMailNtf, MailCode.GsSendMail };
        for (int i = 0; i < replies.Length; i++)
            messages.Register(new GameMessageHandlerKey(GameCommandSource.MasterServer, replies[i]), handler, CenterMessageRoute.Internal);
    }

    /// <summary>区分网关玩家请求与 MS 内部消息来源。</summary>
    /// <param name="source">实际接收连接类型。</param><param name="code">已注册的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort code)
    {
        if (source == GameCommandSource.GateServer) return code >= MailCode.GetMailList && code <= MailCode.MailOp;
        if (source != GameCommandSource.MasterServer) return false;
        return code == MailCode.UpdateMailNtf || (code >= MailCode.GiveMailPrize && code <= MailCode.Delivery);
    }

    /// <summary>在角色逻辑线程解包、转送请求和处理由 MS 授权的附件。</summary>
    /// <param name="context">原连接、来源和包中玩家会话。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 上行请求及下行资产通知共用的路由包。
        BasePackage packet = context.Package;
        if (context.Source == GameCommandSource.GateServer)
        {
            // GS 以现有会话定位角色，客户端请求中没有角色号和附件清单。
            OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
            if (role == null || role.State != OnlineRoleState.Online) return;
            // 仅从玩家输入读取操作种类及邮件 UID。
            var call = new MailCall { Command = packet.ProtoCode, RoleId = (int)role.GetID() };
            if (packet.ProtoCode == MailCode.GetOneMail)
                call.MailUid = GetOneMailArg.Parser.ParseFrom(packet.Data).MailUid;
            else if (packet.ProtoCode == MailCode.MailOp)
            {
                // 号码和动作由 MS 结合当前角色邮箱再次定位。
                MailOpArg op = MailOpArg.Parser.ParseFrom(packet.Data);
                call.MailUid = op.MailUid;
                call.OpType = op.OpType;
            }
            else GetMailListArg.Parser.ParseFrom(packet.Data);
            SendRequest(role, call);
            return;
        }
        if (packet.ProtoCode == MailCode.GsSendMail)
        {
            // 后台投递是否真正入库以 MS 回执为准。
            MailOpRes result = MailOpRes.Parser.ParseFrom(packet.Data);
            if (result.Error.Length > 0) LogMsg.Info("邮件投递失败：" + result.Error, LogMsgType.Error);
            return;
        }
        if (packet.ProtoCode == MailCode.Delivery)
        {
            // 原请求期间切号或换宿主时不把前一角色的信送给新角色。
            MasterTeamRolePacket delivery = MasterTeamRolePacket.Parser.ParseFrom(packet.Data);
            // 业务回包始终定位到原角色及网关会话。
            OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(delivery.Packet.GateSessionId, delivery.Packet.UnitySessionId);
            if (role != null && role.GetID() == delivery.RoleId)
                role.SendToClient(delivery.Packet.ProtoCode, delivery.Packet.Data);
            return;
        }
        // 由 MS 提供的新信目标或实查奖品。
        MailCall notice = MailCall.Parser.ParseFrom(packet.Data);
        if (packet.ProtoCode == MailCode.UpdateMailNtf)
        {
            // 个人仅通知当前宿主上的收件人，全服通知限制在同逻辑区。
            var online = new List<OnlineRole>(RoleManager.Instance.GetOnlineRoleMap().Values);
            for (int i = 0; i < online.Count; i++)
            {
                // 离线角色下次登录从库中取得未读提示。
                OnlineRole target = online[i];
                if (target.State != OnlineRoleState.Online) continue;
                if (notice.RoleId != 0 && (int)target.GetID() != notice.RoleId) continue;
                SendHintRequest(target);
            }
            return;
        }
        // 奖励只发给原申请会话中的同一已装载角色。
        OnlineRole receiver = RoleManager.Instance.GetOnlineRoleBySession(packet.GateSessionId, packet.UnitySessionId);
        if (receiver == null || (int)receiver.GetID() != notice.RoleId) return;
        if (receiver.State == OnlineRoleState.Online && receiver.IsRoleDataReady())
            receiver.GetComponent<GiveMailPrize>().Give(notice);
        else
        {
            notice.Result.Error = "角色背包尚未就绪";
            receiver.SendToClient(MailCode.MailOp, notice.Result.ToByteString());
        }
    }

    /// <summary>角色上线或收到通知时查询当前真实未读状态。</summary><param name="role">刚加载的本机角色。</param>
    public static void SendHintRequest(OnlineRole role)
    {
        SendRequest(role, new MailCall { Command = MailCode.UpdateMailNtf, RoleId = (int)role.GetID() });
    }

    /// <summary>沿现有 GS→MS 链路发包，断链时给原界面对应的失败回执。</summary>
    /// <param name="role">提出请求的在线角色。</param><param name="call">已补入真实收件身份的业务参数。</param>
    private static void SendRequest(OnlineRole role, MailCall call)
    {
        if (MasterTeamLink.Instance.SendToMaster(new BasePackage { GateSessionId = role.GateSessionId,
            UnitySessionId = role.UnitySessionId, ProtoCode = MailCode.RoleRequest, Data = call.ToByteString() })) return;
        // 列表、详情和操作分别返回原公开协议，避免窗口一直显示加载中。
        const string error = "邮箱服务暂未连接";
        if (call.Command == MailCode.GetMailList)
            role.SendToClient(call.Command, new GetMailListRes { Error = error }.ToByteString());
        else if (call.Command == MailCode.GetOneMail)
            role.SendToClient(call.Command, new GetOneMailRes { Error = error }.ToByteString());
        else if (call.Command == MailCode.MailOp)
            role.SendToClient(call.Command, new MailOpRes { Error = error, OpType = call.OpType, MailUid = call.MailUid }.ToByteString());
    }
}
