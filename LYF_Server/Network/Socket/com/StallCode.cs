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
    /// <summary>玩家提交摆摊业务操作。</summary>
    public const ushort Deal = 11409;
    /// <summary>中心库准备挂牌和操作回执。</summary>
    public const ushort Prepare = 11410;
    /// <summary>交易服要求角色当前宿主执行已准备的资产操作。</summary>
    public const ushort Apply = 11411;
    /// <summary>宿主因余额或背包条件不满足而解除占用。</summary>
    public const ushort Abort = 11412;
    /// <summary>中心库公布已提交的挂牌版本与玩家回执。</summary>
    public const ushort Settled = 11413;
    /// <summary>角色登记后查回之前未完成的业务单。</summary>
    public const ushort Resume = 11414;
    /// <summary>本服材料交易使用银两的虚拟物品编号。</summary>
    public const int Silver = 102;
    /// <summary>交易服务离线或正在恢复时显示给玩家的提示。</summary>
    public const string Unavailable = "本服交易市场暂不可用，请稍后再试。";

    /// <summary>识别退出玩家交易的装备养成材料。旧挂单仍允许卖家撤单、取回和提款。</summary>
    /// <param name="itemId">物品表编号，8101 至 8105 分别为一至五级强化石。</param>
    public static bool IsWorkshopOnly(int itemId)
    {
        if (itemId >= 8101 && itemId <= 8105) return true;
        return EquipmentGemRules.TryRead(itemId, out _, out _);
    }

    /// <summary>返回物品在指定玩家市场受限的原因，空串表示可按该市场原规则交易。</summary>
    /// <param name="itemId">玩家选中的物品编号，特技铁牌沿用 7600 加特技族编号。</param>
    /// <param name="localExchange">仅本服集市为真，其他玩家交易入口为假。</param>
    public static string TradeRestriction(int itemId, bool localExchange)
    {
        if (IsWorkshopOnly(itemId)) return "宝石和强化石不能在交易所交易。";
        // 7601～7699 是基础特技物品的编号段，段内保留空号。
        if (itemId < 7601 || itemId > 7699) return "";
        // 以实际物品的特技族及品质判定，普通材料沿用原来的交易范围。
        cfg.ItemInfo item = LubanMgr.Instance.GetItemInfoById(itemId);
        if (item == null || item.ItemSubType != itemId - 7600) return "";
        if (item.Quality == 1 || item.Quality == 2) return "白色和绿色特技不能交易。";
        if (item.Quality == 5 && !localExchange) return "金色特技只能在集市交易。";
        if (localExchange && item.Quality != 5) return "集市仅陈列金色特技。";
        return "";
    }
}
