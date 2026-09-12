using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Message;


namespace LoginServer
{
    internal class LoginApp
    {
        static void Main(string[] args)
        {
            
            
            NetClient client = new NetClient(NetDefine.IPHost, 10110, ClientType.LoginServer);
            client.StartConnect();
            
            NetServer server = new NetServer(client);
            server.StartServer(NetDefine.IPHost,NetDefine.LoginServerPort);
            
            LoginCtrl loginCtrl = new LoginCtrl();
            //注册指令
            server.RegistCommand(NetDefine.CMD_RegistCode,loginCtrl);
            client.RegistCommand(NetDefine.CMD_RegistCode,loginCtrl);
            
            
            server.RegistCommand(NetDefine.CMD_LoginCode,loginCtrl);
            client.RegistCommand(NetDefine.CMD_LoginCode,loginCtrl);
            
            server.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);//获取服务器列表
            client.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);//获取服务器列表
           
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);//登录游戏服务器
            client.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);//登录游戏服务器
            
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);//创建角色
            client.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);//创建角色
            
            server.RegistCommand(NetDefine.CMD_RegistVarifyCode, loginCtrl);//获取验证码
            /*new Timer(_ =>
            {

                RegistReq req = new RegistReq()
                {
                    UserName = "aaaaaa",
                    PhoneNum = "13000000000",
                    Password = "12345"
                };
                client.SendData(NetDefine.CMD_RegistCode, req.ToByteString());

            }, null, 5000, Timeout.Infinite);*/

            #region Grpc

            // 验证码服务端需要先监听此地址；部署时改成实际服务端地址。
            GRPCMgr.Instance.InitVarifyClient("127.0.0.1", 50051);


            #endregion

            while (true)
            {
                Thread.Sleep(1);
            }

        }
    }
}
