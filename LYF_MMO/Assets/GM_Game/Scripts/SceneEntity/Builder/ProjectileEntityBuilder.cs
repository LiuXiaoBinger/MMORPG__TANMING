using MMoRpgCommon.SceneEntity;

/// <summary>
/// 飞行物 Builder，按 ProjectileType 选择对应的投射物子类。
/// 根据投射物类型不同，创建动画投射物、目标追踪投射物、环形投射物或矩形投射物。
/// </summary>
public sealed class ProjectileEntityBuilder : EntityBuilder
{
    /// <summary>
    /// 投射物类型，决定创建哪种具体的投射物实体子类。
    /// </summary>
    private readonly ProjectileType _projectileType;

    /// <summary>
    /// 构造飞行物 Builder，指定投射物类型。
    /// </summary>
    /// <param name="projectileType">投射物类型枚举值。</param>
    public ProjectileEntityBuilder(ProjectileType projectileType)
    {
        _projectileType = projectileType;
    }

    /// <summary>
    /// 构建投射物实体实例，根据投射物类型选择对应的实体子类。
    /// </summary>
    /// <param name="uid">实体运行时唯一身份标识。</param>
    /// <param name="configId">投射物配置表 ID。</param>
    /// <returns>已装配完组件的投射物实体实例。</returns>
    public override ROObject Build(ulong uid, int configId)
    {
        ProjectileEntity entity;
        // 动画型或虚幻动画型投射物：使用动画表现
        if (_projectileType == ProjectileType.Anim || _projectileType == ProjectileType.Unreal)
        {
            entity = MEntityPool.Instance.Acquire(uid, EntityType.Projectile,
                configId, delegate(ulong id, int config)
                {
                    return new AnimProjectileEntity(id, config);
                });
        }
        // 目标型投射物：追踪指定目标位置
        else if (_projectileType == ProjectileType.UnrealTarget || _projectileType == ProjectileType.RealTarget)
        {
            entity = MEntityPool.Instance.Acquire(uid, EntityType.Projectile,
                configId, delegate(ulong id, int config)
                {
                    return new TargetProjectileEntity(id, config);
                });
        }
        // 环形投射物：范围伤害环形区域
        else if (_projectileType == ProjectileType.RealRing)
        {
            entity = MEntityPool.Instance.Acquire(uid, EntityType.Projectile,
                configId, delegate(ulong id, int config)
                {
                    return new RingProjectileEntity(id, config);
                });
        }
        // 默认矩形投射物：直线飞行范围伤害
        else
        {
            entity = MEntityPool.Instance.Acquire(uid, EntityType.Projectile,
                configId, delegate(ulong id, int config)
                {
                    return new RectProjectileEntity(id, config);
                });
        }
        // 投射物基础标记和组件
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Entity));
        // 变换组件
        MComponentMgr.Instance.CreateComponent<EntityTransformComponent>(entity);
        // 投射物业务数据组件
        MComponentMgr.Instance.CreateComponent<EntityProjectileComponent>(entity);
        // 同步标记
        entity.AddComponent(new EntityComponentMarker(entity, EntityComponentType.Sync));
        return entity;
    }
}
