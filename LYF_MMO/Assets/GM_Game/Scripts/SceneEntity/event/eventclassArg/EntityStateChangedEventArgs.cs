/// <summary>
/// 实体动作状态真实变化事件的强类型载荷。
/// 用于 StateChanged 事件，携带切换前后的状态值。
/// </summary>
public sealed class EntityStateChangedEventArgs : IEventArg
{
    /// <summary>
    /// 切换前的动作状态。
    /// </summary>
    public StateDefine PreviousState { get; private set; }

    /// <summary>
    /// 切换后的新动作状态。
    /// </summary>
    public StateDefine CurrentState { get; private set; }

    /// <summary>事件参数是否已被回收；true 表示实例已归还状态变化事件参数池。</summary>
    public bool Deprecated { get; set; }

    /// <summary>
    /// 创建空的状态变化参数实例。
    /// 该构造函数仅供 <see cref="MEventPool{T}"/> 创建池实例，业务代码必须调用 <see cref="Create"/>。
    /// </summary>
    public EntityStateChangedEventArgs()
    {
        ResetData();
    }

    /// <summary>从对象池获取并填写一次状态切换事件的数据。</summary>
    /// <param name="previousState">切换前已结束或被中断的状态。</param>
    /// <param name="currentState">切换后获得执行权限的状态。</param>
    /// <returns>本次状态事件使用的池化参数，派发结束后由分发器归还。</returns>
    public static EntityStateChangedEventArgs Create(StateDefine previousState,
        StateDefine currentState)
    {
        // 从状态变更参数专属对象池获取实例，减少频繁动作切换时的 GC 分配。
        EntityStateChangedEventArgs eventArgs =
            MEventPool<EntityStateChangedEventArgs>.GetEvent();
        eventArgs.ResetData();
        eventArgs.PreviousState = previousState;
        eventArgs.CurrentState = currentState;
        return eventArgs;
    }

    /// <summary>
    /// 创建当前事件参数的独立副本。
    /// </summary>
    /// <returns>包含当前状态快照的池化副本，由调用方在使用结束后主动回收。</returns>
    public IEventArg Clone()
    {
        return Create(PreviousState, CurrentState);
    }

    /// <summary>
    /// 清空状态枚举并归还对象池；重复回收会由对象池的 Deprecated 标记拒绝再次入池。
    /// </summary>
    public void Recycle()
    {
        ResetData();
        MEventPool<EntityStateChangedEventArgs>.Recycle(this);
    }

    /// <summary>将状态字段还原为默认动作状态，保证下次复用不残留上次切换数据。</summary>
    private void ResetData()
    {
        PreviousState = default;
        CurrentState = default;
    }
}
