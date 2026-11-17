using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

/// <summary>移植 RO 画布节点的保存接口，使用原 XML 数据和节点坐标连接现有 Unity 编辑窗口。</summary>
public sealed class MSceneEditorNode
{
    /// <summary>节点的原 Node 元素，保存类型、坐标、备注与端口。</summary>
    public XElement Element;
    /// <summary>Variable data 或 realData 指向的具体波次、触发条件或结果对象。</summary>
    public XElement Data;
    /// <summary>节点按原文件顺序排列的端口列表。</summary>
    public readonly List<MSceneEditorPort> Ports = new List<MSceneEditorPort>();
    /// <summary>NodeEditorFramework 的原始节点类型名称。</summary>
    public string TypeName { get { return (string)Element.Attribute("type"); } }
    /// <summary>配置中的节点身份，与波次编号不同。</summary>
    public string Id { get { return (string)Element.Attribute("ID"); } }
    /// <summary>该节点是否承载 MSceneWaveData。</summary>
    public bool IsWave { get { return Data != null && Data.Name.LocalName == "MSceneWaveData"; } }
    /// <summary>波次的业务编号，非波次节点返回 -1。</summary>
    public int WaveId { get { return ROSceneXml.Int(Data, "base_info/id", -1); } }
    /// <summary>原画布中的左上角位置，单位为画布像素。</summary>
    public Vector2 Position
    {
        get { return new Vector2(ROSceneXml.Float(Element, "nodePos/Vector2/x"), ROSceneXml.Float(Element, "nodePos/Vector2/y")); }
        set
        {
            ROSceneXml.Set(Element, "nodePos/Vector2/x", value.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            ROSceneXml.Set(Element, "nodePos/Vector2/y", value.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
    /// <summary>原编辑器的节点备注，同步保存到 Variables/content/string。</summary>
    public string Content
    {
        get { return ROSceneXml.Text(Element, "Variables/content/string"); }
        set { ROSceneXml.Set(Element, "Variables/content/string", value); }
    }
    /// <summary>画布中使用的短标题，保留业务编号以便与原 AI 关联。</summary>
    public string Title
    {
        get
        {
            if (IsWave) return "Wave " + WaveId + " · " + ROSceneXml.Text(Data, "base_info/spawn_type").Replace("SceneSpawnType_Spawn_Source_", "");
            if (Data == null) return TypeName;
            // 时机和结果枚举比生成的类名更适合识别机制节点。
            string kind = ROSceneXml.Text(Data, "timing_type");
            if (kind.Length == 0) kind = ROSceneXml.Text(Data, "result_type");
            if (kind.Length == 0) kind = ROSceneXml.Text(Data, "condition_type");
            if (kind.Length > 0) return kind.Replace("SSttimingType", "时机·").Replace("SSTResultType", "结果·");
            return TypeName.Replace("MSScene", "服务端·").Replace("MCScene", "客户端·").Replace("MScene", "") + " " + ROSceneXml.Text(Data, "id");
        }
    }
}
