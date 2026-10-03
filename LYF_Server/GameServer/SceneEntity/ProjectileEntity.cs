using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>飞行物/子弹实体，生命周期由 Lifetime 组件管理。</summary>
    public sealed class ProjectileEntity : ROObject
    {
        public ProjectileEntity(ulong uid, int configId)
            : base(uid, EntityType.Projectile, configId)
        {
        }
    }
}








