using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>可采集场景实体。</summary>
    public sealed class CollectionEntity : ROObject
    {
        public CollectionEntity(ulong uid, int configId)
            : base(uid, EntityType.Collection, configId)
        {
        }
    }
}








