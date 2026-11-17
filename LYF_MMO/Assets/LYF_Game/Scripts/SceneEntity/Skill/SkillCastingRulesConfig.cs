using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity 编辑器中保存施法规则的资源，导出工具从中生成客户端与服务端的配置。
/// </summary>
public sealed class SkillCastingRulesConfig : ScriptableObject
{
    /// <summary>按技能主表 ID 配置打断与移动权限；同一技能的各动作段共用一行。</summary>
    public List<SkillCastingRules> Rules = new List<SkillCastingRules>();
}
