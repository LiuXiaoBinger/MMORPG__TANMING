/// <summary>
/// 默认动作状态，对应参考工程 rogamelibs/entity/action/default_action_component.h:11-12。
/// 默认态永远"已结束"，因此不占用动作槽，也不阻止任何状态切换。
/// </summary>
public sealed class DefaultActionComponent : ActionStateComponent<ROEvent>
{
    /// <summary>创建默认态组件。</summary>
    /// <param name="owner">状态机所属的在线角色。</param>
    public DefaultActionComponent(OnlineRole owner)
        : base(owner, StateDefine.kStateDefault)
    {
    }

    /// <summary>默认态恒为已结束，状态机每帧都会看到"可以回默认态"。</summary>
    public override bool IsFinished()
    {
        return true;
    }

    /// <summary>默认态不阻止任何目标状态。</summary>
    /// <param name="state">目标状态标识。</param>
    public override bool IsPermitted(StateDefine state)
    {
        return true;
    }

    /// <summary>默认态不接受任何动作事件，动作必须由具体状态组件承接。</summary>
    /// <param name="args">驱动动作的事件。</param>
    public override bool OnActionEvent(ROEvent args)
    {
        return false;
    }
}
