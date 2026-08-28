

/// <summary>
/// 存储服务端的在线玩家
/// </summary>
public class OnlineRole
{
    //Unity端用户 与网关服务器连接的sessinID
    public int UnitySessionId;
    
    public int GateSessionId;

    //当前玩家信息
    public MainRoleInfo mainRoleInfo;
    
    //这里存储玩家背包数据
}