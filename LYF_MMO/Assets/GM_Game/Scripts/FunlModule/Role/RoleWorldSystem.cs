using System;
using System.Collections.Generic;

/// <summary>
/// 客户端角色世界容器，统一管理本地角色和其他玩家的数据模型。
/// </summary>
public sealed class RoleWorldSystem
{
    /// <summary>本地角色替换或清理时通知界面控制器重新绑定。</summary>
    public event Action<ClientRole> LocalRoleChanged;
    /// <summary>其他玩家按角色编号建立的索引。</summary>
    private readonly Dictionary<int, ClientRole> _otherRoles =
        new Dictionary<int, ClientRole>();

    /// <summary>其他玩家的稳定更新顺序，避免直接遍历字典。</summary>
    private readonly List<ClientRole> _otherRoleOrder = new List<ClientRole>();

    /// <summary>当前登录的本地角色。</summary>
    public ClientRole LocalRole { get; private set; }

    /// <summary>使用开始游戏数据创建唯一的本地角色。</summary>
    /// <param name="roleInfo">服务器返回的完整角色数据。</param>
    /// <returns>创建完成的本地角色。</returns>
    public ClientRole LoadLocalRole(MainRoleInfo roleInfo)
    {
        Clear();
        if (roleInfo == null || roleInfo.BaseInfo == null)
        {
            return null;
        }

        LocalRole = new ClientRole(roleInfo);
        LocalRole.AfterAddedToWorld();
        LocalRoleChanged?.Invoke(LocalRole);
        return LocalRole;
    }

    /// <summary>新增或更新一个其他玩家模型。</summary>
    /// <param name="baseInfo">其他玩家基础数据。</param>
    /// <returns>对应的客户端角色模型。</returns>
    public ClientRole AddOrUpdateOtherRole(RoleBaseInfo baseInfo)
    {
        if (baseInfo == null || baseInfo.RoleId <= 0)
        {
            return null;
        }
        if (LocalRole != null && LocalRole.GetID() == baseInfo.RoleId)
        {
            return LocalRole;
        }

        ClientRole role;
        if (_otherRoles.TryGetValue(baseInfo.RoleId, out role))
        {
            role.UpdateBaseInfo(baseInfo);
            return role;
        }

        role = new ClientRole(baseInfo);
        _otherRoles.Add(baseInfo.RoleId, role);
        _otherRoleOrder.Add(role);
        role.AfterAddedToWorld();
        return role;
    }

    /// <summary>按角色编号查询本地角色或其他玩家。</summary>
    public bool TryGetRole(int roleId, out ClientRole role)
    {
        if (LocalRole != null && LocalRole.GetID() == roleId)
        {
            role = LocalRole;
            return true;
        }
        return _otherRoles.TryGetValue(roleId, out role);
    }

    /// <summary>移除指定其他玩家。</summary>
    public bool RemoveOtherRole(int roleId)
    {
        ClientRole role;
        if (!_otherRoles.TryGetValue(roleId, out role))
        {
            return false;
        }

        _otherRoles.Remove(roleId);
        _otherRoleOrder.Remove(role);
        role.Dispose();
        return true;
    }

    /// <summary>按稳定顺序推进全部角色模型。</summary>
    public void Update(int deltaMilliseconds)
    {
        if (LocalRole != null)
        {
            LocalRole.Update(deltaMilliseconds);
        }
        for (int index = 0; index < _otherRoleOrder.Count; index++)
        {
            _otherRoleOrder[index].Update(deltaMilliseconds);
        }
    }

    /// <summary>清理本地角色和全部其他玩家，用于断线、退出和切换角色。</summary>
    public void Clear()
    {
        if (LocalRole != null)
        {
            LocalRole.Dispose();
            LocalRole = null;
            LocalRoleChanged?.Invoke(null);
        }

        for (int index = _otherRoleOrder.Count - 1; index >= 0; index--)
        {
            _otherRoleOrder[index].Dispose();
        }
        _otherRoleOrder.Clear();
        _otherRoles.Clear();
    }

    /// <summary>释放角色世界持有的全部客户端角色。</summary>
    public void Dispose()
    {
        Clear();
    }
}
