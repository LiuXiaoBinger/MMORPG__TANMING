using System;
using SqlSugar;

public class DBMgr:Singleton<DBMgr>
{
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
        // Server=localhost;Port=3306;DataBase=gm_game;User=root;Password=123456;
        //建库
        Db.DbMaintenance.CreateDatabase();//达梦和Oracle不支持建库
 
        //建表（看文档迁移）
        Db.CodeFirst.InitTables(typeof(AccoutTable)
            , typeof(GameServerTable)
            ,typeof(RoleTable),typeof(RoleBaseArrtTable)
            ,typeof(RoleSkillTable)); //所有库都支持  

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
}
