using System.Threading;

/// <summary>独立队伍服进程，监听游戏服的内网 TCP 连接。</summary>
internal static class MasterServerApp
{
    /// <summary>注册队伍服协议后开始接受游戏服请求。</summary>
    /// <param name="args">省略时监听项目默认端口，可传首个参数指定部署端口。</param>
    private static void Main(string[] args)
    {
        // 副本入口按正式DungeonsTable校验，不固定单一副本编号。
        LubanMgr.Instance.Init();
        // 服务端没有上游转发器，直接在收到请求的游戏服连接上回复。
        NetServer server = new NetServer(null);
        // 同一游戏服的队伍请求沿一条 TCP 链路顺序到达。
        // 无参数或仅端口号保留原开发入口；集群由 XML 指定逻辑区服和监听地址。
        int port = NetDefine.MasterServerPort;
        string host = NetDefine.IPHost;
        RealmTopology realm = null;
        if (args.Length > 0 && !int.TryParse(args[0], out port))
        {
            realm = new RealmTopology(args[0]);
            port = realm.MasterPort;
            host = realm.Host;
        }
        MasterTeamService teams = new MasterTeamService(realm);
        server.RegistCommand(SceneLineCode.Register, teams);
        server.RegistCommand(SceneLineCode.Presence, teams);
        server.RegistCommand(NetDefine.RpcC2M_SendChatMsg, teams);
        server.RegistCommand(NetDefine.PtcG2M_ChatAudioTransfer, teams);
        server.RegistCommand(NetDefine.CMD_MasterTeamRequest, teams);
        server.RegistCommand(NetDefine.CMD_MasterTeamOffline, teams);
        // 区服邮箱和队伍共用此 MS 监听器，各自处理本领域的内部命令。
        RoleMail mail = new RoleMail(realm);
        server.RegistCommand(FriendCode.RoleRequest, new RoleFriend(realm, teams));
        server.RegistCommand(ChatRoomCode.RoleRequest, new ChatRoomService(realm, teams));
        server.RegistCommand(MailCode.RoleRequest, mail);
        server.RegistCommand(MailCode.Subscribe, mail);
        server.RegistCommand(MailCode.GiftArrived, mail);
        server.RegistCommand(MailCode.GsSendMail, mail);
        server.OnSessionDisconnected = teams.RemoveGame;
        server.OnSessionDisconnected += mail.RemoveGame;
        // 独立分区或隔离验证可单独配置监听端口。
        server.StartServer(host, port);
        // 确认超时由 MS 主循环结束，100 毫秒粒度不承担任何副本或战斗运算。
        while (true)
        {
            teams.Update();
            Thread.Sleep(100);
        }
    }
}
