/// <summary>撤下当前角色通过 GM 施加的指定基因。</summary>
internal sealed class DeleteBuffProcessor : CMDProcessor
{
    /// <summary>与 RO 删除 Buff 的命令同名。</summary>
    internal override string Name => "delbuff";

    /// <summary>通过相同的基因和来源移除运行节点，触发原有属性撤销与同步。</summary>
    /// <param name="role">持有基因的在线角色。</param><param name="args">命令名及基因表编号。</param>
    internal override string Process(OnlineRole role, string[] args)
    {
        // 只匹配本人的自施加来源，保留装备等来源的同编号效果。
        int geneId;
        if (args.Length != 2 || !int.TryParse(args[1], out geneId)) return "用法：delbuff <基因ID>";
        if (!role.Gene.Runtime.RemoveGene(geneId, role.GetID())) return "本人没有施加此基因：" + geneId;
        return "已移除基因 " + geneId;
    }
}
