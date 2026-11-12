/// <summary>高级属性运行基因；同一个具体类按构造时固定的属性编号修正角色属性面板。</summary>
public class AdvancedAttributeGene : AttributeGeneBase
{
    /// <summary>本实例实际修正的高级属性编号，构造后不再变化。</summary>
    private readonly GeneAttributeType attribute;

    /// <summary>为指定高级属性创建独立运行节点，节点数值仍来自配置行。</summary>
    /// <param name="attribute">101 至 144 之间的高级属性编号。</param>
    public AdvancedAttributeGene(GeneAttributeType attribute)
    {
        this.attribute = attribute;
    }

    /// <summary>本节点执行时按层数修正对应高级属性，可在此追加其他属性行为。</summary>
    /// <param name="role">实际承受此效果的角色。</param>
    /// <param name="args">普通属性修改不使用事件参数。</param>
    /// <returns>完成本节点属性修改后返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        // 层数参与计算，百分比类型由 ValueType 决定写入百分点还是固定点数。
        role.AttrComp.ChangeAttribute(attribute, Value * StackCount, ValueType);
        return 1;
    }

    /// <summary>移除或刷新前撤销本节点此前施加的属性修正。</summary>
    /// <param name="role">此前接受本节点的同一角色。</param>
    public override void OnRemove(IGeneOwner role)
    {
        // 撤销量与施加量完全对称，避免层数变化后残留修正。
        role.AttrComp.ChangeAttribute(attribute, -Value * StackCount, ValueType);
    }

    /// <summary>本节点固定修正构造时传入的那一个高级属性。</summary>
    public override GeneAttributeType Attribute { get { return attribute; } }
}
