using MMoRpgCommon.SceneEntity;

/// <summary>
/// 动画飞行物实体，对应 AnimBullet。
/// 仅播放动画表现、无实体碰撞的投射物类型。
/// </summary>
public sealed class AnimProjectileEntity : ProjectileEntity
{
    /// <summary>
    /// 构造动画投射物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">投射物配置表 ID</param>
    public AnimProjectileEntity(ulong uid, int configId)
        : base(uid, configId)
    {
    }
}
