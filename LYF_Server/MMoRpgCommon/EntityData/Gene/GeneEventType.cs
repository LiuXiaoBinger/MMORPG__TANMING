/// <summary>宿主传入基因模块的战斗事件编号；具体技能和网络层负责后续接入。</summary>
public enum GeneEventType
{
    /// <summary>没有事件；配置中的次数基因填 0 时表示接受任意有效事件。</summary>
    None = 0,
    /// <summary>宿主的攻击命中目标，编号 1。</summary>
    AttackHit = 1,
    /// <summary>宿主受到一次命中，编号 2。</summary>
    BeHit = 2,
    /// <summary>宿主完成一次有效施法，编号 3。</summary>
    SkillCast = 3,
    /// <summary>宿主击败目标，编号 4。</summary>
    Kill = 4,
    /// <summary>命中事件的暴击条件；沿用同次伤害的暴击结果。</summary>
    CriticalAttack = 5,
    /// <summary>治疗招式实际恢复了队友生命，事件目标为队中生命比例最低的存活成员。</summary>
    AllySkillHeal = 6
}
