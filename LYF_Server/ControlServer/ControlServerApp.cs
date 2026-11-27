using System.Threading;

/// <summary>独立运行的区服调度进程，管理公共地图线路、宿主与玩家占用。</summary>
internal static class ControlServerApp
{
    /// <summary>读取同区 GS 共用的配置，启动内网调度端口。</summary>
    /// <param name="args">第一个参数为区服 XML 文件路径。</param>
    private static void Main(string[] args)
    {
        // 此进程只管理配置中指定的一个逻辑区服。
        RealmTopology realm = new RealmTopology(args[0]);
        // 每个调度进程共用一份场景目录。
        SceneDirectory directory = new SceneDirectory(realm);
        // GS 和网关均以客户端身份接入调度端口。
        NetServer server = new NetServer(null);
        // 监听前完整注册所有允许的内部操作。
        ushort[] codes = { SceneLineCode.Register, SceneLineCode.Allocate, SceneLineCode.List,
            SceneLineCode.Commit, SceneLineCode.Cancel, SceneLineCode.Disconnect, SceneLineCode.Saved,
            SceneLineCode.CreateStaticScene, SceneLineCode.HostLoad, SceneLineCode.ChooseDungeonHost };
        for (int index = 0; index < codes.Length; index++) server.RegistCommand(codes[index], directory);
        server.OnSessionDisconnected = directory.Disconnected;
        server.StartServer(realm.Host, realm.ControlPort);
        Thread.Sleep(Timeout.Infinite);
    }
}
