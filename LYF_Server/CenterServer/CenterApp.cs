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
    /// <summary>
    /// 中心服务器进程入口。负责初始化 Luban 配置、监听中心服端口（默认 10110）、
    /// 建立 SqlSugar 数据库连接、注册登录与角色持久化协议处理器、初始化 Redis，
    /// 并在主线程以 1ms 节拍驱动数据库写任务队列。
    /// </summary>
    internal class CenterApp
    {
        /// <summary>
        /// 进程主入口，按配置→网络监听→数据库→协议注册→Redis→主循环的顺序完成中心服启动。
        /// </summary>
        /// <param name="args">命令行参数，当前未使用。</param>
        static void Main(string[] args)
        {
            // 加载 Luban 技能、职业等静态配置表，供建角与查询时读取。
            LubanMgr.Instance.Init();

            // 中心服作为网络服务器监听，接收 LoginServer/GameServer 转发的协议包。
            NetServer server = new NetServer(null);
            server.StartServer(NetDefine.IPHost, NetDefine.CenterServerPort);

            // 初始化 MySQL 连接并按 CodeFirst 建库建表，返回共享数据库客户端。
            SqlSugarClient db = DBMgr.Instance.initDB();

            // 登录模块控制器：处理注册、登录、服务器列表、选服、建角、开始游戏等账号类协议。
            Center_LoginCtrl loginCtrl = new Center_LoginCtrl(new LoginModle(db));

            server.RegistCommand(NetDefine.CMD_RegistCode, loginCtrl);//注册
            server.RegistCommand(NetDefine.CMD_LoginCode, loginCtrl);//登录
            server.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);//获取服务器列表
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);//登录游戏服务器
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);//创建角色
            server.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);//游戏开始
            RedisMgr.Instance.Init();
            
            // 角色模块控制器：处理进入世界回包、背包容量同步、商城购买转发、角色物品快照保存。
            CenterRoleCtrl  RoleCtrl = new CenterRoleCtrl(new CentRoleModel(db));
            server.RegistCommand(NetDefine.CMD_EnterWroldCode, RoleCtrl);//进入游戏请求
            server.RegistCommand(NetDefine.CMD_SyncKnapsackGridCountCode, RoleCtrl);//同步背包已开格子数量
            server.RegistCommand(NetDefine.CMD_BuyShopItemCode, RoleCtrl);//处理 GameServer 转发的商城购买请求
            server.RegistCommand(NetDefine.CMD_SaveRoleDataCode, RoleCtrl);//接收 GameServer 定时角色物品快照

            // 中心服主循环：单消费者顺序执行数据库写任务，1ms 唤醒一次。
            while (true)
            {
                DBMgr.Instance.Update();
                Thread.Sleep(1);
            }



        }
    }
}
