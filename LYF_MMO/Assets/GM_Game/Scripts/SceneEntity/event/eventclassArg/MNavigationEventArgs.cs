/// <summary>RO同名导航指令；目前仅承载到达后释放技能，不复制未实现的通用导航参数。</summary>
public sealed class MNavigationEventArgs : IEventArg
{
    /// <summary>到达后的技能动作；当前只有这一种实际业务载荷，不使用通用object参数。</summary>
    public MSkillEventArgs NavAction { get; private set; }
    /// <summary>事件池回收标记，true表示本次导航指令已失效。</summary>
    public bool Deprecated { get; set; }

    /// <summary>事件池创建空导航指令。</summary>
    public MNavigationEventArgs() { }

    /// <summary>借出导航事件并接管本次技能指令的回收责任。</summary>
    /// <param name="navAction">本次到达后执行的技能输入指令。</param>
    /// <returns>由实体派发器在同步处理结束时回收的导航事件。</returns>
    public static MNavigationEventArgs Create(MSkillEventArgs navAction)
    {
        // 本次导航派发独占的池化事件，技能载荷跟随其生命周期。
        MNavigationEventArgs args = MEventPool<MNavigationEventArgs>.GetEvent();
        args.NavAction = navAction;
        return args;
    }

    /// <summary>复制导航指令与技能载荷，各自持有独立的事件池实例。</summary>
    /// <returns>由复制件使用者回收的完整指令。</returns>
    public IEventArg Clone() { return Create((MSkillEventArgs)NavAction.Clone()); }

    /// <summary>一并回收到达动作及导航事件，不保留跨帧缓存。</summary>
    public void Recycle()
    {
        NavAction.Recycle();
        NavAction = null;
        MEventPool<MNavigationEventArgs>.Recycle(this);
    }
}
