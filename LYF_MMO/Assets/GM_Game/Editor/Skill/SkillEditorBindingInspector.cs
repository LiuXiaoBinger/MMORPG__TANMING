using System;
using UnityEditor;
using UnityEngine;

/// <summary>提供绑定资产的明确编辑和发布入口，保存 Y 资产不自动修改正式表现。</summary>
[CustomEditor(typeof(SkillEditorBinding))]
public sealed class SkillEditorBindingInspector : Editor
{
    /// <summary>本检查器最近一次发布失败的具体原因，仅显示给当前绑定，不写入配置或运行资产。</summary>
    private string _exportError;
    /// <summary>资产检查器启用后跟随编辑器语言变化重绘，不修改当前资产。</summary>
    private void OnEnable()
    {
        SkillEditorLocalization.Changed += Repaint;
    }

    /// <summary>检查器关闭时解除重绘订阅，避免关闭窗口仍保留对象引用。</summary>
    private void OnDisable()
    {
        SkillEditorLocalization.Changed -= Repaint;
    }

    /// <summary>显示旧表现身份、原事件参考及编辑/导出操作。</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // 原资产可见属性的游标；不显示 HideInInspector 字段，也不翻译字段身份。
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
        // 当前 Inspector 指向的强类型绑定，按钮只作用于此资产。
        SkillEditorBinding binding = (SkillEditorBinding)target;
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Casting rules use a separate asset. Edit animation and particles here. The three Q attacks use the existing variant selection.", "规则由独立技能规则配置控制；此处编辑动画和粒子。Q 三段仍由旧变体选择。"), MessageType.Info);
        if (GUILayout.Button(SkillEditorLocalization.Text("Edit with SkillEditorWindow", "用 SkillEditorWindow 编辑")))
        {
            SkillEditorAssetTools.Open(binding);
        }
        // 运行或即将进入运行时禁止发布，避免将资产磁盘更新误认为当前缓存和施法的即时热替换。
        bool cannotExport = EditorApplication.isPlayingOrWillChangePlaymode;
        if (cannotExport && binding.PresentationId != 0)
        {
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Exit Play Mode before exporting. The current authoring data will be preserved.", "请退出运行模式后导出，当前技能制作数据会保留。"), MessageType.Info);
        }
        using (new EditorGUI.DisabledScope(cannotExport))
        {
            if (binding.PresentationId != 0 && GUILayout.Button(SkillEditorLocalization.Text("Validate and export this presentation", "校验并导出当前表现")))
            {
                _exportError = null;
                try
                {
                    SkillEditorAssetTools.Export(binding);
                }
                catch (Exception exception)
                {
                    // 保留具体配置错误供作者处理，避免一次预期校验失败破坏 Inspector 的 GUI 调用栈。
                    _exportError = exception.Message;
                }
            }
        }
        if (!string.IsNullOrEmpty(_exportError))
        {
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Export failed: ", "导出失败：") + _exportError, MessageType.Error);
        }
    }
}
