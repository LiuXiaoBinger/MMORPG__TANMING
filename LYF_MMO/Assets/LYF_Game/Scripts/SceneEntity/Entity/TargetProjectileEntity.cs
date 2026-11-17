using MMoRpgCommon.SceneEntity;

/// <summary>
/// 目标型投射物的实体类型，由两种目标投射物配置共用。
/// 当前只区分类别，不在本类追踪目标或处理命中。
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
