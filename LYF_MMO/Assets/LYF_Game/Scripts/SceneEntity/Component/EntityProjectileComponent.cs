using MMoRpgCommon.SceneEntity;

/// <summary>
/// 保存投射物类型、目标、速度及存活时间；每帧只累计时间并检查是否超时。
/// 超时后标记实体待销毁，由实体世界统一移除；此组件不更新弹道或判断命中。
/// </summary>
public sealed class EntityProjectileComponent : EntityComponentBase
{

    #region 字段与属性
    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Projectile"/>。</summary>
    public override EntityComponentType ComponentType
    {
        get { return EntityComponentType.Projectile; }
    }

    /// <summary>投射物类别数据；此组件不按类别驱动不同表现。</summary>
    public ProjectileType ProjectileType { get; set; }

    /// <summary>保存目标实体 UID；0 表示无目标，此组件不自行追踪目标。</summary>
    public ulong TargetUID { get; set; }

    /// <summary>保存飞行速度，单位为场景单位/秒；此组件不按该值移动实体。</summary>
    public float Speed { get; set; }

    /// <summary>最大存活时长（毫秒），小于等于 0 表示不做超时销毁。</summary>
    public int LifetimeMilliseconds { get; set; }

    /// <summary>已经经过的飞行时间（毫秒），由 Update 累计。</summary>
    public int ElapsedMilliseconds { get; private set; }
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>构造投射物组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public EntityProjectileComponent(ROObject owner) : base(owner)
    {
    }

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

    /// <summary>投射物计时不依赖实体事件，无需订阅。</summary>
    protected override void EventSubscribe()
    {
        
    }

    /// <summary>没有事件订阅，无需解绑。</summary>
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
    #endregion

    /// <summary>挂载完成回调，当前无额外初始化逻辑。</summary>
    public override void Attached()
    {
    }
}
