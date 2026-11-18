/// <summary>两端真实属性组件提供的属性操作；组件已继承各自生命周期基类，使用接口保留这些继承关系。</summary>
public interface IAttributeComponent
{
    /// <summary>包含已生效基因修正的最大生命值。</summary>
    int MaxHp { get; }
    /// <summary>包含已生效基因修正的最大法力值。</summary>
    int MaxMp { get; }
    /// <summary>当前治疗增益，单位百分点，10 表示额外增加 10%。</summary>
    double HealBonus { get; }
    /// <summary>按各来源计数合成，并处理霸体冲突后的状态位。</summary>
    GeneState States { get; }
    /// <summary>在属性组件中增加或撤销修正；同一节点可以调用多次修改不同属性。</summary>
    /// <param name="attribute">被修改的属性。</param>
    /// <param name="delta">本次增加的点数或百分点；负数表示减少。</param>
    /// <param name="valueType">0 为固定值，1 为相对基础值的百分数。</param>
    void ChangeAttribute(GeneAttributeType attribute, double delta, int valueType);
    /// <summary>使用已经执行的修正计算最终值，不再从基因节点抽取数值。</summary>
    /// <param name="attribute">读取的属性。</param>
    /// <param name="baseValue">不含基因修正的基础值。</param>
    /// <returns>基础值应用当前修正后的结果。</returns>
    double CalculateAttribute(GeneAttributeType attribute, double baseValue);
    /// <summary>按来源数量增加或撤销状态，避免一个来源移除后清掉其他来源。</summary>
    /// <param name="state">本次修改的状态位组合。</param>
    /// <param name="delta">添加填 1，撤销填 -1。</param>
    void ChangeState(GeneState state, int delta);
    /// <summary>直接修改当前生命，属性组件负责上下限、死亡和共享生命处理。</summary>
    /// <param name="delta">本次生命增量；伤害为负，治疗为正。</param>
    /// <param name="firerId">实际施加角色编号，用于死亡和仇恨归因。</param>
    void ChangeHp(int delta, ulong firerId);
    /// <summary>直接修改当前法力，并由属性组件限制资源范围。</summary>
    /// <param name="delta">本次法力增量。</param>
    void ChangeMp(int delta);
    /// <summary>当前怒气点数，范围 0 至 MaxRage，权威值由真实组件保存。</summary>
    int CurrRage { get; }
    /// <summary>怒气上限点数，使用资源默认值，不做基因复算。</summary>
    int MaxRage { get; }
    /// <summary>当前真气点数，范围 0 至 MaxTrueQi，权威值由真实组件保存。</summary>
    int CurrTrueQi { get; }
    /// <summary>真气上限点数，使用资源默认值，不做基因复算。</summary>
    int MaxTrueQi { get; }
    /// <summary>消耗怒气的权威入口；余额不足时不扣减并返回失败，避免白扣。</summary>
    /// <param name="amount">本次要消耗的怒气点数。</param>
    /// <returns>足额扣减时返回 true，否则返回 false。</returns>
    bool TryConsumeRage(int amount);
    /// <summary>消耗真气的权威入口；余额不足时不扣减并返回失败，避免白扣。</summary>
    /// <param name="amount">本次要消耗的真气点数。</param>
    /// <returns>足额扣减时返回 true，否则返回 false。</returns>
    bool TryConsumeTrueQi(int amount);
    /// <summary>恢复怒气，按 0 至 MaxRage 裁切，过大增量不会溢出。</summary>
    /// <param name="amount">本次恢复的怒气点数。</param>
    void RestoreRage(int amount);
    /// <summary>恢复真气，按 0 至 MaxTrueQi 裁切，过大增量不会溢出。</summary>
    /// <param name="amount">本次恢复的真气点数。</param>
    void RestoreTrueQi(int amount);
}
