using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>项目级编辑偏好；默认资源目录与旧配置兜底帧率保存在 ProjectSettings。</summary>
    [FilePath("ProjectSettings/ActionEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ActionEditorSettings : ScriptableSingleton<ActionEditorSettings>
    {
        /// <summary>新草稿默认创建目录，保存为 Assets 内合法路径；修改此设置不会移动旧配置。</summary>
        [SerializeField] string defaultAssetDirectory = "Assets/ActionEditor/Skills";
        /// <summary>无可用来源帧率时的编辑帧率，单位帧/秒，读取及设置均至少为 1。</summary>
        [SerializeField, Min(1f)] float fallbackFrameRate = 30f;

        /// <summary>新草稿目录偏好；读取无效旧值时返回默认 Assets 路径，设置时规范化并保存项目设置。</summary>
        internal string DefaultAssetDirectory
        {
            get
            {
                return TryNormalizeAssetDirectory(defaultAssetDirectory, out var /* 经合法性检查后的目录偏好；读取失败用默认目录，设置失败拒绝保存。 */ path)
                    ? path : "Assets/ActionEditor/Skills";
            }
            set
            {
                if (!TryNormalizeAssetDirectory(value, out var /* 经合法性检查后的目录偏好；读取失败用默认目录，设置失败拒绝保存。 */ path))
                    throw new System.ArgumentException("The directory must be inside Assets without relative traversal.", nameof(value));
                defaultAssetDirectory = path;
                Save(true);
            }
        }

        /// <summary>将路径分隔符统一为斜杠并检查 Assets 范围及非法段；不访问或移动磁盘资源。</summary>
        /// <param name="value">待校验的 Assets 目录字符串，允许斜杠或反斜杠分隔，不接受路径穿越。</param>
        /// <param name="path">输出规范化后的合法 Assets 路径；失败时保持空字符串。</param>
        internal static bool TryNormalizeAssetDirectory(string value, out string path)
        {
            path = string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;
            // 检查后统一使用斜杠的 Assets 路径，拒绝遍历与非法段后才进入创建流程。
            string normalized = value.Replace('\\', '/').TrimEnd('/');
            if (normalized != "Assets" && !normalized.StartsWith("Assets/", System.StringComparison.Ordinal)) return false;
            // 待检查的 Assets 目录段；空段、点号、双点号或非法文件名字符均使路径校验失败。
            foreach (string part in normalized.Split('/'))
                if (part.Length == 0 || part == "." || part == ".." ||
                    part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            path = normalized;
            return true;
        }

        /// <summary>供旧配置使用的兜底帧率，单位帧/秒；最小为 1，设置时持久化项目偏好。</summary>
        internal float FallbackFrameRate
        {
            get => Mathf.Max(1f, fallbackFrameRate);
            set
            {
                fallbackFrameRate = Mathf.Max(1f, value);
                Save(true);
            }
        }

        /// <summary>创建 Project Settings 面板；设置变化只更新目录偏好与兜底帧率，不移动现有资产。</summary>
        [SettingsProvider]
        static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/ACT Action Editor", SettingsScope.Project)
            {
                label = "ACT Action Editor",
                guiHandler = /* Unity 设置面板绘制时提供的搜索上下文；当前控件不使用此值。 */ _ =>
                {
                    // Project Settings 当前编辑偏好实例，仅用户修改控件时保存。
                    var settings = instance;
                    EditorGUI.BeginChangeCheck();
                    // 用户在设置面板输入的新草稿默认目录；校验成功后才保存。
                    string path = EditorGUILayout.TextField("Default Asset Directory", settings.DefaultAssetDirectory);
                    // 用户输入的项目兜底帧率，单位帧/秒；保存前限制为至少 1，不修改当前技能时间轴。
                    float fps = EditorGUILayout.FloatField("Fallback Frame Rate", settings.FallbackFrameRate);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!TryNormalizeAssetDirectory(path, out var /* 检查后统一使用斜杠的 Assets 路径，拒绝遍历与非法段后才进入创建流程。 */ normalized))
                            EditorGUILayout.HelpBox("The directory must be inside Assets.", MessageType.None);
                        else
                            settings.defaultAssetDirectory = normalized;
                        settings.fallbackFrameRate = Mathf.Max(1f, fps);
                        settings.Save(true);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Existing assets are never moved when this setting changes.", MessageType.None);
                    if (GUILayout.Button("Open Directory") && Directory.Exists(settings.DefaultAssetDirectory))
                    {
                        // 当前默认目录的 Unity 资源引用，只用于 Project 窗口定位。
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(settings.DefaultAssetDirectory);
                        if (asset != null) EditorGUIUtility.PingObject(asset);
                    }
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "action", "skill", "timeline", "asset", "frame" })
            };
        }
    }
}
