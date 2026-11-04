using System;
using System.Threading;

namespace GameServer
{
    /// <summary>游戏逻辑服入口：连接中心服、监听网关，并持续推进单线程游戏逻辑。</summary>
    internal class GameServerApp
    {
        /// <summary>加载配置和网络链路后启动游戏主循环。</summary>
        /// <param name="args">命令行参数，当前未使用。</param>
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
            // 组队请求经已有玩家会话认证后转交独立 MS。
            MasterTeamLink.Instance.Connect(NetDefine.IPHost, NetDefine.MasterServerPort);
            while (true)
            {
                // 每轮先消费网络命令，再由 GameGlobal 按 50ms 固定步长推进业务模块。
                GameGlobal.Instance.Update();
                // 当前为忙轮询；后续可根据队列为空状态接入可唤醒等待，降低空闲 CPU 占用。
            }
        }

        /// <summary>读取 GAME_SERVER_INSTANCE_ID；未配置时采用本地开发实例 1，有值时要求为 0 到 255。</summary>
        private static int GetServerInstanceId()
        {
            // 本地单游戏服的默认编号，多实例部署时由环境变量区分。
            const int defaultServerInstanceId = 1;
            // 物品 UID 编码要用到的游戏服实例配置。
            string configuredValue = Environment.GetEnvironmentVariable("GAME_SERVER_INSTANCE_ID");
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                // 当前本地开发环境只有一个 GameServer，未配置时使用实例 1。
                return defaultServerInstanceId;
            }

            // 解析后的实例编号，占 UID 的 8 位。
            int serverInstanceId;
            if (!int.TryParse(configuredValue, out serverInstanceId) || serverInstanceId < 0 || serverInstanceId > 255)
            {
                throw new InvalidOperationException("GAME_SERVER_INSTANCE_ID 必须是 0 到 255 的整数。");
            }

            return serverInstanceId;
        }
    }
}
