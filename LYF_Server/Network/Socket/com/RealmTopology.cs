using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

/// <summary>一次区服部署的节点地址与公共地图分线，Gate、Control 和 GS 共读同一个文件。</summary>
public sealed class RealmTopology
{
    /// <summary>角色表中已有的逻辑区服编号。</summary>
    public readonly int RealmId;
    /// <summary>本区中心、调度、网关和队伍服务的监听地址。</summary>
    public readonly string Host;
    /// <summary>玩家登录后接入网关的 TCP 端口。</summary>
    public readonly int GatePort;
    /// <summary>网关实时通道监听的 UDP 端口。</summary>
    public readonly int UdpPort;
    /// <summary>存档中心的 TCP 端口。</summary>
    public readonly int CenterPort;
    /// <summary>各区 GS 和网关连接同一存档中心的内网地址。</summary>
    public readonly string CenterHost;
    /// <summary>账号登录入口的监听地址，所有逻辑区共用。</summary>
    public readonly string LoginHost;
    /// <summary>客户端进入选服流程前使用的登录端口。</summary>
    public readonly int LoginPort;
    /// <summary>受部署文件控制的选服展示信息及玩家可达的网关地址。</summary>
    public readonly GameServer PublicGate;
    /// <summary>本区新创建角色的公共地图编号；历史单区配置默认使用地图 1。</summary>
    public readonly int SpawnMap;
    /// <summary>公共配置列出的区服文件绝对路径，供中心服加载选服目录。</summary>
    public readonly List<string> RealmFiles = new List<string>();
    /// <summary>共享部署使用显式的可开区清单；空清单代表尚未配置区服。</summary>
    public readonly bool UsesRealmCatalog;
    /// <summary>本区队伍和频道权威服务的 TCP 端口。</summary>
    public readonly int MasterPort;
    /// <summary>本区交易服的内网地址，可与网关部署在不同主机。</summary>
    public readonly string TradeHost;
    /// <summary>本区交易服监听端口，旧配置缺省或取零时关闭市场入口。</summary>
    public readonly int TradePort;
    /// <summary>区服场景调度的 TCP 端口。</summary>
    public readonly int ControlPort;
    /// <summary>以物理节点编号索引 GS，编号同时用于物品 UID 的实例位。</summary>
    public readonly Dictionary<int, SceneNodeRegistration> Games = new Dictionary<int, SceneNodeRegistration>();
    /// <summary>各张地图的可部署分线，保持配置顺序以选择默认线路。</summary>
    public readonly List<PublicSceneLine> Scenes = new List<PublicSceneLine>();

    /// <summary>配置了弹性扩线的地图人数阈值和总线数上限。</summary>
    public readonly Dictionary<int, MapPolicy> MapPolicies = new Dictionary<int, MapPolicy>();

    /// <summary>同地图所有已开放线均达到警戒人数后允许增开一条线。</summary>
    public sealed class MapPolicy
    {
        /// <summary>触发自动扩线的单线人数。</summary>
        public int Warning;
        /// <summary>自动线路和固定线路合计的最大数量。</summary>
        public int MaxLines;
    }

