using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>管理客户端场景中的实体，并额外保存本地玩家和其他玩家的查找表。</summary>
public sealed class SceneWorldSystem
{

    #region 字段、事件与属性
    /// <summary>本地玩家装载或清空时通知控制器；清空时参数为 null。</summary>
    public event Action<PlayerEntity> LocalRoleChanged;

    /// <summary>真正创建、更新和回收实体的底层世界。</summary>
    private readonly SceneEntityWorld _entityWorld;

    /// <summary>以角色 ID 查找其他玩家，不包含本地玩家。</summary>
    private readonly Dictionary<int, RoleEntity> _otherRoles =
        new Dictionary<int, RoleEntity>();

    /// <summary>按加入顺序保留其他玩家的实体引用，移除时同步删除。</summary>
    private readonly List<RoleEntity> _otherRoleOrder = new List<RoleEntity>();

    /// <summary>当前登录玩家的实体，未装载或已清空时为 null。</summary>
    public PlayerEntity LocalRole { get; private set; }
    #endregion

    #region 构造与初始化
    /// <summary>创建供本客户端使用的实体世界。</summary>
    public SceneWorldSystem()
    {
        _entityWorld = new SceneEntityWorld();
    }

    /// <summary>把本帧经过的毫秒数交给所有实体执行常规更新。</summary>
    public void Update(int deltaMilliseconds)
    {
        _entityWorld.Update(deltaMilliseconds);
    }

    /// <summary>在常规更新后推进实体的后续处理，时间单位仍为毫秒。</summary>
    public void LateUpdate(int deltaMilliseconds)
    {
        _entityWorld.LateUpdate(deltaMilliseconds);
    }

    /// <summary>清空角色及其数据，再释放底层实体世界。</summary>
    public void Dispose()
    {
        Clear();
        _entityWorld.Dispose();
    }
    #endregion

    #region 创建与添加
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

    /// <summary>按 entityType 和配置编号 configId 在底层世界创建实体。</summary>
    public ROObject CreateEntity(EntityType entityType, int configId)
    {
        return _entityWorld.Create(entityType, configId);
    }

    /// <summary>按 projectileType 和配置编号 configId 创建投射物实体。</summary>
    public ROObject CreateProjectile(ProjectileType projectileType, int configId)
    {
        return _entityWorld.CreateProjectile(projectileType, configId);
    }
    #endregion

    #region 移除与回收
    /// <summary>按 roleId 同时移除玩家查找表和底层实体；未找到返回 false。</summary>
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

    /// <summary>按运行时 uid 回收实体；玩家索引需通过 RemoveOtherRole 一并处理。</summary>
    public bool RemoveEntity(ulong uid)
    {
        return _entityWorld.Remove(uid);
    }
    #endregion

    #region 查询与判定
    /// <summary>按角色 ID 查找其他玩家并输出 role，不查询 LocalRole。</summary>
    public bool TryGetRole(int roleId, out RoleEntity role)
    {
        return _otherRoles.TryGetValue(roleId, out role);
    }

    /// <summary>从场景实体中筛选敌方或友方目标，再按玩家位置过滤距离；当前规则把怪物当敌方、角色当友方。</summary>
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

    /// <summary>按运行时 uid 查询任意实体，结果写入 entity。</summary>
    public bool TryGetEntity(ulong uid, out ROObject entity)
    {
        return _entityWorld.TryGet(uid, out entity);
    }
    #endregion

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

    #region 核心逻辑
    /// <summary>释放本地角色数据、清空全部实体和玩家索引，并通知本地角色已移除。</summary>
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

    /// <summary>复制统一世界的战斗实体引用供点击射线与目标查询使用。</summary>
    /// <param name="entities">由调用方持有的快照列表，每次调用清空旧内容。</param>
    public void CopyEntities(List<MEntity> entities)
    {
        _entityWorld.CopyEntities(entities);
    }
    #endregion

}
