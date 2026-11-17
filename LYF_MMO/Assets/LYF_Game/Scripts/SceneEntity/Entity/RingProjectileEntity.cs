using MMoRpgCommon.SceneEntity;

/// <summary>
/// 环形类别投射物的实体类型。
/// 当前只区分类别，不在本类计算圆环范围或伤害。
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
