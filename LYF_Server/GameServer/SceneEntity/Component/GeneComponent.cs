using GameServer.SceneEntity;
using MMoRpgCommon.SceneEntity;

/// <summary>服务端场景实体的基因组件，管理节点时机；节点通过真实实体直接调用属性组件。</summary>
public class GeneComponent : RuntimeEntityComponent
{
    /// <summary>当前场景实体独立持有的具体节点容器。</summary>
    public GeneRuntime Runtime { get; private set; }
    /// <summary>原场景死亡检查读取实际属性组件记录的伤害来源。</summary>
    public ulong LastDamageFirerId { get { return Owner.Components.Get<AttributesComponent>(EntityComponentType.Attributes).LastDamageFirerId; } }
    /// <summary>绑定真实场景实体并保留 Gene 组件类别。</summary>
    /// <param name="owner">当前持有节点的真实场景实体。</param>
    public GeneComponent(ROObject owner) : base(owner, EntityComponentType.Gene) { }
    /// <summary>把宿主直接传入公共模块，节点执行时访问此实体的属性组件。</summary>
    public override void Initialize() { Runtime = new GeneRuntime(Owner, LubanMgr.Instance.GetGeneFactory()); }
    /// <summary>推进各节点，具体资源行为在节点 OnCall 中直接执行。</summary>
    /// <param name="deltaMilliseconds">本次服务器逻辑帧毫秒数。</param>
    public override void Update(int deltaMilliseconds) { Runtime.Update(deltaMilliseconds); }
    /// <summary>实体离场时先撤销节点效果，再解除宿主引用。</summary>
    public override void Shutdown() { Runtime.Clear(); base.Shutdown(); }
}
