/// <summary>即时资源节点在挂载或刷新时直接调用具体子类的 OnCall，自身不常驻。</summary>
public abstract class InstantResourceGeneBase : GeneBase
{
    /// <summary>即时资源只执行一次；组合中其他持续节点仍可保留。</summary>
    public override bool IsInstant { get { return true; } }
}
