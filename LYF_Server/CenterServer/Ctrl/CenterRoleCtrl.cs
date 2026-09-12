


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
            case NetDefine.CMD_SyncKnapsackGridCountCode:
                OnSyncKnapsackGridCountHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_BuyShopItemCode:
                OnBuyShopItemHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SaveRoleDataCode:
                OnSaveRoleDataHandle(serverBase, basePackage);
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

    /// <summary>
    /// 接收 GameServer 主循环确认后的格子数并写入数据库。
    /// </summary>
    private void OnSyncKnapsackGridCountHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SyncKnapsackGridCountReq req = SyncKnapsackGridCountReq.Parser.ParseFrom(basePackage.Data);
        OpenKnapsackGridRet ret = _centRoleModel.SyncKnapsackGridCount(req);
        serverBase.SendData(basePackage, NetDefine.CMD_SyncKnapsackGridCountCode, ret.ToByteString());
    }

    /// <summary>
    /// 接收 GameServer 转发的商城请求。
    /// 当前中心服没有完成货币扣除和物品发放，所以只返回功能未就绪，不写入购买记录。
    /// </summary>
    private void OnBuyShopItemHandle(ServerBase serverBase, BasePackage basePackage)
    {
        BuyShopItemReq request;
        try
        {
            request = BuyShopItemReq.Parser.ParseFrom(basePackage.Data);
        }
        catch (Google.Protobuf.InvalidProtocolBufferException ex)
        {
            LogMsg.Info("中心服商城购买请求解析失败: " + ex.Message, LogMsgType.Error);
            BuyShopItemRes invalidResponse = new BuyShopItemRes
            {
                Error = new ErrorInfo
                {
                    Code = CmdCode.InvalidQuantity,
                    Message = "购买请求格式无效"
                }
            };
            serverBase.SendData(basePackage, NetDefine.CMD_BuyShopItemCode,
                invalidResponse.ToByteString());
            return;
        }

        BuyShopItemRes response = _centRoleModel.ProcessShopPurchase(request);
        serverBase.SendData(basePackage, NetDefine.CMD_BuyShopItemCode, response.ToByteString());
    }

    /// <summary>解析 GameServer 角色保存请求并投递数据库任务，网络线程不直接写库。</summary>
    private static void OnSaveRoleDataHandle(ServerBase serverBase, BasePackage basePackage)
    {
        try
        {
            SaveRoleDataReq request = SaveRoleDataReq.Parser.ParseFrom(basePackage.Data);
            DBMgr.Instance.PushTask(new CRoleWriteTask(request, serverBase, basePackage));
        }
        catch (Google.Protobuf.InvalidProtocolBufferException ex)
        {
            LogMsg.Info("角色保存请求解析失败: " + ex.Message, LogMsgType.Error);
            SaveRoleDataRet result = new SaveRoleDataRet
            {
                CmdCode = CmdCode.ReqParamError,
                Message = "角色保存请求格式无效"
            };
            serverBase.SendData(basePackage, NetDefine.CMD_SaveRoleDataCode, result.ToByteString());
        }
    }

   
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
       
    }
}
