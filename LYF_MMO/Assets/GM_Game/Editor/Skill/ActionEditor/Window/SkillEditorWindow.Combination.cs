using UnityEditor;
using UnityEngine;

/// <summary>在原技能编辑器基础属性中编辑 RO 组合类型和子技能顺序，不复制子技能时间轴。</summary>
public partial class SkillEditorWindow
{
    /// <summary>显示与 RO 对齐的类型和有序子技能 ID 列表，使用原窗口序列化与 Undo。</summary>
    void DrawCombinationInspector()
    {
        _serializedConfig.Update();
        // 当前资产的运行类型属性，保持旧资产默认值为普通技能。
        SerializedProperty type = _serializedConfig.FindProperty("TypeToken");
        type.intValue = EditorGUILayout.Popup("运行类型", type.intValue, new[] { "普通技能", "组合技能" });
        if (type.intValue == 1)
        {
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("skillID"), new GUIContent("主技能 ID"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("skillName"), new GUIContent("组合名称"));
            // Unity 原生数组控件负责数量、增删和重排，元素值仍是原技能主表 ID。
            SerializedProperty children = _serializedConfig.FindProperty("CombineSkillList");
            EditorGUILayout.PropertyField(children, new GUIContent("子技能顺序"), true);
            EditorGUILayout.HelpBox("从上到下自动执行。每段沿用子技能的动画、特效、声音和位移；组合主技能结算一次消耗和冷却。保存后通过绑定资产导出，整体在游戏中施放。", MessageType.Info);
            // 逐项提供原技能编辑入口，重复 ID 表示重复执行同一技能。
            for (int index = 0; index < children.arraySize; index++)
            {
                // 当前序号引用的稳定技能主表 ID。
                int skillId = children.GetArrayElementAtIndex(index).intValue;
                if (GUILayout.Button("编辑／预览第 " + (index + 1) + " 段：" + skillId))
                {
                    // 只在用户点击时查找现有作者资产，不扫描或改写其他配置。
                    SkillConfigSO child = FindCombinationChild(skillId);
                    if (child != null)
                    {
                        // 新窗口保留主组合配置页，子技能继续使用现有时间轴预览。
                        SkillEditorWindow window = CreateInstance<SkillEditorWindow>();
                        window.Show();
                        window.LoadAction(child, previewModel);
                    }
                    else Debug.LogError("未找到子技能作者资产：" + skillId);
                }
            }
        }
        ApplyInspectorProperties();
    }

    /// <summary>按主表技能 ID 查找原作者资产，组合列表不保存路径或复制对象。</summary>
    /// <param name="skillId">被引用的正技能 ID。</param>
    public static SkillConfigSO FindCombinationChild(int skillId)
    {
        // 查找范围限定为项目自己的技能制作资产。
        string[] guids = AssetDatabase.FindAssets("t:SkillConfigSO", new[] { "Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring" });
        // 资产 GUID 的零起始索引。
        for (int index = 0; index < guids.Length; index++)
        {
            // 实际资产引用，不能用文件名推断技能身份。
            SkillConfigSO child = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(AssetDatabase.GUIDToAssetPath(guids[index]));
            if (child != null && child.skillID == skillId) return child;
        }
        return null;
    }
}
