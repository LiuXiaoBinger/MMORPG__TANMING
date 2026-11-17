using System;
using UnityEditor;
using UnityEngine;

/// <summary>仅编辑器使用的界面文案选择器，语言偏好独立保存于本机 EditorPrefs。</summary>
public static class SkillEditorLocalization
{
    /// <summary>本项目技能编辑器独立的个人偏好键，不使用技能资产或场景保存状态。</summary>
    private const string PreferenceKey = "LYF.YCombatEditor.Language";

    /// <summary>当前编辑器进程缓存的语言，域重载时从个人偏好恢复。</summary>
    private static SkillEditorLanguage _language = LoadLanguage();

    /// <summary>语言实际变化时通知已打开窗口刷新显示；监听方不得修改技能数据或预览时钟。</summary>
    public static event Action Changed;

    /// <summary>当前个人界面语言，只接受中文或英文；设置相同值不会产生通知或保存操作。</summary>
    public static SkillEditorLanguage Language
    {
        get { return _language; }
        set
        {
            if (value != SkillEditorLanguage.Chinese && value != SkillEditorLanguage.English)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "技能编辑器只支持中文和英文。");
            }
            if (_language == value)
            {
                return;
            }
            _language = value;
            EditorPrefs.SetInt(PreferenceKey, (int)value);
            Changed?.Invoke();
            // 属性面板和多个编辑窗口共享个人偏好，重绘不会标脏资源或改变播放状态。
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    /// <summary>选择当前语言的展示文案，调用方传入的内部身份和资源名称不在此转换。</summary>
    /// <param name="english">英文按钮、标签、提示或对话框文案。</param>
    /// <param name="chinese">含义对应的简体中文展示文案。</param>
    /// <returns>当前语言对应的原始文案。</returns>
    public static string Text(string english, string chinese)
    {
        if (_language == SkillEditorLanguage.English)
        {
            return english;
        }
        return chinese;
    }

    /// <summary>为 IMGUI 按钮、菜单和标签创建当前语言的显示内容。</summary>
    /// <param name="english">英文展示文案。</param>
    /// <param name="chinese">对应中文展示文案。</param>
    /// <returns>仅供本次绘制使用的 GUIContent，不保存到技能资源。</returns>
    public static GUIContent Content(string english, string chinese)
    {
        return new GUIContent(Text(english, chinese));
    }

    /// <summary>恢复合法个人偏好；无偏好或历史无效值均使用中文默认值。</summary>
    /// <returns>本机保存的中文或英文界面语言。</returns>
    private static SkillEditorLanguage LoadLanguage()
    {
        // 个人偏好整数只表示界面语言，不是技能配置或协议枚举。
        int savedLanguage = EditorPrefs.GetInt(PreferenceKey, (int)SkillEditorLanguage.Chinese);
        if (savedLanguage == (int)SkillEditorLanguage.English)
        {
            return SkillEditorLanguage.English;
        }
        return SkillEditorLanguage.Chinese;
    }
}
