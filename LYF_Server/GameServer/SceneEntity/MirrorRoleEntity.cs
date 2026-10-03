using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>镜像角色实体；原角色关系由上层业务通过已有 UID 映射维护。</summary>
    public sealed class MirrorRoleEntity : Role
    {
        public MirrorRoleEntity(ulong uid, int configId)
            : base(uid, EntityType.MirrorRole, configId)
        {
        }
    }
}








