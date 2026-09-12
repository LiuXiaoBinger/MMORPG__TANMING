using System;
using SqlSugar;
using System.Collections.Concurrent;

public class DBMgr:Singleton<DBMgr>
{
    private const int MaxTasksPerUpdate = 100;
    private readonly ConcurrentQueue<CRoleWriteTask> _tasks = new ConcurrentQueue<CRoleWriteTask>();
    private SqlSugarClient _db;

    public SqlSugarClient initDB()
    { 
        ConnectionConfig connectionConfig =new ConnectionConfig()
        { 
            ConnectionString = "Server=localhost;Port=3308;DataBase=game_db;User=root;Password=123456",
            DbType = DbType.MySql,
            IsAutoCloseConnection = true
        };
        //创建数据库对象 (用法和EF Dappper一样通过new保证线程安全)
        SqlSugarClient Db = new SqlSugarClient(connectionConfig);
        _db = Db;
        // Server=localhost;Port=3306;DataBase=gm_game;User=root;Password=123456;
        //建库
        Db.DbMaintenance.CreateDatabase();//达梦和Oracle不支持建库
 
        //建表（看文档迁移）
        Db.CodeFirst.InitTables(typeof(AccoutTable)
            , typeof(GameServerTable)
            ,typeof(RoleTable),typeof(RoleBaseArrtTable)
            ,typeof(RoleSkillTable)
            ,typeof(RoleKnapsackTable)
            ,typeof(ItemTable)
            ,typeof(EquipTable)
            ,typeof(EquipXLGeneTable)
            ,typeof(RoleShopPurchaseTable)
            ,typeof(RoleCountTable)); //角色通用次数

        /*for (int i = 0; i < 30; i++)
        {
            GameServerTable gameServerTable = new GameServerTable()
            {
                ServerName = (i + 1) + "区 五域大陆",
                RunState = 1,
                IsNew = 1,
                IpHost = NetDefine.IPHost,
                Port = NetDefine.GateServerPort,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
            };
            Db.Insertable(gameServerTable).ExecuteCommand();
        }*/
                
        return Db;
    }

    /// <summary>网络线程只投递数据库任务，实际执行由 CenterServer 主循环完成。</summary>
    public void PushTask(CRoleWriteTask task)
    {
        if (task != null)
        {
            _tasks.Enqueue(task);
        }
    }

    /// <summary>单消费者顺序执行写任务，保证同一角色的版本比较与事务提交不并发。</summary>
    public int Update()
    {
        if (_db == null)
        {
            return 0;
        }

        int processed = 0;
        CRoleWriteTask task;
        while (processed < MaxTasksPerUpdate && _tasks.TryDequeue(out task))
        {
            task.Execute(_db);
            processed++;
        }
        return processed;
    }
}
