using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>非玩家角色场景实体。</summary>
    public sealed class NpcEntity : ROObject
    {
        public NpcEntity(ulong uid, int configId)
            : base(uid, EntityType.Npc, configId)
        {
        }
    }
}








