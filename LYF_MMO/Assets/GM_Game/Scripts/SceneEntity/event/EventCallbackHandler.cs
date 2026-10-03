/// <summary>
/// 实体事件回调委托。载荷类型由事件类型确定，监听器按需转换为对应具名参数类型。
/// 这是所有实体事件监听器的统一签名。
/// </summary>
/// <param name="eventArg">事件参数对象，无载荷事件为 null。</param>
public delegate void EventCallbackHandler(IEventArg eventArg);
