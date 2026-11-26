using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

/// <summary>公共地图中的一道门。客户端和区服读取同份地图、门心与双向落点配置。</summary>
public sealed class PublicCityPortal
{
    /// <summary>按地图编号存放本图的所有入口，顺序与策划文件一致。</summary>
    private static readonly Dictionary<int, List<PublicCityPortal>> Doors = new Dictionary<int, List<PublicCityPortal>>();
    /// <summary>地图编号对应客户端 Scenes 目录下的相对地址，不含扩展名。</summary>
    private static readonly Dictionary<int, string> Locations = new Dictionary<int, string>();
    /// <summary>用实际加载的 Unity 场景名识别本地图的入口。</summary>
    private static readonly Dictionary<string, int> SceneIds = new Dictionary<string, int>();
    /// <summary>旧主城南侧的城际入口。</summary>
    public static PublicCityPortal Town => Find(0);
    /// <summary>天空中之城南门的城际入口。</summary>
    public static PublicCityPortal Sky => Find(1);
    /// <summary>这道门所在的公共地图编号。</summary>
    public readonly int MapId;
    /// <summary>地图内入口编号，零沿用早期双城传送包的默认值。</summary>
    public readonly int DoorId;
    /// <summary>门心的世界横坐标，单位米。</summary>
    public readonly float X;
    /// <summary>门口地面的世界高度，单位米。</summary>
    public readonly float Y;
    /// <summary>门心的世界纵坐标，单位米。</summary>
    public readonly float Z;
    /// <summary>从对面进入本图时的落脚点，单位米，沿用角色位置协议格式。</summary>
    public readonly string ArrivalPosition;
    /// <summary>同一条通路另一端的门，包含目标地图与落点。</summary>
    public PublicCityPortal Destination { get; private set; }

    /// <summary>每个进程只在首次查询时登记公共地图和成对的出入口。</summary>
    static PublicCityPortal()
    {
        // 配置随两端共用的 Network 程序集发布。
        XmlDocument config = new XmlDocument();
        using (Stream source = typeof(PublicCityPortal).Assembly.GetManifestResourceStream("PublicCityPortals.xml"))
            config.Load(source);
        // 地图先登记，后续门可直接加入所属地图。
        XmlNodeList maps = config.SelectNodes("/world/maps/map");
        for (int i = 0; i < maps.Count; i++)
        {
            // 同一地图由服务端编号和 Unity 场景资源地址共同标识。
            XmlElement map = (XmlElement)maps[i];
            int id = int.Parse(map.GetAttribute("id"), CultureInfo.InvariantCulture);
            string location = map.GetAttribute("scene");
            Doors.Add(id, new List<PublicCityPortal>());
            Locations.Add(id, location);
            SceneIds.Add(Path.GetFileName(location), id);
        }
        // 每条通路登记两个方向，返回落点由到达的那端决定。
        XmlNodeList passages = config.SelectNodes("/world/passage");
        for (int i = 0; i < passages.Count; i++)
        {
            // XML 注释不参与子节点序号。
            XmlNodeList ends = passages[i].SelectNodes("door");
            PublicCityPortal first = new PublicCityPortal((XmlElement)ends[0]);
            PublicCityPortal second = new PublicCityPortal((XmlElement)ends[1]);
            first.Destination = second;
            second.Destination = first;
        }
    }

    /// <summary>读取一端的门心、落点及本地图入口编号。</summary>
    /// <param name="entry">通路下的一条 door 配置。</param>
    private PublicCityPortal(XmlElement entry)
    {
        MapId = int.Parse(entry.GetAttribute("map"), CultureInfo.InvariantCulture);
        DoorId = int.Parse(entry.GetAttribute("id"), CultureInfo.InvariantCulture);
        X = float.Parse(entry.GetAttribute("x"), CultureInfo.InvariantCulture);
        Y = float.Parse(entry.GetAttribute("y"), CultureInfo.InvariantCulture);
        Z = float.Parse(entry.GetAttribute("z"), CultureInfo.InvariantCulture);
        ArrivalPosition = entry.GetAttribute("arrival");
        Doors[MapId].Add(this);
    }

    /// <summary>取得公共地图使用的 Unity 场景地址，老存档的未知地图沿用主城。</summary>
    /// <param name="mapId">服务端持有的地图编号。</param>
    public static string SceneLocation(int mapId)
    {
        // 配好的公共地图会走各自的场景资产。
        if (Locations.TryGetValue(mapId, out string location)) return location;
        return Locations[0];
    }

    /// <summary>按实际 Unity 场景取得全部可踩入的门，副本没有公共地图入口。</summary>
    /// <param name="sceneName">当前 Unity 场景名，不带文件扩展名。</param>
    public static IReadOnlyList<PublicCityPortal> InScene(string sceneName)
    {
        // 沿地图目录定位，不将副本资源误识别为室内公共地图。
        if (SceneIds.TryGetValue(sceneName, out int id)) return Doors[id];
        return Array.Empty<PublicCityPortal>();
    }

    /// <summary>根据网关绑定的地图和客户端选中的本地门号定位入口。</summary>
    /// <param name="mapId">角色当前的权威地图编号。</param>
    /// <param name="doorId">当前地图内的入口号，旧协议默认使用零号门。</param>
    public static PublicCityPortal Find(int mapId, int doorId = 0)
    {
        // 只在角色所在地图的门中查找。
        if (!Doors.TryGetValue(mapId, out List<PublicCityPortal> entries)) return null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].DoorId == doorId) return entries[i];
        return null;
    }

    /// <summary>查找当前地图直达指定地图的通路，供 GS 按实际坐标复核。</summary>
    /// <param name="mapId">离场角色的地图编号。</param>
    /// <param name="destination">调度服已分配的目标地图编号。</param>
    public static PublicCityPortal ToMap(int mapId, int destination)
    {
        // 双城、教堂和餐馆各有一条通往对端地图的路。
        if (!Doors.TryGetValue(mapId, out List<PublicCityPortal> entries)) return null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Destination.MapId == destination) return entries[i];
        return null;
    }

    /// <summary>用水平圆和门口高度判定角色是否走入这道门。</summary>
    /// <param name="x">角色世界横坐标，米。</param><param name="y">角色高度，米。</param>
    /// <param name="z">角色世界纵坐标，米。</param><param name="radius">入口水平半径，米。</param>
    public bool Contains(float x, float y, float z, float radius)
    {
        // 水平差值分别沿世界 X、Z 轴计算。
        float dx = x - X;
        float dz = z - Z;
        return dx * dx + dz * dz <= radius * radius && Math.Abs(y - Y) <= 2.8f;
    }
}
