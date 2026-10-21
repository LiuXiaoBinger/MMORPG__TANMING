using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>首领怪实体；行为组件沿用怪物档案并额外标记首领等级。</summary>
    public sealed class BossEntity : MonsterEntity
    {
        public BossEntity(ulong uid, int configId)
            : base(uid, EntityType.Boss, configId)
        {
        }
    }
}








