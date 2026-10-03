using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目飞行物创建路径使用的飞行物组件。</summary>
    public sealed class ProjectileComponent : RuntimeEntityComponent
    {
        public ProjectileComponent(ROObject owner) : base(owner, EntityComponentType.Projectile) { }
    }
}
