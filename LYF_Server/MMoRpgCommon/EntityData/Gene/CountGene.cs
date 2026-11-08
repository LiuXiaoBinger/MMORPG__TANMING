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

    /// <summary>次数节点独立执行事件行为，只在关联效果确实生效后扣除一次。</summary>
    /// <param name="role">收到事件的真实角色。</param>
    /// <param name="args">当前实际战斗事件。</param>
    /// <returns>成功激活的关联节点数。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        // 事件基类保留概率、目标和冷却判断，本类型负责自己的次数消费。
        int activated = base.OnCall(role, args);
        if (activated > 0) RemainingTriggers--;
        return activated;
    }
}
