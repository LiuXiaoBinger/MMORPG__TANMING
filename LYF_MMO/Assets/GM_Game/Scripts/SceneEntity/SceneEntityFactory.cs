using System;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 客户端实体工厂，统一复现参考 ObjectFactory 的 Builder 入口。
/// 根据实体类型或投射物类型创建对应的 Builder，由 Builder 完成实体构造和组件装配。
/// </summary>
public static class SceneEntityFactory
{
    /// <summary>
    /// 构造普通场景实体（角色、怪物、NPC 等）。
    /// </summary>
    /// <param name="entityType">实体类型枚举</param>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">配置表 ID</param>
    /// <returns>构造完成的实体实例（尚未加入世界）</returns>
    public static ROObject Build(EntityType entityType, ulong uid,
        int configId)
    {
        EntityBuilder builder = CreateBuilder(entityType);
        return builder.Build(uid, configId);
    }

    /// <summary>
    /// 构造投射物实体。
    /// </summary>
    /// <param name="projectileType">投射物形态类型</param>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">投射物配置表 ID</param>
    /// <returns>构造完成的投射物实体实例（尚未加入世界）</returns>
    public static ROObject BuildProjectile(ProjectileType projectileType,
        ulong uid, int configId)
    {
        ProjectileEntityBuilder builder = new ProjectileEntityBuilder(projectileType);
        return builder.Build(uid, configId);
    }

    /// <summary>
    /// 根据实体类型，创建对应的实体构造器实例（工厂方法）
    /// </summary>
    /// <param name="entityType">实体类型枚举</param>
    /// <returns>对应类型的 EntityBuilder 实体构造器</returns>
    /// <exception cref="ArgumentOutOfRangeException">传入不支持的实体类型时抛出</exception>
    private static EntityBuilder CreateBuilder(EntityType entityType)
    {
        // 玩家角色实体（其他玩家）
        // 客户端本地玩家实体
        // Player 是 Role 的组合类别，必须优先判断 Player，避免被 Role 位先截获。
        if ((entityType & EntityType.Player) != EntityType.None)
        {
            return new PlayerEntityBuilder();
        }
        // 玩家角色实体（其他玩家）
        if ((entityType & EntityType.Role) != EntityType.None)
        {
            return new RoleEntityBuilder();
        }
        // 普通怪物实体
        if ((entityType & EntityType.Monster) != EntityType.None)
        {
            return new MonsterEntityBuilder();
        }
        // 精英怪物实体
        if ((entityType & EntityType.Elite) != EntityType.None)
        {
            return new EliteEntityBuilder();
        }
        // BOSS怪物实体
        if ((entityType & EntityType.Boss) != EntityType.None)
        {
            return new BossEntityBuilder();
        }
        // NPC实体
        if ((entityType & EntityType.Npc) != EntityType.None)
        {
            return new NpcEntityBuilder();
        }
        // 测试替身实体（Dummy）
        if ((entityType & EntityType.Dummy) != EntityType.None)
        {
            return new DummyEntityBuilder();
        }
        // 陷阱实体
        if ((entityType & EntityType.Trap) != EntityType.None)
        {
            return new TrapEntityBuilder();
        }
        // 宠物实体
        if ((entityType & EntityType.Pet) != EntityType.None)
        {
            return new PetEntityBuilder();
        }
        // 鹰类单位实体
        if ((entityType & EntityType.Eagle) != EntityType.None)
        {
            return new EagleEntityBuilder();
        }
        // 采集物实体
        if ((entityType & EntityType.Collection) != EntityType.None)
        {
            return new CollectionEntityBuilder();
        }
        // 镜像角色实体（分身/镜像）
        if ((entityType & EntityType.MirrorRole) != EntityType.None)
        {
            return new MirrorRoleEntityBuilder();
        }
        // 佣兵实体
        if ((entityType & EntityType.Mercenary) != EntityType.None)
        {
            return new MercenaryEntityBuilder();
        }
        // 未匹配到任何支持的实体类型，抛出异常
        throw new ArgumentOutOfRangeException("entityType", entityType, "不支持的客户端实体类别");
    }
}
