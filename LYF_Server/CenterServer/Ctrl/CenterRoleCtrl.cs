


using Google.Protobuf;

public class CenterRoleCtrl:IContainer
{
    
    // 登录与角色业务的数据访问层。
    private CentRoleModel _centRoleModel = null;

    /// <summary>
    /// 创建登录控制器。
    /// </summary>
    /// <param name="loginModle">登录和角色相关业务模块。</param>
    public CenterRoleCtrl(CentRoleModel  centRoleModel)
    {
        _centRoleModel = centRoleModel;
    }
    public void OnInit()
    {
        
    }
    /// <summary>
    /// 中心服务器作为服务器，接收game客户端
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
        LogMsg.Info("OnEnterWroldHandle::" + req.ToString());
        
        //返回角色的技能信息
        RoleSkillInfoRet ret= _centRoleModel.RoleSkillInfo(req);
        LogMsg.Info("OnEnterWroldHandle=>RoleSkillInfoRet::" + ret.ToString());
        serverBase.SendData(basePackage,NetDefine.CMD_RoleSkillInfoCode,ret.ToByteString());
    
        //返回角色背包数据
        RoleKanpsackInfoRet  kanpsackInfoRet =_centRoleModel.RoleKanpaskInfo(req);
        LogMsg.Info("OnEnterWroldHandle=>RoleKanpsackInfoRet::" + kanpsackInfoRet.ToString());
        serverBase.SendData(basePackage,NetDefine.CMD_RoleKnapsackInfoCode,kanpsackInfoRet.ToByteString());
    }

   
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
       
    }
}