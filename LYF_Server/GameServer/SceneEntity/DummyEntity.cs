using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>召唤物或无仇恨的假怪物实体。</summary>
    public sealed class DummyEntity : MonsterEntity
    {
        public DummyEntity(ulong uid, int configId)
            : base(uid, EntityType.Dummy, configId)
        {
        }
    }
}








