using UnityEditor;
using UnityEngine;

/// <summary>
/// 时段切换面板的 Inspector：把"应用哪套配置、立即还是插值"做成按钮，编辑态就能直接点。
/// 面板本身不写任何 Shader 全局量，点按钮只是调用 RODemoLightingPanel → MEnvorimentSetting 的原版链路。
/// </summary>
[CustomEditor(typeof(RODemoLightingPanel))]
[CanEditMultipleObjects]
public class RODemoLightingPanelEditor : Editor
{
    /// <summary>面板组件实例（延迟到 OnEnable 取，避免构造期访问序列化数据）。</summary>
    private RODemoLightingPanel _panel;

    /// <summary>缓存被编辑的面板组件。</summary>
    private void OnEnable()
    {
        _panel = (RODemoLightingPanel)target;
    }

    /// <summary>绘制默认字段 + 四个操作按钮 + 当前生效的全局量只读摘要。</summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (_panel.environment == null)
        {
            EditorGUILayout.HelpBox("environment 未指定：面板不会应用任何配置。", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("按 RO 原版配置切换（立即生效）", EditorStyles.boldLabel);
        int count = _panel.dayTimes.Length;
        for (int i = 0; i < count; i = i + 1)
        {
            MDayTimeData dayTime = _panel.dayTimes[i];
            string label = "配置 " + i;
            if (dayTime != null)
            {
                label = dayTime.name + "（" + dayTime.daytimeType.ToString() + "）";
            }
            if (GUILayout.Button(label))
            {
                _panel.ApplyIndex(i, true);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("原版 2 秒插值", EditorStyles.boldLabel);
        if (GUILayout.Button("插值切到下一套配置（Play 模式下可看到 2 秒过渡）"))
        {
            _panel.ApplyNextWithLerp();
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("按当前参数重新应用（改完资产字段后点它）"))
        {
            _panel.ReapplyCurrent();
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前 Shader 全局量（只读）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(BuildGlobalsText(), MessageType.None);

        if (Application.isPlaying == false)
        {
            EditorGUILayout.HelpBox("Play 模式快捷键：1/2/3 立即切档，L 插值切下一档，R 重新应用。", MessageType.Info);
        }
    }

    /// <summary>读回当前四个角色光照全局量，证明按钮确实改变了 Shader 输入。</summary>
    /// <returns>多行文本。</returns>
    private static string BuildGlobalsText()
    {
        Color mainColor = Shader.GetGlobalColor("_ROCLightColor0");
        Color backColor = Shader.GetGlobalColor("_ROCBackColor");
        Color ambientColor = Shader.GetGlobalColor("_ROCAmbientColor");
        Vector4 lightDir = Shader.GetGlobalVector("_ROCLightDir0");
        return "_ROCLightColor0 = " + mainColor.ToString("F4") + "\n"
            + "_ROCBackColor = " + backColor.ToString("F4") + "\n"
            + "_ROCAmbientColor = " + ambientColor.ToString("F4") + "\n"
            + "_ROCLightDir0 = " + lightDir.ToString("F4");
    }
}
