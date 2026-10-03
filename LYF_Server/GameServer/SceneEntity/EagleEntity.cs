using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>鹰类随从实体；使用轻量召唤和飞行移动组件档案。</summary>
    public sealed class EagleEntity : MonsterEntity
    {
        public EagleEntity(ulong uid, int configId)
            : base(uid, EntityType.Eagle, configId)
        {
        }
    }
}








