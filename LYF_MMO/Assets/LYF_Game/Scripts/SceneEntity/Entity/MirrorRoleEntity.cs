using MMoRpgCommon.SceneEntity;

/// <summary>
/// 镜像角色的实体类型，使用 MEntity 的模型与组件管理。
/// 本类只指定 MirrorRole 类别，外观与属性由其他数据入口提供。
/// </summary>
public sealed class MirrorRoleEntity : MEntity
{
    /// <summary>
    /// 构造镜像角色实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">镜像配置表 ID</param>
    public MirrorRoleEntity(ulong uid, int configId)
        : base(uid, EntityType.MirrorRole, configId)
    {
    }
}
