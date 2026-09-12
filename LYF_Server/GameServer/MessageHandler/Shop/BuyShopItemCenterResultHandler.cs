/// <summary>
/// 处理 CenterServer 返回的商城购买结果。
/// 结果由现有会话链路原样转发到对应 GateServer，不在 UI 或 GameServer 伪造业务状态。
/// </summary>
public sealed class BuyShopItemCenterResultHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer &&
            commandCode == NetDefine.CMD_BuyShopItemCode;
    }

    public void Handle(GameMessageHandlerContext context)
    {
        // 商城结果已在统一入口转发，保留 Handler 作为该协议的领域注册点。
    }
}
