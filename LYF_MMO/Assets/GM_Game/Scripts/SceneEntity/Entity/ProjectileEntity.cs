using MMoRpgCommon.SceneEntity;

/// <summary>
/// 飞行物实体基类，对应参考项目 ROBullet。
/// 所有投射物实体的根类，负责弹道飞行、碰撞检测和生命周期。
/// 具体弹道形态（直线、追踪、环形、矩形）由派生类区分。
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
