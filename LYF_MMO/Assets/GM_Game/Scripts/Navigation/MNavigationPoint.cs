/// <summary>公共导航网格的世界坐标，沿用Unity的XYZ轴和世界单位，两端不进行坐标轴转换。</summary>
public struct MNavigationPoint
{
    /// <summary>世界坐标X分量，单位与场景模型一致。</summary>
    public float X;

    /// <summary>世界坐标Y高度，保留桥面、楼层等上下分离的导航面。</summary>
    public float Y;

    /// <summary>世界坐标Z分量，单位与场景模型一致。</summary>
    public float Z;

    /// <summary>从导出时的世界坐标构造一个公共导航顶点。</summary>
    /// <param name="x">世界坐标X分量。</param>
    /// <param name="y">世界坐标Y高度。</param>
    /// <param name="z">世界坐标Z分量。</param>
    public MNavigationPoint(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
