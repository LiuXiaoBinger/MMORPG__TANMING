using System.Xml.Linq;

/// <summary>保存原画布的一条端口连接，不重新编号已有连线。</summary>
public sealed class MSceneEditorConnection
{
    /// <summary>连线原 XML，用于删除时同步文档。</summary>
    public XElement Element;
    /// <summary>原 port1ID 指向的端口。</summary>
    public MSceneEditorPort First;
    /// <summary>原 port2ID 指向的端口。</summary>
    public MSceneEditorPort Second;
}
