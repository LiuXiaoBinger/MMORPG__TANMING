using MMoRpgCommon.SceneEntity;

/// <summary>
/// 环形飞行物实体，对应 RealRingBullet。
/// 以环形/圆形范围进行伤害判定的投射物，通常用于范围 AOE 技能。
/// </summary>
public sealed class RingProjectileEntity : ProjectileEntity
{
    /// <summary>
    /// 构造环形范围投射物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">投射物配置表 ID</param>
    public RingProjectileEntity(ulong uid, int configId)
        : base(uid, configId)
    {
    }
}
