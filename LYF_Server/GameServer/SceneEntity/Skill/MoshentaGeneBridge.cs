/// <summary>将本副本的属性和状态直接创建为公共基因节点，交给现有容器管理。</summary>
internal static class MoshentaGeneBridge
{
    /// <summary>挂载原效果对应的具体节点，不构造或修改 Luban 配置。</summary>
    /// <param name="runtime">实际宿主的基因容器。</param>
    /// <param name="buff">原副本效果实例，负责这些叶子的生命周期。</param>
    /// <param name="slot">原效果内唯一的叶子槽。</param>
    /// <param name="attribute">需要修正的本项目属性类型。</param>
    /// <param name="value">固定点数或百分数。</param>
    /// <param name="percent">数值是否表示基础值的百分比。</param>
    /// <param name="state">需要施加的动作限制；None 表示属性基因。</param>
    internal static void Add(GeneRuntime runtime, MoshentaBuffInstance buff, int slot, GeneAttributeType attribute, float value, bool percent, GeneState state)
    {
        // 编号仍区分原效果和叶子槽，仅用于当前运行集合，不写入任何配置索引。
        int id = -1000000000 - (buff.Id % 10000000) * 16 - slot;
        // 类型决定节点行为；属性与状态均直接使用 Common 中的具体基因。
        GeneBase gene;
        if (state != GeneState.None) gene = new StateGene { States = state };
        else gene = GeneFactory.CreateAttribute(attribute);
        gene.GeneID = id;
        gene.Value = value;
        if (percent) gene.ValueType = 1;
        // 原副本效果负责寿命，默认常驻，移除时通过保存的原节点引用精确卸载。
        ulong sourceId = 0;
        if (buff.Caster != null) sourceId = buff.Caster.UnitId;
        buff.Genes.Add(runtime.AddGene(gene, sourceId, slot, sourceId));
    }
}
