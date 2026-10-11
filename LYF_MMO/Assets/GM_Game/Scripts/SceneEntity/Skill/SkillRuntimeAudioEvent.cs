using System;
using UnityEngine;

/// <summary>编辑器导出的单次三维音效指令，按动作开始后的秒数触发。</summary>
[Serializable]
public sealed class SkillRuntimeAudioEvent
{
    /// <summary>作者音效列表的零起始序号，用于同一时刻稳定排序。</summary>
    public int SourceEventIndex;
    /// <summary>作者时间轴的非负触发帧。</summary>
    public int Frame;
    /// <summary>动作开始后的触发时间，单位秒。</summary>
    public float TimeSeconds;
    /// <summary>由运行配置保持资源依赖的原音频剪辑。</summary>
    public AudioClip Clip;
    /// <summary>单次声音音量，取值零至一。</summary>
    public float Volume = 1f;
}
