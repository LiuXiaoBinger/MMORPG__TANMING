using System;
using SqlSugar;

/// <summary>
/// 角色物品表，保存背包中的物品实例。
/// </summary>
[SugarTable("item", TableDescription = "角色物品表")]
internal class ItemTable
{
    /// <summary>
    /// 物品实例编号，与 RoleID 组成数据库复合主键；业务使用 UIDHelper 分配编号来区分具体物品。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public long ItemUID { get; set; }

    /// <summary>
    /// 所属角色ID，与 ItemUID 组成复合主键，对应 RoleTable.Id。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleID { get; set; }

    /// <summary>
    /// 物品配置ID，对应物品配置表或配置文件中的ID。
    /// </summary>
    public int ItemTypeID { get; set; }

    /// <summary>
    /// 物品所在的 KnapsackType 分类，包含普通、装备、消耗品、材料、穿戴和虚拟物品栏。
    /// </summary>
    public int BagType { get; set; }

    /// <summary>
    /// 物品在所属背包中的零起始格子索引。
    /// </summary>
    public int BagIndex { get; set; }

    /// <summary>
    /// 物品数量。
    /// </summary>
    public int count { get; set; }

    /// <summary>物品绑定等运行时状态位。</summary>
    public int ItemSign { get; set; }

    /// <summary>买入价格使用的货币类型。</summary>
    public int MoneyType { get; set; }

    /// <summary>当前堆叠物品的总买入价格。</summary>
    public long TotalPrice { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>物品过期时间，空值表示永久有效。</summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? ExpireDate { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTime UpdateDate { get; set; }
}
