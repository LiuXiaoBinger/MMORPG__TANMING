using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>精英怪实体；行为组件沿用怪物档案并额外标记精英等级。</summary>
    public sealed class EliteEntity : MonsterEntity
    {
        public EliteEntity(ulong uid, int configId)
            : base(uid, EntityType.Elite, configId)
        {
        }
    }
}








