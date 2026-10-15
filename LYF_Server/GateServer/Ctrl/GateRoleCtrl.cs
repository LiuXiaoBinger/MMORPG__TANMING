

/// <summary>
/// 网关角色控制器。把 Unity 的进入世界、开背包格子、商城购买等请求透传给 GameServer，
/// 并把 GameServer 的角色、背包、商城、次数同步等回包按 Unity 会话回传；网关不执行业务结算。
/// </summary>
public class GateRoleCtrl:IContainer
{
    /// <summary>IContainer 初始化回调，网关角色控制器无预加载资源，留空实现。</summary>
    public void OnInit()
    {

    }
    /// <summary>
    /// 网关服务器作为服务器，接收 Unity 客户端的角色类请求并按协议号分发转发。
    /// </summary>
    /// <param name="serverBase">收到 Unity 请求的网关传输对象。</param>
    /// <param name="basePackage">携带原会话标识的请求协议包。</param>
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
