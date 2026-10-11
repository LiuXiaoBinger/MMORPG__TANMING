using System;

/// <summary>动画根位移的水平接管窗口，重力和碰撞仍由实体移动组件负责。</summary>
[Serializable]
public sealed class SkillRuntimeRootMotionSegment
{
    /// <summary>含起点的窗口开始时间，单位秒。</summary>
    public float StartSeconds;
    /// <summary>不含终点的窗口结束时间，单位秒且大于起点。</summary>
    public float EndSeconds;
    /// <summary>水平动画位移倍率，非负有限值，零禁用该窗口的水平位移。</summary>
    public float Scale = 1f;
}
