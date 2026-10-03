using UnityEngine;

/// <summary>
/// 模型加载和动画控制器的原始配置数据。
/// 按实体模型会话保存，用于对象池复用时恢复加载参数。
/// </summary>
public sealed class MModelOriginData
{
    /// <summary>模型预制体的资源路径/ Location。</summary>
    public string ModelLocation { get; set; }
    /// <summary>动画控制器 RuntimeAnimatorController 的资源路径。</summary>
    public string ControllerLocation { get; set; }
    /// <summary>是否使用简化版 MObjectAnimator 而非完整 MAnimator。</summary>
    public bool UseObjectAnimator { get; set; }
    /// <summary>是否延迟加载模型，用于需要先隐藏后显示的场景。</summary>
    public bool LoadModelDelay { get; set; }
    /// <summary>自定义光源方向，用于模型预览等特殊光照场景。</summary>
    public Vector3 CustomLightDirection { get; set; }

    /// <summary>重置所有字段为默认值，用于对象池复用前清理。</summary>
    public void Reset()
    {
        ModelLocation = string.Empty;
        ControllerLocation = string.Empty;
        UseObjectAnimator = false;
        LoadModelDelay = false;
        CustomLightDirection = Vector3.zero;
    }
}
