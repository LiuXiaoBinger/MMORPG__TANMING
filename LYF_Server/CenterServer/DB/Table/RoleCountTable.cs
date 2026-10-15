using System;
using SqlSugar;

/// <summary>角色通用次数持久化表，商城和副本共用 Action、CountKey 复合键。</summary>
[SugarTable("role_count")]
public class RoleCountTable
{
    /// <summary>所属角色 ID，复合主键之一。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleId { get; set; }
    /// <summary>次数大类（商城/副本等），复合主键之一。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Action { get; set; }

    /// <summary>遗留小写 key 字段，当前业务代码未读取，实际使用 CountKey。</summary>
    public int key { get; set; }
    /// <summary>次数细分类目键，与 Action 共同定位一条次数记录。</summary>
    public int CountKey { get; set; }
    /// <summary>当前累计次数。</summary>
    public long Count { get; set; }
    /// <summary>次数上次刷新时间（本地时间）。</summary>
    public DateTime LastRefreshTime { get; set; }
    /// <summary>写入该次快照时携带的角色物品版本。</summary>
    public long Version { get; set; }
    /// <summary>记录创建时间。</summary>
    public DateTime CreateDate { get; set; }
    /// <summary>记录最近更新时间。</summary>
    public DateTime UpdateDate { get; set; }
}
