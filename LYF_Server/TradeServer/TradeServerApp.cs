using System.Threading;

/// <summary>本区交易服务入口，负责连接存档中心并接受网关和 GS。</summary>
internal static class TradeServerApp
{
    /// <summary>读取可编辑的区服配置，启动本区唯一的市场监听。</summary>
    /// <param name="args">首项为区服 XML 路径。</param>
    private static void Main(string[] args)
    {
        // 每个进程只服务一个逻辑区；端口零为运营关闭市场。
        RealmTopology realm = new RealmTopology(args[0]);
        if (realm.TradePort == 0) return;
        // 市场索引和 GS 的角色归属都只在这个区服有效。
        StallManager market = new StallManager(realm);
        // 先注册市场和角色路由入口，再开放监听并向中心装载数据。
        NetServer server = new NetServer(null);
        server.RegistCommand(StallCode.Read, market);
        server.RegistCommand(StallCode.StallSellItemCancel, market);
        server.RegistCommand(StallCode.Deal,market);
        server.RegistCommand(StallCode.Abort,market);
        server.RegistCommand(StallCode.Bind, market);
        server.RegistCommand(StallCode.Unbind, market);
        server.OnSessionDisconnected = market.Disconnected;
        server.StartServer(realm.TradeHost, realm.TradePort);
        market.Load();
        Thread.Sleep(Timeout.Infinite);
    }
}
