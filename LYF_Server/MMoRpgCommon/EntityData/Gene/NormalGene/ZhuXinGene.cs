/// <summary>诛心属性值（PVP 增伤）基因，装备词条对该属性的增量记录。</summary>
public class ZhuXinGene : GeneBase
{
    /// <summary>本词条提供的诛心属性值（PVP 增伤）增量；这里只保存数据，不在此计算角色最终属性。</summary>
    public int Value { get; set; }
}
