using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cfg;
using SqlSugar;

namespace CenterServer
{
    internal class CenterApp
    {
        
        static void Main(string[] args)
        {
            LubanMgr.Instance.Init();
           Dictionary<int,SkillInfo> jobskill= LubanMgr.Instance.GetSkillInfosByJob(1);
           foreach (KeyValuePair<int, SkillInfo> item in jobskill)
           {
               LogMsg.Info(jobskill[item.Key].ToString());
           }
            NetServer server = new NetServer(null);
            server.StartServer(NetDefine.IPHost, NetDefine.CenterServerPort);

            SqlSugarClient db = DBMgr.Instance.initDB(); 

            Center_LoginCtrl loginCtrl = new Center_LoginCtrl(new LoginModle(db));

            server.RegistCommand(NetDefine.CMD_RegistCode, loginCtrl);//注册
            server.RegistCommand(NetDefine.CMD_LoginCode, loginCtrl);//登录
            server.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);//获取服务器列表
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);//登录游戏服务器
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);//创建角色
            server.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);//游戏开始
            RedisMgr.Instance.Init();
            
            CenterRoleCtrl  RoleCtrl = new CenterRoleCtrl(new CentRoleModel(db));
            server.RegistCommand(NetDefine.CMD_EnterWroldCode, RoleCtrl);//进入游戏请求
           
            while (true)
            {
                Thread.Sleep(1);
            }



        }
    }
}
