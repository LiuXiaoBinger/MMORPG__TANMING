using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>宠物实体；主人关系由上层业务通过已有 UID 映射维护。</summary>
    public sealed class PetEntity : MonsterEntity
    {
        public PetEntity(ulong uid, int configId)
            : base(uid, EntityType.Pet, configId)
        {
        }
    }
}








