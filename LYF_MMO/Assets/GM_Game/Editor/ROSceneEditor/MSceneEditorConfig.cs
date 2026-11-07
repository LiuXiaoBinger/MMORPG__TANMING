using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

/// <summary>沿用 RO 的 MSceneEditorConfig / Objects / Nodes / Connections 数据结构；编辑和保存均保留原始未知字段。</summary>
public sealed class MSceneEditorConfig
{
    /// <summary>当前文档的完整 XML；只在序列化边界使用通用 XML 类型。</summary>
    public XDocument Document;
    /// <summary>主工程内当前编辑文件的绝对或 Assets 相对路径。</summary>
    public string FilePath;
    /// <summary>从原画布解析的节点，顺序与文件一致。</summary>
    public readonly List<MSceneEditorNode> Nodes = new List<MSceneEditorNode>();
    /// <summary>已解析的原画布端口连接。</summary>
    public readonly List<MSceneEditorConnection> Connections = new List<MSceneEditorConnection>();
    /// <summary>节点类型映射中没有找到端口的连接数，验收时必须为零。</summary>
    public int MissingConnectionCount;
    /// <summary>场景区域、空气墙和交互物件的原始配置。</summary>
    public XElement[] TriggerObjects;
    /// <summary>编辑器是否存在未保存的修改。</summary>
    public bool IsDirty;
    /// <summary>当前文档中下一个不会重复的数字引用编号。</summary>
    private long nextId;

    /// <summary>按 RO 原结构读取完整编辑配置，不依赖 MoonClient 或 PbLocal 二进制程序集。</summary>
    /// <param name="path">要打开的主工程配置文件。</param>
    public static MSceneEditorConfig LoadFromFile(string path)
    {
        // 配置原文保存所有暂未在 LYF 实现的机制字段和画布分组。
        MSceneEditorConfig config = new MSceneEditorConfig();
        config.FilePath = path;
        config.Document = XDocument.Load(path);
        if (config.Document.Root.Name.LocalName != "MSceneEditorConfig") throw new FormatException("不是 RO 场景编辑配置。");
        // 扫描原有数字 ID，仅给新增节点分配其后的编号。
        XAttribute[] attributes = config.Document.Descendants().Attributes().ToArray();
        for (int i = 0; i < attributes.Length; i++)
        {
            // 只有 ID/refID 属性参与引用编号分配。
            string name = attributes[i].Name.LocalName;
            if (name != "ID" && name != "refID") continue;
            // 原始引用编号在有符号 64 位整数范围内。
            long value;
            if (long.TryParse(attributes[i].Value, out value)) config.nextId = Math.Max(config.nextId, value);
        }
        config.Rebuild();
        return config;
    }

    /// <summary>保存原 XML，同时把前置波次端口关系写回原 pre_wave_list。</summary>
    /// <param name="path">保存位置，通常为当前主工程配置路径。</param>
    public void SaveToFile(string path)
    {
        SynchronizePreWaves();
        Document.Save(path);
        FilePath = path;
        IsDirty = false;
    }

