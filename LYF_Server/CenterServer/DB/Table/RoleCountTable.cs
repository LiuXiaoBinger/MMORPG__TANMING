using System;
using SqlSugar;

/// <summary>角色通用次数持久化表，商城和副本共用 Action、CountKey 复合键。</summary>
[SugarTable("role_count")]
public class RoleCountTable
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleId { get; set; }
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Action { get; set; }
  
    public int key { get; set; }
    public int CountKey { get; set; }
    public long Count { get; set; }
    public DateTime LastRefreshTime { get; set; }
    public long Version { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
