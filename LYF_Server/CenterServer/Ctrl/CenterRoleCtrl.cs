

using Google.Protobuf;

/// <summary>
/// 中心服角色持久化协议控制器。接收 GameServer 转发的进入世界、背包容量同步、
/// 商城购买与角色物品快照保存等请求，通过 <see cref="CentRoleModel"/> 读写 MySQL。
/// </summary>
public class CenterRoleCtrl:IContainer
{

    // 登录与角色业务的数据访问层。
    private CentRoleModel _centRoleModel = null;

    #region 构造与初始化
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
    #endregion

    /// <summary>
    /// 为进入世界的角色分别读取技能与背包快照，并用两种协议返回。
    /// </summary>
    /// <param name="serverBase">接收游戏服请求的中心服连接，用于返回加载结果。</param>
    /// <param name="basePackage">包含角色编号和原两层会话编号的进入世界请求。</param>
    private void OnEnterWroldHandle(BaseLink serverBase, BasePackage basePackage)
    {
        EnterWroldReq req = EnterWroldReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnEnterWroldHandle::" + req.ToString());
        
        //返回角色的技能信息
        RoleSkillInfoRet ret= _centRoleModel.RoleSkillInfo(req);
        LogMsg.Info("OnEnterWroldHandle=>RoleSkillInfoRet::" + ret.ToString());
        serverBase.Send(basePackage,NetDefine.CMD_RoleSkillInfoCode,ret.ToByteString());
    
        //返回角色背包数据
        RoleKanpsackInfoRet  kanpsackInfoRet =_centRoleModel.RoleKanpaskInfo(req);
        LogMsg.Info("OnEnterWroldHandle=>RoleKanpsackInfoRet::" + kanpsackInfoRet.ToString());
        serverBase.Send(basePackage,NetDefine.CMD_RoleKnapsackInfoCode,kanpsackInfoRet.ToByteString());

    }

    #region 事件回调
    /// <summary>
    /// 中心服务器作为服务器，接收 GameServer 转发的角色协议包并按协议号分发。
    /// </summary>
    /// <param name="serverBase">当前网络服务器对象。</param>
    /// <param name="basePackage">包含协议号与 protobuf 数据的网络包。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
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
    /// 接收 GameServer 主循环确认后的格子数并写入数据库。
    /// </summary>
    private void OnSyncKnapsackGridCountHandle(BaseLink serverBase, BasePackage basePackage)
    {
        SyncKnapsackGridCountReq req = SyncKnapsackGridCountReq.Parser.ParseFrom(basePackage.Data);
        OpenKnapsackGridRet ret = _centRoleModel.SyncKnapsackGridCount(req);
        serverBase.Send(basePackage, NetDefine.CMD_SyncKnapsackGridCountCode, ret.ToByteString());
    }

    /// <summary>
    /// 接收 GameServer 转发的商城请求。
    /// 当前中心服没有完成货币扣除和物品发放，所以只返回功能未就绪，不写入购买记录。
    /// </summary>
    private void OnBuyShopItemHandle(BaseLink serverBase, BasePackage basePackage)
    {
        // 游戏服转发的商城购买参数，交给中心服处理购买业务。
        BuyShopItemReq request = BuyShopItemReq.Parser.ParseFrom(basePackage.Data);

        BuyShopItemRes response = _centRoleModel.ProcessShopPurchase(request);
        serverBase.Send(basePackage, NetDefine.CMD_BuyShopItemCode, response.ToByteString());
    }

    /// <summary>解析 GameServer 角色保存请求并投递数据库任务，网络线程不直接写库。</summary>
    private static void OnSaveRoleDataHandle(BaseLink serverBase, BasePackage basePackage)
    {
        // 游戏服提交的角色保存数据，入队后由 CenterApp 主循环调用 DBMgr.Update 顺序写入。
        SaveRoleDataReq request = SaveRoleDataReq.Parser.ParseFrom(basePackage.Data);
        DBMgr.Instance.PushTask(new CRoleWriteTask(request, serverBase, basePackage));
    }

    /// <summary>
    /// IContainer 客户端命令回调。角色持久化协议均由服务器间命令（OnServerCommand）处理，
    /// 中心服不直接接收 Unity 客户端命令，故此回调留空实现。
    /// </summary>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage)
    {

    }
    #endregion
}
