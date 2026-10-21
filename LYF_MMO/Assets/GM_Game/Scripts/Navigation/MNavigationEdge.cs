/// <summary>邻接构建期间保存的三角形有向边；地图初始化完成后不再持有。</summary>
internal sealed class MNavigationEdge
{
    /// <summary>该边所属三角形的零起始编号。</summary>
    public int Triangle;
    /// <summary>焊接后较小的顶点编号，用于精确共享边查找。</summary>
    public int VertexA;
    /// <summary>焊接后较大的顶点编号，用于区分同起点的边。</summary>
    public int VertexB;
    /// <summary>边第一端点的世界坐标。</summary>
    public MNavigationPoint A;
    /// <summary>边第二端点的世界坐标。</summary>
    public MNavigationPoint B;
    /// <summary>是否已通过完全一致的端点找到邻边；其余边继续检测分块接缝。</summary>
    public bool Connected;
}
