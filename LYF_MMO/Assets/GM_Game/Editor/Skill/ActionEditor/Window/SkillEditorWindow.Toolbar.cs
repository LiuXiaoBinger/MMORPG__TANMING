// 资源、预览、语言、帧导航及保存工具栏；语言只存个人偏好，不属于技能修改。
using Ethan.ActionEditor;
using Ethan.ActionEditor.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>技能窗口工具栏，个人语言控件与技能数据编辑的变更检测相互独立。</summary>
public partial class SkillEditorWindow
{
    /// <summary>绘制资源、预览与时间轴工具；切换语言只更新展示缓存，不产生技能编辑事务。</summary>
    void DrawToolbar()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, TOOLBAR_HEIGHT), new Color(.16f, .17f, .19f));
        GUILayout.BeginArea(new Rect(10, 5, position.width - 20, TOOLBAR_HEIGHT - 10));
        EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
        GUILayout.Label("ACT", EditorStyles.boldLabel, GUILayout.Width(34));
        if (GUILayout.Button(SkillEditorLocalization.Text("New", "新建"), EditorStyles.miniButton, GUILayout.Width(44))) CreateNewConfig();
        using (new EditorGUI.DisabledScope(configFile == null))
            if (GUILayout.Button(SkillEditorLocalization.Text("Save", "保存"), EditorStyles.miniButton, GUILayout.Width(44))) SaveConfig();
        if (GUILayout.Button(SkillEditorLocalization.Text("Undo", "撤销"), EditorStyles.miniButton, GUILayout.Width(44))) UndoLatest();
        if (GUILayout.Button(SkillEditorLocalization.Text("Redo", "重做"), EditorStyles.miniButton, GUILayout.Width(44))) RedoLatest();
        GUILayout.Space(10);
        EditorGUIUtility.labelWidth = 44;
        EditorGUI.BeginChangeCheck();
        // 资源选择只管理当前技能，语言下拉不在此变更检测范围内。
        SkillConfigSO selected = (SkillConfigSO)EditorGUILayout.ObjectField(SkillEditorLocalization.Text("Action", "技能"), configFile, typeof(SkillConfigSO), false, GUILayout.MinWidth(150));
        // 本次 GUI 事件是否选择了另一个技能资源。
        bool actionChanged = EditorGUI.EndChangeCheck();
        using (new EditorGUI.DisabledScope(configFile == null))
        {
            if (GUILayout.Button(SkillEditorLocalization.Text("Action properties", "技能属性"), EditorStyles.miniButton, GUILayout.Width(110))) { selType = SelType.Basic; selIdx = -1; }
            if (GUILayout.Button(SkillEditorLocalization.Text("Tools", "工具"), EditorStyles.miniButton, GUILayout.Width(48))) ShowActionTools();
        }
        if (GUILayout.Button(SkillEditorLocalization.Text("Help", "帮助"), EditorStyles.miniButton, GUILayout.Width(42))) ActionEditorHelp.OpenUserGuide();
        GUILayout.Space(6);
        // 保留语言控件之外的 GUI 标记，避免影响后续技能属性变更检测。
        bool previousGuiChanged = GUI.changed;
        EditorGUIUtility.labelWidth = 60;
        EditorGUI.BeginChangeCheck();
        // 两种语言显示自称，整数索引只对应个人界面语言。
        int languageIndex = EditorGUILayout.Popup(SkillEditorLocalization.Text("Language", "语言"), (int)SkillEditorLocalization.Language, new[] { "中文", "English" }, GUILayout.Width(144));
        // 语言改变需退出旧布局，防止本次事件进入技能 Undo 和序列化逻辑。
        bool languageChanged = EditorGUI.EndChangeCheck();
        if (languageChanged) { SkillEditorLocalization.Language = (SkillEditorLanguage)languageIndex; GUI.changed = previousGuiChanged; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
        EditorGUIUtility.labelWidth = 82;
        EditorGUI.BeginChangeCheck();
        previewModel = (GameObject)EditorGUILayout.ObjectField(SkillEditorLocalization.Text("Preview actor", "预览角色"), previewModel, typeof(GameObject), true, GUILayout.MinWidth(220));
        if (EditorGUI.EndChangeCheck()) { StopPreviewSession(); EnsurePreviewInstance(); if (configFile != null) ActionEditorUserSettings.instance.SetPreviewModel(configFile, previewModel); }
        GUILayout.Space(12);
        EditorGUIUtility.labelWidth = 76;
        EditorGUI.BeginChangeCheck();
        animSourceModel = (GameObject)EditorGUILayout.ObjectField(SkillEditorLocalization.Text("Clip source", "动画来源"), animSourceModel, typeof(GameObject), false, GUILayout.MinWidth(200));
        if (EditorGUI.EndChangeCheck()) LoadClipsFromAsset();
        // 场景目标只属于预览，不存入技能配置。
        previewTarget = (GameObject)EditorGUILayout.ObjectField(SkillEditorLocalization.Text("Preview target", "预览目标"), previewTarget, typeof(GameObject), true, GUILayout.Width(230));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
        // 按钮文案只读取当前播放状态。
        string playLabel = SkillEditorLocalization.Text("Play", "播放");
        if (playFrame) playLabel = SkillEditorLocalization.Text("Pause", "暂停");
        using (new EditorGUI.DisabledScope(configFile == null || !IsPreviewAllowed))
            if (GUILayout.Button(playLabel, EditorStyles.miniButton, GUILayout.Width(48))) SetPreviewPlaying(!playFrame);
        if (GUILayout.Button(SkillEditorLocalization.Text("Stop", "停止"), EditorStyles.miniButton, GUILayout.Width(48))) StopPreviewSession();
        if (GUILayout.Button(SkillEditorLocalization.Text("Previous", "上一帧"), EditorStyles.miniButton, GUILayout.Width(62))) SeekPreviewFrame(frameSelectIndex - 1);
        if (GUILayout.Button(SkillEditorLocalization.Text("Next", "下一帧"), EditorStyles.miniButton, GUILayout.Width(50))) SeekPreviewFrame(frameSelectIndex + 1);
        EditorGUIUtility.labelWidth = 40;
        EditorGUI.BeginChangeCheck();
        // 用户输入的整数播放头帧，只导航预览而不改变作者事件。
        int selectedFrame = EditorGUILayout.IntField(SkillEditorLocalization.Text("Frame", "帧"), frameSelectIndex, GUILayout.Width(92));
        if(EditorGUI.EndChangeCheck()) SeekPreviewFrame(selectedFrame);
        // 时间轴帧率单位为帧每秒，空窗口沿用原30帧默认值。
        float fps = GetPreviewFrameRate();
        GUILayout.Label(new GUIContent(SkillEditorLocalization.Text($"END {GetPreviewContentEndFrame()} / Tail {GetPreviewEndFrame()}  {frameSelectIndex / fps:F2}s  {fps:0.##} FPS", $"结束 {GetPreviewContentEndFrame()} / 尾 {GetPreviewEndFrame()}  {frameSelectIndex / fps:F2}秒  {fps:0.##} FPS"), SkillEditorLocalization.Text("END is the authored action boundary; Tail is preview only, allowing delayed effects and audio to finish.", "结束为原技能边界；尾为纯预览末帧，用于查看延迟特效和声音尾部。")), EditorStyles.miniLabel, GUILayout.Width(220));
        GUILayout.FlexibleSpace();
        if (configFile != null)
        {
            // 保存状态仅对应技能资源，语言偏好不参与脏标记。
            string savedLabel = SkillEditorLocalization.Text("Saved", "已保存");
            if (EditorUtility.IsDirty(configFile)) savedLabel = SkillEditorLocalization.Text("Unsaved", "未保存");
            GUILayout.Label(savedLabel, EditorStyles.miniLabel, GUILayout.Width(52));
        }
        if (GUILayout.Button(SkillEditorLocalization.Text($"Issues {_validationErrorCount + _validationWarningCount}", $"问题 {_validationErrorCount + _validationWarningCount}"), EditorStyles.miniButton, GUILayout.Width(64))) showValidation = !showValidation;
        GUILayout.Label(SkillEditorLocalization.Text("Zoom", "缩放"), EditorStyles.miniLabel, GUILayout.Width(32));
        pixelsPerFrame = GUILayout.HorizontalSlider(pixelsPerFrame, MIN_PPF, MAX_PPF, GUILayout.Width(65));
        if (GUILayout.Button(SkillEditorLocalization.Text("Fit", "适配"), EditorStyles.miniButton, GUILayout.Width(34))) FitTimeline();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
        EditorGUIUtility.labelWidth = 0;
        if (languageChanged) GUIUtility.ExitGUI(); // 布局已平衡，跳过技能事务而保持预览继续。
        if (actionChanged) { SelectConfig(selected); GUI.FocusControl(null); GUIUtility.ExitGUI(); }
    }
}
