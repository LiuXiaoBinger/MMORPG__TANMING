using System;
using System.Collections.Generic;

/// <summary>客户端 JsonUtility 与服务端 JSON 解析器共用的强类型配置根，不包含运行时状态。</summary>
[Serializable]
public sealed class SkillCastingRulesCollection
{
    /// <summary>各技能的编辑规则；一技能一行，缺行时使用禁止互断、禁止施法移动的稳定默认值。</summary>
    public List<SkillCastingRules> Rules = new List<SkillCastingRules>();
}
