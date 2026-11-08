/// <summary>由真实角色或实体实现的属性访问约定，不创建宿主包装对象。
/// 客户端 MEntity 与服务端实体已有不同继承链，因此此处使用接口连接公共基因代码。</summary>
public interface IGeneOwner
{
    /// <summary>当前角色真实的属性组件，节点通过它直接修改属性和资源。</summary>
    IAttributeComponent AttrComp { get; }
    /// <summary>当前角色自身的基因容器，用于事件计时、随机判定和施加子节点。</summary>
    GeneRuntime GeneRuntime { get; }
}
