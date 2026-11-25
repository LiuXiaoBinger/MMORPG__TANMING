/// <summary>本服玩家市场的客户端与内网协议号。</summary>
public static class StallCode
{
    /// <summary>客户端向网关查询本服市场。</summary>
    public const ushort Browse = 11400;
    /// <summary>浏览回执，由网关按真实玩家会话转发。</summary>
    public const ushort Result = 11401;
    /// <summary>网关提交带已登录角色路由的查询。</summary>
    public const ushort Read = 11402;
    /// <summary>GS 登记本机和当前在线角色。</summary>
    public const ushort Bind = 11403;
    /// <summary>GS 离场或下线时解除本机的角色绑定。</summary>
    public const ushort Unbind = 11404;
    /// <summary>交易服按区分页从中心服装载已提交挂单。</summary>
    public const ushort Load = 11405;
    /// <summary>卖家撤单请求，网关附上当前角色身份后转给交易服。</summary>
    public const ushort StallSellItemCancel = 11406;
    /// <summary>交易服请求中心服撤下托管挂单，中心服用同一协议回执。</summary>
    public const ushort StallSellItemCancelCommit = 11407;
    /// <summary>卖家撤单操作的最终结果。</summary>
    public const ushort StallSellItemCancelResult = 11408;
    /// <summary>交易服务离线或正在恢复时显示给玩家的提示。</summary>
    public const string Unavailable = "本服交易市场暂不可用，请稍后再试。";
}
