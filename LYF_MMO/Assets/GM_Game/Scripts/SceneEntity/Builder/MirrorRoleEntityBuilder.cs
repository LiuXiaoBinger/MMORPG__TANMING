using MMoRpgCommon.SceneEntity;

/// <summary>
/// 镜像角色 Builder，对应 MirrorRoleBuilder 的角色表现和 AI 组件。
/// 镜像角色用于战斗假人、演示场景等，具备角色表现但由 AI 驱动。
/// </summary>
public sealed class MirrorRoleEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建镜像角色实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">镜像角色配置表 ID。</param>
    /// <returns>已装配完组件的镜像角色实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取镜像角色实体实例
        MirrorRoleEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.MirrorRole,
            configId, delegate(ulong id, int config)
            {
                return new MirrorRoleEntity(id, config);
            });
        // 基础公共组件
        AttachCommonComponents(entity);
        // 战斗组件标记
        AttachCombatComponents(entity);
        // 角色业务数据组件
        MComponentMgr.Instance.CreateComponent<EntityRoleComponent>(entity);
        // 镜像角色专属标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.MirrorRole));
        // AI 标记：镜像角色由 AI 驱动行为
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.AI));
        // 导航标记：镜像角色需要寻路
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Navigation));
        return entity;
    }
}
