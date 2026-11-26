using System.Collections.Concurrent;
using Google.Protobuf;

/// <summary>MS 的角色邮箱入口。三个客户端 RPC、业务投递和 GS 附件发放沿用 RO 的职责，存取交本区中心服。</summary>
internal sealed class RoleMail : IContainer
{
    /// <summary>本分区使用的角色存档中心连接。</summary>
    private readonly NetClient center;
    /// <summary>允许接收本区新信通知的在线游戏服连接。</summary>
    private readonly ConcurrentDictionary<int, BaseLink> games = new ConcurrentDictionary<int, BaseLink>();
    /// <summary>当前 MS 所属的正式逻辑区号。</summary>
    private readonly int realmId;
    /// <summary>本区发信使用的 RO 系统邮件模板表。</summary>
    private readonly MailTable templates = new MailTable();

    /// <summary>连接该区配置的中心服并接收数据库回执。</summary><param name="realm">本区部署，空时沿用单机默认区。</param>
    internal RoleMail(RealmTopology realm)
    {
        // 单进程调试使用和 GS 相同的中心服默认地址。
        string host = NetDefine.IPHost;
        // 角色库存储服务的 TCP 端口。
        int port = NetDefine.CenterServerPort;
        realmId = 1;
        if (realm != null) { host = realm.CenterHost; port = realm.CenterPort; realmId = realm.RealmId; }
        center = new NetClient(host, port, ClientType.MasterServer);
        center.RegistCommand(MailCode.MailOperation, this);
        center.StartConnect();
    }

    /// <summary>邮箱无需预先载入所有离线角色。</summary>
    public void OnInit() { }

    /// <summary>游戏服断线后移除通知订阅。</summary><param name="id">MS 本地的 GS 连接编号。</param>
    internal void RemoveGame(int id) { games.TryRemove(id, out _); }

    /// <summary>接收经 GS 确认的角色操作，或来自服务端奖励模块的发信。</summary>
    /// <param name="link">发出请求的 GS 内部连接。</param><param name="packet">保留网关返回地址的协议包。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 来自监听端的会话编号仅由 MS 本机分配。
        int game = ((Session)link).SessionID;
        if (packet.ProtoCode == MailCode.Subscribe) { games[game] = link; return; }
        // 内网操作只携带原 RPC 的必要参数。
        MailCall call;
        if (packet.ProtoCode == MailCode.GsSendMail)
        {
            // 业务系统可以向离线角色投递，角色零表示本区全服。
            GsSendMailArg post = GsSendMailArg.Parser.ParseFrom(packet.Data);
            call = new MailCall { Command = MailCode.GsSendMail, RoleId = post.RoleId, SendMail = templates.Resolve(post.Mail) };
            if (call.SendMail == null)
            {
                link.Send(packet, MailCode.GsSendMail, new MailOpRes { Error = "邮件模板不存在" }.ToByteString());
                return;
            }
        }
        else call = MailCall.Parser.ParseFrom(packet.Data);
        call.GameLink = game;
        call.RealmId = realmId;
        if (!center.Send(packet, MailCode.MailOperation, call.ToByteString()))
        {
            call.Result = new MailOpRes { Error = "邮箱存储暂不可用", OpType = call.OpType, MailUid = call.MailUid };
            Reply(link, packet, call);
        }
    }

    /// <summary>中心确认后发回摘要、详情、操作回执，含附件的读取由 GS 完成实际发放。</summary>
    /// <param name="link">连接本区中心服的客户端。</param><param name="packet">数据库操作结果及原路由。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet)
    {
        // 可信存储返回的业务结果，不接受客户端自带附件。
        MailCall call = MailCall.Parser.ParseFrom(packet.Data);
        if (call.Command == MailCode.GsSendMail && call.Result.Error.Length == 0)
        {
            // 通知仅提示收件箱已变化，客户端打开后照常拉摘要。
            var notice = new MailCall { RoleId = call.RoleId, RealmId = call.RealmId, HasUnread = true };
            // 只枚举仍订阅本区的 GS，不要求收件人当前在线。
            var targets = games.Values;
            var links = new System.Collections.Generic.List<BaseLink>(targets);
            for (int i = 0; i < links.Count; i++) links[i].Send(MailCode.UpdateMailNtf, notice.ToByteString());
        }
        // 发起调用的 GS 可能在数据库回执前重启。
        Session game = SessionMgr.Instance.GetSession(call.GameLink);
        if (game != null) Reply(game, packet, call);
    }

    /// <summary>生成 RO 的三个业务回执，保留角色号供 GS 核对当前会话。</summary>
    /// <param name="game">原请求所属的 GS。</param><param name="packet">原网关和玩家会话。</param><param name="call">中心服实查结果。</param>
    private static void Reply(BaseLink game, BasePackage packet, MailCall call)
    {
        if (call.Command == MailCode.GsSendMail)
        {
            game.Send(packet, MailCode.GsSendMail, call.Result.ToByteString());
            return;
        }
        // 根据查询种类封装公开协议，正文仅在单封查询时下发。
        IMessage response;
        switch (call.Command)
        {
            case MailCode.GetMailList:
                // 仅复制中心返回的信封摘要。
                var list = new GetMailListRes { Error = call.Result.Error, HasUnread = call.HasUnread };
                for (int i = 0; i < call.Mails.Count; i++) list.Mails.Add(call.Mails[i].BaseInfo);
                response = list;
                break;
            case MailCode.GetOneMail:
                // 不存在或过期时保留错误，正文为空。
                var detail = new GetOneMailRes { Error = call.Result.Error };
                if (call.Mails.Count > 0) detail.MailInfo = call.Mails[0];
                response = detail;
                break;
            case MailCode.MailOp:
                if (call.Mails.Count > 0) { game.Send(packet, MailCode.GiveMailPrize, call.ToByteString()); return; }
                response = call.Result;
                break;
            case MailCode.UpdateMailNtf:
                if (call.Result.Error.Length > 0) return;
                response = new UpdateMailNtf { HasUnread = call.HasUnread };
                break;
            default: return;
        }
        packet.ProtoCode = call.Command;
        packet.Data = response.ToByteString();
        game.Send(MailCode.Delivery, new MasterTeamRolePacket { RoleId = (ulong)call.RoleId, Packet = packet }.ToByteString());
    }
}
