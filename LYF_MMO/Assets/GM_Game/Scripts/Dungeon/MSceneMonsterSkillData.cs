using System;

/// <summary>魔神塔一个原技能的客户端只读时间轴；权威伤害由服务器执行。</summary>
[Serializable]
public sealed class MSceneMonsterSkillData
{
    /// <summary>原SkillTable技能编号。</summary>
    public int Id;
    /// <summary>原 SkillTargetRangeType 为 ESTR_Target 时需要真实角色目标。</summary>
    public bool NeedTarget;
    /// <summary>原 AnimList 的单段动作，起点相对本次 FireSkill，单位秒。</summary>
    [Serializable]
    public sealed class AnimationPart
    {
        /// <summary>该段原动画资源完整路径，继续覆盖公共 Animator 的 Skill 槽。</summary>
        public string ClipPath;
        /// <summary>该段原 begin_time，归一化播放时间须减去此起点。</summary>
        public float BeginTime;
    }
    /// <summary>原分段动画时间轴；未配置时保持 ClipPath 的单段行为。</summary>
    public AnimationPart[] Animations = new AnimationPart[0];
    /// <summary>原ClipName解析后的正式动画资源地址。</summary>
    public string ClipPath;
    /// <summary>原SingingClips首段地址；空值保持当前待机。</summary>
    public string SingingClipPath;
    /// <summary>原MSkillData.Time，单位秒，包含晚于动画的结果。</summary>
    public float Time;
    /// <summary>原动作状态占用秒数，协议DurationMs据此换算倍率，尾事件仍按Time执行。</summary>
    public float SkillStateTime;
    /// <summary>原动画长度，单位秒，不用整个技能时长拉伸短动画。</summary>
    public float AnimationLength;
    /// <summary>原吟唱声音经原FMOD事件离线导出的音频地址。</summary>
    public string SingingAudioPath;
    /// <summary>攻击阶段按原At排序的特效。</summary>
    public MSceneMonsterFxData[] Effects;
    /// <summary>吟唱阶段特效，结束吟唱时回收。</summary>
    public MSceneMonsterFxData[] SingingEffects;
    /// <summary>攻击阶段原音频事件。</summary>
    public MSceneMonsterAudioData[] Audio;
    /// <summary>原ResultList的受击与弹道表现，零基序号保持原配置。</summary>
    public MSceneMonsterResultData[] Results;
}
