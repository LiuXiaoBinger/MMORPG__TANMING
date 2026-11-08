using System;

/// <summary>即时资源节点在首次施加和刷新时发布一次结果，自身不保留常驻实例。</summary>
public abstract class InstantResourceGeneBase : GeneBase
{
    /// <summary>此节点只在施加时结算，组合内其他节点仍遵守根寿命。</summary>
    public override bool IsInstant { get { return true; } }

    /// <summary>立即发布包含来源与当前层数的资源结果，不直接依赖角色或协议类型。</summary>
    /// <param name="instance">提供来源、角色归因与当前层数的根实例。</param>
    /// <param name="publish">宿主结算回调，未订阅时不执行资源业务。</param>
    public override void PublishInstant(GeneBase instance, Action<GenePulse> publish)
    {
        publish?.Invoke(new GenePulse(instance, this));
    }
}
