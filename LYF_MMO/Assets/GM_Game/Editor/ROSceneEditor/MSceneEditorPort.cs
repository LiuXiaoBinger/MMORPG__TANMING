using System.Xml.Linq;

/// <summary>原 NodeEditorFramework 端口的轻量引用，保留原 ID 和端口语义。</summary>
public sealed class MSceneEditorPort
{
    /// <summary>端口所属节点，用于画布定位和预览流程遍历。</summary>
    public MSceneEditorNode Owner;
    /// <summary>XML 序列化边界中的原始端口元素。</summary>
    public XElement Element;
    /// <summary>连接文件引用的稳定端口 ID。</summary>
    public string Id { get { return (string)Element.Attribute("ID"); } }
    /// <summary>RO 原端口名称，例如 prevInKnob 和 resultOutKnob。</summary>
    public string Name { get { return (string)Element.Attribute("name"); } }
    /// <summary>该端口是否表示输入，供交互连接和连线方向使用。</summary>
    public bool IsInput
    {
        get
        {
            if (Name.EndsWith("InKnob", System.StringComparison.OrdinalIgnoreCase)) return true;
            return Name == "inKnob" || Name == "fromClientKnob" || Name == "fromServerKnob";
        }
    }
}
