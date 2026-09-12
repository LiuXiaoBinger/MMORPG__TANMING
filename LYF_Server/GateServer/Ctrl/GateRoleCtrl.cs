

public class GateRoleCtrl:IContainer
{
    public void OnInit()
    {
        
    }
    /// <summary>
    /// 网关服务器作为服务器，接收unnity客户端
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
            
            case NetDefine.CMD_EnterWroldCode:
                OnEnterWroldHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_OpenKnapsackGridCode:
                OnOpenKnapsackGridHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_BuyShopItemCode:
                OnBuyShopItemHandle(serverBase, basePackage);
                break;
            
            default:
                break;
        }
    }

    private void OnEnterWroldHandle(ServerBase serverBase, BasePackage basePackage)
    {
        EnterWroldReq req = EnterWroldReq.Parser.ParseFrom(basePackage.Data);
        //todo验证信息的合法性
        
        //发送给游戏逻辑服务器
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnEnterWroldHandle::" + req.ToString());
    }

    /// <summary>
    /// 网关不执行业务，只保留 Unity 会话信息并将请求转发给 GameServer。
    /// </summary>
    private void OnOpenKnapsackGridHandle(ServerBase serverBase, BasePackage basePackage)
    {
        if (serverBase == null || serverBase._client == null)
        {
            return;
        }

        serverBase._client.SendData(basePackage);
    }

    /// <summary>
    /// 网关只透传商城购买请求，保留 UnitySessionId 和 GateSessionId，禁止在此扣费或发货。
    /// </summary>
    private static void OnBuyShopItemHandle(ServerBase serverBase, BasePackage basePackage)
    {
        if (serverBase == null || serverBase._client == null || basePackage == null)
        {
            return;
        }

        serverBase._client.SendData(basePackage);
    }

    /// <summary>
    /// 网关服务器作为客户端，接收游戏逻辑服务器数据
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session seesion = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        GateClientResponseForwarder.ForwardToUnity(seesion, basePackage);
    }
}
