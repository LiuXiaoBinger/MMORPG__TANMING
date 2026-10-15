


using Google.Protobuf;

/// <summary>
/// 中心服角色持久化协议控制器。接收 GameServer 转发的进入世界、背包容量同步、
/// 商城购买与角色物品快照保存等请求，通过 <see cref="CentRoleModel"/> 读写 MySQL。
/// </summary>
public class CenterRoleCtrl:IContainer
{

    // 登录与角色业务的数据访问层。
    private CentRoleModel _centRoleModel = null;

    /// <summary>
    /// 创建角色持久化控制器。
    /// </summary>
    /// <param name="centRoleModel">中心服角色与背包数据访问模块。</param>
    public CenterRoleCtrl(CentRoleModel  centRoleModel)
    {
        _centRoleModel = centRoleModel;
    }

    /// <summary>IContainer 初始化回调，本控制器无预加载资源，留空实现。</summary>
    public void OnInit()
    {

    }
    /// <summary>
    /// 中心服务器作为服务器，接收 GameServer 转发的角色协议包并按协议号分发。
    /// </summary>
    /// <param name="serverBase">当前网络服务器对象。</param>
    /// <param name="basePackage">包含协议号与 protobuf 数据的网络包。</param>
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

    /// <summary>
    /// IContainer 客户端命令回调。角色持久化协议均由服务器间命令（OnServerCommand）处理，
    /// 中心服不直接接收 Unity 客户端命令，故此回调留空实现。
    /// </summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {

    }
}
