    
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
    
    // 背包类型：0 为普通背包，1/2/3 分别为装备、消耗品、材料，四类互相独立。
    // 旧数据未填该字段时默认值 0，继续按普通背包记录兼容读取。
    public byte roleKnapsack { get; set; } = 0;
    // 新角色默认 9x9 共 81 格，Count 的语义是已开启容量而不是物品数量。
    public byte roleKnapsackcount { get; set; } = 81;
    
    //创建时间
    public DateTime CreateDate { get; set; }

    //更新时间
    public DateTime UpdateDate { get; set; }
}
