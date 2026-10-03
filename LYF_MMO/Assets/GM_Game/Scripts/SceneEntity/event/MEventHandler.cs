using System;

/// <summary>
/// 实体事件处理器，负责保存并调用强类型事件参数回调。
/// 封装具体的事件回调委托，使分发器可以统一管理不同类型的事件回调。
/// </summary>
public class MEventHandler
{
    /// <summary>
    /// 实际的事件回调委托，接收强类型事件参数。
    /// </summary>
    private EventCallbackHandler _callbackHandler = null;
#if LIBS_EDITOR
        private Action<RONetEvent> onSceneTriggerObjectSync;
#endif

    /// <summary>
    /// 获取当前处理器持有的回调委托。
    /// </summary>
    public EventCallbackHandler CallbackHandler { get { return _callbackHandler; } }

    /// <summary>
    /// 构造事件处理器，绑定指定回调。
    /// </summary>
    /// <param name="callback">事件触发时执行的回调委托。</param>
    public MEventHandler(EventCallbackHandler callback)
    {
        _callbackHandler = callback;
    }
#if LIBS_EDITOR
        public MEventHandler(Action<RONetEvent> onSceneTriggerObjectSync)
        {
            this.onSceneTriggerObjectSync = onSceneTriggerObjectSync;
        }
#endif

    /// <summary>
    /// 调用绑定的事件回调。
    /// </summary>
    /// <param name="e">事件参数，无载荷时可能为 null。</param>
    public void Invoke(IEventArg e)
    {
        // 空条件运算符安全调用，避免回调未绑定时报空引用
        _callbackHandler?.Invoke(e);
    }
}
