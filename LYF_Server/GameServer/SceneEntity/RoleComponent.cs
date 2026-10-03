using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 RORoleComponent 的场景角色组件。</summary>
    public sealed class RoleComponent : RuntimeEntityComponent
    {
        public RoleComponent(ROObject owner) : base(owner, EntityComponentType.Role) { }
    }
}
