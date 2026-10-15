using System.Threading;

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
        /// <param name="args">命令行参数，当前未使用。</param>
        public static void Main(string[] args)
        {
            //网关逻辑客户端 去连接游戏逻辑服务器服务器
            NetClient client = new NetClient(NetDefine.IPHost, NetDefine.GameServerPort, ClientType.GateServer);
            client.StartConnect();
            //网关服务器 开启服务器端
            NetServer server = new NetServer(client);
            server.StartServer(NetDefine.IPHost,NetDefine.GateServerPort);
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
            server.RegistCommand(NetDefine.CMD_BuyShopItemCode, gateRoleCtrl);//商城购买请求
            client.RegistCommand(NetDefine.CMD_RoleSkillInfoCode, gateRoleCtrl);//技能数据返回unity
            client.RegistCommand(NetDefine.CMD_SyncRoleEnterWorldCode, gateRoleCtrl);//同步主角给其他玩家
            client.RegistCommand(NetDefine.CMD_SyncotherOnlineCode, gateRoleCtrl);//同步其他玩家给主角
            client.RegistCommand(NetDefine.CMD_RoleKnapsackInfoCode, gateRoleCtrl);//返回角色背包信息
            client.RegistCommand(NetDefine.CMD_RoleShopPurchaseInfoCode, gateRoleCtrl);//返回角色商城购买记录
            client.RegistCommand(NetDefine.CMD_OpenKnapsackGridCode, gateRoleCtrl);//开启背包格子结果
            client.RegistCommand(NetDefine.CMD_BuyShopItemCode, gateRoleCtrl);//商城购买结果
            client.RegistCommand(NetDefine.CMD_CountUpdateSyncInfoCode, gateRoleCtrl);//同步商城和副本次数

            // 物品变更回包，由 GameServer 主动推送给 Unity。
            client.RegistCommand(NetDefine.CMD_SC_UpdateItemInfoCode, gateRoleCtrl);
            // 技能请求由网关透传，权威结果和起止通知按 Unity 会话原路返回。
            GateSkillCtrl gateSkillCtrl = new GateSkillCtrl();
            // Unity→GameServer 施法请求上行转发。
            server.RegistCommand(NetDefine.CMD_SkillCastCode, gateSkillCtrl);
            // GameServer→Unity 施法权威结果、开始/结束通知下行转发。
            client.RegistCommand(NetDefine.CMD_SkillCastCode, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_SkillCastStartCode, gateSkillCtrl);
            client.RegistCommand(NetDefine.CMD_SkillCastEndCode, gateSkillCtrl);
            // 移动只做原路上报和附近通知转发，网关不参与位置结算。
            GateMoveCtrl gateMoveCtrl = new GateMoveCtrl();
            server.RegistCommand(NetDefine.CMD_MoveOperationCode, gateMoveCtrl);
            client.RegistCommand(NetDefine.CMD_SyncMoveCode, gateMoveCtrl);
            // 网关系程不做业务，仅挂起等待网络事件。
            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}
