/// <summary>
/// 记录带伤害效果的装备词条；这里只存伤害数值，不执行结算。
/// </summary>
public abstract class DamageBuffGeneBase : GeneBase
{
    /// <summary>
    /// Buff 伤害数值。
    /// </summary>
    public int DamageValue { get; set; }
}
