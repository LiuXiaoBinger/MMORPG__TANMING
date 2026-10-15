using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 客户端场景实体世界，独立于角色业务数据和 Unity 表现控制器。
/// 负责实体的 UID 分配、注册索引、帧驱动更新和对象池回收闭环。
/// 是场景中所有实体的唯一管理入口。
/// </summary>
public sealed class SceneEntityWorld : IDisposable
{
    /// <summary>按 UID 索引的实体字典，用于 O(1) 查找</summary>
    private readonly Dictionary<ulong, ROObject> _entities =
        new Dictionary<ulong, ROObject>();

    /// <summary>实体加入顺序列表，用于遍历驱动 Update/LateUpdate</summary>
    private readonly List<ROObject> _order = new List<ROObject>();

    /// <summary>下一个待分配的 UID，从 1 开始自增，跳过已占用值</summary>
    private ulong _nextUid = 1UL;

    /// <summary>是否已释放，Dispose 后禁止再添加实体</summary>
    private bool _disposed;

    /// <summary>当前世界中实体总数</summary>
    public int Count
    {
        get { return _entities.Count; }
    }

    /// <summary>
    /// 每帧逻辑更新入口，先正向驱动所有实体 Update，再反向清理待销毁实体。
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数</param>
    public void Update(int deltaMilliseconds)
    {
        // 正向遍历驱动各实体逻辑更新
        for (int index = 0; index < _order.Count; index++)
        {
            ROObject entity = _order[index];
            entity.Update(deltaMilliseconds);
        }

        // 反向遍历清理 PendingDestroy 实体，避免遍历时修改列表
        for (int index = _order.Count - 1; index >= 0; index--)
        {
            if (_order[index].LifecycleState == EntityLifecycleState.PendingDestroy)
            {
                Remove(_order[index].UID);
            }
        }
    }

    /// <summary>
    /// 帧末更新入口，驱动状态机动画刷新和模型表现。
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数，内部转换为秒</param>
    public void LateUpdate(int deltaMilliseconds)
    {
        float deltaSeconds = deltaMilliseconds * 0.001f;
        for (int index = 0; index < _order.Count; index++)
        {
            _order[index].LateUpdate(deltaSeconds);
        }
    }

    /// <summary>
    /// 将已构造好的实体加入世界并触发初始化。
    /// </summary>
    /// <param name="entity">待加入实体，UID 必须非零且不重复</param>
    /// <returns>加入成功返回 true；已释放、空实体或 UID 冲突返回 false</returns>
    public bool Add(ROObject entity)
    {
        // 实体无效：空实体、UID 为零或 UID 已被占用（内部短路保护空引用）。
        bool entityInvalid = entity == null ||
            entity.UID == 0UL ||
            _entities.ContainsKey(entity.UID);
        if (_disposed || entityInvalid)
        {
            return false;
        }

        _entities.Add(entity.UID, entity);
        _order.Add(entity);
        // 加入世界后立即执行初始化，装配组件并进入 Active 状态
        entity.Initialize();
        // 广播实体创建事件，供外部系统监听
        entity.DispatchEvent(MEventType.Created, null);
        return true;
    }

    /// <summary>
    /// 创建普通场景实体（角色、怪物、NPC 等）并加入世界。
    /// </summary>
    /// <param name="entityType">实体类型枚举</param>
    /// <param name="configId">配置表 ID</param>
    /// <returns>创建并加入成功的实体；工厂返回空或加入失败时返回 null</returns>
    public ROObject Create(EntityType entityType, int configId)
    {
        ulong uid = AllocateUid();
        ROObject entity = SceneEntityFactory.Build(entityType, uid, configId);
        if (entity == null)
        {
            return null;
        }
        if (!Add(entity))
        {
            // 加入世界失败时必须回收已构造实体，避免对象泄漏
            ReleaseUnmanagedEntity(entity);
            return null;
        }
        return entity;
    }

    /// <summary>
    /// 创建投射物实体并加入世界。
    /// </summary>
    /// <param name="projectileType">投射物形态类型</param>
    /// <param name="configId">投射物配置表 ID</param>
    /// <returns>创建并加入成功的投射物实体；工厂返回空或加入失败时返回 null</returns>
    public ROObject CreateProjectile(ProjectileType projectileType, int configId)
    {
        ulong uid = AllocateUid();
        ROObject entity = SceneEntityFactory.BuildProjectile(projectileType, uid,
            configId);
        if (entity == null)
        {
            return null;
        }
        if (!Add(entity))
        {
            ReleaseUnmanagedEntity(entity);
            return null;
        }
        return entity;
    }

