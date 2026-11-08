/// <summary>最大生命值运行基因，按根实例层数提供固定值或百分比修正。</summary>
public class MaxHPGene : AttributeGeneBase
{
    /// <summary>本节点执行时直接修改角色的 MaxHp，可在此追加其他属性行为。</summary>
    /// <param name="role">实际承受此效果的角色。</param>
    /// <param name="args">普通属性修改不使用事件参数。</param>
    /// <returns>完成本节点属性修改后返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        role.AttrComp.ChangeAttribute(GeneAttributeType.MaxHp, Value * StackCount, ValueType);
        return 1;
    }

    /// <summary>移除或刷新前撤销本节点此前施加的属性修正。</summary>
    /// <param name="role">此前接受本节点的同一角色。</param>
    public override void OnRemove(IGeneOwner role)
    {
        role.AttrComp.ChangeAttribute(GeneAttributeType.MaxHp, -Value * StackCount, ValueType);
    }

    /// <summary>此节点只修正最大生命值，不会改变其他属性。</summary>
    public override GeneAttributeType Attribute { get { return GeneAttributeType.MaxHp; } }
}
