/// <summary>保存技能施加的单项持续属性，护体可在寿命内把加成线性收回。</summary>
public sealed class SkillAttributeGene : AttributeGeneBase
{
    /// <summary>此节点登记到统一属性组件的属性编号。</summary>
    public override GeneAttributeType Attribute { get; }

    /// <summary>是否在本节点的整段寿命内从初值匀速恢复到零。</summary>
    private readonly bool decay;

    /// <summary>当前已经登记的属性量，移除只撤销这部分。</summary>
    private double applied;

    /// <summary>建立固定或随寿命线性衰减的属性节点。</summary>
    /// <param name="attribute">该节点唯一修正的属性。</param>
    /// <param name="decay">为真时下调护体加成，为假时在整段寿命保持。</param>
    public SkillAttributeGene(GeneAttributeType attribute, bool decay = false)
    {
        Attribute = attribute;
        this.decay = decay;
    }

    /// <summary>登记当前层数对应的完整初值。</summary>
    /// <param name="role">实际承载属性变化的实体。</param>
    /// <param name="args">挂载不需要额外命中数据。</param>
    /// <returns>已经应用本节点时返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        applied = Value * StackCount;
        role.AttrComp.ChangeAttribute(Attribute, applied, ValueType);
        return 1;
    }

    /// <summary>每步将防御加成调整成当前寿命对应的数值。</summary>
    /// <param name="milliseconds">这一帧落在有效寿命之内的毫秒数。</param>
    /// <param name="role">接收调整的同一个属性宿主。</param>
    public override void AdvanceTime(long milliseconds, IGeneOwner role)
    {
        if (!decay) return;
        // GeneRuntime 在本回调之后才扣除有效毫秒，先求本帧结束时的剩余比例。
        double remainingFraction = (RemainingMilliseconds - milliseconds) / (DurationSeconds * 1000.0);
        // 以完整初值为参照，每帧只把相对于上一帧的差量写入属性组件。
        double next = Value * remainingFraction;
        role.AttrComp.ChangeAttribute(Attribute, next - applied, ValueType);
        applied = next;
    }

    /// <summary>到期、驱散或刷新时撤回尚存的属性量。</summary>
    /// <param name="role">此前登记过此节点的实体。</param>
    public override void OnRemove(IGeneOwner role)
    {
        role.AttrComp.ChangeAttribute(Attribute, -applied, ValueType);
        applied = 0;
    }
}
