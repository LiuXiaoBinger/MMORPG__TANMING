using System;

/// <summary>两座公共城市共用的传送门坐标，单位为 Unity 世界米。</summary>
public sealed class PublicCityPortal
{
    /// <summary>主城平台上的门，通往天空中之城。</summary>
    public static readonly PublicCityPortal Town = new PublicCityPortal(0, 62.15f, 18.71f, 93.67f, "62.14934_19.414_80.66898");
    /// <summary>天空中之城入城广场上的门，通往主城。</summary>
    public static readonly PublicCityPortal Sky = new PublicCityPortal(1, 0f, 0.008f, -52.7f, "0_0.08_-65.7");
    /// <summary>所在公共地图的编号。</summary>
    public readonly int MapId;
    /// <summary>门中心的东西向坐标。</summary>
    public readonly float X;
    /// <summary>门底座所在平台的高度。</summary>
    public readonly float Y;
    /// <summary>门中心的南北向坐标。</summary>
    public readonly float Z;
    /// <summary>另一座城传入时的安全落点，使用原有角色位置协议格式。</summary>
    public readonly string ArrivalPosition;
    /// <summary>本扇门通往的另一座公共城市。</summary>
    public PublicCityPortal Destination
    {
        get { if (MapId == 0) return Sky; return Town; }
    }

    /// <summary>登记本城的门和进城落点。</summary>
    /// <param name="mapId">公共地图编号。</param><param name="x">东西坐标。</param>
    /// <param name="y">地面高度。</param><param name="z">南北坐标。</param><param name="arrival">进城出生坐标。</param>
    private PublicCityPortal(int mapId, float x, float y, float z, string arrival)
    {
        MapId = mapId;
        X = x;
        Y = y;
        Z = z;
        ArrivalPosition = arrival;
    }

    /// <summary>按公共地图查找传送门，独立副本没有城际入口。</summary>
    /// <param name="mapId">当前地图编号。</param>
    public static PublicCityPortal Find(int mapId)
    {
        if (mapId == Sky.MapId) return Sky;
        if (mapId == Town.MapId) return Town;
        return null;
    }

    /// <summary>检查角色是否位于门的圆柱范围，同一平面但不同楼层不能触发。</summary>
    /// <param name="x">角色横坐标。</param><param name="y">角色高度。</param>
    /// <param name="z">角色纵坐标。</param><param name="radius">判定半径，单位米。</param>
    public bool Contains(float x, float y, float z, float radius)
    {
        // 角色与门中心在水平面上的两轴距离。
        float dx = x - X;
        float dz = z - Z;
        return dx * dx + dz * dz <= radius * radius && Math.Abs(y - Y) <= 2.8f;
    }
}
