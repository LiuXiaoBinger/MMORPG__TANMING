using MMoRpgCommon.SceneEntity;

/// <summary>
/// 角色 Builder，对应参考项目 RoleBuilder 的组件装配顺序。
/// 用于构建场景中其他玩家或非本地角色实体，包含公共组件、战斗组件和角色数据组件。
/// </summary>
public sealed class RoleEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建角色实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">角色配置表 ID。</param>
    /// <returns>已装配完组件的角色实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取角色实体实例
        RoleEntity entity = MEntityPool.Instance.Acquire(uid, EntityType.Role,
            configId, delegate(ulong id, int config)
            {
                return new RoleEntity(id, config);
            });
        // 挂载基础公共组件
        AttachCommonComponents(entity);
        // 挂载战斗组件标记
        AttachCombatComponents(entity);
        // 挂载角色业务数据组件
        MComponentMgr.Instance.CreateComponent<EntityRoleComponent>(entity);
        // 挂载角色待机表现组件，在模型 Animator 就绪后装配 Idle 和 FIdle 两条动画。
        MComponentMgr.Instance.CreateComponent<MIdleComponent>(entity);
        // 挂载角色跳跃表现组件，其他角色与本地角色复用同一套三阶段动画。
        MComponentMgr.Instance.CreateComponent<MJumpComponent>(entity);
        return entity;
    }
}
