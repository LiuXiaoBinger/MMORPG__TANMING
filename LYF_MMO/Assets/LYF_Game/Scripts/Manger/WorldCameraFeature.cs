using System;

/// <summary>镜头页依次展示的十二项功能，每一位对应一个可独立保存的开关。</summary>
[Flags]
public enum WorldCameraFeature
{
    /// <summary>鼠标中键恢复默认距离并朝向角色前方。</summary>
    Reset = 1 << 0,
    /// <summary>左右移动时沿水平方向转动镜头。</summary>
    TurnWithMovement = 1 << 1,
    /// <summary>与首领交战时抬高并拉远观察位置。</summary>
    RaiseForBoss = 1 << 2,
    /// <summary>与普通敌人交战时抬高并拉远观察位置。</summary>
    RaiseForCombat = 1 << 3,
    /// <summary>自动寻路时逐渐面向角色行进方向。</summary>
    TurnOnPath = 1 << 4,
    /// <summary>连续两次左击同一处地面时朝向该点。</summary>
    DoubleClickGround = 1 << 5,
    /// <summary>播放技能表现轨道单独配置的镜头重音。</summary>
    SkillAccent = 1 << 6,
    /// <summary>交战中将当前目标首领保持在角色前方的视线内。</summary>
    FaceBoss = 1 << 7,
    /// <summary>滚动鼠标滚轮调整观察距离。</summary>
    WheelZoom = 1 << 8,
    /// <summary>按住鼠标右键可改变水平角。</summary>
    DragYaw = 1 << 9,
    /// <summary>按住鼠标右键可改变俯仰角。</summary>
    DragPitch = 1 << 10,
    /// <summary>施放轻功时标出角色正下方的地面位置。</summary>
    QinggongGround = 1 << 11
}
