/// <summary>基因表 EffectType 的效果分类；每种类型都允许关联其他类型的基因。</summary>
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
    /// <summary>组合关联基因，支持跨表页和嵌套，共享根基因寿命和层数。</summary>
    Composite = 5,
    /// <summary>收到 TriggerEvent 对应事件后施加关联基因，自身不消耗次数。</summary>
    Event = 6,
    /// <summary>收到匹配事件后施加关联基因，每次消耗一次 TriggerCount。</summary>
    Count = 7,
    /// <summary>施加时立即恢复生命，不等待周期或保留常驻资源实例。</summary>
    InstantHeal = 8,
    /// <summary>施加时立即恢复法力，不等待周期或保留常驻资源实例。</summary>
    InstantMana = 9
}
