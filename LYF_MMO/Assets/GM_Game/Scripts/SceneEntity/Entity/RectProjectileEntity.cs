using MMoRpgCommon.SceneEntity;

/// <summary>
/// 矩形类别投射物的实体类型。
/// 当前只区分类别，不在本类计算矩形范围或伤害。
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
