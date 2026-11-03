using System.Collections.Generic;

/// <summary>供控制器读取的一份刷新记录：是否整页刷新、哪些格子变化、变化来自哪里。</summary>
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

    /// <summary>复制 dirtySlots 中的格子编号，保存 fullRefresh 指定的整页刷新标志和 context 来源；创建的记录总是标记为待处理。</summary>
    public ClientDirtySnapshot(bool fullRefresh, IList<int> dirtySlots,
        ClientChangeContext context)
    {
        IsDirty = true;
        FullRefresh = fullRefresh;
        DirtySlots = new List<int>(dirtySlots);
        Context = context;
    }
}
