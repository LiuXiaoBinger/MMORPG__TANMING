



using System;
using SqlSugar;

/// <summary>
/// 用户表
/// </summary>

[SugarTable("game_server",TableDescription = "服务器列表")]
public class GameServerTable
{
    //id
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增  IsPrimaryKey是否是主键 IsIdentity是否自增长
    public int Id { get; set; }
    //状态
    [SugarColumn(DefaultValue = "1",IsOnlyIgnoreInsert =  true)] //IsOnlyIgnoreInsert插入时可以忽略这个字段
    public byte State { get; set; }
    
    //服务器名称名
    [SugarColumn(Length = 30)]
    public string ServerName { get; set; }
    
    //运行状态 1.爆满 2.拥挤 3.正常
    [SugarColumn(Length = 15)]
    public byte RunState { get; set; }
    
    //是否是新服
    public int IsNew { get; set; }
    [SugarColumn(Length = 30)]
    public string IpHost { get; set; }
    public int Port { get; set; }
    
    //创建时间和更新时间
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}