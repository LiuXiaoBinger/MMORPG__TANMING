/// <summary>当前法力基因，施加时立即发布恢复结果，由宿主按资源上限裁切。</summary>
public class CurrMPGene : InstantResourceGeneBase
{
    /// <summary>直接执行本节点的资源效果，不创建中转结果。</summary>
    /// <param name="role">实际承受效果的角色或怪物。</param>
    /// <param name="args">本资源效果不依赖额外事件参数。</param>
    /// <returns>完成资源操作后返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        // 百分比回蓝使用法力上限，固定数值直接按层数结算。
        int amount = ResolveAmount(role.AttrComp.MaxMp);
        role.AttrComp.ChangeMp(amount);
        return 1;
    }

    /// <summary>当前法力变更使用即时恢复分类。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.InstantMana; } }
}
