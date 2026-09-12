
public class Game_LoginCtrl
{
    /// <summary>
    /// 开始游戏请求
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public static void HandleClientStartGame(ServerBase serverBase, BasePackage basePackage)
    {
        // 开始游戏请求由逻辑线程转发给 CenterServer，角色初始化等待中心服结果。
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        //发送给中心服务器
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="serverBase"></param>
    /// <param name="basePackage"></param>
    public static void HandleClientCreateRole(ServerBase serverBase, BasePackage basePackage)
    {
        // 创建角色数据由 CenterServer 持久化，GameServer 只负责转发请求。
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        //发送给中心服务器
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }
    public static void HandleClientLoginGameServer(ServerBase serverBase, BasePackage basePackage)
    {
        // 登录 GameServer 请求沿原协议转发，结果仍通过同一网关会话返回客户端。
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        //basePackage.UnitySessionId = (serverBase as Session).SessionID;
        serverBase._client.SendData(basePackage);
        LogMsg.Info("OnLoginGameServerHandle::" + req.ToString());
    }

    public static void HandleCenterStartGame(BasePackage basePackage)
    {
        // 开始游戏成功后创建在线角色；背包快照由后续 RoleKanpsackInfo 回包装载。
        StartGameRet ret = StartGameRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("OnStartGameResultHandle::" + ret.ToString());
        if (ret != null && ret.CmdCode == CmdCode.Succeed && ret.MainRoleInfo != null &&
            ret.MainRoleInfo.BaseInfo != null)
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