    /// <summary>根据原数字引用重新建立轻量节点对象，原 XML 内容不变。</summary>
    public void Rebuild()
    {
        Nodes.Clear();
        Connections.Clear();
        MissingConnectionCount = 0;
        // 原文档对象索引用于解析 Variable data 引用。
        Dictionary<string, XElement> objects = new Dictionary<string, XElement>();
        // 原端口索引用于解析 Connections。
        Dictionary<string, MSceneEditorPort> ports = new Dictionary<string, MSceneEditorPort>();
        // 画布是配置唯一保存节点与连线的区域。
        XElement canvas = Document.Root.Element("canvas");
        // 配置对象只保存引用容器的第一个具体类型元素。
        XElement[] sourceObjects = canvas.Element("Objects").Elements().ToArray();
        for (int i = 0; i < sourceObjects.Length; i++) objects[(string)sourceObjects[i].Attribute("refID")] = sourceObjects[i].Elements().FirstOrDefault();
        // 节点数组保持原顺序，便于精确保存和检查数量。
        XElement[] sourceNodes = canvas.Element("Nodes").Elements("Node").ToArray();
        for (int i = 0; i < sourceNodes.Length; i++)
        {
            // 一个原画布节点对应一个轻量编辑对象。
            MSceneEditorNode node = new MSceneEditorNode();
            node.Element = sourceNodes[i];
            // 波次使用 data，触发器、时机、条件和结果使用 realData；dataEx 继续保留原引用。
            XElement variable = node.Element.Element("Variables").Elements("Variable").FirstOrDefault(item => (string)item.Attribute("name") == "data" || (string)item.Attribute("name") == "realData");
            if (variable != null) objects.TryGetValue((string)variable.Attribute("refID"), out node.Data);
            // 每个端口保持原名称和数字 ID。
            XElement[] sourcePorts = node.Element.Element("Ports").Elements("Port").ToArray();
            for (int p = 0; p < sourcePorts.Length; p++)
            {
                // 端口反向指向当前节点，供连线定位。
                MSceneEditorPort port = new MSceneEditorPort { Owner = node, Element = sourcePorts[p] };
                node.Ports.Add(port);
                ports[port.Id] = port;
            }
            Nodes.Add(node);
        }
        // 所有原连线均须能解析到现有端口。
        XElement[] sourceConnections = canvas.Element("Connections").Elements("Connection").ToArray();
        for (int i = 0; i < sourceConnections.Length; i++)
        {
            // 当前连线的两个端点来自原 ID 映射。
            MSceneEditorPort first;
            MSceneEditorPort second;
            if (!ports.TryGetValue((string)sourceConnections[i].Attribute("port1ID"), out first)) { MissingConnectionCount++; continue; }
            if (!ports.TryGetValue((string)sourceConnections[i].Attribute("port2ID"), out second)) { MissingConnectionCount++; continue; }
            Connections.Add(new MSceneEditorConnection { Element = sourceConnections[i], First = first, Second = second });
        }
        TriggerObjects = Document.Root.Element("data").Element("scene_trigger_objects").Elements().ToArray();
    }

