using MMoRpgCommon.SceneEntity;

/// <summary>
/// 各类投射物共用的实体基类，统一使用 Projectile 实体类别。
/// 当前只复用 MEntity 的生命周期；本类不实现弹道、碰撞或伤害。
/// </summary>
public class ProjectileEntity : MEntity
{
    /// <summary>
    /// 构造投射物实体基类
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">投射物配置表 ID</param>
    public ProjectileEntity(ulong uid, int configId)
        : base(uid, EntityType.Projectile, configId)
    {
    }
}
