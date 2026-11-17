/// <summary>
/// 实体事件监听函数的统一签名。
/// 监听器按事件类型读取相应参数；原参数仅在本次同步回调期间有效。
/// </summary>
/// <param name="eventArg">事件参数对象，无载荷事件为 null。</param>
public delegate void EventCallbackHandler(IEventArg eventArg);
