

public class GameRoleCtrl:IContainer
{
    public void OnInit()
    {
        
    }
    /// <summary>
    /// game服务器作为服务器，接收gate客户端
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

    /// <summary>
    /// 角色请求进入游戏世界
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    private void OnEnterWroldHandle(ServerBase serverBase, BasePackage basePackage)
    {
        EnterWroldReq req = EnterWroldReq.Parser.ParseFrom(basePackage.Data);
        if (req == null)
        {
            return;
        }
        //todo验证信息的合法性
        
        //发送给中心服务器
        if (serverBase._client != null)
        {
            serverBase._client.SendData(basePackage);
        }
        LogMsg.Info("OnEnterWroldHandle::" + req.ToString());
        
        //同步其他角色数据 给玩家 
        OnlineRole role = GameGlobal.Instance.GetOlineRoleByRoleId(req.RoleId);
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return;
        }
        
        //1将自己同步给其他玩家
        GameWorldBCd.Instance.RoleEnterWroldBC(role.mainRoleInfo);
        //2.把其他在线玩家的数据，同步给当前玩家
        GameWorldBCd.Instance.OtherOnlineWroldBC(serverBase, basePackage, role.mainRoleInfo);
    }

    /// <summary>
    /// game服务器作为客户端，接收游戏中心数据
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        if (basePackage == null)
        {
            return;
        }

        Session seesion = SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_RoleSkillInfoCode:
                OnRoleSkillInfoResultHandle(seesion, basePackage);
                break;
            
        }
    }
    /// <summary>
    /// 角色技能信息返回数据
    /// </summary>
    /// <param name="seesion"></param>
    /// <param name="basePackage"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnRoleSkillInfoResultHandle(Session seesion, BasePackage basePackage)
    {
        if (seesion == null || basePackage == null || basePackage.Data == null)
        {
            return;
        }

        RoleSkillInfoRet ret = RoleSkillInfoRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnRoleSkillInfoResultHandle::" + ret.ToString());
        //把结果数据返回给gate
        seesion.SendData(basePackage);
       
    }
}
