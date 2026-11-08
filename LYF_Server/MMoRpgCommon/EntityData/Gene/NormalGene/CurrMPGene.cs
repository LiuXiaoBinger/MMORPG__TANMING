/// <summary>当前法力基因，施加时立即发布恢复结果，由宿主按资源上限裁切。</summary>
public class CurrMPGene : InstantResourceGeneBase
{
    /// <summary>当前法力变更使用即时恢复分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.InstantMana; } }
}
