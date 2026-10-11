using System.Collections.Generic;
using UnityEngine;

/// <summary>按 SkillPresentationInfo.Id 定位的只读技能表现资产，不参与权威结算。</summary>
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

    /// <summary>事件最后触发上限，单位秒，不含音频或粒子自然尾；旧资产零值沿用原动画长度，不延长动作解锁。</summary>
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

    /// <summary>RO 同名程序位移指令，默认空兼容旧资产；与动画根窗口禁止重叠。</summary>
    public List<MSkillChargeData> ChargeList = new List<MSkillChargeData>();

    /// <summary>只消费实际动画水平根位移的半开时间窗口，不修改施法移动权限。</summary>
    public List<SkillRuntimeRootMotionSegment> RootMotionSegments = new List<SkillRuntimeRootMotionSegment>();
}
