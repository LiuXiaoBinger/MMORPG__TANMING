using UnityEngine;

/// <summary>一个仅存在于编辑预览中的模型实例与动作播放信息。</summary>
public sealed class MScenePreviewInstance
{
    /// <summary>带 DontSave 标志的临时模型，停止预览时销毁。</summary>
    public GameObject GameObject;
    /// <summary>当前采样动作，空表示原节点没有可用模型动作。</summary>
    public AnimationClip Clip;
    /// <summary>该动作开始的编辑器时间，单位为秒。</summary>
    public double StartedAt;
}
