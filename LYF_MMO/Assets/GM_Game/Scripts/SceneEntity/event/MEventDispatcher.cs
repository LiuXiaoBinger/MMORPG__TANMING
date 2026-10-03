using System;
using System.Collections.Generic;

/// <summary>
/// 单个实体的事件分发器，对应参考项目 MEventDispatcher。
/// 负责按事件类型注册、注销和派发事件处理器。
/// 派发时复制监听器列表快照，使监听器在回调中注销自身不会破坏当前遍历。
/// </summary>
public sealed class MEventDispatcher
{
    /// <summary>
    /// 事件类型到监听器列表的映射表。
    /// 每个事件类型维护一个独立的处理器列表，支持同一事件多个监听器。
    /// </summary>
    private readonly Dictionary<MEventType, List<MEventHandler>> _listeners =
        new Dictionary<MEventType, List<MEventHandler>>();

    /// <summary>
    /// 注册事件监听器。同一事件类型下重复注册同一个处理器会被忽略。
    /// </summary>
    /// <param name="eventType">目标事件类型。</param>
    /// <param name="handler">事件处理器实例，不能为 null。</param>
    /// <returns>注册成功返回 true；handler 为 null 或重复注册时返回 false。</returns>
    public bool AddEventListener(MEventType eventType, MEventHandler handler)
    {
        if (handler == null)
        {
            return false;
        }

        // 查找该事件类型的处理器列表，不存在则新建
        // 当前事件类型对应的监听器列表。
        List<MEventHandler> handlers;
        if (!_listeners.TryGetValue(eventType, out handlers))
        {
            handlers = new List<MEventHandler>();
            _listeners.Add(eventType, handlers);
        }

        // 遍历检查是否已存在相同处理器，避免重复注册
        for (int index = 0; index < handlers.Count; index++)
        {
            if (handlers[index] == handler)
            {
                return false;
            }
        }

        handlers.Add(handler);
        return true;
    }

    /// <summary>
    /// 注销事件监听器。
    /// </summary>
    /// <param name="eventType">目标事件类型。</param>
    /// <param name="handler">要移除的事件处理器实例。</param>
    /// <returns>成功移除返回 true；未找到或 handler 为 null 时返回 false。</returns>
    public bool RemoveEventListener(MEventType eventType, MEventHandler handler)
    {
        if (handler == null)
        {
            return false;
        }

        List<MEventHandler> handlers;
        if (!_listeners.TryGetValue(eventType, out handlers))
        {
            return false;
        }

        // 从后往前遍历移除，避免索引偏移
        for (int index = handlers.Count - 1; index >= 0; index--)
        {
            if (handlers[index] == handler)
            {
                handlers.RemoveAt(index);
                // 列表清空后移除该事件类型条目，避免空列表占用
                if (handlers.Count == 0)
                {
                    _listeners.Remove(eventType);
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 派发事件到所有已注册的监听器。
    /// 所有监听器共享同一个事件参数实例，回调异常不会阻断其他监听器。
    /// </summary>
    /// <param name="eventType">要派发的事件类型。</param>
    /// <param name="eventArgs">事件载荷，无载荷事件可传 null。</param>
    public void DispatchEvent(MEventType eventType, IEventArg eventArgs)
    {
        try
        {
            // 当前事件类型对应的监听器列表；查询失败表示本次没有订阅者。
            List<MEventHandler> handlers;
            if (!_listeners.TryGetValue(eventType, out handlers) || handlers.Count == 0)
            {
                return;
            }

            // 创建监听器快照，允许监听器在回调中安全增删注册关系。
            List<MEventHandler> snapshot = new List<MEventHandler>(handlers);
            for (int index = 0; index < snapshot.Count; index++)
            {
                // 当前索引对应的监听器实例。
                MEventHandler handler = snapshot[index];
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    // 按 RO 分发器模式将同一个参数实例传给所有监听器。
                    handler.Invoke(eventArgs);
                }
                catch (Exception exception)
                {
                    // 单个监听器失败不能阻断其余监听器的同步处理。
                    UnityEngine.Debug.LogError("实体事件监听失败: " + eventType + "，" + exception);
                }
            }
        }
        finally
        {
            // 无监听器、快照构造失败或任意回调异常时均由此处唯一结束参数生命周期。
            if (eventArgs != null)
            {
                eventArgs.Recycle();
            }
        }
    }

    /// <summary>
    /// 清空所有事件监听器。通常在实体回收到对象池时调用。
    /// </summary>
    public void ClearEventListeners()
    {
        _listeners.Clear();
    }
}
