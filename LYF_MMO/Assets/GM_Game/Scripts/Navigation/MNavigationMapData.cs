/// <summary>单个场景导出的公共三角网格；供客户端和纯.NET服务端读取，不包含Unity内部多边形连接或跳跃链接。</summary>
public sealed class MNavigationMapData
{
    /// <summary>导出来源的Unity场景名，用于识别主城或副本地图。</summary>
    public string SceneName = string.Empty;

    /// <summary>烘焙该网格使用的Unity角色类型编号；仅作来源记录，不是场景或服务器实体编号。</summary>
    public int AgentTypeID;

    /// <summary>烘焙结果的世界坐标顶点，保留原顺序及高度，不再次按角色半径收缩。</summary>
    public MNavigationPoint[] Vertices = new MNavigationPoint[0];

    /// <summary>每连续三个元素组成一个三角形，元素为Vertices数组的零起始索引，保留原绕序。</summary>
    public int[] Indices = new int[0];

    /// <summary>每个三角形对应的Unity导航区域编号，长度等于Indices长度除以三。</summary>
    public int[] Areas = new int[0];
}
