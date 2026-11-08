/// <summary>有限次数的事件节点；每次成功产生关联效果后扣除一次，重复施加补满。</summary>
public class CountGene : EventGene
{
    /// <summary>每次首次施加或刷新后恢复的总次数，工厂保证大于零。</summary>
    public int TriggerLimit { get; set; }
    /// <summary>当前节点尚可成功触发的次数，与根叠加层数独立。</summary>
    public int RemainingTriggers { get; private set; }
    /// <summary>有限次数事件的固定分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Count; } }
    /// <summary>最后一次效果已执行后，当前次数节点不再触发。</summary>
    public override bool IsExhausted { get { return RemainingTriggers == 0; } }

    /// <summary>补满本节点并继续恢复嵌套子树，不重置周期或冷却。</summary>
    public override void ResetTriggers()
    {
        RemainingTriggers = TriggerLimit;
        base.ResetTriggers();
    }

    /// <summary>仅在关联项实际施加成功后扣除一次。</summary>
    protected override void OnTriggered() { RemainingTriggers--; }
}
