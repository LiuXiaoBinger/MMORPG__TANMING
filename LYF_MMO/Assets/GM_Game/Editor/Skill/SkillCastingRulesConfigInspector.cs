using System;
using UnityEditor;
using UnityEngine;

/// <summary>展示 RO 互断/移动字段和固定跳跃策略，并提供同源两端导出入口。</summary>
[CustomEditor(typeof(SkillCastingRulesConfig))]
public sealed class SkillCastingRulesConfigInspector : Editor
{
    /// <summary>启用后订阅显示语言重绘，语言切换不导出规则。</summary>
    private void OnEnable()
    {
        SkillEditorLocalization.Changed += Repaint;
    }

    /// <summary>关闭检查器时解除显示层订阅。</summary>
    private void OnDisable()
    {
        SkillEditorLocalization.Changed -= Repaint;
    }

    /// <summary>绘制可编辑规则列表；不把未实现的跳跃取消包装成可勾选字段。</summary>
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text("InteruptSkill lists the old skill IDs that this new skill may interrupt. MoveCasting permits horizontal movement while casting. Starting a jump during a skill is disabled; existing airborne gravity and landing continue. The three Q attacks share 10010.", "InteruptSkill：新技能名单填写允许打断的旧技能 ID。MoveCasting：允许施法中水平移动。技能中主动起跳固定禁止；已有空中重力和落地继续。Q 三段共用 10010。"), MessageType.Info);
        serializedObject.Update();
        // 原规则资产可见属性游标；嵌套规则保持原列表顺序和字段名称。
        SerializedProperty property = serializedObject.GetIterator();
        if (property.NextVisible(true))
        {
            do
            {
                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                {
                    SkillEditorPropertyLabels.DrawProperty(property);
                }
            } while (property.NextVisible(false));
        }
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button(SkillEditorLocalization.Text("Export client and server casting rules", "导出客户端与服务端施法规则")))
        {
            try
            {
                SkillCastingRulesExportTool.ExportRules((SkillCastingRulesConfig)target);
            }
            catch (Exception exception)
            {
                // 原异常文本只翻译已知框架说明，技能 ID、路径和未知异常原样保留。
                Debug.LogError(SkillEditorLocalization.Text("Casting rules export failed: ", "施法规则导出失败：") + SkillEditorPropertyLabels.HostMessage(exception.Message));
            }
        }
    }
}
