using System.Collections.Generic;

/// <summary>编辑导出、客户端装载和服务端装载共用的规则校验，防止非法 ID 或重复配置覆盖权限。</summary>
public static class SkillCastingRulesValidation
{
    /// <summary>逐行验证施法规则，全部通过才允许替换运行索引或导出文件。</summary>
    /// <param name="rules">待验证的完整技能规则列表，列表缺失视为无效。</param>
    /// <param name="error">失败时给出具体技能或重复 ID 原因，成功时为空。</param>
    /// <returns>技能 ID 唯一且互断列表只含唯一正数技能 ID 时为 true。</returns>
    public static bool Validate(List<SkillCastingRules> rules, out string error)
    {
        error = string.Empty;
        if (rules == null)
        {
            error = "施法规则列表缺失。";
            return false;
        }
        // 主表 ID 集合只在导出或进程首次装载时分配，用于拒绝重复规则行。
        HashSet<int> skillIds = new HashSet<int>();
        // 规则行的零起始下标，每行代表一个技能主表 ID。
        for (int index = 0; index < rules.Count; index++)
        {
            // 当前规则行，不允许空行或非正数主表 ID。
            SkillCastingRules rule = rules[index];
            if (rule == null || rule.SkillId <= 0 || !skillIds.Add(rule.SkillId))
            {
                error = "施法规则含空行、非法技能 ID 或重复技能 ID，行=" + index;
                return false;
            }
            if (rule.InteruptSkill == null)
            {
                error = "技能 " + rule.SkillId + " 的 InteruptSkill 列表缺失。";
                return false;
            }
            // 本行允许打断的技能 ID 集合；相同 ID 不能重复列出。
            HashSet<int> interruptedIds = new HashSet<int>();
            // 当前互断 ID 的零起始下标，列表的每个值都必须为正数。
            for (int interruptIndex = 0; interruptIndex < rule.InteruptSkill.Count; interruptIndex++)
            {
                // 被允许打断的旧技能主表 ID，而不是 UUID 或新技能 ID。
                int interruptedId = rule.InteruptSkill[interruptIndex];
                if (interruptedId <= 0 || !interruptedIds.Add(interruptedId))
                {
                    error = "技能 " + rule.SkillId + " 的 InteruptSkill 含非法或重复 ID。";
                    return false;
                }
            }
        }
        return true;
    }
}
