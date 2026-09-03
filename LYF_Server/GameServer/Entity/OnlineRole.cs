

/// <summary>
/// 存储服务端的在线玩家
/// </summary>
public class OnlineRole
{
    #region 成员变量
    //Unity端用户 与网关服务器连接的sessinID
    public int UnitySessionId;
    public int GateSessionId;
    //当前玩家信息
    public MainRoleInfo mainRoleInfo;
    
    //基础属性 以后战斗是以基础属性进行累加计算伤害
    private RoleBaseAttrEntity _roleBaseAttrEntity;
    //这里存储玩家背包数据
    

    #endregion
    
    
    
    
}