using MMoRpgCommon.SceneEntity;

/// <summary>
/// 镜像角色实体，对应参考项目 MirrorRoleBuilder。
/// 用于技能分身、镜像幻影等场景，外观与玩家角色一致但无真实属性。
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
