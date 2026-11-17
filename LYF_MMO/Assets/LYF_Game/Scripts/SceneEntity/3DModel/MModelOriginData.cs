using UnityEngine;

/// <summary>
/// 记录本次模型加载的资源地址和动画器选择，Reset 时清空。
/// 延迟加载和光照字段保留在数据结构中，MModel 当前不读取它们执行对应逻辑。
/// </summary>
public sealed class MModelOriginData
{
    /// <summary>模型预制体的资源路径/ Location。</summary>
    public string ModelLocation { get; set; }
    /// <summary>动画控制器 RuntimeAnimatorController 的资源路径。</summary>
    public string ControllerLocation { get; set; }
    /// <summary>是否使用简化版 MObjectAnimator 而非完整 MAnimator。</summary>
    public bool UseObjectAnimator { get; set; }
    /// <summary>预留的延迟加载标记，MModel 当前未据此控制加载。</summary>
    public bool LoadModelDelay { get; set; }
    /// <summary>预留的自定义光照方向，MModel 当前未据此修改光照。</summary>
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
