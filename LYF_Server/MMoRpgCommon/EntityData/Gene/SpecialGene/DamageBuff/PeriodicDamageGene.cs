/// <summary>按毫秒间隔对宿主产生持续伤害的具体运行节点。</summary>
public class PeriodicDamageGene : DamageBuffGeneBase
{
    /// <summary>直接执行本节点的资源效果，不创建中转结果。</summary>
    /// <param name="role">实际承受效果的角色或怪物。</param>
    /// <param name="args">本资源效果不依赖额外事件参数。</param>
    /// <returns>完成资源操作后返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        // 本次伤害由节点自身计算承伤倍率，属性组件只处理资源边界与归因。
        int amount = ResolveAmount(role.AttrComp.MaxHp);
        amount = System.Math.Max(0, (int)(amount
            * role.AttrComp.CalculateAttribute(GeneAttributeType.DamageTaken, 100) / 100));
        role.AttrComp.ChangeHp(-amount, FirerId);
        return 1;
    }

}
