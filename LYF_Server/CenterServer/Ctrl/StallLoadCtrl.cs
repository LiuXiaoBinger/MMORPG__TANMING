using System;
using Google.Protobuf;
using SqlSugar;

/// <summary>按区、游标读取交易目录；监听协议需要网络框架的 IContainer。</summary>
internal sealed class StallLoadCtrl : IContainer
{
    /// <summary>复用中心服配置，为各交易连接的读取单独建立 SqlSugar 会话。</summary>
    private readonly ConnectionConfig config;
    /// <summary>每次返回的挂单上限，保证远低于 TCP 帧长。</summary>
    private const int PageSize = 128;
    /// <summary>保存中心服实际使用的数据库连接参数。</summary>
    /// <param name="config">当前共享存档库配置。</param>
    internal StallLoadCtrl(ConnectionConfig config) { this.config = config; }
    /// <summary>建表已由中心服启动时完成。</summary>
    public void OnInit() { }
    /// <summary>市场加载仅走监听端。</summary><param name="link">框架连接。</param><param name="packet">框架包。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet) { }
    /// <summary>回送一页带寄存余量的市场记录，成交资产和托管物品正文不下发。</summary>
    /// <param name="link">发起装载的内网交易服。</param><param name="packet">区号和上一页末挂牌号。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 每条查询均重新构造结果，忽略调用方传入的显示字段。
        StallLoadPage request = StallLoadPage.Parser.ParseFrom(packet.Data);
        // 稳定按挂牌号读取，防止不同区服之间串货。
        StallLoadPage page = new StallLoadPage { RealmId = request.RealmId };
        using (SqlSugarClient db = new SqlSugarClient(config))
        {
            // 只取浏览所需的列；额外一行用来判断是否还有下一批。
            var rows = db.Queryable<TradeListingTable>()
                .Where(row => row.ServerId == request.RealmId && row.MarketType == 0)
                .Where(row => row.HeldQty > 0 && row.ListingId > request.AfterId)
                .OrderBy(row => row.ListingId).Take(PageSize + 1)
                .Select(row => new { row.ListingId, row.ItemTypeId, row.CurrencyItemId, row.UnitPrice,
                    row.HeldQty, row.ExpiresAt, row.Status, row.SellerRoleId }).ToList();
            page.More = rows.Count > PageSize;
            for (int index = 0; index < Math.Min(rows.Count, PageSize); index++)
            {
                // 库中时间以 UTC 保存，读取时显式指定以免使用机器时区。
                var row = rows[index];
                page.Offers.Add(new StallOffer { ListingId = row.ListingId, ItemId = row.ItemTypeId,
                    CurrencyId = row.CurrencyItemId, Price = row.UnitPrice, Quantity = row.HeldQty,
                    ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds(),
                    Status = row.Status, SellerId = row.SellerRoleId });
                page.AfterId = row.ListingId;
            }
        }
        link.Send(StallCode.Load, page.ToByteString());
    }
}
