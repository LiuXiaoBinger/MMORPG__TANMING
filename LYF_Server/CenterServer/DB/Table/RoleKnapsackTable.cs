    
using System;
using SqlSugar;

[SugarTable("roleKnapsack",TableDescription = "服务器列表")]
public class RoleKnapsackTable
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增 IsPrimaryKey:表示是否是主键，IsIdentity:表示是否自增长
    public int Id { get; set; }

    //状态
    [SugarColumn(DefaultValue = "1", IsOnlyIgnoreInsert = true)]
    public byte State { get; set; }

    //角色id int
    public int RoleId { get; set; }
    
    //背包数据
    //[SugarColumn(Length = 1000)]
    //public string Knapsack { get; set; }
    //item当前id
    public int curitemid { get; set; }
    
    //现在背包数量 Knapsacktype,count 背包类型，以及背包数量

    public byte roleKnapsack { get; set; } = 0;
    public byte roleKnapsackcount { get; set; } = 100;
    
    //创建时间
    public DateTime CreateDate { get; set; }

    //更新时间
    public DateTime UpdateDate { get; set; }
}