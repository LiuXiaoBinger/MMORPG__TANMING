using System;
using SqlSugar;

/// <summary>
/// 角色 NPC 商店购买次数表。
/// 一条记录表示角色在指定 NPC 商店购买指定商品的次数。
/// </summary>
[SugarTable("roleNpcShopPurchase", TableDescription = "角色NPC商店购买次数表")]
public class RoleNpcShopPurchaseTable
{
    /// <summary>
    /// 角色 ID，与 NpcID、ShopID 组成复合主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int RoleID { get; set; }

    /// <summary>
    /// NPC 配置 ID，与 RoleID、ShopID 组成复合主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int NpcID { get; set; }

    /// <summary>
    /// 商店商品配置 ID，与 RoleID、NpcID 组成复合主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int ShopID { get; set; }

    /// <summary>
    /// 当前每日限购计数所属日期；日期变化时将 DailyPurchaseCount 重置为零。
    /// </summary>
    public DateTime DailyPurchaseDate { get; set; }

    /// <summary>
    /// 当前每日已购买次数。
    /// </summary>
    public int DailyPurchaseCount { get; set; }

    /// <summary>
    /// 永久累计购买次数。
    /// </summary>
    public int TotalPurchaseCount { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// 最后更新时间。
    /// </summary>
    public DateTime UpdateDate { get; set; }
}
