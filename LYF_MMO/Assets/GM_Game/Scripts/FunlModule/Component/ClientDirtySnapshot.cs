using System.Collections.Generic;

/// <summary>一次模块脏标记的只读消费快照。</summary>
public sealed class ClientDirtySnapshot
{
    /// <summary>模块是否需要处理。</summary>
    public bool IsDirty { get; private set; }
    /// <summary>是否需要完整重建模块展示。</summary>
    public bool FullRefresh { get; private set; }
    /// <summary>需要局部刷新的格位快照。</summary>
    public IReadOnlyList<int> DirtySlots { get; private set; }
    /// <summary>最近一次变化的来源上下文。</summary>
    public ClientChangeContext Context { get; private set; }

    /// <summary>创建脏标记快照。</summary>
    public ClientDirtySnapshot(bool fullRefresh, IList<int> dirtySlots,
        ClientChangeContext context)
    {
        IsDirty = true;
        FullRefresh = fullRefresh;
        DirtySlots = new List<int>(dirtySlots);
        Context = context;
    }
}
