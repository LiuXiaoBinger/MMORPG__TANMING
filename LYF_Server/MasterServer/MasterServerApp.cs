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
        MasterTeamService teams = new MasterTeamService();
        server.RegistCommand(NetDefine.RpcC2M_SendChatMsg, teams);
        server.RegistCommand(NetDefine.PtcG2M_ChatAudioTransfer, teams);
        server.RegistCommand(NetDefine.CMD_MasterTeamRequest, teams);
        server.RegistCommand(NetDefine.CMD_MasterTeamOffline, teams);
        server.OnSessionDisconnected = teams.RemoveGame;
        // 独立分区或隔离验证可单独配置监听端口。
        int port = NetDefine.MasterServerPort;
        if (args.Length > 0) port = int.Parse(args[0]);
        server.StartServer(NetDefine.IPHost, port);
        // 确认超时由 MS 主循环结束，100 毫秒粒度不承担任何副本或战斗运算。
        while (true)
        {
            teams.Update();
            Thread.Sleep(100);
        }
    }
}
