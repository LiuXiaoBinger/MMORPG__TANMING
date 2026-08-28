

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
    /// 网关服务器作为客户端，接收游戏逻辑服务器数据
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session seesion = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_RoleSkillInfoCode:
                OnRoleSkillInfoResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_SyncRoleEnterWorldCode:
            case NetDefine.CMD_SyncotherOnlineCode:
                OnSyncOtherOnlineResultHandle(seesion, basePackage);
                break;
            case NetDefine.CMD_RoleKnapsackInfoCode:
                OnRoleKnapsackInfoResultHandle(seesion, basePackage);
                break;
        }
    }

    /// <summary>
    /// 角色背包信息
    /// </summary>
    /// <param name="seesion"></param>
    /// <param name="basePackage"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnRoleKnapsackInfoResultHandle(Session seesion, BasePackage basePackage)
    {
        RoleKanpsackInfoRet ret = RoleKanpsackInfoRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRoleKnapsackInfoResultHandle::" + ret.ToString());
        //把结果数据返回给gate
        seesion.SendData(basePackage);
    }
    /// <summary>
    /// 同步其他玩家
    /// </summary>
    /// <param name="seesion"></param>
    /// <param name="basePackage"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnSyncOtherOnlineResultHandle(Session seesion, BasePackage basePackage)
    {
        RoleBaseInfo ret = RoleBaseInfo.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRoleSkillInfoResultHandle::" + ret.ToString());
        
        //把结果数据返回给gate
        seesion.SendData(basePackage);
    }

    private void OnRoleSkillInfoResultHandle(Session seesion, BasePackage basePackage)
    {
        RoleSkillInfoRet ret = RoleSkillInfoRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRoleSkillInfoResultHandle::" + ret.ToString());
        if (ret.CmdCode != CmdCode.Succeed)
        {
            seesion.SendError(basePackage,ret.CmdCode);
            return;
        }
        //把结果数据返回给gate
        seesion.SendData(basePackage);
    }
}