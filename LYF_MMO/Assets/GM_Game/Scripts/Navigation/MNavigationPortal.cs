/// <summary>两个三角形之间可穿越的共享边段，接缝时仅保留双方实际重叠范围。</summary>
internal sealed class MNavigationPortal
{
    /// <summary>共享边另一侧的三角形零起始编号。</summary>
    public int Triangle;
    /// <summary>共享边段第一端点的世界坐标。</summary>
    public MNavigationPoint A;
    /// <summary>共享边段第二端点的世界坐标。</summary>
    public MNavigationPoint B;
}
