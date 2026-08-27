using System.Threading;

namespace GameServer
{
    internal class GameServerApp
    {
        public static void Main(string[] args)
        {
            //游戏逻辑客户端 去连接中心服务器
            NetClient client = new NetClient(NetDefine.IPHost, NetDefine.CenterServerPort, ClientType.GameServer);
            client.StartConnect();
            //游戏逻辑服务器 开启服务器端
            NetServer server = new NetServer(client);
            server.StartServer(NetDefine.IPHost,NetDefine.GameServerPort);
            GameGlobal.Instance.Init();
            Game_LoginCtrl  gameLoginCtrl = new Game_LoginCtrl();
            //注册指令
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, gameLoginCtrl);//登录游戏服务器
            client.RegistCommand(NetDefine.CMD_LoginGameServerCode, gameLoginCtrl);//登录游戏服务器
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, gameLoginCtrl);//创建角色
            client.RegistCommand(NetDefine.CMD_CreateRoleCode, gameLoginCtrl);//创建角色
            server.RegistCommand(NetDefine.CMD_StartGameCode, gameLoginCtrl);//游戏开始
            client.RegistCommand(NetDefine.CMD_StartGameCode, gameLoginCtrl);//游戏开始
            //server.RegistCommand(NetDefine.CMD_StartGameCode, gameLoginCtrl);//游戏开始
            //client.RegistCommand(NetDefine.CMD_RoleSkillInfoCode, gameLoginCtrl);//游戏开始
            GameRoleCtrl  gameRoleCtrl = new GameRoleCtrl();
            server.RegistCommand(NetDefine.CMD_EnterWroldCode, gameRoleCtrl);//进入游戏请求
            client.RegistCommand(NetDefine.CMD_RoleSkillInfoCode, gameRoleCtrl);//技能数据返回unity
           
            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}