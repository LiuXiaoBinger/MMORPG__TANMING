using System;
using System.Collections.Generic;

/// <summary>普通移动或跳跃动作的声音轨道，不登记为可施放技能。</summary>
[Serializable]
public sealed class SkillRuntimeLocomotionAction
{
    /// <summary>原角色表现动画的完整资源路径，只在实际播放该剪辑时派发声音。</summary>
    public string AnimationLocation;
    /// <summary>作者动画的完整长度，单位秒。</summary>
    public float DurationSeconds;
    /// <summary>按时间和作者序号排序的单次声音指令。</summary>
    public List<SkillRuntimeAudioEvent> AudioEvents = new List<SkillRuntimeAudioEvent>();
}
