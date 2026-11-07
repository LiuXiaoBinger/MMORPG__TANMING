/// <summary>基因表 EffectType 的效果分类；组合只负责装配其引用的叶子效果。</summary>
public enum GeneEffectType
{
    /// <summary>按 AttributeId 对属性施加固定值或百分比修正。</summary>
    Attribute = 0,
    /// <summary>每隔 Interval 毫秒产生一次生命伤害。</summary>
    PeriodicDamage = 1,
    /// <summary>每隔 Interval 毫秒产生一次生命恢复。</summary>
    PeriodicHeal = 2,
    /// <summary>每隔 Interval 毫秒产生一次法力恢复。</summary>
    PeriodicMana = 3,
    /// <summary>在基因存续期间提供 State 指定的状态。</summary>
    State = 4,
    /// <summary>组合 AttachGeneId0 至 AttachGeneId4 的叶子效果，共享父基因寿命和层数。</summary>
    Composite = 5
}
