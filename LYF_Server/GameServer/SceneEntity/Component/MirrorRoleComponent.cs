using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROMirrorRoleComponent 的镜像角色组件。</summary>
    public sealed class MirrorRoleComponent : RuntimeEntityComponent
    {
        public MirrorRoleComponent(ROObject owner) : base(owner, EntityComponentType.MirrorRole) { }
    }
}
