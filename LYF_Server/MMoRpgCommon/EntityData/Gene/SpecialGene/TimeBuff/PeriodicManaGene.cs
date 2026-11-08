/// <summary>按毫秒间隔产生法力恢复的具体运行节点。</summary>
public class PeriodicManaGene : TimeBuffGeneBase
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

    /// <summary>周期结果交由宿主执行法力恢复。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.PeriodicMana; } }
}
