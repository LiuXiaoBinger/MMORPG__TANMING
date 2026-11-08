/// <summary>角色窗口的一行展示输入；节点引用直接来自客户端实际 GeneBase 集合，不参与业务结算。</summary>
public sealed class RoleGeneViewData
{
    /// <summary>服务器同步的实际根，界面只读取层数和显示寿命。</summary>
    public GeneBase Node { get; private set; }
    /// <summary>服务器提供的实际基因名称。</summary>
    public string Name { get; private set; }
    /// <summary>事件时机、剩余次数或效果类别的中文说明。</summary>
    public string Details { get; private set; }
    /// <summary>构造本次列表变化需要的一行展示信息。</summary>
    /// <param name="node">实际根节点，倒计时继续由客户端基因组件维护。</param>
    /// <param name="name">当前服务器基因名称。</param>
    /// <param name="details">控制器生成的效果类别或事件/次数说明。</param>
    public RoleGeneViewData(GeneBase node, string name, string details) { Node = node; Name = name; Details = details; }
}
