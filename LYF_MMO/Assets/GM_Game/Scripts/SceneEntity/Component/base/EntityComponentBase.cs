using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 场景实体组件基类，独立于角色背包等业务组件。
/// 普通实体组件统一继承本类，由 <see cref="MComponentMgr"/> 从组件池取出/回收，
/// 通过宿主实体 <see cref="MEntity"/> 完成事件订阅、派发与生命周期回调。
/// </summary>
public abstract class EntityComponentBase : IEntityComponent
{
    /// <summary>本组件通过 <see cref="RegisterEvent"/> 订阅的宿主事件及其处理器列表，回收时据此批量解绑。</summary>
    private readonly Dictionary<MEventType, List<MEventHandler>> _registeredEvents =
        new Dictionary<MEventType, List<MEventHandler>>();

    /// <summary>标记组件是否已经挂载过宿主；对象池复用时避免重复触发 OnAttachToHost。</summary>
    private bool _attached;

    private bool _enabled = true; // 组件是否可用

    private bool _detached = true; // 组件是否被卸载（组件模式下，没有挂载到对象的组件都是无效的）

    /// <summary>组件是否启用。get 时要求同时满足 _enabled 且未被卸载；set 只控制开关 _enabled</summary>
    public virtual bool Enabled
    {
        get { return _enabled && !_detached; }
        set { _enabled = value; }
    }

    /// <summary>组件所属实体，普通组件和专用组件均通过此引用访问实体。</summary>
    protected MEntity Entity { get; private set; }

    /// <summary>供状态机/动作组件等内部模块访问宿主实体，对外只读。</summary>
    internal MEntity EntityForState { get { return Entity; } }

    /// <summary>组件类型标识，由子类声明，用于组件容器按类型索引。</summary>
    public abstract EntityComponentType ComponentType { get; }

