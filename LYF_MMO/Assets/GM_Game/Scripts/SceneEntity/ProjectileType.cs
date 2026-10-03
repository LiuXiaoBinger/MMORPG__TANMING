/// <summary>
/// 参考项目 bullet 子类对应的飞行物形态枚举。
/// 用于工厂根据不同弹道表现创建对应的投射物实体类型。
/// </summary>
public enum ProjectileType
{
    /// <summary>动画投射物：仅播放动画无实体碰撞表现</summary>
    Anim = 0,
    /// <summary>虚幻（无实体）投射物：无碰撞体的纯表现飞行物</summary>
    Unreal = 1,
    /// <summary>虚幻追踪投射物：无碰撞体但追踪目标的飞行物</summary>
    UnrealTarget = 2,
    /// <summary>实体投射物：带碰撞体的直线飞行物</summary>
    Real = 3,
    /// <summary>实体追踪投射物：带碰撞体并追踪目标的飞行物</summary>
    RealTarget = 4,
    /// <summary>环形实体投射物：环形范围伤害的飞行物</summary>
    RealRing = 5,
    /// <summary>矩形实体投射物：矩形范围伤害的飞行物</summary>
    RealRect = 6
}
