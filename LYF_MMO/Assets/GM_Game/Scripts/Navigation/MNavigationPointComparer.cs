using System.Collections.Generic;

/// <summary>在纯.NET字典中按世界XYZ精确识别重复顶点，不合并不同高度的导航层。</summary>
internal sealed class MNavigationPointComparer : IEqualityComparer<MNavigationPoint>
{
    /// <summary>比较导出顶点的三个坐标分量。</summary>
    /// <param name="a">第一份世界坐标。</param>
    /// <param name="b">第二份世界坐标。</param>
    public bool Equals(MNavigationPoint a, MNavigationPoint b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    /// <summary>组合三个浮点坐标，避免拼接整数键的异或聚集。</summary>
    /// <param name="point">需要放入顶点或空间桶字典的坐标。</param>
    public int GetHashCode(MNavigationPoint point)
    {
        unchecked
        {
            return (point.X.GetHashCode() * 397 ^ point.Y.GetHashCode()) * 397 ^ point.Z.GetHashCode();
        }
    }
}