    /// <summary>构造组件实例，绑定宿主实体。</summary>
    /// <param name="owner">组件所属宿主对象，必须是 <see cref="MEntity"/> 或其子类，为空时抛出异常。</param>
    protected EntityComponentBase(ROObject owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }
        Entity = owner as MEntity;
    }

    /// <summary>组件每帧更新，由组件容器按顺序驱动。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public virtual void Update(int deltaMilliseconds)
    {
    }

    /// <summary>组件每帧更新入口，由实体容器按固定顺序驱动；子类按需覆写。</summary>
    /// <param name="fDeltaT">距上一帧时间（秒）。</param>
    public virtual void Update(float fDeltaT)
    {
        
    }

    /// <summary>帧后更新，由组件容器在 LateUpdate 阶段驱动，常用于动画表现刷新。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public virtual void LateUpdate(float deltaTime)
    {
    }

    /// <summary>组件初始化，由组件容器在挂载后统一调用一次；子类按需重写。</summary>
    public virtual void Initialize()
    {
    }

    /// <summary>组件销毁，释放自身持有的运行时资源；事件解绑由基类统一处理。</summary>
    public virtual void Shutdown()
    {
    }

    /// <summary>按参考 MComponent.RegisterEvent 订阅宿主事件，并记录以便回收时解绑。</summary>
    /// <param name="eventType">宿主事件类型。</param>
    /// <param name="handler">事件回调处理器。</param>
    protected void RegisterEvent(MEventType eventType, MEventHandler handler)
    {
        if (Entity == null || handler == null)
        {
            return;
        }
        if (!Entity.AddEventListener(eventType, handler))
        {
            return;
        }

        // 首次订阅该类型事件时新建处理器列表，避免字典缺少键
        List<MEventHandler> handlers;
        if (!_registeredEvents.TryGetValue(eventType, out handlers))
        {
            handlers = new List<MEventHandler>();
            _registeredEvents.Add(eventType, handlers);
        }
        handlers.Add(handler);
    }

    /// <summary>手动解绑一个已订阅的宿主事件，并从内部记录表中移除。</summary>
    /// <param name="eventType">宿主事件类型。</param>
    /// <param name="handler">要解绑的事件回调处理器。</param>
    protected void RemoveEventListener(MEventType eventType, MEventHandler handler)
    {
        if (Entity == null || handler == null)
        {
            return;
        }
        Entity.RemoveEventListener(eventType, handler);
        RemoveRegisteredEvent(eventType, handler);
    }

    /// <summary>通过宿主实体向上派发事件。</summary>
    /// <param name="eventType">要派发的事件类型。</param>
    /// <param name="eventArgs">强类型事件载荷，无载荷事件传 null。</param>
    protected void DispatchEvent(MEventType eventType, IEventArg eventArgs)
    {
        if (Entity != null)
        {
            Entity.DispatchEvent(eventType, eventArgs);
        }
    }

    /// <summary>对象池取出组件时重新绑定宿主，避免为同一组件类型重复分配对象。</summary>
    /// <param name="owner">新的宿主对象，必须是 <see cref="MEntity"/> 或其子类。</param>
    internal void RebindOwner(ROObject owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }
        Entity = owner as MEntity;
        // 清理上一次复用时残留的事件记录，避免向旧实体解绑
        _registeredEvents.Clear();
        if (!_attached)
        {
            OnAttachToHost(owner);
            _attached = true;
        }
    }

    /// <summary>对象池回收前重置组件状态并解除宿主引用。</summary>
    internal void ResetForPool()
    {
        OnDetachFromHost();
        _attached = false;
        // 统一解绑所有已订阅事件并清空记录表
        ClearRegisteredEvents();
    }

    /// <summary>解绑本组件通过宿主实体订阅的全部事件，并清空内部记录表。</summary>
    internal void ClearRegisteredEvents()
    {
        if (Entity != null)
        {
            // 先复制键快照再反向遍历解绑，避免遍历字典过程中被修改
            List<MEventType> eventTypes = new List<MEventType>(_registeredEvents.Keys);
            for (int typeIndex = 0; typeIndex < eventTypes.Count; typeIndex++)
            {
                MEventType eventType = eventTypes[typeIndex];
                List<MEventHandler> handlers = _registeredEvents[eventType];
                for (int handlerIndex = handlers.Count - 1; handlerIndex >= 0; handlerIndex--)
                {
                    Entity.RemoveEventListener(eventType, handlers[handlerIndex]);
                }
            }
        }
        _registeredEvents.Clear();
        Entity = null;
    }

    /// <summary>从内部事件记录表中移除指定处理器，列表清空后删除对应键。</summary>
    /// <param name="eventType">宿主事件类型。</param>
    /// <param name="handler">要移除的事件回调处理器。</param>
    private void RemoveRegisteredEvent(MEventType eventType, MEventHandler handler)
    {
        List<MEventHandler> handlers;
        if (!_registeredEvents.TryGetValue(eventType, out handlers))
        {
            return;
        }
        // 反向遍历删除，避免下标错位
        for (int index = handlers.Count - 1; index >= 0; index--)
        {
            if (handlers[index] == handler)
            {
                handlers.RemoveAt(index);
                break;
            }
        }
        if (handlers.Count == 0)
        {
            _registeredEvents.Remove(eventType);
        }
    }

    /// <summary>组件从宿主实体卸载时回调，子类用于释放与宿主相关的运行时引用。</summary>
    public virtual void OnDetachFromHost()
    {
        _enabled = true;
        _detached = true;
        EventUnsubscribe();
    }

    /// <summary>组件挂载到宿主实体时回调，子类用于初始化宿主相关状态。</summary>
    /// <param name="hostObj">新挂载的宿主对象。</param>
    public virtual void OnAttachToHost(ROObject hostObj)
    {
        Entity = hostObj as MEntity;
        _detached = false;
        EventSubscribe();
    }

    /// <summary>组件挂载完成回调，在组件加入容器并完成初始化后触发。</summary>
    public virtual void Attached()
    {
    }

    /// <summary>注册对宿主事件的监听，子类按需覆写。在挂接到宿主后调用</summary>
    protected virtual void EventSubscribe() { }

    /// <summary>注销事件监听，与 EventSubscribe 成对，子类按需覆写</summary>
    protected virtual void EventUnsubscribe() { }
}
