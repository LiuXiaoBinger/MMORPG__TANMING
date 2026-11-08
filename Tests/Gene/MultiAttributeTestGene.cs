/// <summary>仅供独立回归的多属性基因，验证一个 OnCall 可以独立执行不止一种属性行为。</summary>
public class MultiAttributeTestGene : GeneBase
{
    /// <summary>类别只用于配置识别，不决定本测试节点具体执行哪几个属性。</summary>
    public override GeneEffectType EffectType { get { return GeneEffectType.Attribute; } }
    /// <summary>一次调用同时增加生命上限和外功攻击，攻击使用独立的两倍计算规则。</summary>
    /// <param name="role">实际接受这些属性修改的测试角色。</param>
    /// <param name="args">此节点不依赖事件参数。</param>
    /// <returns>节点成功执行返回 1。</returns>
    public override int OnCall(IGeneOwner role, GeneTriggerArgs args = default(GeneTriggerArgs))
    {
        role.AttrComp.ChangeAttribute(GeneAttributeType.MaxHp, Value * StackCount, 0);
        role.AttrComp.ChangeAttribute(GeneAttributeType.AtkExternalMin, Value * StackCount * 2, 0);
        return 1;
    }
    /// <summary>本子类自行撤销它实际执行的两项修正。</summary>
    /// <param name="role">此前接受这些修改的角色。</param>
    public override void OnRemove(IGeneOwner role)
    {
        role.AttrComp.ChangeAttribute(GeneAttributeType.MaxHp, -Value * StackCount, 0);
        role.AttrComp.ChangeAttribute(GeneAttributeType.AtkExternalMin, -Value * StackCount * 2, 0);
    }
}
