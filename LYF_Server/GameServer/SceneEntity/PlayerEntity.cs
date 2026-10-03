using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>玩家在场景中的运行时实体，持久角色数据由角色管理器单独维护。</summary>
    public sealed class PlayerEntity : Role
    {
        public PlayerEntity(ulong uid, int configId)
            : base(uid, EntityType.Role | EntityType.Player, configId)
        {
        }
    }
}








