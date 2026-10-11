// 通用交互窗口与预设属性制作，宿主提交条件通过后的实际效果。
using System;
using Ethan.ActionEditor;
using UnityEditor;
using UnityEngine;

/// <summary>技能窗口的双语属性与原编辑操作适配，不改运行时或资产字段身份。</summary>
public partial class SkillEditorWindow
{
    /// <summary>在当前交互窗口列表中生成不冲突的原英文 ID。</summary>
    /// <param name="prefix">原英文 ID 前缀，不随显示语言改变。</param>
    string UniqueWindowId(string prefix)
    {
        // 待生成的原英文交互窗口 ID。
        string id = prefix;
        // 用于避开 ID 冲突的递增数字后缀，从 1 开始。
        int suffix = 1;
        while (interactionWindows.Exists(/* 检查标识是否重复的原交互窗口，仅读取其 windowId。 */ w => w != null && w.id == id)) id = prefix + "-" + suffix++;
        return id;
    }

    /// <summary>在当前帧按原默认范围添加交互窗口，保持原撤销操作。</summary>
    void AddInteractionHere()
    {
        PushUndo();
        interactionWindows.Add(new InteractionWindow {
            id = UniqueWindowId("interaction"), keyNumber = frameSelectIndex,
            endKeyNumber = frameSelectIndex + 5, damageMultiplier = 1, suppressHitReaction = false });
        selType = SelType.Interaction;
        selIdx = interactionWindows.Count - 1;
    }

    /// <summary>绘制原交互窗口、条件说明及预设按钮，框架说明双语显示。</summary>
    /// <param name="index">原列表的零起始索引。</param>
    void DrawInteractionInspector(int index)
    {
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Inclusive frame window. Matching signals evaluate reusable conditions; the highest-priority valid window wins.", "窗口包含起止帧。匹配信号后评估可复用条件，优先采用有效窗口中优先级最高的一项。"), MessageType.None);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(SkillEditorLocalization.Text("Evade preset", "闪避预设"))) ApplyInteractionPreset(index, InteractionResponse.Evade);
        if (GUILayout.Button(SkillEditorLocalization.Text("Block preset", "格挡预设"))) ApplyInteractionPreset(index, InteractionResponse.Block);
        if (GUILayout.Button(SkillEditorLocalization.Text("Parry preset", "招架预设"))) ApplyInteractionPreset(index, InteractionResponse.Parry);
        EditorGUILayout.EndHorizontal();
        DrawSerializedSelection("interactionWindows", index);
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text("All conditions must pass. Missing references block playback. Create reusable conditions through Create > Combat > Interaction Conditions. The host submits signals through TryResolveInteraction.", "所有条件必须通过，缺失引用会阻止播放。通过 Create > Combat > Interaction Conditions 创建条件资源；宿主通过 TryResolveInteraction 提交信号。"), MessageType.None);
    }

    /// <summary>按原闪避、格挡或招架数值应用预设，响应 ID 仍从原枚举名生成。</summary>
    /// <param name="index">原列表的零起始索引。</param>
    /// <param name="response">原闪避、格挡或招架响应类型。</param>
    void ApplyInteractionPreset(int index, InteractionResponse response)
    {
        if (index < 0 || index >= interactionWindows.Count) return;
        RunAuthoringOperation(SkillEditorLocalization.Text("Apply interaction preset", "应用交互预设"), () => {
            // 当前交互窗口原对象，预设编辑仍作用于原引用。
            var w = interactionWindows[index];
            w.id = UniqueWindowId(response.ToString().ToLowerInvariant());
            w.signal = InteractionQuery.IncomingHit;
            w.response = response;
            if (response == InteractionResponse.Parry) w.priority = 30;
            else if (response == InteractionResponse.Evade) w.priority = 20;
            else w.priority = 10;
            if (response == InteractionResponse.Evade) w.sourceAngle = 360;
            else w.sourceAngle = 120;
            if (response == InteractionResponse.Block) w.damageMultiplier = .2f;
            else w.damageMultiplier = 0;
            w.suppressHitReaction = true;
            w.maxActivations = 0;
        });
    }
}
