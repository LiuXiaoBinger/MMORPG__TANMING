using System;
using System.Threading;

namespace GameServer
{
    internal class GameServerApp
    {
        public static void Main(string[] args)
        {
            LubanMgr.Instance.Init();
            // 每个逻辑服实例必须使用不同编号，避免跨进程生成重复物品 UID。
            UIDHelper.ConfigureServerInstanceId(GetServerInstanceId());
            // GameServer 作为客户端连接 CenterServer，接收登录、角色和持久化结果。
            NetClient client = new NetClient(NetDefine.IPHost, NetDefine.CenterServerPort, ClientType.GameServer);
            RoleNetSender.Configure(client);
            client.StartConnect();
            // GameServer 作为服务端监听 GateServer 转发的客户端请求。
            NetServer server = new NetServer(client);
            server.OnSessionDisconnected = GameGlobal.Instance.EnqueueGateSessionDisconnected;
            server.StartServer(NetDefine.IPHost,NetDefine.GameServerPort);
            GameGlobal.Instance.Init();
            
            // 协议注册只绑定网络入口和独立处理器，业务仍由逻辑线程执行。
            GameMessageProtocolRegister.Register(server, client);
            while (true)
            {
                // 每轮先消费网络命令，再由 GameGlobal 按 50ms 固定步长推进业务模块。
                GameGlobal.Instance.Update();
                // 当前为忙轮询；后续可根据队列为空状态接入可唤醒等待，降低空闲 CPU 占用。
            }
        }

        private static int GetServerInstanceId()
        {
            const int defaultServerInstanceId = 1;
            string configuredValue = Environment.GetEnvironmentVariable("GAME_SERVER_INSTANCE_ID");
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                // 当前本地开发环境只有一个 GameServer，未配置时使用实例 1。
                return defaultServerInstanceId;
            }

            int serverInstanceId;
            if (!int.TryParse(configuredValue, out serverInstanceId) || serverInstanceId < 0 || serverInstanceId > 255)
            {
                throw new InvalidOperationException("GAME_SERVER_INSTANCE_ID 必须是 0 到 255 的整数。");
            }

            return serverInstanceId;
        }
    }
}
