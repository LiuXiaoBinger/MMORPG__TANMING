    
using System;
using SqlSugar;

/// <summary>
/// 角色背包容量表，按角色与背包类型各存一条已开启格子容量记录。
/// </summary>
[SugarTable("roleKnapsack",TableDescription = "角色背包容量表")]
public class RoleKnapsackTable
{
    /// <summary>记录主键，自增。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增 IsPrimaryKey:表示是否是主键，IsIdentity:表示是否自增长
    public int Id { get; set; }

    /// <summary>启用状态（遗留字段，当前业务代码未读取）。</summary>
    [SugarColumn(DefaultValue = "1", IsOnlyIgnoreInsert = true)]
    public byte State { get; set; }

    /// <summary>所属角色 ID，对应 RoleTable.Id。</summary>
    public int RoleId { get; set; }

    //背包数据
    //[SugarColumn(Length = 1000)]
    //public string Knapsack { get; set; }
    /// <summary>遗留“当前物品 id”字段，当前业务代码未读取。</summary>
    public int curitemid { get; set; }

    // 背包类型：0 为普通背包，1/2/3 分别为装备、消耗品、材料，四类互相独立。
    // 旧数据未填该字段时默认值 0，继续按普通背包记录兼容读取。
    public byte roleKnapsack { get; set; } = 0;
    // 新角色默认 9x9 共 81 格，Count 的语义是已开启容量而不是物品数量。
    public byte roleKnapsackcount { get; set; } = 81;

    /// <summary>记录创建时间。</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>记录最近更新时间。</summary>
    public DateTime UpdateDate { get; set; }
}
