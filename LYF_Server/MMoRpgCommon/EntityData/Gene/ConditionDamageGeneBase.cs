/// <summary>按真实战斗条件提高本次伤害的常驻节点；不修改任何属性，只在扣血前给出百分点。</summary>
public abstract class ConditionDamageGeneBase : GeneBase
{
    /// <summary>配置中的条件阈值，单位百分点；具体含义由派生类型定义，0 表示该类型不需要阈值。</summary>
    public int Condition { get; set; }
    /// <summary>条件增伤不施加属性，挂载时不执行 OnCall。</summary>
    public override bool CallsOnAttach { get { return false; } }
    /// <summary>增伤由结算方主动取当前值，不响应战斗事件。</summary>
    public override bool WaitsForEvent { get { return false; } }
    /// <summary>节点自身不执行独立动作，加成只通过 ResolveBonus 返回。</summary>
    /// <param name="role">当前持有该节点的角色。</param>
    /// <param name="args">条件增伤不使用事件参数。</param>
    /// <returns>始终返回 0，表示本次未施加属性或资源。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs)) { return 0; }
    /// <summary>按本次真实战斗数据计算增伤百分点；不满足条件返回零。</summary>
    /// <param name="context">结算前的真实双方资源与目标类别。</param>
    /// <returns>本次伤害的增伤百分点，可叠加层数。</returns>
    public abstract double ResolveBonus(GeneDamageContext context);
}
