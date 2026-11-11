/// <summary>一次伤害结算前的真实双方资源快照；由权威战斗结算方填写，基因节点只读。</summary>
public struct GeneDamageContext
{
    /// <summary>攻击者当前生命，单位生命点。</summary>
    public int AttackerHp { get; private set; }
    /// <summary>攻击者生命上限，单位生命点；无效时按满血处理。</summary>
    public int AttackerMaxHp { get; private set; }
    /// <summary>目标当前生命，取本次扣血之前的值。</summary>
    public int TargetHp { get; private set; }
    /// <summary>目标生命上限，单位生命点；无效时按满血处理。</summary>
    public int TargetMaxHp { get; private set; }
    /// <summary>目标是否为首领实体，取真实实体类别位，不看名称。</summary>
    public bool TargetIsBoss { get; private set; }
    /// <summary>本次命中是否来自目标背后，取客户端同款朝向与向量的点积判定。</summary>
    public bool TargetBehind { get; private set; }

    /// <summary>保存本次结算前的真实数值，不做任何估算或补零；不含背面判定。</summary>
    /// <param name="attackerHp">攻击者当前生命。</param>
    /// <param name="attackerMaxHp">攻击者生命上限。</param>
    /// <param name="targetHp">目标本次扣血前的生命。</param>
    /// <param name="targetMaxHp">目标生命上限。</param>
    /// <param name="targetIsBoss">目标是否为首领实体。</param>
    public GeneDamageContext(int attackerHp, int attackerMaxHp, int targetHp, int targetMaxHp, bool targetIsBoss)
        : this(attackerHp, attackerMaxHp, targetHp, targetMaxHp, targetIsBoss, false) { }

    /// <summary>保存本次结算前的真实数值与背面判定。</summary>
    /// <param name="attackerHp">攻击者当前生命。</param>
    /// <param name="attackerMaxHp">攻击者生命上限。</param>
    /// <param name="targetHp">目标本次扣血前的生命。</param>
    /// <param name="targetMaxHp">目标生命上限。</param>
    /// <param name="targetIsBoss">目标是否为首领实体。</param>
    /// <param name="targetBehind">本次命中是否来自目标背后。</param>
    public GeneDamageContext(int attackerHp, int attackerMaxHp, int targetHp, int targetMaxHp, bool targetIsBoss, bool targetBehind)
    {
        AttackerHp = attackerHp;
        AttackerMaxHp = attackerMaxHp;
        TargetHp = targetHp;
        TargetMaxHp = targetMaxHp;
        TargetIsBoss = targetIsBoss;
        TargetBehind = targetBehind;
    }

    /// <summary>目标当前生命占上限的百分数；上限缺失时按满血处理，避免除零。</summary>
    public double TargetHpPercent
    {
        get
        {
            if (TargetMaxHp <= 0) return 100;
            return TargetHp * 100.0 / TargetMaxHp;
        }
    }

    /// <summary>攻击者已损失生命占上限的百分数；上限缺失时按未损失处理。</summary>
    public double AttackerLostHpPercent
    {
        get
        {
            if (AttackerMaxHp <= 0) return 0;
            int lost = AttackerMaxHp - AttackerHp;
            if (lost <= 0) return 0;
            return lost * 100.0 / AttackerMaxHp;
        }
    }
}
