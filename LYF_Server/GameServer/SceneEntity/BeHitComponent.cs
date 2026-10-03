using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 BeHitComponent 的受击组件。</summary>
    public sealed class BeHitComponent : RuntimeEntityComponent
    {
        public BeHitComponent(ROObject owner) : base(owner, EntityComponentType.BeHit) { }
    }
}
