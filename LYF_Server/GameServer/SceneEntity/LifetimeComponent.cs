using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 LifeTimeComponent 的自动销毁组件。</summary>
    public sealed class LifetimeComponent : RuntimeEntityComponent
    {
        public LifetimeComponent(ROObject owner) : base(owner, EntityComponentType.Lifetime) { }
    }
}
