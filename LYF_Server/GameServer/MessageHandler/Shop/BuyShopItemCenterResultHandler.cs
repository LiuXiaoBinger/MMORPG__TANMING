/// <summary>
/// 处理 CenterServer 返回的商城购买结果。
/// 结果由现有会话链路原样转发到对应 GateServer，不在 UI 或 GameServer 伪造业务状态。
/// </summary>
public sealed class BuyShopItemCenterResultHandler : IGameMessageHandler
{
    /// <summary>仅登记 CenterServer 来源的商城购买结果协议。</summary>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer &&
            commandCode == NetDefine.CMD_BuyShopItemCode;
    }

    /// <summary>不再重复转发；统一消息入口已把结果交给对应网关会话。</summary>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 商城结果已在统一入口转发，保留 Handler 作为该协议的领域注册点。
    }
}
