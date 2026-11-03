using MMoRpgCommon.SceneEntity;

/// <summary>
/// 创建或复用镜像角色，添加公共组件、角色数据和战斗相关标记。
/// AI 与导航在这里仅登记组件类别，没有创建具体的行为组件。
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
        // 登记 AI 类别，标记本身不驱动行为
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.AI));
        // 登记导航类别，标记本身不寻路
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Navigation));
        return entity;
    }
}
