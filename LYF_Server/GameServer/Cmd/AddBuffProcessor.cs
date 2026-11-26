/// <summary>将 RO 的自施加 addbuff 接入本项目基因表和宿主运行器。</summary>
internal sealed class AddBuffProcessor : CMDProcessor
{
    /// <summary>RO 文本命令名。</summary>
    internal override string Name => "addbuff";

    /// <summary>由当前角色给自己施加配置中的基因，刷新和叠层由原运行节点决定。</summary>
    /// <param name="role">施加者和承受者为同一个在线角色。</param><param name="args">命令名、基因表编号。</param>
    internal override string Process(OnlineRole role, string[] args)
    {
        // 来自基因表的根编号，未知编号不能传给要求配置存在的工厂。
        int geneId;
        if (args.Length != 2 || !int.TryParse(args[1], out geneId)) return "用法：addbuff <基因ID>";
        if (LubanMgr.Instance.GetGeneInfoById(geneId) == null) return "基因配置不存在：" + geneId;
        // 来源取本人实体编号，与装备 UID 分开，状态变化沿原属性通知下发。
        ulong self = role.GetID();
        // 运行器返回实际生效的根节点，回执据此报告刷新后的层数。
        GeneBase gene = role.Gene.Runtime.AddGene(geneId, self, firerId: self);
        if (gene == null) return "当前场景无法添加此基因：" + geneId;
        return "已施加 " + gene.DisplayName + "（" + geneId + "），层数 " + gene.StackCount;
    }
}
