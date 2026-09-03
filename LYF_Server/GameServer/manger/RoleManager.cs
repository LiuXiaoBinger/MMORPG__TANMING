using System.Collections.Generic;

/// <summary>
/// 在线角色管理器。
/// </summary>
public class RoleManager : Singleton<RoleManager>
{
    // Key: RoleID, Value: 在线角色数据。
    private readonly Dictionary<int, OnlineRole> m_onlineRoleMap = new Dictionary<int, OnlineRole>();

    /// <summary>
    /// 添加在线角色。角色已存在时覆盖原有数据。
    /// </summary>
    public void AddOnlineRole(int roleID, OnlineRole onlineRole)
    {
        if (roleID <= 0 || onlineRole == null)
        {
            return;
        }

        m_onlineRoleMap[roleID] = onlineRole;
    }

    /// <summary>
    /// 根据角色 ID 获取在线角色。
    /// </summary>
    public OnlineRole GetOnlineRole(int roleID)
    {
        OnlineRole onlineRole;
        return m_onlineRoleMap.TryGetValue(roleID, out onlineRole) ? onlineRole : null;
    }

    /// <summary>
    /// 移除在线角色。
    /// </summary>
    public bool RemoveOnlineRole(int roleID)
    {
        return m_onlineRoleMap.Remove(roleID);
    }

    /// <summary>
    /// 获取全部在线角色数据。
    /// </summary>
    public Dictionary<int, OnlineRole> GetOnlineRoleMap()
    {
        return m_onlineRoleMap;
    }
}
