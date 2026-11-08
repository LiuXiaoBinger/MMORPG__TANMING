/// <summary>独立测试使用的角色，持有真实 GeneRuntime 与属性修正实现，不进入正式场景。</summary>
public class GeneTestOwner : IGeneOwner
{
    /// <summary>本测试角色实际接收节点修改的属性组件。</summary>
    public GeneTestAttributes Attributes { get; private set; }
    /// <summary>公共节点读取的同一个测试属性组件。</summary>
    public IAttributeComponent AttrComp { get { return Attributes; } }
    /// <summary>本测试角色的真实公共节点容器。</summary>
    public GeneRuntime GeneRuntime { get; private set; }
    /// <summary>创建独立测试角色，不依赖网络、数据库或 Unity。</summary>
    /// <param name="factory">本轮使用的真实配置节点工厂。</param>
    /// <param name="scenario">测试宿主的场景类别。</param>
    /// <param name="random">需要确定概率时使用的随机源。</param>
    public GeneTestOwner(GeneFactory factory, int scenario, System.Random random)
    {
        Attributes = new GeneTestAttributes();
        GeneRuntime = new GeneRuntime(this, factory, scenario, random);
    }
}
