using Google.Protobuf;

/// <summary>只注册玩家市场查询，旧式单 GS 网关也能明确提示未开放；实现网络框架 IContainer。</summary>
internal sealed class GateStallCtrl : IContainer
{
    /// <summary>区服模式的已认证玩家路由，旧启动模式为空。</summary>
    private readonly RealmGameLink router;
    /// <summary>接入当前区服路由。</summary><param name="router">已建立的分区路由。</param>
    internal GateStallCtrl(RealmGameLink router) { this.router = router; }
    /// <summary>登记无需初始化状态。</summary>
    public void OnInit() { }
    /// <summary>结果由独立交易连接转发。</summary><param name="link">框架连接。</param><param name="packet">框架消息。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet) { }
    /// <summary>通过实际网关会话查询本区市场。</summary><param name="link">客户端连接。</param><param name="packet">查询条件。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        if (router != null) { router.Send(packet); return; }
        if (packet.ProtoCode == StallCode.StallSellItemCancel)
        {
            // 单 GS 模式同样回送原挂牌号，以便出售页展示停服原因。
            StallSellItemCancelArg cancel = StallSellItemCancelArg.Parser.ParseFrom(packet.Data);
            link.Send(StallCode.StallSellItemCancelResult, new StallSellItemCancelRes { ListingId = cancel.ListingId, Reason = StallCode.Unavailable }.ToByteString());
            return;
        }
        link.Send(StallCode.Result, new StallResult { Query = StallQuery.Parser.ParseFrom(packet.Data), Reason = StallCode.Unavailable }.ToByteString());
    }
}
