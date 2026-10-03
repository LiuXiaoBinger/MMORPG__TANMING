using System;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 通用对象层，统一负责普通实体组件、对象事件和基础生命周期。
/// 实现 ROObject 接口，是实体继承链的根抽象类。
/// 专用快捷组件（Move/Buff/Skill/Machine）由 MComponentMgr 单独管理，不进入普通组件容器。
/// </summary>
public abstract class MObject : ROObject
{
    /// <summary>普通实体组件容器，管理可按类型查询的组件集合</summary>
    private readonly EntityComponentContainer _components = new EntityComponentContainer();

    /// <summary>事件分发器，负责按事件类型分发到所有监听器</summary>
    private readonly MEventDispatcher _eventDispatcher = new MEventDispatcher();

    /// <summary>
    /// 基础构造函数，初始化身份信息和生命周期状态。
    /// </summary>
    /// <param name="uid">运行时唯一 UID，必须非零</param>
    /// <param name="entityType">实体类型枚举</param>
    /// <param name="configId">配置表 ID</param>
    /// <exception cref="ArgumentException">UID 为 0 时抛出</exception>
    protected MObject(ulong uid, EntityType entityType, int configId)
    {
        if (uid == 0UL)
        {
            throw new ArgumentException("实体 UID 必须有效", "uid");
        }

        UID = uid;
        EntityType = entityType;
        ConfigId = configId;
        LifecycleState = EntityLifecycleState.Created;
    }

    /// <summary>实体运行时唯一身份 UID</summary>
    public virtual ulong UID { get; protected set; }

    /// <summary>实体类型枚举</summary>
    public virtual EntityType EntityType { get; protected set; }

    /// <summary>当前生命周期状态</summary>
    public virtual EntityLifecycleState LifecycleState { get; protected set; }

    /// <summary>配置表 ID（非运行时身份）</summary>
    public virtual int ConfigId { get; protected set; }

    /// <summary>所在地图 ID</summary>
    public virtual int MapId { get; set; }

    /// <summary>动画播放速度倍率</summary>
    public virtual float AtorSpeed { get; set; }

    /// <summary>普通组件容器</summary>
    public virtual EntityComponentContainer Components { get { return _components; } }

    /// <summary>逻辑 Transform 快照（由派生类实现）</summary>
    public abstract EntityTransformSnapshot Transform { get; }

    /// <summary>实体状态机（由派生类实现）</summary>
    public abstract MStateMachine Machine { get; protected set; }

    /// <summary>当前状态机状态（由派生类实现）</summary>
    public abstract StateDefine CurrentState { get; }

    /// <summary>关联 Unity 表现控制器（由派生类实现）</summary>
    public abstract void SetEntityCtrl(RoleCtrlBase ctrl);

    /// <summary>获取表现控制器（由派生类实现）</summary>
    public abstract RoleCtrlBase GetEntityCtrl();

