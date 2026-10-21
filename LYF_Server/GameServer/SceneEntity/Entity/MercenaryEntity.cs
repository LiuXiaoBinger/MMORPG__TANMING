using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>佣兵实体；怪物公共组件上叠加召唤和佣兵专用组件。</summary>
    public sealed class MercenaryEntity : MonsterEntity
    {
        public MercenaryEntity(ulong uid, int configId)
            : base(uid, EntityType.Mercenary, configId)
        {
        }
    }
}








