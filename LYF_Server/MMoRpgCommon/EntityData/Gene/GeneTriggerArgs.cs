/// <summary>一次真实战斗事件的强类型参数；由权威结算方提供命中结果与对方基因容器。</summary>
public struct GeneTriggerArgs
{
    /// <summary>当前已经发生的命中、受击、施法或击败事件。</summary>
    public GeneEventType Event { get; private set; }
    /// <summary>事件对方的运行容器；没有对方时为 null，目标类效果不能触发。</summary>
    public GeneRuntime TargetRuntime { get; private set; }
    /// <summary>产生事件的技能配置编号；没有关联技能时为 0。</summary>
    public int SkillId { get; private set; }
    /// <summary>本次实际扣除的生命点数，已经按目标剩余生命裁切。</summary>
    public int Damage { get; private set; }
    /// <summary>权威伤害判定是否暴击；没有暴击判定的事件为 false。</summary>
    public bool IsCritical { get; private set; }
    /// <summary>尝试刷新本次技能自身冷却的强类型回调；只由成功提交的普通技能提供，其他事件为空。</summary>
    public System.Func<int, bool> TryResetSkillCooldown { get; private set; }

    /// <summary>建立一次事件，不复制目标容器或推测战斗结果。</summary>
    /// <param name="geneEvent">宿主已经发生的事件。</param>
    /// <param name="targetRuntime">对方实体的原运行容器，允许为空。</param>
    /// <param name="skillId">实际施放技能编号。</param>
    /// <param name="damage">本次实际伤害，单位生命点。</param>
    /// <param name="isCritical">权威暴击结果。</param>
    public GeneTriggerArgs(GeneEventType geneEvent, GeneRuntime targetRuntime = null, int skillId = 0, int damage = 0, bool isCritical = false)
    {
        Event = geneEvent;
        TargetRuntime = targetRuntime;
        SkillId = skillId;
        Damage = damage;
        IsCritical = isCritical;
        TryResetSkillCooldown = null;
    }

    /// <summary>建立一次带冷却刷新入口的施法事件；只有真实提交成功的普通技能使用本重载。</summary>
    /// <param name="geneEvent">宿主已经发生的事件。</param>
    /// <param name="targetRuntime">对方实体的原运行容器，允许为空。</param>
    /// <param name="skillId">实际施放技能编号。</param>
    /// <param name="damage">本次实际伤害，单位生命点。</param>
    /// <param name="isCritical">权威暴击结果。</param>
    /// <param name="tryResetSkillCooldown">技能组件提供的单技能冷却刷新回调，允许为空。</param>
    public GeneTriggerArgs(GeneEventType geneEvent, GeneRuntime targetRuntime, int skillId, int damage, bool isCritical,
        System.Func<int, bool> tryResetSkillCooldown)
    {
        Event = geneEvent;
        TargetRuntime = targetRuntime;
        SkillId = skillId;
        Damage = damage;
        IsCritical = isCritical;
        TryResetSkillCooldown = tryResetSkillCooldown;
    }
}
