using MMoRpgCommon.SceneEntity;

/// <summary>
/// 旧场景实体调用的兼容契约。
///
/// ROObject 现在是接口而不是实体基类，实际实体继承链严格保持
/// MObject -&gt; MSceneObject -&gt; MEntity -&gt; 具体实体。旧系统仍可继续使用
/// ROObject 作为参数、集合元素或泛型约束，不会把兼容层插入实体继承层级。
/// </summary>
public interface ROObject
{
    /// <summary>实体运行时唯一身份 UID，世界索引、对象池和异步回调均使用此值</summary>
    ulong UID { get; }

    /// <summary>实体类型枚举（玩家、怪物、NPC 等）</summary>
    EntityType EntityType { get; }

    /// <summary>当前生命周期状态（Created/Active/Sleeping/PendingDestroy/Destroyed）</summary>
    EntityLifecycleState LifecycleState { get; }

    /// <summary>配置表 ID（怪物表 ID、NPC ID 等非运行时身份）</summary>
    int ConfigId { get; }

    /// <summary>所在地图 ID</summary>
    int MapId { get; set; }

    /// <summary>逻辑 Transform 快照（位置、朝向等）</summary>
    EntityTransformSnapshot Transform { get; }

    /// <summary>动画播放速度倍率（1.0 为正常速度）</summary>
    float AtorSpeed { get; set; }

    /// <summary>普通组件容器（专用组件不走此容器）</summary>
    EntityComponentContainer Components { get; }

    /// <summary>实体状态机实例</summary>
    MStateMachine Machine { get; }

    /// <summary>当前状态机状态定义</summary>
    StateDefine CurrentState { get; }

    /// <summary>
    /// 添加事件监听器
    /// </summary>
    /// <param name="eventType">监听的事件类型</param>
    /// <param name="handler">事件回调</param>
    /// <returns>添加成功返回 true</returns>
    bool AddEventListener(MEventType eventType, MEventHandler handler);

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="eventType">监听的事件类型</param>
    /// <param name="handler">要移除的回调</param>
    /// <returns>移除成功返回 true</returns>
    bool RemoveEventListener(MEventType eventType, MEventHandler handler);

    /// <summary>
    /// 派发事件到所有监听器
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventArgs">具名强类型事件参数，无载荷时传 null</param>
    void DispatchEvent(MEventType eventType, IEventArg eventArgs);

    /// <summary>清空所有事件监听，销毁和池复用时调用</summary>
    void ClearEventListeners();

    /// <summary>
    /// 挂载普通组件（专用快捷组件由 MComponentMgr 单独管理）
    /// </summary>
    /// <param name="component">待挂载的组件实例</param>
    void AddComponent(EntityComponentBase component);

    /// <summary>执行初始化流程，装配组件并进入 Active 状态</summary>
    void Initialize();

    /// <summary>
    /// 每帧逻辑更新
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数</param>
    void Update(int deltaMilliseconds);

    /// <summary>
    /// 帧末更新，驱动状态机动画刷新
    /// </summary>
    /// <param name="deltaSeconds">帧间隔秒数</param>
    void LateUpdate(float deltaSeconds);

    /// <summary>进入睡眠状态，暂停逻辑更新</summary>
    void Sleep();

    /// <summary>从睡眠唤醒，恢复 Active 状态</summary>
    void Wake();

    /// <summary>标记为待销毁，由世界在下一帧统一移除回收</summary>
    void MarkPendingDestroy();

    /// <summary>
    /// 生成实体快照，用于同步或序列化
    /// </summary>
    /// <returns>包含身份、位置、状态的快照对象</returns>
    EntitySnapshot ToSnapshot();

    /// <summary>立即销毁实体，清理组件、事件和模型表现</summary>
    void Destroy();

    /// <summary>
    /// 关联 Unity 表现控制器（RoleCtrlBase）
    /// </summary>
    /// <param name="ctrl">表现控制器实例</param>
    void SetEntityCtrl(RoleCtrlBase ctrl);

    /// <summary>获取关联的 Unity 表现控制器</summary>
    /// <returns>表现控制器引用，未关联时为 null</returns>
    RoleCtrlBase GetEntityCtrl();
}
