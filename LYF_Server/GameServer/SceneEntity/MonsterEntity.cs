using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>普通怪物场景实体。</summary>
    public class MonsterEntity : ROObject
    {
        public MonsterEntity(ulong uid, EntityType entityType, int configId)
            : base(uid, entityType, configId)
        {
        }
    }
}








