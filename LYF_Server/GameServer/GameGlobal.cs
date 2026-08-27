


using System.Collections.Generic;

public class GameGlobal:Singleton<GameGlobal>
{
    //用于存储在线玩家的数据结构
    private Dictionary<int, OnlineRole> _onlineRoles;
    public void Init()
    {
        _onlineRoles = new Dictionary<int, OnlineRole>();   
    }

    public void AddOlineRole(int roleid, OnlineRole role)
    {

        _onlineRoles[roleid] = role;
    }
    public void RemoveOlineRole(int roleid)
    {
        if (_onlineRoles.ContainsKey(roleid))
        {
            _onlineRoles.Remove(roleid);    
        }
    }

    public OnlineRole GetOlineRoleByRoleId(int roleid)
    {
        if (_onlineRoles != null && _onlineRoles.TryGetValue(roleid, out OnlineRole role))
        {
            return role;
        }

        return null;
    }

    public Dictionary<int, OnlineRole> GetAllOlineRole()
    {
        if (_onlineRoles == null)
        {
            _onlineRoles = new Dictionary<int, OnlineRole>();
        }
        return _onlineRoles;
    }
}