    /// <summary>将当前世界的战斗实体引用复制到调用方容器，选择查询不直接修改世界顺序。</summary>
    /// <param name="entities">调用方持有的快照容器，结果仍引用世界中的原实体。</param>
    public void CopyEntities(List<MEntity> entities)
    {
        entities.Clear();
        // 实体加入顺序索引，只读取当前世界的原对象。
        for (int index = 0; index < _order.Count; index++)
        {
            // 非战斗场景对象不参与本轮目标选择。
            MEntity entity = _order[index] as MEntity;
            if (entity != null)
            {
                entities.Add(entity);
            }
        }
    }

    /// <summary>
    /// 按 UID 尝试获取实体。
    /// </summary>
    /// <param name="uid">目标实体 UID</param>
    /// <param name="entity">找到的实体引用，未找到时为 null</param>
    /// <returns>存在返回 true，不存在返回 false</returns>
    public bool TryGet(ulong uid, out ROObject entity)
    {
        return _entities.TryGetValue(uid, out entity);
    }

    /// <summary>
    /// 按 UID 获取实体并强制类型转换为目标类型。
    /// </summary>
    /// <typeparam name="T">目标实体类型</typeparam>
    /// <param name="uid">目标实体 UID</param>
    /// <returns>类型匹配的实体实例；不存在或类型不符返回 null</returns>
    public T Get<T>(ulong uid) where T : class, ROObject
    {
        ROObject entity;
        if (!_entities.TryGetValue(uid, out entity))
        {
            return null;
        }
        return entity as T;
    }

    /// <summary>
    /// 按 UID 移除实体，执行销毁并回收进对象池。
    /// </summary>
    /// <param name="uid">待移除实体 UID</param>
    /// <returns>存在并成功移除返回 true；不存在返回 false</returns>
    public bool Remove(ulong uid)
    {
        ROObject entity;
        if (!_entities.TryGetValue(uid, out entity))
        {
            return false;
        }

        // 在实体回池前取消当前目标，防止复用后原引用指向另一只怪物。
        MEntity selected = entity as MEntity;
        // 本地玩家就绪：全局实例、场景世界与本地角色均存在（内部短路保护空引用）。
        bool localRoleReady = Global.Instance != null &&
            Global.Instance.SceneWorld != null &&
            Global.Instance.SceneWorld.LocalRole != null;
        if (selected != null && localRoleReady &&
            Global.Instance.SceneWorld.LocalRole.Target == selected)
        {
            MSkillTargetMgr.singleton.ResetTarget(null);
        }
        _entities.Remove(uid);
        _order.Remove(entity);
        // 先执行实体自身销毁流程，再回收对象池
        entity.Destroy();
        MEntity pooledEntity = entity as MEntity;
        if (pooledEntity != null)
        {
            MEntityPool.Instance.Release(pooledEntity);
        }
        return true;
    }

    /// <summary>清空世界内所有实体，逐个销毁并回收对象池。</summary>
    public void Clear()
    {
        // 反向遍历逐个销毁回收
        for (int index = _order.Count - 1; index >= 0; index--)
        {
            ROObject entity = _order[index];
            entity.Destroy();
            MEntity pooledEntity = entity as MEntity;
            if (pooledEntity != null)
            {
                MEntityPool.Instance.Release(pooledEntity);
            }
        }
        _order.Clear();
        _entities.Clear();
    }

    /// <summary>释放世界资源，清理全部实体并标记已释放。</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Clear();
        _disposed = true;
    }

    /// <summary>
    /// 分配下一个有效 UID，跳过 0 和已被占用的值。
    /// </summary>
    /// <returns>未被占用的新 UID</returns>
    private ulong AllocateUid()
    {
        while (_nextUid == 0UL || _entities.ContainsKey(_nextUid))
        {
            _nextUid++;
        }
        ulong uid = _nextUid;
        _nextUid++;
        return uid;
    }

    /// <summary>清理 Builder 已创建但未能加入世界的实体，保证各级对象池闭环。</summary>
    private static void ReleaseUnmanagedEntity(ROObject entity)
    {
        if (entity == null)
        {
            return;
        }
        entity.Destroy();
        MEntity pooledEntity = entity as MEntity;
        if (pooledEntity != null)
        {
            MEntityPool.Instance.Release(pooledEntity);
        }
    }
}
