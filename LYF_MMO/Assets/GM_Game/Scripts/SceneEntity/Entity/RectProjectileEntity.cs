using MMoRpgCommon.SceneEntity;

/// <summary>
/// 矩形范围飞行物实体，对应 RealRectBullet。
/// 以矩形范围进行伤害判定的投射物，通常用于直线扇形或长方形区域技能。
/// </summary>
public sealed class RectProjectileEntity : ProjectileEntity
{
    /// <summary>
    /// 构造矩形范围投射物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">投射物配置表 ID</param>
    public RectProjectileEntity(ulong uid, int configId)
        : base(uid, configId)
    {
    }
}
