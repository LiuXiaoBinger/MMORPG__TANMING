using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>角色场景实体基类，细分为玩家和镜像角色。</summary>
    public class Role : RoleEntity
    {
        public Role(ulong uid, EntityType entityType, int configId)
            : base(uid, entityType, configId)
        {
        }
    }
}