    /// <summary>启动时读取配置并检查节点和地图线路能唯一定位。</summary>
    /// <param name="path">区服配置文件路径。</param>
    public RealmTopology(string path)
    {
        // 仅读取指定文件；配置文档没有外部资源依赖。
        XmlDocument document = new XmlDocument { XmlResolver = null };
        document.Load(path);
        // 根节点的共用端口对该区的所有 GS 生效。
        XmlElement root = document.DocumentElement;
        RealmId = Number(root, "id", 1, int.MaxValue);
        Host = root.GetAttribute("host");
        SpawnMap = 1;
        if (root.HasAttribute("spawnMap")) SpawnMap = Number(root, "spawnMap", 0, int.MaxValue);
        TradeHost = Host;
        if (root.HasAttribute("tradeHost")) TradeHost = root.GetAttribute("tradeHost");
        if (root.HasAttribute("tradePort")) TradePort = Number(root, "tradePort", 0, 65535);
        GatePort = Number(root, "gatePort", 1, 65535);
        UdpPort = Number(root, "udpPort", 1, 65535);
        PublicGate = new GameServer { ServerId = RealmId, ServerName = root.GetAttribute("name"),
            IpHost = root.GetAttribute("publicHost"), Prot = GatePort, RunState = 3 };
        if (PublicGate.ServerName.Length == 0) PublicGate.ServerName = RealmId + "区";
        if (PublicGate.IpHost.Length == 0) PublicGate.IpHost = Host;
        if (root.HasAttribute("runState")) PublicGate.RunState = Number(root, "runState", 1, 3);
        if (root.HasAttribute("isNew")) PublicGate.IsNew = Number(root, "isNew", 0, 1);
        // 旧版单文件保留内嵌的中心服端口。新部署文件把各区共享的地址放在一个位置。
        XmlElement shared = root;
        CenterHost = Host;
        LoginHost = Host;
        LoginPort = NetDefine.LoginServerPort;
        if (root.HasAttribute("shared"))
        {
            UsesRealmCatalog = true;
            // 相对引用始终按所属 XML 所在目录解析，各服务可从自己的输出目录启动。
            string sharedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), root.GetAttribute("shared")));
            XmlDocument services = new XmlDocument { XmlResolver = null };
            services.Load(sharedPath);
            shared = services.DocumentElement;
            CenterHost = shared.GetAttribute("centerHost");
            LoginHost = shared.GetAttribute("loginHost");
            LoginPort = Number(shared, "loginPort", 1, 65535);
            // 配置目录决定哪些逻辑区有资格登记。
            XmlNodeList entries = shared.SelectNodes("realm");
            for (int index = 0; index < entries.Count; index++)
            {
                // 每个条目指到一份独立的区服部署文件。
                XmlElement entry = (XmlElement)entries[index];
                RealmFiles.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sharedPath), entry.GetAttribute("file"))));
            }
        }
        CenterPort = Number(shared, "centerPort", 1, 65535);
        MasterPort = Number(root, "masterPort", 1, 65535);
        ControlPort = Number(root, "controlPort", 1, 65535);
        // GS 先登记，让后面每个场景都能检查实际宿主。
        XmlNodeList nodes = root.SelectNodes("game");
        for (int index = 0; index < nodes.Count; index++)
        {
            // 当前物理节点的地址独立于逻辑区服。
            XmlElement row = (XmlElement)nodes[index];
            // 同一配置不能出现两个相同的物品编号分配源。
            int id = Number(row, "id", 0, 255);
            Games.Add(id, new SceneNodeRegistration { RealmId = RealmId, GameId = id,
                Host = row.GetAttribute("host"), Port = Number(row, "port", 1, 65535) });
        }
        // 地图号和线路号组成玩家可寻址的唯一键。
        HashSet<long> keys = new HashSet<long>();
        nodes = root.SelectNodes("scene");
        for (int index = 0; index < nodes.Count; index++)
        {
            // 该行是一份可独立分配人数与宿主的公共场景。
            XmlElement row = (XmlElement)nodes[index];
            // 宿主选择、路线展示与人数上限分别读取。
            PublicSceneLine scene = new PublicSceneLine { MapId = Number(row, "map", 0, int.MaxValue),
                LineId = Number(row, "line", 1, int.MaxValue), GameId = Number(row, "game", 0, 255),
                Capacity = Number(row, "capacity", 1, int.MaxValue) };
            if (!Games.ContainsKey(scene.GameId) || !keys.Add(((long)scene.MapId << 32) | (uint)scene.LineId))
                throw new InvalidDataException("公共地图分线重复或宿主不存在：" + row.OuterXml);
            Scenes.Add(scene);
        }
        if (Scenes.Count == 0) throw new InvalidDataException("区服至少需要一条公共地图分线。");
        if (root.HasAttribute("spawnMap") && !Scenes.Exists(scene => scene.MapId == SpawnMap))
            throw new InvalidDataException("出生地图缺少公共线路：" + SpawnMap);
        nodes = root.SelectNodes("expansion");
        for (int index = 0; index < nodes.Count; index++)
        {
            // 每张允许自动扩线的地图从其常驻线路继承容量。
            XmlElement row = (XmlElement)nodes[index];
            int map = Number(row, "map", 0, int.MaxValue);
            List<PublicSceneLine> fixedLines = Scenes.FindAll(scene => scene.MapId == map);
            if (fixedLines.Count == 0) throw new InvalidDataException("弹性地图缺少常驻线路：" + map);
            MapPolicies.Add(map, new MapPolicy { Warning = Number(row, "warning", 1, fixedLines[0].Capacity),
                MaxLines = Number(row, "maxLines", fixedLines.Count, int.MaxValue) });
        }
    }

    /// <summary>从部署节点读取十进制整数。</summary>
    /// <param name="row">当前服务或场景节点。</param><param name="name">配置属性名。</param>
    /// <param name="minimum">可用的最小值。</param><param name="maximum">可用的最大值。</param>
    private static int Number(XmlElement row, string name, int minimum, int maximum)
    {
        // 配置错误需在启动时指出字段，避免运行中才找不到端口或线路。
        int value;
        if (!int.TryParse(row.GetAttribute(name), out value) || value < minimum || value > maximum)
            throw new InvalidDataException("区服配置字段无效：" + name + "，节点：" + row.Name);
        return value;
    }
}
