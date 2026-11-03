using System.Collections.Generic;

/// <summary>运行时三角形及其可穿越共享边；顶点直接引用公共地图的世界坐标值。</summary>
internal sealed class MNavigationTriangle
{
    /// <summary>原网格第一个顶点的世界位置。</summary>
    public MNavigationPoint A;
    /// <summary>原网格第二个顶点的世界位置。</summary>
    public MNavigationPoint B;
    /// <summary>原网格第三个顶点的世界位置。</summary>
    public MNavigationPoint C;
    /// <summary>三个顶点的平均位置，用来判断相邻三角形在公共边哪一侧及行进方向。</summary>
    public MNavigationPoint Center;
    /// <summary>三角形最小X，用于先排除不在查询范围内的三角形。</summary>
    public float MinX;
    /// <summary>三角形最大X，单位与世界坐标一致。</summary>
    public float MaxX;
    /// <summary>三角形最小Y，用于排除相距搜索高度之外的楼层。</summary>
    public float MinY;
    /// <summary>三角形最大Y，保留坡面高度范围。</summary>
    public float MaxY;
    /// <summary>三角形最小Z，用于先排除不在查询范围内的三角形。</summary>
    public float MinZ;
    /// <summary>三角形最大Z，单位与世界坐标一致。</summary>
    public float MaxZ;
    /// <summary>与相邻三角形的可通过边，仅含存在正长度重叠的连接。</summary>
    public readonly List<MNavigationPortal> Portals = new List<MNavigationPortal>();
}
