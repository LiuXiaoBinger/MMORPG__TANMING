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



    public void Update(int deltaMilliseconds)
    {
        _entityWorld.Update(deltaMilliseconds);
    }



    public void LateUpdate(int deltaMilliseconds)
    {
        _entityWorld.LateUpdate(deltaMilliseconds);
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



    /// <summary>复制统一世界的战斗实体引用供点击射线与目标查询使用。</summary>
    /// <param name="entities">由调用方持有的快照列表，每次调用清空旧内容。</param>
    public void CopyEntities(List<MEntity> entities)
    {
        _entityWorld.CopyEntities(entities);
    }

    /// <summary>沿 RO 获取敌友候选；当前怪物为敌方，其他角色为友方，不假设玩家阵营。</summary>
    /// <param name="targets">候选结果容器，查询覆盖原内容，实体仍为世界原引用。</param>
    /// <param name="player">当前查询的本地主角，用其位置计算选择距离。</param>
    /// <param name="findEnemy">true 取怪物，false 取角色。</param>
    /// <param name="range">选择距离米数，负数表示不限距离。</param>
    public void GetOpponent(List<MEntity> targets, PlayerEntity player,
        bool findEnemy, float range)
    {
        _entityWorld.CopyEntities(targets);
        // 倒序移除不符合敌友类别或超距的对象，保留原对象引用。
        for (int index = targets.Count - 1; index >= 0; index--)
        {
            // 当前世界快照中的候选，不额外构造选择实体。
            MEntity entity = targets[index];
            // 当前敌友规则只使用已接入的怪物和角色类型。
            bool matches = entity is RoleEntity;
            if (findEnemy)
            {
                matches = MSkillTargetDefine.IsEnemy(entity);
            }
            if (!matches || (range >= 0f &&
                (entity.Position - player.Position).sqrMagnitude > range * range))
            {
                targets.RemoveAt(index);
            }
        }
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

