using MMoRpgCommon.SceneEntity;

/// <summary>
/// 动画类投射物的实体类型。
/// 当前只沿用投射物基类的初始化，不在本类播放动画或检测碰撞。
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
