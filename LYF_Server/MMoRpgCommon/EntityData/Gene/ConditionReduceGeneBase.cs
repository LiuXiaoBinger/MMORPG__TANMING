/// <summary>按真实战斗条件降低本次受到伤害的常驻节点；不修改任何属性，只在扣血前给出减免百分点。</summary>
public abstract class ConditionReduceGeneBase : GeneBase
{
    /// <summary>配置中的条件阈值，单位百分点或类别编号；具体含义由派生类型定义。</summary>
    public int Condition { get; set; }
    /// <summary>减伤不施加属性，挂载时不执行 OnCall。</summary>
    public override bool CallsOnAttach { get { return false; } }
    /// <summary>减伤由受击结算方主动取当前值，不响应战斗事件。</summary>
    public override bool WaitsForEvent { get { return false; } }
    /// <summary>节点自身不执行独立动作，减免只通过 ResolveReduction 返回。</summary>
    /// <param name="role">当前持有该节点的角色。</param>
    /// <param name="args">条件减伤不使用事件参数。</param>
    /// <returns>始终返回 0，表示本次未施加属性或资源。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs)) { return 0; }
    /// <summary>按本次真实受击数据计算减免百分点；不满足条件返回零。</summary>
    /// <param name="attackerIsBoss">伤害来源是否为真实首领实体。</param>
    /// <returns>本次受到伤害的减免百分点，可叠加层数。</returns>
    public abstract double ResolveReduction(bool attackerIsBoss);
}
