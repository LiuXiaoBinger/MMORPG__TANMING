using System;
using System.Collections.Generic;

/// <summary>
/// 一个技能的施法规则：能打断哪些技能，以及施法时能否水平移动。
/// 按技能主表 ID 配置，同一技能的多个动作段共用规则。
/// </summary>
[Serializable]
public sealed class SkillCastingRules
{
    /// <summary>原 SkillInfo.Id，必须大于零，不使用表现行 ID。</summary>
    public int SkillId;

    /// <summary>沿用 RO 字段名；本技能允许打断的正在施法技能 ID，空列表表示不允许互断。</summary>
    public List<int> InteruptSkill = new List<int>();

    /// <summary>沿用 RO MoveCasting；true 允许施法中的普通水平移动，不代表允许主动跳跃或根运动。</summary>
    public bool MoveCasting;
}
