using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>创建本项目 E→2 组合示例，并将原技能编辑器的组合列表发布到两端同源规则。</summary>
public static class SkillCombinationAssetTools
{
    /// <summary>新增示例技能的主表 ID，原 10090 为轻功，不能占用。</summary>
    public const int ExampleSkillId = 10100;
    /// <summary>组合作者资产和绑定所在目录，沿用现有技能编辑资产分类。</summary>
    private const string AuthoringDirectory = "Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/";

    /// <summary>创建一次正式 E→2 作者资产，已有草稿保持用户编辑内容。</summary>
    [MenuItem("Tools/技能/YCombat/创建E接2组合技能")]
    public static void CreateExample()
    {
        // 当前 E 技能的正式绑定，复用其预览模型及职业身份。
        SkillEditorBinding source = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(AuthoringDirectory + "SkillEditorBinding_3004001.asset");
        // 独立组合作者资产路径，不覆盖任何原子技能。
        string path = AuthoringDirectory + "SkillConfigSO_Combine_10100.asset";
        // 已有草稿复用原资产，首次才设置默认的 E→2 顺序。
        SkillConfigSO config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SkillConfigSO>();
            config.InitializeExplicitTiming(30f);
            config.SetActionEditorSchemaVersion(1);
            config.skillID = ExampleSkillId;
            config.skillName = "清风接斩天";
            config.skillDescription = "依次施放清风剑法（E）和斩天剑（2）。";
            config.TypeToken = 1;
            config.CombineSkillList.Add(10040);
            config.CombineSkillList.Add(10060);
            AssetDatabase.CreateAsset(config, path);
        }
        // 主组合没有独立动画表现行，绑定只保存身份和预览模型。
        string bindingPath = AuthoringDirectory + "SkillEditorBinding_Combine_10100.asset";
        // 正式组合绑定供 Inspector 打开与导出，已有绑定不重置。
        SkillEditorBinding binding = AssetDatabase.LoadAssetAtPath<SkillEditorBinding>(bindingPath);
        if (binding == null)
        {
            binding = ScriptableObject.CreateInstance<SkillEditorBinding>();
            binding.SkillId = ExampleSkillId;
            binding.EffectId = 2010001;
            binding.ProfessionId = source.ProfessionId;
            binding.ModelKey = source.ModelKey;
            binding.PreviewModel = source.PreviewModel;
            binding.Config = config;
            AssetDatabase.CreateAsset(binding, bindingPath);
        }
        AssetDatabase.SaveAssetIfDirty(config);
        AssetDatabase.SaveAssetIfDirty(binding);
        Export(binding);
        Selection.activeObject = binding;
        SkillEditorAssetTools.Open(binding);
    }

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
