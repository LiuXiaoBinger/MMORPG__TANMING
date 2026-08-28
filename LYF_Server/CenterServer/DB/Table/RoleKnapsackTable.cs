    
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
    [SugarColumn(Length = 1000)]
    public string Knapsack { get; set; }
    
    //创建时间
    public DateTime CreateDate { get; set; }

    //更新时间
    public DateTime UpdateDate { get; set; }
}