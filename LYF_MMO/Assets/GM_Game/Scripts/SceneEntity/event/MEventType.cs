/// <summary>
/// 实体内部使用的事件名称。
/// 输入、移动、动作、战斗和生命周期通过这些名称找到监听器；声明事件不代表已有业务发送它。
/// </summary>
public enum MEventType
{
    /// <summary>无效类型，作为枚举起始占位。</summary>
    Invalid = 0,

    // ===== 实体生命周期事件 =====
    /// <summary>实体已创建。</summary>
    Created,
    /// <summary>实体已初始化完成。</summary>
    Initialized,
    /// <summary>实体进入休眠状态（对象池休眠或场景卸载）。</summary>
    Sleeping,
    /// <summary>实体从休眠中唤醒。</summary>
    Woken,
    /// <summary>实体即将销毁（回收前）。</summary>
    PendingDestroy,
    /// <summary>实体正在销毁。</summary>
    Destroying,
    /// <summary>实体已完全销毁。</summary>
    Destroyed,

    // ===== 变换与移动事件 =====
    /// <summary>实体逻辑位置或朝向发生变化。</summary>
    TransformChanged,
    /// <summary>实体开始移动。</summary>
    MoveStarted,
    /// <summary>实体停止移动。</summary>
    MoveStopped,
    /// <summary>实体开始跳跃，事件不携带额外载荷。</summary>
    JumpStarted,
    /// <summary>实体落地结束跳跃，事件不携带额外载荷。</summary>
    JumpEnded,

    // ===== 状态与属性事件 =====
    /// <summary>实体动作状态发生切换。</summary>
    StateChanged,
    /// <summary>实体基础属性发生变化。</summary>
    AttributesChanged,
    /// <summary>实体 HP（生命值）发生变化。</summary>
    HpChanged,
    /// <summary>实体 MP（法力值）发生变化。</summary>
    MpChanged,
    /// <summary>实体 Buff 列表发生变化。</summary>
    BuffChanged,

    // ===== 技能与动作状态事件 =====
    /// <summary>技能开始释放。</summary>
    SkillStarted,
    /// <summary>技能释放完成。</summary>
    SkillFinished,
    /// <summary>吟唱开始。</summary>
    SingingStarted,
    /// <summary>特殊动作开始。</summary>
    SpecialActionStarted,
    /// <summary>恐惧逃跑开始。</summary>
    FearRunStarted,
    /// <summary>眩晕开始。</summary>
    StunStarted,
    /// <summary>出生动画开始。</summary>
    BornStarted,
    /// <summary>攀爬开始。</summary>
    ClimbStarted,
    /// <summary>采集开始。</summary>
    CollectionStarted,
    /// <summary>钓鱼开始。</summary>
    FishingStarted,
    /// <summary>跳舞开始。</summary>
    DanceStarted,

    // ===== 战斗事件 =====
    /// <summary>进入战斗状态。</summary>
    CombatStarted,
    /// <summary>退出战斗状态。</summary>
    CombatEnded,
    /// <summary>受到伤害。</summary>
    DamageReceived,
    /// <summary>实体死亡。</summary>
    Death,
    /// <summary>实体复活。</summary>
    Revived,
    /// <summary>投射物命中目标。</summary>
    ProjectileHit,

    // ===== 输入事件（由 EntityInputEventBridge 派发） =====
    /// <summary>统一输入事件；具体动作数据由 InputEventArgs 承载。</summary>
    InputKey,
    /// <summary>请求移动到目标点事件；载荷为 MMoveEventArgs，由移动组件消费。</summary>
    MEvent_Move,
    /// <summary>请求停止移动事件；当前无额外载荷。</summary>
    MEvent_StopMove,
    /// <summary>自定义事件类型，用于业务扩展。</summary>
    Custom,
    /// <summary>导航请求，参数为 MNavigationEventArgs；可以指定目的点及到达后的技能动作。</summary>
    MEvent_Navigation
}