    /// <summary>复制现有 RO 节点模板，同时复制其数据对象并分配新的端口 ID。</summary>
    /// <param name="source">要复制的原波次或机制节点。</param>
    public MSceneEditorNode DuplicateNode(MSceneEditorNode source)
    {
        // 深拷贝只影响新增节点，原模板保留完整。
        XElement copy = new XElement(source.Element);
        copy.SetAttributeValue("ID", NewId());
        // data/dataEx 复制后必须分别使用新引用。
        XElement[] variables = copy.Element("Variables").Elements("Variable").ToArray();
        for (int i = 0; i < variables.Length; i++)
        {
            // 找到模板引用容器并保留其原类型声明。
            string reference = (string)variables[i].Attribute("refID");
            XElement original = Document.Root.Element("canvas").Element("Objects").Elements().First(item => (string)item.Attribute("refID") == reference);
            // 副本的对象引用编号不能复用原节点数据。
            XElement dataCopy = new XElement(original);
            string newReference = NewId();
            dataCopy.SetAttributeValue("refID", newReference);
            variables[i].SetAttributeValue("refID", newReference);
            Document.Root.Element("canvas").Element("Objects").Add(dataCopy);
        }
        // 新节点不复制连线，避免无意把机制连接到原流程。
        XElement[] newPorts = copy.Element("Ports").Elements().ToArray();
        for (int i = 0; i < newPorts.Length; i++) newPorts[i].SetAttributeValue("ID", NewId());
        Document.Root.Element("canvas").Element("Nodes").Add(copy);
        // 波次编号独立于画布 ID，按已有最大波次编号递增。
        int nextWave = 0;
        for (int i = 0; i < Nodes.Count; i++) nextWave = Math.Max(nextWave, Nodes[i].WaveId + 1);
        Rebuild();
        // 新节点总是追加在文件末尾。
        MSceneEditorNode added = Nodes[Nodes.Count - 1];
        added.Position = source.Position + new Vector2(260f, 80f);
        if (added.IsWave)
        {
            ROSceneXml.Set(added.Data, "base_info/id", nextWave.ToString());
            added.Data.Element("base_info").Element("pre_wave_list").RemoveNodes();
        }
        else if (added.Data != null && added.Data.Element("id") != null)
        {
            // 同类触发器或组的副本使用新业务编号，避免与原节点共享身份。
            int nextBusinessId = 0;
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Data == null || Nodes[i].Data.Name != added.Data.Name) continue;
                nextBusinessId = Math.Max(nextBusinessId, ROSceneXml.Int(Nodes[i].Data, "id") + 1);
            }
            ROSceneXml.Set(added.Data, "id", nextBusinessId.ToString());
        }
        IsDirty = true;
        return added;
    }

    /// <summary>连接两个原端口，连接记录仍写成 port1ID/port2ID。</summary>
    /// <param name="first">用户首先选择的端口。</param>
    /// <param name="second">用户随后选择的相反方向端口。</param>
    public void Connect(MSceneEditorPort first, MSceneEditorPort second)
    {
        if (first.Owner == second.Owner || first.IsInput == second.IsInput) return;
        for (int i = 0; i < Connections.Count; i++)
        {
            if (Connections[i].First == first && Connections[i].Second == second) return;
            if (Connections[i].First == second && Connections[i].Second == first) return;
        }
        Document.Root.Element("canvas").Element("Connections").Add(new XElement("Connection", new XAttribute("port1ID", first.Id), new XAttribute("port2ID", second.Id)));
        IsDirty = true;
        Rebuild();
        SynchronizePreWaves();
    }

    /// <summary>移除用户选择的连接，同时同步原前置波次字段。</summary>
    /// <param name="connection">当前配置中的连接。</param>
    public void Disconnect(MSceneEditorConnection connection)
    {
        connection.Element.Remove();
        Connections.Remove(connection);
        SynchronizePreWaves();
        IsDirty = true;
    }

    /// <summary>删除当前节点和其连线，保留其他节点的原编号。</summary>
    /// <param name="node">用户明确选中删除的节点。</param>
    public void RemoveNode(MSceneEditorNode node)
    {
        // 原始节点页和阶段页共用此删除入口，真正删除时同步清除所有显式阶段引用。
        ROSceneStageBindings.RemoveNodeReferences(this, node);
        for (int i = Connections.Count - 1; i >= 0; i--)
        {
            if (Connections[i].First.Owner == node || Connections[i].Second.Owner == node) Connections[i].Element.Remove();
        }
        // 该节点的私有序列化对象随节点一起移除。
        XElement[] variables = node.Element.Element("Variables").Elements("Variable").ToArray();
        for (int i = 0; i < variables.Length; i++)
        {
            // 数据引用由原编辑器为每个节点独立生成。
            string reference = (string)variables[i].Attribute("refID");
            XElement item = Document.Root.Element("canvas").Element("Objects").Elements().First(value => (string)value.Attribute("refID") == reference);
            item.Remove();
        }
        node.Element.Remove();
        IsDirty = true;
        Rebuild();
        SynchronizePreWaves();
    }

    /// <summary>按原 MSceneWaveNode.OnBeforeSave 的端口逻辑维护前置波次列表。</summary>
    public void SynchronizePreWaves()
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            // 仅波次节点需要同步 pre_wave_list。
            MSceneEditorNode node = Nodes[i];
            if (!node.IsWave) continue;
            // 已有百分比条件按波次编号保留。
            XElement list = node.Data.Element("base_info").Element("pre_wave_list");
            if (list == null) continue;
            // 当前输入端口实际连接的前置波次编号。
            List<int> previous = new List<int>();
            for (int c = 0; c < Connections.Count; c++)
            {
                // 连接方向由原端口名确认，不能把跟随路径误当作前置波次。
                MSceneEditorConnection connection = Connections[c];
                if (connection.First.Owner == node && connection.First.Name == "prevInKnob") previous.Add(connection.Second.Owner.WaveId);
                if (connection.Second.Owner == node && connection.Second.Name == "prevInKnob") previous.Add(connection.First.Owner.WaveId);
            }
            // 未连接的原条件需按原编辑器行为移除。
            XElement[] old = list.Elements().ToArray();
            for (int p = 0; p < old.Length; p++) if (!previous.Contains(ROSceneXml.Int(old[p], "pre_wave_id"))) old[p].Remove();
            for (int p = 0; p < previous.Count; p++)
            {
                // 新增连接的默认条件为该波次怪物全部结束。
                int id = previous[p];
                if (list.Elements().Any(item => ROSceneXml.Int(item, "pre_wave_id") == id)) continue;
                list.Add(new XElement("MScenePreWaveInfo", new XElement("pre_wave_id", id), new XElement("pre_wave_blood", 0), new XElement("pre_wave_num", 0)));
            }
        }
    }

    /// <summary>分配原编辑格式支持的十进制 64 位引用 ID。</summary>
    private string NewId()
    {
        nextId++;
        return nextId.ToString(CultureInfo.InvariantCulture);
    }
}
