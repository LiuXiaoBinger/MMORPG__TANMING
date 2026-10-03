/// <summary>
/// RO 风格动作状态组件的通用基类。
/// 仅声明状态枚举和动画名，提供默认的事件转移判定：只要状态机允许即可切入。
/// 具体动作状态（如默认、技能、受击、死亡等）继承本类，只需指定 _selfState 和 StateName。
/// </summary>
public abstract class MBasicActionComponent : MActionComponent
{
    /// <summary>构造函数，指定宿主实体。</summary>
    /// <param name="owner">宿主场景实体对象。</param>
    protected MBasicActionComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>转移到该状态时保留战斗坐骑显示，默认 true。</summary>
    public override bool WithBattleVehicle { get { return true; } }

    /// <summary>
    /// 状态转移判定：只要状态机允许转移到本状态即可切入。
    /// </summary>
    /// <param name="e">触发事件参数。</param>
    /// <param name="last">上一个状态定义。</param>
    /// <returns>true 表示允许转移到本状态。</returns>
    protected override bool OnGetEvent(IEventArg e, StateDefine last)
    {
        if (Entity == null || Entity.Machine == null) return false;
        return Entity.Machine.CanTransfer(SelfState);
    }
}
