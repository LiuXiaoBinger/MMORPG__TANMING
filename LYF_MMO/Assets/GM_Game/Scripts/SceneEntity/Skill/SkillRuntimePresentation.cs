using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按表现表 ID 加载的技能表现资源，保存动画信息、特效、声音和程序位移指令。
/// 它只描述客户端表现，不计算服务器的命中和伤害。
/// </summary>
public sealed class SkillRuntimePresentation : ScriptableObject
{
    /// <summary>原表现表行的唯一 ID，运行时按此值验证异步加载结果。</summary>
    public int PresentationId;

    /// <summary>原技能主表 ID，仅用于校验编辑资产与正式表现行的归属。</summary>
    public int SkillId;

    /// <summary>旧表现表动画剪辑的 YooAsset Location，保持原表值不变。</summary>
    public string AnimationLocation;

    /// <summary>旧表现表 DurationMs 换算的完整动画秒数，用于动画身份校验；晚于动画的制作事件另用时间轴范围。</summary>
    public float DurationSeconds;

    /// <summary>作者制作时间轴的最后触发秒数，至少覆盖原动画；零兼容尚未重新导出的旧资产。</summary>
    public float TimelineDurationSeconds;

    /// <summary>时间轴允许触发事件的最后时刻，单位秒，不包含声音或粒子继续播放的尾部时间。
    /// TimelineDurationSeconds 为 0 时使用完整动画时长；这不改变技能状态的结束时刻。</summary>
    public float EffectiveTimelineDurationSeconds
    {
        get
        {
            if (TimelineDurationSeconds == 0f)
            {
                return DurationSeconds;
            }
            return TimelineDurationSeconds;
        }
    }

    /// <summary>可由当前粒子处理器准确定位的事件，按时间和原事件序号升序排列。</summary>
    public List<SkillRuntimeFxEvent> FxEvents = new List<SkillRuntimeFxEvent>();

    /// <summary>编辑器音效轨道的单次三维声音，按触发秒数和作者序号排序。</summary>
    public List<SkillRuntimeAudioEvent> AudioEvents = new List<SkillRuntimeAudioEvent>();

    /// <summary>由程序计算的位移段；空列表表示此资源没有额外位移指令。</summary>
    public List<MSkillChargeData> ChargeList = new List<MSkillChargeData>();
}
