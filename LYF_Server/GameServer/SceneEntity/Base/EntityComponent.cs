using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROEntityComponent 的服务端组件。</summary>
    public sealed class EntityComponent : RuntimeEntityComponent
    {
        public EntityComponent(ROObject owner) : base(owner, EntityComponentType.Entity) { }
    }
}
