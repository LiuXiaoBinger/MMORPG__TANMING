using System;
using SqlSugar;

/// <summary>角色通用次数记录，以角色、业务行为和具体商品或对象共同定位一条存档。</summary>
[SugarTable("role_count")]
public class RoleCountTable
{
    /// <summary>将旧的角色、行为二列主键扩为包含具体商品的三列主键，保留已有行。</summary>
    /// <param name="db">建库后、CodeFirst 核验表结构前使用的中心服 MySQL 客户端。</param>
    public static void MigratePrimaryKey(SqlSugarClient db)
    {
        // 新库为空，已迁移库返回三列；只有原有二列结构需要一次原位 ALTER。
        string columns = db.Ado.GetString("SELECT GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) " +
            "FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='role_count' AND INDEX_NAME='PRIMARY'");
        if (string.Equals(columns, "RoleId,Action", StringComparison.OrdinalIgnoreCase))
            db.Ado.ExecuteCommand("ALTER TABLE role_count DROP PRIMARY KEY, ADD PRIMARY KEY (RoleId,Action,CountKey)");
    }

    /// <summary>所属角色 ID，复合主键之一。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleId { get; set; }
    /// <summary>次数大类（商城/副本等），复合主键之一。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Action { get; set; }

    /// <summary>遗留小写 key 字段，当前业务代码未读取，实际使用 CountKey。</summary>
    public int key { get; set; }
    /// <summary>次数细分类目键；不同商品或对象在同一角色、同一行为下分别保存。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
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
