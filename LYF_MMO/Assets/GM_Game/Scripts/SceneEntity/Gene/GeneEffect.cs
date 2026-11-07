using System.Collections.Generic;

/// <summary>实例中的效果节点，保留关联子节点的独立周期及触发次数。</summary>
public class GeneEffect
{
    /// <summary>来自 Luban 的只读定义；未触发的关联效果由根实例控制寿命。</summary>
    public cfg.Gene Definition { get; private set; }
    /// <summary>本次应用采用的每层数值，可由装备或技能调用方覆盖表内默认值。</summary>
    public float Value { get; internal set; }
    /// <summary>距上一次周期效果发生经过的毫秒数；刷新寿命时不重置。</summary>
    internal long ElapsedMilliseconds;
    /// <summary>当前节点关联的完整子树；事件和次数节点触发前不激活其子项。</summary>
    public IReadOnlyList<GeneEffect> Children { get; private set; }
    /// <summary>组合中的直接父节点，用于沿整条关联路径检查场景条件；根节点为 null。</summary>
    public GeneEffect Parent { get; private set; }
    /// <summary>次数节点尚可触发的次数，其他类型为 0；与叠加层数独立。</summary>
    public int RemainingTriggers { get; internal set; }

    /// <summary>创建一个与父基因共同存亡的效果。</summary>
    /// <param name="definition">当前节点的任意类型基因定义。</param>
    /// <param name="value">每层效果数值，百分比时 10 表示 10%。</param>
    /// <param name="children">已经建立的关联节点，后续只改变运行值，不改变列表成员。</param>
    public GeneEffect(cfg.Gene definition, float value, List<GeneEffect> children)
    {
        Definition = definition;
        Value = value;
        Children = children.AsReadOnly();
        RemainingTriggers = definition.TriggerCount;
        // 子项与父项仍为原运行对象，场景查询不会丢失中间组合的约束。
        for (int i = 0; i < children.Count; i++) children[i].Parent = this;
    }

    /// <summary>重复施加时恢复当前触发节点及其关联次数，不改变已有周期进度。</summary>
    public virtual void ResetTriggers()
    {
        RemainingTriggers = Definition.TriggerCount;
        // 按配置槽位顺序恢复完整子树中的次数节点。
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].ResetTriggers();
        }
    }
}
