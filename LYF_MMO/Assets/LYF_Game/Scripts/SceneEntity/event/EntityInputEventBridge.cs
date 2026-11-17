/// <summary>
/// 把输入参数交给目标实体的事件分发器。
/// 目标不存在时直接回收参数；成功交给分发器后，由分发器负责回收。
/// </summary>
public static class EntityInputEventBridge
{
    /// <summary>向目标实体派发统一输入参数。</summary>
    /// <param name="entity">接收输入的目标实体。</param>
    /// <param name="eventArgs">统一输入参数。</param>
    public static void DispatchInput(ROObject entity, InputEventArgs eventArgs)
    {
        // 参数为空时没有可交接或回收的对象，直接结束本次派发。
        if (eventArgs == null)
        {
            return;
        }
        // 实体为空时无法交给分发器统一回收，桥接层作为当前所有者必须归还池化参数。
        if (entity == null)
        {
            eventArgs.Recycle();
            return;
        }
        // 从进入 DispatchEvent 开始，参数所有权转交分发器；其 finally 会在异常路径统一回收一次。
        entity.DispatchEvent(MEventType.InputKey, eventArgs);
    }
    
    /// <summary>向目标实体发送 MEvent_Move 移动事件。</summary>
    /// <param name="entity">接收移动请求的实体。</param>
    /// <param name="eventArgs">移动组件所需的事件参数，交接后由分发器回收。</param>
    public static void DispatchInputMove(ROObject entity, IEventArg eventArgs)
    {
        // 参数为空时没有可交接或回收的对象，直接结束本次派发。
        if (eventArgs == null)
        {
            return;
        }
        // 实体为空时由桥接层终止事件生命周期，避免移动参数遗留在使用中状态。
        if (entity == null)
        {
            eventArgs.Recycle();
            return;
        }
        // 分发器负责成功和异常两条路径的唯一回收，桥接层不得再次回收同一参数。
        entity.DispatchEvent(MEventType.MEvent_Move, eventArgs);
    }
}
