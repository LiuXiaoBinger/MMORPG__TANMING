using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>将原技能编辑器的组合列表发布到客户端和服务器的同源规则。</summary>
public static class SkillCombinationAssetTools
{
    /// <summary>校验主技能、子技能和引用顺序，然后更新已有施法规则资产并同源导出。</summary>
    /// <param name="binding">原编辑器中保存的组合作者绑定。</param>
    public static void Export(SkillEditorBinding binding)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出运行模式后导出组合技能。");
        // 组合配置引用的真实主技能 ID 列表，不复制动画事件。
        SkillConfigSO config = binding.Config;
        if (config.TypeToken != 1 || config.skillID != binding.SkillId) throw new InvalidOperationException("组合类型或主技能身份不一致。");
        if (config.CombineSkillList.Count == 0) throw new InvalidOperationException("组合技能至少需要一项子技能。");
        // 子技能按列表顺序校验，重复引用允许，当前仅开放普通子技能。
        for (int index = 0; index < config.CombineSkillList.Count; index++)
        {
            // 原子技能作者配置；自己的编辑内容和 GUID 保持不变。
            SkillConfigSO child = SkillEditorWindow.FindCombinationChild(config.CombineSkillList[index]);
            if (child == null || child.TypeToken != 0 || child == config)
                throw new InvalidOperationException("组合第 " + (index + 1) + " 段必须引用已存在的普通技能。");
        }
        // 唯一规则资产，保留此前的互断和移动配置。
        SkillCastingRulesConfig rules = AssetDatabase.LoadAssetAtPath<SkillCastingRulesConfig>(SkillCastingRulesExportTool.ConfigAssetPath);
        Undo.RecordObject(rules, "导出组合技能");
        // 目标规则行，只有本组合的运行类型和列表会更新。
        SkillCastingRules rule = null;
        // 原配置列表的稳定索引，不修改其他技能行。
        for (int index = 0; index < rules.Rules.Count; index++)
        {
            if (rules.Rules[index].SkillId == binding.SkillId) rule = rules.Rules[index];
        }
        if (rule == null)
        {
            rule = new SkillCastingRules { SkillId = binding.SkillId };
            rules.Rules.Add(rule);
        }
        rule.TypeToken = config.TypeToken;
        rule.CombineSkillList = new List<int>(config.CombineSkillList);
        SkillCastingRulesExportTool.ExportRules(rules);
        AssetDatabase.SaveAssetIfDirty(config);
    }
}