    /// <summary>是否为主角（本机玩家）</summary>
    public bool IsPlayer
    {
        get { return (EntityType & EntityType.Player) != EntityType.None; }
    }
    /// <summary>是否具有角色类别位；本地玩家和其他角色均返回 true。</summary>
    public bool IsRole
    {
        get { return (EntityType & EntityType.Role) != EntityType.None; }
    }
    /// <summary>
    /// 添加事件监听器
    /// </summary>
    /// <param name="eventType">监听的事件类型</param>
    /// <param name="handler">事件回调</param>
    /// <returns>添加成功返回 true</returns>
    public virtual bool AddEventListener(MEventType eventType, MEventHandler handler)
    {
        return _eventDispatcher.AddEventListener(eventType, handler);
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="eventType">监听的事件类型</param>
    /// <param name="handler">要移除的回调</param>
    /// <returns>移除成功返回 true</returns>
    public virtual bool RemoveEventListener(MEventType eventType, MEventHandler handler)
    {
        return _eventDispatcher.RemoveEventListener(eventType, handler);
    }

    /// <summary>
    /// 派发事件到所有监听器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventArgs">具名强类型事件参数，无载荷时传 null</param>
    public virtual void DispatchEvent(MEventType eventType, IEventArg eventArgs)
    {
        _eventDispatcher.DispatchEvent(eventType, eventArgs);
    }

    /// <summary>清空所有事件监听</summary>
    public virtual void ClearEventListeners()
    {
        _eventDispatcher.ClearEventListeners();
    }

    /// <summary>
    /// 添加普通组件。专用快捷组件必须由 MComponentMgr 单独管理。
    /// 仅在实体 Created 状态下允许挂载，初始化后禁止再添加。
    /// </summary>
    /// <param name="component">待挂载的组件实例</param>
    /// <exception cref="ArgumentNullException">component 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">实体已初始化后调用时抛出</exception>
    public virtual void AddComponent(EntityComponentBase component)
    {
        if (component == null)
        {
            throw new ArgumentNullException("component");
        }
        if (LifecycleState != EntityLifecycleState.Created)
        {
            throw new InvalidOperationException("实体初始化后不能再挂载普通组件");
        }

        // 将组件重绑定到当前实体
        component.RebindOwner(this);
        _components.Add(component);
    }

    public T GetMComponentByT<T>() where T : EntityComponentBase
    {
        return Components.GetMComponentByT<T>();
    }
    /// <summary>
    /// 执行初始化流程，装配组件并进入 Active 状态。
    /// 仅在 Created 状态下有效，重复调用直接忽略。
    /// </summary>
    public virtual void Initialize()
    {
        if (LifecycleState != EntityLifecycleState.Created)
        {
            return;
        }

        OnBeforeInitialize();
        _components.Initialize();
        LifecycleState = EntityLifecycleState.Active;
        DispatchEvent(MEventType.Initialized, null);
    }

    /// <summary>初始化前回调，派生类可在此追加额外组件</summary>
    protected virtual void OnBeforeInitialize()
    {
    }

    /// <summary>
    /// 每帧逻辑更新，仅在 Active 状态下驱动组件更新
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数</param>
    public virtual void Update(int deltaMilliseconds)
    {
        /*if (LifecycleState != EntityLifecycleState.Active)
        {
            return;
        }

        _components.Update(deltaMilliseconds);*/
        if (_components != null)
        {
            _components.Update(deltaMilliseconds);
        }
        
    }

    /// <summary>
    /// 帧末更新，仅在 Active 状态下驱动组件 LateUpdate
    /// </summary>
    /// <param name="deltaSeconds">帧间隔秒数</param>
    public virtual void LateUpdate(float deltaSeconds)
    {
        if (LifecycleState != EntityLifecycleState.Active)
        {
            return;
        }
        _components.LateUpdate(deltaSeconds);
    }

    /// <summary>进入睡眠状态，暂停逻辑更新并广播 Sleep 事件</summary>
    public virtual void Sleep()
    {
        if (LifecycleState == EntityLifecycleState.Active)
        {
            LifecycleState = EntityLifecycleState.Sleeping;
            DispatchEvent(MEventType.Sleeping, null);
        }
    }

    /// <summary>从睡眠唤醒，恢复 Active 状态并广播 Wake 事件</summary>
    public virtual void Wake()
    {
        if (LifecycleState == EntityLifecycleState.Sleeping)
        {
            LifecycleState = EntityLifecycleState.Active;
            DispatchEvent(MEventType.Woken, null);
        }
    }

    /// <summary>标记为待销毁，由世界在下一帧统一移除回收，广播 PendingDestroy 事件</summary>
    public virtual void MarkPendingDestroy()
    {
        if (LifecycleState == EntityLifecycleState.Destroyed)
        {
            return;
        }

        LifecycleState = EntityLifecycleState.PendingDestroy;
        DispatchEvent(MEventType.PendingDestroy, null);
    }

    /// <summary>
    /// 生成实体快照，包含身份、类型、位置等基础信息
    /// </summary>
    /// <returns>实体快照对象</returns>
    public virtual EntitySnapshot ToSnapshot()
    {
        EntitySnapshot snapshot = new EntitySnapshot();
        snapshot.UID = UID;
        snapshot.EntityType = EntityType;
        snapshot.LifecycleState = LifecycleState;
        snapshot.ConfigId = ConfigId;
        snapshot.MapId = MapId;
        snapshot.Transform = new EntityTransformSnapshot();
        snapshot.Transform.X = Transform.X;
        snapshot.Transform.Y = Transform.Y;
        snapshot.Transform.Z = Transform.Z;
        snapshot.Transform.Yaw = Transform.Yaw;
        return snapshot;
    }

    /// <summary>
    /// 立即销毁实体，执行完整清理流程：
    /// 派发销毁事件 → 关闭普通组件 → 派生类清理专用组件 → 标记 Destroyed → 广播销毁完成 → 清空事件监听
    /// </summary>
    public virtual void Destroy()
    {
        if (LifecycleState == EntityLifecycleState.Destroyed)
        {
            return;
        }

        DispatchEvent(MEventType.Destroying, null);
        // 普通组件先结束，此时仍可访问实体模型和四个专用组件完成表现清理。
        _components.Shutdown();
        OnDestroying();
        LifecycleState = EntityLifecycleState.Destroyed;
        DispatchEvent(MEventType.Destroyed, null);
        ClearEventListeners();
    }

    /// <summary>派发销毁事件后执行，派生实体在这里释放专用组件。</summary>
    protected virtual void OnDestroying()
    {
    }

    /// <summary>
    /// 实体池取出对象时重新写入身份和基础生命周期，恢复 Created 状态。
    /// 仅允许对已 Destroyed 的实体调用，防止误复用活跃对象。
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    /// <exception cref="InvalidOperationException">实体未销毁时调用抛出</exception>
    /// <exception cref="ArgumentException">UID 为 0 时抛出</exception>
    internal virtual void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        if (LifecycleState != EntityLifecycleState.Destroyed)
        {
            throw new InvalidOperationException("只有已销毁实体可以从实体池复用");
        }
        if (uid == 0UL)
        {
            throw new ArgumentException("实体 UID 必须有效", "uid");
        }
        ClearEventListeners();
        UID = uid;
        EntityType = entityType;
        ConfigId = configId;
        MapId = 0;
        AtorSpeed = 1f;
        LifecycleState = EntityLifecycleState.Created;
    }
}
