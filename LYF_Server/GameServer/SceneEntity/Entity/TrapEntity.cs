using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>陷阱场景实体。</summary>
    public sealed class TrapEntity : ROObject
    {
        public TrapEntity(ulong uid, int configId)
            : base(uid, EntityType.Trap, configId)
        {
        }
    }
}








