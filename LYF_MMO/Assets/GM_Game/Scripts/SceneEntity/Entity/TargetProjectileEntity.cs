using MMoRpgCommon.SceneEntity;

/// <summary>
/// 目标追踪飞行物实体，对应 RealTargetBullet/UnrealTargetBullet。
/// 发射后自动追踪目标实体，命中目标后消失。
/// </summary>
public sealed class TargetProjectileEntity : ProjectileEntity
{
    /// <summary>
    /// 构造追踪投射物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">投射物配置表 ID</param>
    public TargetProjectileEntity(ulong uid, int configId)
        : base(uid, configId)
    {
    }
}
