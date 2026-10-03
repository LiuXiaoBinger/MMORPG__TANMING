using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>场景角色实体基类；不承载背包、次数等持久角色业务数据。</summary>
    public class RoleEntity : ROObject
    {
        public RoleEntity(ulong uid, EntityType entityType, int configId)
            : base(uid, entityType, configId)
        {
        }
    }
}








