using System;

/// <summary>RO角速度旋转区间；原MArtSkill按AngularVelocity乘每帧秒数旋转角色。</summary>
[Serializable]
public sealed class MSkillRotateData
{
    /// <summary>旋转开始时刻，从技能开始计时，单位秒，非负。</summary>
    public float At;

    /// <summary>旋转结束时刻，单位秒，不小于At。</summary>
    public float End;

    /// <summary>带符号的水平旋转角速度，单位度/秒；正负决定旋转方向。</summary>
    public float AngularVelocity;
}

