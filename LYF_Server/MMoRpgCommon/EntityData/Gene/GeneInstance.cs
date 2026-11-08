using System.Collections.Generic;

/// <summary>一个来源施加到实体上的基因；同编号不同来源独立保存时间和层数。</summary>
public class GeneInstance
{
    /// <summary>本次施加的根运行节点，持有自己的关联树及类型行为。</summary>
    public GeneBase Root { get; private set; }
    /// <summary>施加者或装备实例编号，0 表示没有独立来源。</summary>
    public ulong SourceId { get; private set; }
    /// <summary>同一装备内的效果槽，0 为普通施加，1 至 9 区分基础六项与实例三词条。</summary>
    public int SourceSlot { get; private set; }
    /// <summary>实际施加角色的编号；与装备来源 UID 分开，用于资源效果归因。</summary>
    public ulong FirerId { get; private set; }
    /// <summary>当前层数，至少一层；同来源重复施加受 MaxStack 限制。</summary>
    public int StackCount { get; internal set; }
    /// <summary>剩余毫秒数，-1 表示常驻，0 表示已经到期。</summary>
    public long RemainingMilliseconds { get; internal set; }
    /// <summary>已激活节点的只读列表，共享层数和寿命；触发节点另持有待触发子树。</summary>
    public IReadOnlyList<GeneBase> Nodes { get; private set; }

    /// <summary>建立首层实例，并从秒制配置初始化毫秒寿命。</summary>
    /// <param name="root">工厂本次创建的独立根运行节点。</param>
    /// <param name="sourceId">来源实体或装备的运行时编号。</param>
    /// <param name="nodes">已解析的活跃节点，之后不改变列表成员。</param>
    /// <param name="sourceSlot">同来源的独立效果槽，普通施加填 0。</param>
    /// <param name="firerId">施加角色编号，不使用装备 UID 代替。</param>
    public GeneInstance(GeneBase root, ulong sourceId, List<GeneBase> nodes, int sourceSlot = 0, ulong firerId = 0)
    {
        Root = root;
        SourceId = sourceId;
        SourceSlot = sourceSlot;
        FirerId = firerId;
        StackCount = 1;
        Nodes = nodes.AsReadOnly();
        RefreshDuration();
    }

    /// <summary>重复施加时刷新父寿命；保留周期进度，避免刷新导致持续效果永不触发。</summary>
    public virtual void RefreshDuration()
    {
        RemainingMilliseconds = -1;
        if (Root.DurationSeconds > 0)
        {
            RemainingMilliseconds = (long)(Root.DurationSeconds * 1000);
        }
    }
}
