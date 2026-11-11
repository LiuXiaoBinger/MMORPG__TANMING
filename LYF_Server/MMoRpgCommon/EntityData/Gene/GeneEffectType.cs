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
    InstantMana = 9,
    /// <summary>目标本次扣血前生命高于条件阈值时，按百分点提高本次伤害。</summary>
    TargetHighHpDamage = 10,
    /// <summary>目标本次扣血前生命低于条件阈值时，按百分点提高本次伤害。</summary>
    TargetLowHpDamage = 11,
    /// <summary>攻击者每损失条件阈值个百分点生命，按百分点提高本次伤害。</summary>
    SelfHpLostDamage = 12,
    /// <summary>按目标真实类别提高本次伤害；条件 1 为首领，2 为非首领。</summary>
    TargetKindDamage = 13,
    /// <summary>按攻击命中事件携带的实际伤害百分点即时回复攻击者生命。</summary>
    DamageHeal = 14,
    /// <summary>按伤害来源真实类别减免本次受到的伤害；条件 1 为首领、2 为普通怪。</summary>
    AttackerKindReduce = 15,
    /// <summary>从目标背后攻击时按百分点提高本次伤害，朝向由权威结算方判定。</summary>
    TargetBehindDamage = 16,
    /// <summary>成功提交施法后刷新本次技能自身的剩余冷却，由技能组件提供的权威入口执行。</summary>
    SkillCooldownReset = 17
}
