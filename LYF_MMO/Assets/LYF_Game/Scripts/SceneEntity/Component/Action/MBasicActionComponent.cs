/// <summary>
/// 简单动作状态的共用基类：收到动作事件后，向宿主状态机询问能否切入。
/// 各派生类提供自己的状态枚举、动画名和触发事件；结束条件由具体实现负责。
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
