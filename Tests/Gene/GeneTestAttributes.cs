/// <summary>测试专用属性组件；记录真实 OnCall 的资源修改，不在业务模块保留测试计数。</summary>
public class GeneTestAttributes : IAttributeComponent
{
    /// <summary>使用正式公共实现保存节点已经施加的修正。</summary>
    private readonly AttributeModifiers modifiers = new AttributeModifiers();
    /// <summary>测试角色的当前生命，默认留出治疗空间。</summary>
    public int Hp = 500;
    /// <summary>测试角色的当前法力。</summary>
    public int Mp;
    /// <summary>本测试收到的生命与法力操作总次数。</summary>
    public int Calls;
    /// <summary>最近一次节点请求的生命增量。</summary>
    public int LastHpDelta;
    /// <summary>最近一次节点请求的法力增量。</summary>
    public int LastMpDelta;
    /// <summary>最近一次生命操作保留的实际施加角色。</summary>
    public ulong LastFirerId;
    /// <summary>测试基础生命上限为 1000，并应用节点实际施加的修正。</summary>
    public int MaxHp { get { return (int)CalculateAttribute(GeneAttributeType.MaxHp, 1000); } }
    /// <summary>测试基础法力上限为 100。</summary>
    public int MaxMp { get { return (int)CalculateAttribute(GeneAttributeType.MaxMp, 100); } }
    /// <summary>测试基础治疗增益为零。</summary>
    public double HealBonus { get { return CalculateAttribute(GeneAttributeType.HealBonus, 0); } }
    /// <summary>由正式状态计数逻辑合成的有效状态。</summary>
    public GeneState States { get { return modifiers.States; } }
    /// <summary>接收具体节点实际执行的属性修改。</summary>
    /// <param name="attribute">被修改的属性。</param>
    /// <param name="delta">此次增量。</param>
    /// <param name="valueType">固定或百分比类别。</param>
    public void ChangeAttribute(GeneAttributeType attribute, double delta, int valueType) { modifiers.ChangeAttribute(attribute, delta, valueType); }
    /// <summary>读取实际施加后的属性。</summary>
    /// <param name="attribute">被读取的属性。</param>
    /// <param name="baseValue">测试基础值。</param>
    /// <returns>真实公共修正实现的计算结果。</returns>
    public double CalculateAttribute(GeneAttributeType attribute, double baseValue) { return modifiers.CalculateAttribute(attribute, baseValue); }
    /// <summary>接收具体状态节点的增加或撤销。</summary>
    /// <param name="state">当前状态位。</param>
    /// <param name="delta">来源数增量。</param>
    public void ChangeState(GeneState state, int delta) { modifiers.ChangeState(state, delta); }
    /// <summary>记录并执行节点直接请求的生命变化。</summary>
    /// <param name="delta">此次生命增量。</param>
    /// <param name="firerId">实际施加角色编号。</param>
    public void ChangeHp(int delta, ulong firerId)
    {
        Calls++;
        LastHpDelta = delta;
        LastFirerId = firerId;
        Hp = System.Math.Max(0, System.Math.Min(MaxHp, Hp + delta));
    }
    /// <summary>记录并执行节点直接请求的法力变化。</summary>
    /// <param name="delta">此次法力增量。</param>
    public void ChangeMp(int delta)
    {
        Calls++;
        LastMpDelta = delta;
        Mp = System.Math.Max(0, System.Math.Min(MaxMp, Mp + delta));
    }
}
