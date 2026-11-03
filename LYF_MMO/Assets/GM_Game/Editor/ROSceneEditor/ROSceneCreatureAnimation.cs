using System;

/// <summary>一个已迁移动作的显示名称与主工程资源路径。</summary>
[Serializable]
public sealed class ROSceneCreatureAnimation
{
    /// <summary>沿用测试工程的中文动作名称。</summary>
    public string displayName;
    /// <summary>主工程中 AnimationClip 的 Assets 相对路径。</summary>
    public string clipPath;
}
