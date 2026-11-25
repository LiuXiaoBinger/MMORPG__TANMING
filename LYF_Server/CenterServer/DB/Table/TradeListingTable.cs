using System;
using SqlSugar;

/// <summary>中心服持有的一口价寄售记录，读取展示字段时保留托管快照在库中。</summary>
[SugarTable("trade_listing")]
public sealed class TradeListingTable
{
    /// <summary>挂牌主键，作为恢复目录时的稳定分页游标。</summary>
    [SugarColumn(IsPrimaryKey = true)]
    public long ListingId { get; set; }
    /// <summary>逻辑区服编号，同一市场覆盖本区全部地图线路。</summary>
    public int ServerId { get; set; }
    /// <summary>市场类型，零代表本服摆摊。</summary>
    public int MarketType { get; set; }
    /// <summary>物品寄存者的角色号。</summary>
    public int SellerRoleId { get; set; }
    /// <summary>对应物品配置编号。</summary>
    public int ItemTypeId { get; set; }
    /// <summary>挂牌计价货币配置号。</summary>
    public int CurrencyItemId { get; set; }
    /// <summary>以最小货币单位表示的单件价格。</summary>
    public long UnitPrice { get; set; }
    /// <summary>最初寄存的物品件数。</summary>
    public int InitialQty { get; set; }
    /// <summary>仍由市场托管的件数。</summary>
    public int HeldQty { get; set; }
    /// <summary>已返还卖家的件数。</summary>
    public int ReturnedQty { get; set; }
    /// <summary>挂单状态，1 在售、2 售罄、3 已撤、4 到期。</summary>
    public int Status { get; set; }
    /// <summary>持久化挂牌的变更版本。</summary>
    public long ListingVersion { get; set; }
    /// <summary>已成交尚未领取的税后收入；旧挂单缺省为零。</summary>
    [SugarColumn(IsNullable = true)] public long? Income { get; set; }
    /// <summary>持有本挂单操作权的交易单，空值允许下一个操作。</summary>
    [SugarColumn(IsNullable = true)] public long? PendingTicket { get; set; }
    /// <summary>累计税金尚不足一银两的万分位尾数，范围零至 9999。</summary>
    [SugarColumn(IsNullable = true)] public int? TaxRemainder { get; set; }
    /// <summary>成交手续费，以万分比保存。</summary>
    public int FeeBps { get; set; }
    /// <summary>挂单建立的 UTC 时间。</summary>
    public DateTime ListedAt { get; set; }
    /// <summary>挂单截止的 UTC 时间。</summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>关闭的 UTC 时间，在售时为空。</summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? ClosedAt { get; set; }
    /// <summary>托管物品或装备的实例号。</summary>
    public long EscrowItemUid { get; set; }
    /// <summary>托管实例的 Protobuf 快照，浏览不读取此列。</summary>
    [SugarColumn(ColumnDataType = "LONGBLOB", IsNullable = true)]
    public byte[] ItemPayload { get; set; }
    /// <summary>托管快照格式版本。</summary>
    public int PayloadVersion { get; set; }
    /// <summary>托管材料原成本的货币，历史记录可为空。</summary>
    [SugarColumn(IsNullable = true)] public int? CostCurrency { get; set; }
    /// <summary>仍由市场托管的原始购入成本，以货币最小单位计。</summary>
    public long HeldAcquisitionCost { get; set; }
}
