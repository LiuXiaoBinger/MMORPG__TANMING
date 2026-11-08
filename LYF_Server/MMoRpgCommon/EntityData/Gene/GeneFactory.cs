using System;

/// <summary>配置转换边界的抽象工厂；公共运行模块仅依赖运行节点，不引用配置程序集。</summary>
public abstract class GeneFactory
{
    /// <summary>按编号创建一棵独立运行树，具体工厂负责配置类型映射和必要字段转换。</summary>
    /// <param name="geneId">要创建的配置编号。</param>
    /// <param name="value">仅覆盖根节点数值，子节点使用各自配置数值。</param>
    /// <returns>拥有独立可变状态的根节点。</returns>
    public abstract GeneBase CreateGene(int geneId, float? value = null);
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
