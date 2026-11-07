using System.Collections.Generic;

/// <summary>一个来源施加到实体上的基因；同编号不同来源独立保存时间和层数。</summary>
public class GeneInstance
{
    /// <summary>父定义，决定基因编号、适用场景、寿命与叠加规则。</summary>
    public cfg.Gene Definition { get; private set; }
    /// <summary>施加者或装备实例编号，0 表示没有独立来源。</summary>
    public ulong SourceId { get; private set; }
    /// <summary>当前层数，至少一层；同来源重复施加受 MaxStack 限制。</summary>
    public int StackCount { get; internal set; }
    /// <summary>剩余毫秒数，-1 表示常驻，0 表示已经到期。</summary>
    public long RemainingMilliseconds { get; internal set; }
    /// <summary>已激活节点的只读列表，共享层数和寿命；触发节点另持有待触发子树。</summary>
    public IReadOnlyList<GeneEffect> Effects { get; private set; }

    /// <summary>建立首层实例，并从秒制配置初始化毫秒寿命。</summary>
    /// <param name="definition">本次添加的父基因配置。</param>
    /// <param name="sourceId">来源实体或装备的运行时编号。</param>
    /// <param name="effects">已解析的活跃节点，之后不改变列表成员。</param>
    public GeneInstance(cfg.Gene definition, ulong sourceId, List<GeneEffect> effects)
    {
        Definition = definition;
        SourceId = sourceId;
        StackCount = 1;
        Effects = effects.AsReadOnly();
        RefreshDuration();
    }

    /// <summary>重复施加时刷新父寿命；保留周期进度，避免刷新导致持续效果永不触发。</summary>
    public virtual void RefreshDuration()
    {
        RemainingMilliseconds = -1;
        if (Definition.GeneTime > 0)
        {
            RemainingMilliseconds = (long)Definition.GeneTime * 1000;
        }
    }
}
