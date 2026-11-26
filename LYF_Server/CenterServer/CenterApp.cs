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
        /// <param name="args">可选的区服 XML 路径，用于指定中心服监听地址和端口。</param>
        static void Main(string[] args)
        {
            // 加载 Luban 技能、职业等静态配置表，供建角与查询时读取。
            LubanMgr.Instance.Init();
            // 共享中心使用实例零生成本库交易凭证与挂牌号。
            UIDHelper.ConfigureServerInstanceId(0);

            // 中心服作为网络服务器监听，接收 LoginServer/GameServer 转发的协议包。
            NetServer server = new NetServer(null);

            // 初始化 MySQL 连接并按 CodeFirst 建库建表，返回共享数据库客户端。
            SqlSugarClient db = DBMgr.Instance.initDB();

            // 登录模块控制器：处理注册、登录、服务器列表、选服、建角、开始游戏等账号类协议。
            // 同一中心服读取所有获准开放的区号，并登记当前在线的区服网关。
            RealmTopology realm = null;
            if (args.Length > 0) realm = new RealmTopology(args[0]);
            LoginModle model = new LoginModle(db, realm);
            Center_LoginCtrl loginCtrl = new Center_LoginCtrl(model);
            server.RegistCommand(SceneLineCode.RealmOnline, loginCtrl);
            server.RegistCommand(SceneLineCode.RealmOffline, loginCtrl);
            server.OnSessionDisconnected = model.Realms.Close;
            server.RegistCommand(NetDefine.CMD_RegistCode, loginCtrl);//注册
            server.RegistCommand(NetDefine.CMD_LoginCode, loginCtrl);//登录
            server.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);//获取服务器列表
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);//登录游戏服务器
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);//创建角色
            server.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);//游戏开始
            // 市场托管数据的读取与撤单共用中心服数据库配置。
            StallLoadCtrl stall = new StallLoadCtrl(db.CurrentConnectionConfig);
            server.RegistCommand(StallCode.Load, stall);
            // 本区 MS 经内部连接向共享角色库操作邮箱。
            server.RegistCommand(MailCode.MailOperation, new MailStore(db.CurrentConnectionConfig));
            server.RegistCommand(FriendCode.Operation, new RoleMessage(db.CurrentConnectionConfig));
            server.RegistCommand(ChatRoomCode.Operation, new ChatRoomManager(db.CurrentConnectionConfig));
            server.RegistCommand(StallCode.StallSellItemCancelCommit, stall);
            server.RegistCommand(StallCode.Prepare, stall);
            server.RegistCommand(StallCode.Resume, stall);
            server.RegistCommand(StallCode.Abort, stall);
            RedisMgr.Instance.Init();
            
            // 角色模块控制器：处理进入世界回包、背包容量同步、商城购买转发、角色物品快照保存。
            CenterRoleCtrl  RoleCtrl = new CenterRoleCtrl(new CentRoleModel(db));
            // 接收公共地图宿主确认的过门落点，供下一次选角读取。
            server.RegistCommand(SceneLineCode.SavePublicArrival, RoleCtrl);
            server.RegistCommand(NetDefine.CMD_EnterWroldCode, RoleCtrl);//进入游戏请求
            server.RegistCommand(NetDefine.CMD_SyncKnapsackGridCountCode, RoleCtrl);//同步背包已开格子数量
            server.RegistCommand(NetDefine.CMD_BuyShopItemCode, RoleCtrl);//处理 GameServer 转发的商城购买请求
            server.RegistCommand(NetDefine.CMD_SaveRoleDataCode, RoleCtrl);//接收 GameServer 定时角色物品快照
            // 独立部署从同一份区服文件读取内网地址，数据库和处理器就绪后再开放端口。
            string host = NetDefine.IPHost;
            int port = NetDefine.CenterServerPort;
            if (realm != null)
            {
                host = realm.CenterHost;
                port = realm.CenterPort;
            }
            server.StartServer(host, port);

            // 中心服主循环：单消费者顺序执行数据库写任务，1ms 唤醒一次。
            while (true)
            {
                DBMgr.Instance.Update();
                Thread.Sleep(1);
            }



        }
    }
}
