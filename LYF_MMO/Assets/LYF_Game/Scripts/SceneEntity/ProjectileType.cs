/// <summary>
/// 投射物配置类别，构建器用它选择动画、目标、环形或矩形实体子类。
/// 当前这些子类仅区分类别，枚举本身不会实现轨迹、碰撞或伤害。
/// </summary>
public enum ProjectileType
{
    /// <summary>动画类别，构建器使用 AnimProjectileEntity</summary>
    Anim = 0,
    /// <summary>Unreal 类别，当前与 Anim 共用 AnimProjectileEntity</summary>
    Unreal = 1,
    /// <summary>无实体目标类别，当前使用 TargetProjectileEntity</summary>
    UnrealTarget = 2,
    /// <summary>Real 类别，当前进入构建器默认分支，使用 RectProjectileEntity</summary>
    Real = 3,
    /// <summary>实体目标类别，当前使用 TargetProjectileEntity</summary>
    RealTarget = 4,
    /// <summary>环形类别，当前使用 RingProjectileEntity</summary>
    RealRing = 5,
    /// <summary>矩形类别，当前使用 RectProjectileEntity</summary>
    RealRect = 6
}
