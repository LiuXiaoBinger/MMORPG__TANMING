using MMoRpgCommon.SceneEntity;

/// <summary>
/// 飞行物运动与命中信息组件，对应参考项目 ROBullet 体系。
/// 记录投射物类型、目标 UID、飞行速度和存活时长，超时自动标记待销毁。
/// </summary>
public sealed class EntityProjectileComponent : EntityComponentBase
{
    /// <summary>构造投射物组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityProjectileComponent(ROObject owner) : base(owner)
    {
    }

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Projectile"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Projectile; }
    }

    /// <summary>投射物表现类型，决定使用动画、特效或模型等表现形式。</summary>
    public ProjectileType ProjectileType { get; set; }

    /// <summary>目标实体 UID，用于追踪和命中判定；0 表示无目标。</summary>
    public ulong TargetUID { get; set; }

    /// <summary>飞行速度（单位/秒）。</summary>
    public float Speed { get; set; }

    /// <summary>最大存活时长（毫秒），小于等于 0 表示不做超时销毁。</summary>
    public int LifetimeMilliseconds { get; set; }

    /// <summary>已经经过的飞行时间（毫秒），由 Update 累计。</summary>
    public int ElapsedMilliseconds { get; private set; }

    /// <summary>每帧累计飞行时间，超过存活时长后把宿主标记为待销毁。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (LifetimeMilliseconds <= 0)
        {
            return;
        }
        ElapsedMilliseconds += deltaMilliseconds;
        // 超时后交给实体世界统一回收，避免在此直接销毁导致遍历异常
        if (ElapsedMilliseconds >= LifetimeMilliseconds)
        {
            Entity.MarkPendingDestroy();
        }
    }

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }

    protected override void EventSubscribe()
    {
        
    }

    protected override void EventUnsubscribe()
    {
        
    }

    /// <summary>对象池回收时重置投射物字段为默认值，保证下次复用干净。</summary>
    public override void Shutdown()
    {
        ProjectileType = ProjectileType.Anim;
        TargetUID = 0L;
        Speed = 0f;
        LifetimeMilliseconds = 0;
        ElapsedMilliseconds = 0;
    }
}





