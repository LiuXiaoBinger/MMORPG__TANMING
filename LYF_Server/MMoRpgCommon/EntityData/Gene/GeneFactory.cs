using System;

/// <summary>配置转换边界的抽象工厂；公共运行模块仅依赖运行节点，不引用配置程序集。</summary>
public abstract class GeneFactory
{
    /// <summary>按编号创建一棵独立运行树，具体工厂负责配置类型映射和必要字段转换。</summary>
    /// <param name="geneId">要创建的配置编号。</param>
    /// <param name="value">仅覆盖根节点数值，子节点使用各自配置数值。</param>
    /// <returns>拥有独立可变状态的根节点。</returns>
    public abstract GeneBase CreateGene(int geneId, float? value = null);
    /// <summary>配置和协议边界共同使用的具体类型工厂，不创建配置包装节点。</summary>
    /// <param name="effect">当前实际效果类别。</param>
    /// <param name="attribute">属性节点负责的具体属性；其他类别使用 None。</param>
    /// <returns>尚未挂载或执行的独立具体节点。</returns>
    public static GeneBase CreateNode(GeneEffectType effect, GeneAttributeType attribute = GeneAttributeType.None)
    {
        switch (effect)
        {
            case GeneEffectType.Attribute: return CreateAttribute(attribute);
            case GeneEffectType.PeriodicDamage: return new PeriodicDamageGene();
            case GeneEffectType.PeriodicHeal: return new PeriodicHealGene();
            case GeneEffectType.PeriodicMana: return new PeriodicManaGene();
            case GeneEffectType.State: return new StateGene();
            case GeneEffectType.Composite: return new CompositeGene();
            case GeneEffectType.Event: return new EventGene();
            case GeneEffectType.Count: return new CountGene();
            case GeneEffectType.InstantHeal: return new CurrHPGene();
            case GeneEffectType.InstantMana: return new CurrMPGene();
            case GeneEffectType.TargetHighHpDamage: return new TargetHighHpDamageGene();
            case GeneEffectType.TargetLowHpDamage: return new TargetLowHpDamageGene();
            case GeneEffectType.SelfHpLostDamage: return new SelfHpLostDamageGene();
            case GeneEffectType.TargetKindDamage: return new TargetKindDamageGene();
            case GeneEffectType.DamageHeal: return new DamageHealGene();
            case GeneEffectType.AttackerKindReduce: return new AttackerKindReduceGene();
            case GeneEffectType.TargetBehindDamage: return new TargetBehindDamageGene();
            case GeneEffectType.SkillCooldownReset: return new SkillCooldownResetGene();
            default: throw new InvalidOperationException("未知基因效果类型：" + effect);
        }
    }

    /// <summary>复用现有具体属性类，类型本身固定它所修正的属性。</summary>
    /// <param name="attribute">表内合法的属性编号。</param>
    /// <returns>对应属性的独立运行节点。</returns>
    public static AttributeGeneBase CreateAttribute(GeneAttributeType attribute)
    {
        switch (attribute)
        {
            case GeneAttributeType.MaxHp: return new MaxHPGene();
            case GeneAttributeType.MaxMp: return new MaxMPGene();
            case GeneAttributeType.AtkExternalMin: return new AtkExternalMinGene();
            case GeneAttributeType.AtkExternalMax: return new AtkExternalMaxGene();
            case GeneAttributeType.AtkInternalMin: return new AtkInternalMinGene();
            case GeneAttributeType.AtkInternalMax: return new AtkInternalMaxGene();
            case GeneAttributeType.DefExternal: return new DefExternalGene();
            case GeneAttributeType.DefExternalDelta: return new DefExternalDeltaGene();
            case GeneAttributeType.DefInternal: return new DefInternalGene();
            case GeneAttributeType.DefInternalDelta: return new DefInternalDeltaGene();
            case GeneAttributeType.HitPoint: return new HitPointGene();
            case GeneAttributeType.BlockPoint: return new BlockPointGene();
            case GeneAttributeType.ZhenJi: return new ZhenJiGene();
            case GeneAttributeType.WuGu: return new WuGuGene();
            case GeneAttributeType.CritPoint: return new CritPointGene();
            case GeneAttributeType.CritResistPoint: return new CritResistPointGene();
            case GeneAttributeType.CritDamage: return new CritDamageGene();
            case GeneAttributeType.CritDamageReduce: return new CritDamageReduceGene();
            case GeneAttributeType.HealStrength: return new HealStrengthGene();
            case GeneAttributeType.HealBonus: return new HealBonusGene();
            case GeneAttributeType.ZhuXin: return new ZhuXinGene();
            case GeneAttributeType.JianRen: return new JianRenGene();
            case GeneAttributeType.Qi: return new QiGene();
            case GeneAttributeType.MoveSpeed: return new MoveSpeedGene();
            case GeneAttributeType.DamageTaken: return new DamageTakenGene();
            case GeneAttributeType.HealingReceived: return new HealingReceivedGene();
            default: throw new InvalidOperationException("未知基因属性编号：" + attribute);
        }
    }
}
