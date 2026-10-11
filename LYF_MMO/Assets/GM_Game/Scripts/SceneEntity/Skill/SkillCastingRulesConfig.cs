using System.Collections.Generic;
using UnityEngine;

/// <summary>可编辑的 RO 施法权限配置；通过导出菜单同时更新客户端与服务端数据，不改旧技能表。</summary>
public sealed class SkillCastingRulesConfig : ScriptableObject
{
    /// <summary>技能主表 ID 对应的互断和移动权限，Q 三段共享 10010 一行。</summary>
    public List<SkillCastingRules> Rules = new List<SkillCastingRules>();
}
