using System.Threading;

namespace GateServer
{
    internal class GateServerApp
    {
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
            client.RegistCommand(NetDefine.CMD_RoleSkillInfoCode, gateRoleCtrl);//技能数据返回unity
            client.RegistCommand(NetDefine.CMD_SyncRoleEnterWorldCode, gateRoleCtrl);//同步主角给其他玩家
            client.RegistCommand(NetDefine.CMD_SyncotherOnlineCode, gateRoleCtrl);//同步其他玩家给主角
            client.RegistCommand(NetDefine.CMD_RoleKnapsackInfoCode, gateRoleCtrl);//返回角色背包信息
            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}