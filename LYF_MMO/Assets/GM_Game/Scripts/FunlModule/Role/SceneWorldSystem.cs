using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>客户端全部场景实体的唯一管理器。</summary>
public sealed class SceneWorldSystem
{
    public event Action<PlayerEntity> LocalRoleChanged;
    private readonly SceneEntityWorld _entityWorld;
    private readonly Dictionary<int, RoleEntity> _otherRoles =
        new Dictionary<int, RoleEntity>();
    private readonly List<RoleEntity> _otherRoleOrder = new List<RoleEntity>();

    public PlayerEntity LocalRole { get; private set; }

    public SceneWorldSystem()
    {
        _entityWorld = new SceneEntityWorld();
    }

    /// <summary>创建本地玩家实体并直接加入统一实体世界。</summary>
    public PlayerEntity LoadLocalRole(MainRoleInfo roleInfo)
    {
        Clear();
        if (roleInfo == null || roleInfo.BaseInfo == null)
        {
            return null;
        }
        LocalRole = _entityWorld.Create(EntityType.Player, 0) as PlayerEntity;
        if (LocalRole == null)
        {
            return null;
        }
        LocalRole.InitializeLocalRole(roleInfo);
        // 角色组件必须先完成入世界生命周期，再向界面和控制器公布本地玩家。
        LocalRole.AfterAddedToWorld();
        LocalRoleChanged?.Invoke(LocalRole);
        return LocalRole;
    }

    /// <summary>创建或更新其他角色实体。</summary>
    public RoleEntity AddOrUpdateOtherRole(RoleBaseInfo baseInfo)
    {
        if (baseInfo == null || baseInfo.RoleId <= 0)
        {
            return null;
        }
        if (LocalRole != null && LocalRole.GetID() == baseInfo.RoleId)
        {
            return null;
        }
        RoleEntity role;
        if (_otherRoles.TryGetValue(baseInfo.RoleId, out role))
        {
            role.UpdateBaseInfo(baseInfo);
            return role;
        }
        role = _entityWorld.Create(EntityType.Role, 0) as RoleEntity;
        if (role == null)
        {
            return null;
        }
        role.UpdateBaseInfo(baseInfo);
        _otherRoles.Add(baseInfo.RoleId, role);
        _otherRoleOrder.Add(role);
        return role;
    }

    public bool TryGetRole(int roleId, out RoleEntity role)
    {
        return _otherRoles.TryGetValue(roleId, out role);
    }

    public bool RemoveOtherRole(int roleId)
    {
        RoleEntity role;
        if (!_otherRoles.TryGetValue(roleId, out role))
        {
            return false;
        }
        _otherRoles.Remove(roleId);
        _otherRoleOrder.Remove(role);
        _entityWorld.Remove(role.UID);
        return true;
    }

    public void Update(int deltaMilliseconds)
    {
        _entityWorld.Update(deltaMilliseconds);
    }

    public void LateUpdate(int deltaMilliseconds)
    {
        _entityWorld.LateUpdate(deltaMilliseconds);
    }

    public void Clear()
    {
        _otherRoles.Clear();
        _otherRoleOrder.Clear();
        if (LocalRole != null)
        {
            LocalRole.DisposeRoleData();
        }
        _entityWorld.Clear();
        LocalRole = null;
        LocalRoleChanged?.Invoke(null);
    }

    public void Dispose()
    {
        Clear();
        _entityWorld.Dispose();
    }

    public bool TryGetEntity(ulong uid, out ROObject entity)
    {
        return _entityWorld.TryGet(uid, out entity);
    }

    public ROObject CreateEntity(EntityType entityType, int configId)
    {
        return _entityWorld.Create(entityType, configId);
    }

    public ROObject CreateProjectile(ProjectileType projectileType, int configId)
    {
        return _entityWorld.CreateProjectile(projectileType, configId);
    }

    public bool RemoveEntity(ulong uid)
    {
        return _entityWorld.Remove(uid);
    }
}

