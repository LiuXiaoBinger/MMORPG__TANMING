


using System.Collections.Generic;

public class GameGlobal:Singleton<GameGlobal>
{
    public void Init()
    {
    }

    public void AddOlineRole(int roleid, OnlineRole role)
    {
        RoleManager.Instance.AddOnlineRole(roleid, role);
    }

    public void RemoveOlineRole(int roleid)
    {
        RoleManager.Instance.RemoveOnlineRole(roleid);
    }

    public OnlineRole GetOlineRoleByRoleId(int roleid)
    {
        return RoleManager.Instance.GetOnlineRole(roleid);
    }

    public Dictionary<int, OnlineRole> GetAllOlineRole()
    {
        return RoleManager.Instance.GetOnlineRoleMap();
    }
}
