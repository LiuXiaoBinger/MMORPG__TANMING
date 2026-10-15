using MMoRpgCommon.SceneEntity;

/// <summary>
/// 玩家 Builder，玩家实体复用 RoleBuilder 的公共组件。
/// 本地玩家实体需要同时挂载公共组件、战斗组件和角色数据组件。
/// </summary>
public sealed class PlayerEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 构建玩家实体实例。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">玩家配置表 ID。</param>
    /// <returns>已装配完组件的玩家实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        // 从实体对象池获取玩家实体实例，池中无可用对象时通过工厂新建
        // 本地玩家同时属于角色和玩家类别，供 Role 位筛选及 Player 位精确能力判断。
        EntityType playerEntityType = EntityType.Role | EntityType.Player;
        PlayerEntity entity = MEntityPool.Instance.Acquire(uid, playerEntityType,
            configId, delegate(ulong id, int config)
            {
                return new PlayerEntity(id, config);
            });
        // 挂载所有实体共享的基础组件
        AttachCommonComponents(entity);
        // 挂载战斗相关组件标记
        AttachCombatComponents(entity);
        // 挂载角色业务数据组件
        MComponentMgr.Instance.CreateComponent<EntityRoleComponent>(entity);
        // 挂载角色待机表现组件，在模型 Animator 就绪后装配 Idle 和 FIdle 两条动画。
        MComponentMgr.Instance.CreateComponent<MIdleComponent>(entity);
        // 挂载角色跳跃表现组件，统一装配起跳、空中循环和落地动画。
        MComponentMgr.Instance.CreateComponent<MJumpComponent>(entity);
        // 本地主角挂载RO导航到达模块，接收输入交接并发送技能请求。
        MComponentMgr.Instance.CreateComponent<MNavigationComponent>(entity);

        return entity;
    }
}
