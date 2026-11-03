using System;

/// <summary>
/// 保存一个实体事件回调，供分发器以同一入口调用。
/// 普通回调接收 IEventArg，具体参数类型由对应事件约定。
/// </summary>
public class MEventHandler
{
    /// <summary>
    /// 实际的事件回调委托，接收强类型事件参数。
    /// </summary>
    private EventCallbackHandler _callbackHandler = null;
#if LIBS_EDITOR
        /// <summary>LIBS_EDITOR 分支保存的网络回调；当前 Invoke 不调用此字段。</summary>
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
        /// <summary>保存编辑器分支传入的网络回调，不设置普通实体事件回调。</summary>
        /// <param name="onSceneTriggerObjectSync">要保存的网络事件处理函数。</param>
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
