using System;
using System.Collections.Generic;

/// <summary>
/// JSON 的根数据结构，Rules 保存每个技能的施法规则。
/// 客户端与服务端约定使用相同字段格式；这里不保存施法运行状态。
/// </summary>
[Serializable]
public sealed class SkillCastingRulesCollection
{
    /// <summary>各技能的编辑规则；一技能一行，缺行时使用禁止互断、禁止施法移动的稳定默认值。</summary>
    public List<SkillCastingRules> Rules = new List<SkillCastingRules>();
}
