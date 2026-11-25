using System.Threading;

using Google.Protobuf;

namespace GateServer
{
    /// <summary>
    /// 网关服务器进程入口。网关以客户端身份连接游戏逻辑服（10130），同时以服务器身份
    /// 监听 Unity 客户端连接（默认 10140）；注册登录、角色、背包、商城与技能等协议的双向转发，
    /// 自身不执行业务结算，只负责会话寻址与协议/错误码校验转发。
    /// </summary>
    internal class GateServerApp
    {
        /// <summary>
        /// 进程主入口：连接游戏服→监听 Unity→注册各协议双向转发→进入休眠主循环。
        /// </summary>
        /// <param name="args">可选的区服拓扑 XML 路径。无参数时使用原单节点开发入口。</param>
        public static void Main(string[] args)
        {
            // 单节点与区服集群共用原 TCP/KCP 协议登记表。
            NetClient client;
            // 多节点部署按玩家场景选择不同内网上游。
            RealmGameLink router = null;
            // 监听地址和端口默认与旧客户端兼容。
            string host = NetDefine.IPHost;
            int port = NetDefine.GateServerPort;
            int udpPort = KcpDefine.GateUdpPort;
            if (args.Length > 0)
            {
                // 独立区服的网关和各个 GS 读取相同的部署描述。
                RealmTopology config = new RealmTopology(args[0]);
                router = new RealmGameLink(config);
                client = router;
                host = config.Host;
                port = config.GatePort;
                udpPort = config.UdpPort;
            }
            else client = new NetClient(host, NetDefine.GameServerPort, ClientType.GateServer);
            // 玩家连接仍由原网关接受。
            NetServer server = new NetServer(client);
            // TCP 往返测量由接入会话直接应答，在开始监听前登记处理器。
            server.RegistCommand(NetDefine.RpcC2T_SyncTime, new GateSyncTimeCtrl());

            // 实时通道：移动与技能只走可靠 UDP（KCP），其余业务仍走上面的 TCP 监听。
            // 本次运行的 UDP 网络实例，负责监听、认证与 KCP 会话路由。
            UdpNetwork udpNetwork = new UdpNetwork();
            if (!udpNetwork.Start(host, udpPort))
            {
                // 启动失败时仅告警：客户端绑定会被明确拒绝，并沿现有断线恢复流程重建连接。
                LogMsg.Info("KCP UDP 通道启动失败，客户端实时绑定将被拒绝", LogMsgType.Warn);
            }

            GateLoginCtrl  gateLoginCtrl = new GateLoginCtrl();
            //注册指令
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, gateLoginCtrl);//登录游戏服务器
            client.RegistCommand(NetDefine.CMD_LoginGameServerCode, gateLoginCtrl);//登录游戏服务器
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, gateLoginCtrl);//创建角色
            client.RegistCommand(NetDefine.CMD_CreateRoleCode, gateLoginCtrl);//创建角色
            server.RegistCommand(NetDefine.CMD_StartGameCode, gateLoginCtrl);//游戏开始
            client.RegistCommand(NetDefine.CMD_StartGameCode, gateLoginCtrl);//游戏开始

            GateRoleCtrl  gateRoleCtrl = new GateRoleCtrl();
            server.RegistCommand(NetDefine.CMD_EnterWroldCode, gateRoleCtrl);//进入游戏请求
            server.RegistCommand(NetDefine.CMD_OpenKnapsackGridCode, gateRoleCtrl);//开启背包格子请求
            server.RegistCommand(NetDefine.CMD_EquipOperationCode, gateRoleCtrl);//装备实例穿脱和特技切换请求
            server.RegistCommand(NetDefine.CMD_BuyShopItemCode, gateRoleCtrl);//商城购买请求
            client.RegistCommand(NetDefine.CMD_RoleSkillInfoCode, gateRoleCtrl);//技能数据返回unity
            client.RegistCommand(NetDefine.CMD_SyncRoleEnterWorldCode, gateRoleCtrl);//同步主角给其他玩家
            client.RegistCommand(NetDefine.CMD_SyncotherOnlineCode, gateRoleCtrl);//同步其他玩家给主角
            client.RegistCommand(NetDefine.CMD_RoleKnapsackInfoCode, gateRoleCtrl);//返回角色背包信息
            client.RegistCommand(NetDefine.CMD_RoleShopPurchaseInfoCode, gateRoleCtrl);//返回角色商城购买记录
            client.RegistCommand(NetDefine.CMD_OpenKnapsackGridCode, gateRoleCtrl);//开启背包格子结果
            client.RegistCommand(NetDefine.CMD_EquipOperationCode, gateRoleCtrl);//装备实例操作结果
            client.RegistCommand(NetDefine.CMD_RoleAttributeUpdateCode, gateRoleCtrl);//最终装备与基因属性通知
            client.RegistCommand(NetDefine.CMD_BuyShopItemCode, gateRoleCtrl);//商城购买结果
            client.RegistCommand(NetDefine.CMD_CountUpdateSyncInfoCode, gateRoleCtrl);//同步商城和副本次数

