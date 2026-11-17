/// <summary>建立相邻三角形关系时临时保存的一条边，记录所属三角形及两个端点。</summary>
internal sealed class MNavigationEdge
{
    /// <summary>该边所属三角形的零起始编号。</summary>
    public int Triangle;
    /// <summary>完全重合的顶点共用编号后，两个端点中较小的编号，用于精确共享边查找。</summary>
    public int VertexA;
    /// <summary>完全重合的顶点共用编号后，两个端点中较大的编号，用于区分同起点的边。</summary>
    public int VertexB;
    /// <summary>边第一端点的世界坐标。</summary>
    public MNavigationPoint A;
    /// <summary>边第二端点的世界坐标。</summary>
    public MNavigationPoint B;
    /// <summary>是否已通过完全一致的端点找到邻边；其余边继续检测分块接缝。</summary>
    public bool Connected;
}
