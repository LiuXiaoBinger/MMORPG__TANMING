/// <summary>周期基因产生的一次强类型结算输入；不包含协议或持久化数据。</summary>
public struct GenePulse
{
    /// <summary>父基因编号，用于定位本次周期效果的来源配置。</summary>
    public int GeneId { get; private set; }
    /// <summary>施加者或装备实例编号，沿用父基因的来源。</summary>
    public ulong SourceId { get; private set; }
    /// <summary>施加该效果的角色编号；装备 UID 仅保存在 SourceId。</summary>
    public ulong FirerId { get; private set; }
    /// <summary>本次应结算生命伤害、生命恢复还是法力恢复。</summary>
    public GeneEffectType EffectType { get; private set; }
    /// <summary>包含层数后的数值；伤害和恢复均为正数。</summary>
    public float Value { get; private set; }
    /// <summary>0 为固定点数，1 为生命或法力上限的百分数，10 表示 10%。</summary>
    public int ValueType { get; private set; }

    /// <summary>复制本次脉冲所需的配置和运行数据，避免后续层数变化修改已发出的结果。</summary>
    /// <param name="instance">触发效果的父实例。</param>
    /// <param name="effect">本次达到周期的叶子效果。</param>
    public GenePulse(GeneInstance instance, GeneEffect effect)
    {
        GeneId = instance.Definition.Id;
        SourceId = instance.SourceId;
        FirerId = instance.FirerId;
        EffectType = (GeneEffectType)effect.Definition.EffectType;
        Value = effect.Value * instance.StackCount;
        ValueType = effect.Definition.ValueType;
    }

    /// <summary>把脉冲换算为资源点数；百分比生命伤害及治疗均以目标生命上限为基数。</summary>
    /// <param name="maxHp">结算时目标的生命上限。</param>
    /// <param name="maxMp">结算时目标的法力上限。</param>
    /// <returns>未应用承伤或受疗修正的非负资源点数，向零取整。</returns>
    public int ResolveAmount(int maxHp, int maxMp)
    {
        if (ValueType == 0)
        {
            return (int)Value;
        }
        // 持续回蓝以法力上限为基数，另外两种周期效果使用生命上限。
        int maximum = maxHp;
        if (EffectType == GeneEffectType.PeriodicMana || EffectType == GeneEffectType.InstantMana)
        {
            maximum = maxMp;
        }
        return (int)(maximum * (double)Value / 100.0);
    }
}
