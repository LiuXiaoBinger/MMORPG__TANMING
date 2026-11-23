using UnityEngine;

/// <summary>保存本机世界镜头模式、定制俯角及自定义选项，供设置窗口和场景相机共用。</summary>
public static class WorldCameraSettings
{
    /// <summary>首次进入世界时使用智能 3D，保留桌面端完整的右键环绕操作。</summary>
    private const string ModeKey = "LYF.Camera.Mode";
    /// <summary>单独保存自定义开关位，切换到固定预设时保留原组合。</summary>
    private const string FeaturesKey = "LYF.Camera.CustomFeatures";
    /// <summary>定制 2.5D 俯角的存档键，FreeLook 的竖轴范围为零至一。</summary>
    private const string PitchKey = "LYF.Camera.TunedPitch";
    /// <summary>默认 2.5D 在中层与顶层轨道之间的固定俯视位置。</summary>
    public const float DefaultOverheadPitch = 0.72f;

    /// <summary>两种 2.5D 共用的操作，保留水平转动、滚轮和轻功落点。</summary>
    private const WorldCameraFeature Overhead = WorldCameraFeature.Reset | WorldCameraFeature.TurnWithMovement |
        WorldCameraFeature.TurnOnPath | WorldCameraFeature.WheelZoom | WorldCameraFeature.DragYaw | WorldCameraFeature.QinggongGround;
    /// <summary>普通 3D 额外允许俯仰、首领拉高和技能镜头效果。</summary>
    private const WorldCameraFeature Normal = Overhead | WorldCameraFeature.DragPitch |
        WorldCameraFeature.RaiseForBoss | WorldCameraFeature.SkillAccent;
    /// <summary>录屏首次展示的自定义组合，后续只由自定义页改写。</summary>
    private const WorldCameraFeature InitialCustom = WorldCameraFeature.Reset | WorldCameraFeature.RaiseForBoss |
        WorldCameraFeature.RaiseForCombat | WorldCameraFeature.SkillAccent | WorldCameraFeature.FaceBoss |
        WorldCameraFeature.WheelZoom | WorldCameraFeature.DragYaw | WorldCameraFeature.DragPitch | WorldCameraFeature.QinggongGround;

    /// <summary>保留在内存中的自定义组合，游戏帧循环无需读取磁盘设置。</summary>
    private static WorldCameraFeature _custom = (WorldCameraFeature)PlayerPrefs.GetInt(FeaturesKey, (int)InitialCustom);
    /// <summary>当前生效模式，首次读取后跨场景共用。</summary>
    public static WorldCameraMode Mode { get; private set; } =
        (WorldCameraMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, (int)WorldCameraMode.Smart3D), 0, (int)WorldCameraMode.Custom);
    /// <summary>上一次切入定制 2.5D 时捕获的俯角，换场景或重启后继续使用。</summary>
    public static float TunedPitch { get; private set; } = Mathf.Clamp01(PlayerPrefs.GetFloat(PitchKey, DefaultOverheadPitch));

    /// <summary>当前模式生效的完整功能组合，用于驱动实机镜头和预设勾选状态。</summary>
    public static WorldCameraFeature Features
    {
        get
        {
            switch (Mode)
            {
                case WorldCameraMode.Smart3D: return Normal | WorldCameraFeature.RaiseForCombat;
                case WorldCameraMode.Normal3D: return Normal;
                case WorldCameraMode.Custom: return _custom;
                default: return Overhead;
            }
        }
    }

    /// <summary>切换世界镜头模式，并在切入定制 2.5D 的当帧捕获俯角。</summary>
    /// <param name="mode">镜头页选中的唯一模式。</param>
    public static void SelectMode(WorldCameraMode mode)
    {
        if (Mode == mode) return;
        // 当前世界的相机，登录阶段也可以先保存模式再在入场时应用。
        CameraMgr camera = CameraMgr.Instance;
        if (mode == WorldCameraMode.Tuned25D && camera != null)
        {
            TunedPitch = camera.WorldPitch;
            PlayerPrefs.SetFloat(PitchKey, TunedPitch);
        }
        Mode = mode;
        PlayerPrefs.SetInt(ModeKey, (int)mode);
        if (camera != null) camera.ApplyWorldMode();
    }

    /// <summary>判断当前模式中指定的镜头功能是否开启。</summary>
    /// <param name="feature">需要查询的单项功能位。</param>
    public static bool Allows(WorldCameraFeature feature)
    {
        return (Features & feature) != 0;
    }

    /// <summary>保存自定义页的一项选择，预设模式的组合由模式本身决定。</summary>
    /// <param name="feature">用户操作的单项功能位。</param>
    /// <param name="enabled">勾选时开启对应功能。</param>
    public static void SetCustomFeature(WorldCameraFeature feature, bool enabled)
    {
        if (Mode != WorldCameraMode.Custom) return;
        if (enabled) _custom |= feature;
        else _custom &= ~feature;
        PlayerPrefs.SetInt(FeaturesKey, (int)_custom);
    }
}
