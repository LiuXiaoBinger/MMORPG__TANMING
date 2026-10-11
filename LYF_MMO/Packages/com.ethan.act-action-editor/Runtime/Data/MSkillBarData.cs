using System;

/// <summary>RO技能进度条制作数据，保留触发时刻和独立持续时间。</summary>
[Serializable]
public sealed class MSkillBarData
{
    /// <summary>技能条出现时刻，从技能开始计时，单位秒，非负。</summary>
    public float At;

    /// <summary>技能条持续显示时间，单位秒，非负；终点为At加Duration。</summary>
    public float Duration;

    /// <summary>RO技能条配置索引，用于关联宿主显示规则。</summary>
    public int Index;
}

