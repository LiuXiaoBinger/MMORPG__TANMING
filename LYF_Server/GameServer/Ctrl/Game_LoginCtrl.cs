
/// <summary>
/// 登录、创角、开始游戏的转发控制器。
/// GameServer 作为中间层：客户端请求原样转发 CenterServer 持久化，中心服结果再由本类落地为在线角色；
/// 本类只在 GameServer 逻辑线程被对应协议处理器调用，不直接收发 Socket。
/// </summary>
public class Game_LoginCtrl
{
    /// <summary>
    /// 处理客户端开始游戏请求：解析后沿原链路转发 CenterServer，角色建立等待中心服回包。
    /// </summary>
    /// <param name="serverBase">接收该请求的 GameServer 传输对象，用于回连 CenterServer。</param>
    /// <param name="basePackage">客户端开始游戏协议包。</param>
    public static void HandleClientStartGame(BaseLink serverBase, BasePackage basePackage)
    {
        // 开始游戏请求由逻辑线程转发给 CenterServer，角色初始化等待中心服结果。
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        // 沿中心服连接原样转发协议包，不修改会话字段。
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 处理客户端创建角色请求：转发 CenterServer 完成持久化建角。
    /// </summary>
    /// <param name="serverBase">接收该请求的 GameServer 传输对象。</param>
    /// <param name="basePackage">客户端创建角色协议包。</param>
    public static void HandleClientCreateRole(BaseLink serverBase, BasePackage basePackage)
    {
        // 创建角色数据由 CenterServer 持久化，GameServer 只负责转发请求。
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        // 沿中心服连接原样转发协议包。
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 处理客户端登录 GameServer 请求：原样转发 CenterServer 做身份校验。
    /// </summary>
    /// <param name="serverBase">接收该请求的 GameServer 传输对象。</param>
    /// <param name="basePackage">客户端登录游戏服协议包。</param>
    public static void HandleClientLoginGameServer(BaseLink serverBase, BasePackage basePackage)
    {
        // 登录 GameServer 请求沿原协议转发，结果仍通过同一网关会话返回客户端。
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        // 历史实现曾在此覆写 UnitySessionId；当前由网关统一维护会话字段，此处不再改写。
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.Send(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 处理 CenterServer 返回的开始游戏结果：成功时创建在线角色并登记到 RoleManager。
    /// </summary>
    /// <param name="basePackage">中心服开始游戏回包，携带主角色快照与复合会话标识。</param>
    public static void HandleCenterStartGame(BasePackage basePackage)
    {
        // 开始游戏成功后创建在线角色；背包快照由后续 RoleKanpsackInfo 回包装载。
        StartGameRet ret = StartGameRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnStartGameResultHandle::" + ret.ToString());
        // 回包非空且命令码为开始游戏成功。
        bool isStartGameSuccess = ret != null && ret.CmdCode == CmdCode.Succeed;
        // 回包非空且主角色快照与其基础信息完整（ret 判空前置，避免空引用）。
        bool hasCompleteMainRole = ret != null && ret.MainRoleInfo != null && ret.MainRoleInfo.BaseInfo != null;
        if (isStartGameSuccess && hasCompleteMainRole)
        {
            OnlineRole onlineRole = new OnlineRole()
            {
                UnitySessionId = basePackage.UnitySessionId,
                GateSessionId = basePackage.GateSessionId,
                mainRoleInfo = ret.MainRoleInfo,
            };
            onlineRole.InitSerializes();
            // 背包容量和物品统一由 EnterWorld 返回的 RoleKanpsackInfo 装载，开始游戏回包只建立角色。
            RoleManager.Instance.AddOnlineRole(ret.MainRoleInfo.BaseInfo.RoleId, onlineRole);
        }
        else if (ret != null && ret.CmdCode == CmdCode.Succeed)
        {
            // 网关已先将异常回包转换为错误码通知客户端，本服仅拒绝创建不完整的角色状态。
            LogMsg.Info("开始游戏回包缺少主角色信息，拒绝创建在线角色", LogMsgType.Error);
        }
    }
}