            // 物品变更回包，由 GameServer 主动推送给 Unity。
            client.RegistCommand(NetDefine.CMD_SC_UpdateItemInfoCode, gateRoleCtrl);
            // 技能请求由网关透传，权威结果和起止通知按 Unity 会话原路返回。
            GateSkillCtrl gateSkillCtrl = new GateSkillCtrl();
            // Unity→GameServer 施法请求上行转发。
            server.RegistCommand(NetDefine.CMD_SkillCastCode, gateSkillCtrl);
            server.RegistCommand(NetDefine.CMD_SkillUpgradeCode, gateSkillCtrl);
            server.RegistCommand(NetDefine.CMD_MartialManual, gateSkillCtrl);
            // GameServer→Unity 施法权威结果、开始/结束通知下行转发。
            client.RegistCommand(NetDefine.CMD_SkillCastCode, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_SkillUpgradeCode, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_MartialManual, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_SkillCastStartCode, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_SkillCastEndCode, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_MonsterAttrUpdateCode, gateSkillCtrl);
            // 移动上报、附近通知和本人纠正都复用网关移动转发。
            GateMoveCtrl gateMoveCtrl = new GateMoveCtrl();
            server.RegistCommand(NetDefine.CMD_MoveOperationCode, gateMoveCtrl);
            server.RegistCommand(NetDefine.CMD_DungeonInteractCode, gateMoveCtrl);
            // 原对话命令沿现有网关转发保持真实复合会话身份。
            server.RegistCommand(NetDefine.CMD_DungeonCommandEventCode, gateMoveCtrl);
            client.RegistCommand(NetDefine.CMD_DungeonInteractResultCode, gateMoveCtrl);
            // 复活沿用真实复合会话转发，网关不恢复生命或修改坐标。
            server.RegistCommand(NetDefine.CMD_RoleReviveCode, gateMoveCtrl);
            client.RegistCommand(NetDefine.CMD_RoleReviveResultCode, gateMoveCtrl);
            client.RegistCommand(NetDefine.CMD_SyncMoveCode, gateMoveCtrl);
            client.RegistCommand(NetDefine.CMD_MovePositionCorrectionCode, gateMoveCtrl);
            // 队伍与副本入场：6 条上行原样转给游戏服，4 条下行按 Unity 会话回发。
            // 网关只做会话寻址，队伍状态与实例分配全部由 GameServer 权威决定。
            // 发言请求和两种客户端回包由正式聊天通道转发。
            GateChatCtrl gateChatCtrl = new GateChatCtrl();
            server.RegistCommand(NetDefine.RpcC2M_SendChatMsg, gateChatCtrl);
            server.RegistCommand(NetDefine.RpcC2M_UploadChatAudio, gateChatCtrl);
            server.RegistCommand(NetDefine.RpcC2M_DownloadChatAudio, gateChatCtrl);
            client.RegistCommand(NetDefine.RpcC2M_UploadChatAudio, gateChatCtrl);
            client.RegistCommand(NetDefine.RpcC2M_DownloadChatAudio, gateChatCtrl);
            client.RegistCommand(NetDefine.RpcC2M_SendChatMsg, gateChatCtrl);
            client.RegistCommand(NetDefine.PtcT2C_ChattingMsgNtf, gateChatCtrl);
            GateTeamCtrl gateTeamCtrl = new GateTeamCtrl();
            server.RegistCommand(NetDefine.CMD_CreateTeamCode, gateTeamCtrl);
            server.RegistCommand(NetDefine.CMD_InviteJoinTeamCode, gateTeamCtrl);
            server.RegistCommand(NetDefine.CMD_AcceptTeamInvatationCode, gateTeamCtrl);
            server.RegistCommand(NetDefine.CMD_LeaveTeamCode, gateTeamCtrl);
            server.RegistCommand(NetDefine.CMD_BegInTeamByTeamIdCode, gateTeamCtrl);
            server.RegistCommand(NetDefine.CMD_CaptainRequestEnterFBCode, gateTeamCtrl);
            server.RegistCommand(NetDefine.PtcC2M_MemberReplyEnterFBPtc, gateTeamCtrl);
            client.RegistCommand(NetDefine.PtcM2C_AskMemberEnterFBNtf, gateTeamCtrl);
            client.RegistCommand(NetDefine.PtcM2C_SyncMemberReplyEnterFBNtf, gateTeamCtrl);
            client.RegistCommand(NetDefine.PtcM2C_FailEnterFBNtf, gateTeamCtrl);
            client.RegistCommand(NetDefine.CMD_TeamInfoCode, gateTeamCtrl);
            client.RegistCommand(NetDefine.CMD_TeamInvatationCode, gateTeamCtrl);
            client.RegistCommand(NetDefine.CMD_TeamEnterDungeonCode, gateTeamCtrl);
            client.RegistCommand(NetDefine.CMD_RoleLeaveCode, gateTeamCtrl);
            // 队伍请求失败时游戏服回的是通用错误协议，必须注册同一入口才能转给客户端提示。
            client.RegistCommand(NetDefine.CMD_ErrCode, gateTeamCtrl);
            // 绑定协议只在网关与客户端之间处理，不转发给游戏服。
            // 绑定协议处理器实例，只在网关与客户端之间分配参数并回包。
            GateKcpCtrl gateKcpCtrl = new GateKcpCtrl();
            server.RegistCommand(NetDefine.CMD_KcpBindCode, gateKcpCtrl);
            server.RegistCommand(StallCode.Browse, new GateStallCtrl(router));
            if (router != null)
            {
                server.RegistCommand(SceneLineCode.List, gateMoveCtrl);
                server.RegistCommand(SceneLineCode.Change, gateMoveCtrl);
                server.OnSessionDisconnected = router.PlayerLeft;
            }
            server.StartServer(host, port);
            if (router != null) router.StartRealm();
            else StartLegacy(client);
            // 网关系程不做业务，仅挂起等待网络事件。
            while (true)
            {
                Thread.Sleep(1);
            }
        }
        /// <summary>保留无参数单 GS 入口，把实际可用的本机一区登记到选服目录。</summary>
        /// <param name="game">旧式网关转发游戏消息的唯一 GS 连接。</param>
        private static void StartLegacy(NetClient game)
        {
            // 老式网关没有集群的查档链路，使用独立内网连接登记一区。
            NetClient directory = new NetClient(NetDefine.IPHost, NetDefine.CenterServerPort, ClientType.GateServer);
            // 仅当前 GS 已接通时允许中心服发布一区。
            bool gameReady = false;
            // 首次连上任一侧或重连时，只向已经建立的中心链路发送。
            bool centerReady = false;
            // 中心或游戏服先后上线及重连时共用同一区号。
            ByteString notice = new SceneNodeRegistration { RealmId = 1 }.ToByteString();
            directory.OnConnSucceed = () =>
            {
                lock (directory)
                {
                    centerReady = true;
                    if (gameReady) directory.Send(SceneLineCode.RealmOnline, notice);
                }
            };
            directory.OnConnFailed = () => { lock (directory) centerReady = false; };
            game.OnConnSucceed = () =>
            {
                lock (directory)
                {
                    gameReady = true;
                    if (centerReady) directory.Send(SceneLineCode.RealmOnline, notice);
                }
            };
            game.OnConnFailed = () =>
            {
                lock (directory)
                {
                    gameReady = false;
                    if (centerReady) directory.Send(SceneLineCode.RealmOffline, ByteString.Empty);
                }
            };
            directory.StartConnect();
            game.StartConnect();
        }
    }
}
