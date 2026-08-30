using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

/// <summary>
/// 将聊天预制体中的 legacy UGUI Text/InputField 一次性迁移到 TextMeshPro。
/// </summary>
public static class ConvertChatPrefabToTMP
{
    private const string PrefabPath = "Assets/artres/Resources/UI/Prefabs/Chat.prefab";
    private const string ChatSettingPrefabPath = "Assets/artres/Resources/UI/Prefabs/Chatseting.prefab";

    [MenuItem("Tools/Chat/Convert Chatseting To TMP")]
    public static void ConvertChatsettingToTMP()
    {
        TMP_FontAsset commonFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/artres/Resources/UI/Font/Common SDF.asset");
        if (commonFont == null)
        {
            Debug.LogError("找不到 Common SDF 字体资源。");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(ChatSettingPrefabPath);
        try
        {
            UnpackNestedPrefabInstances(root);
            foreach (Text legacyText in root.GetComponentsInChildren<Text>(true).ToArray())
            {
                ConvertText(legacyText);
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = commonFont;
                text.SetMaterialDirty();
            }

            RemoveCustomBehaviourScripts(root);
            RemoveMissingScripts(root);
            EnsureStandardToggle(root, "MainTogle");
            EnsureStandardButton(root, "Btn_guanbi");
            if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
            PrefabUtility.SaveAsPrefabAsset(root, ChatSettingPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Chatseting.prefab 已移除旧自定义逻辑并迁移到 TMP / Common SDF。");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Chat/Add Chatseting To Sample Scene")]
    public static void AddChatsettingToSampleScene()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject chat = GameObject.Find("Chat");
        if (chat == null)
        {
            Debug.LogError("SampleScene 中找不到 Chat 对象。");
            return;
        }

        GameObject existing = GameObject.Find("Chatseting");
        if (existing == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChatSettingPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("找不到 Chatseting.prefab。");
                return;
            }
            existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chat.transform);
            existing.name = "Chatseting";
            existing.transform.localPosition = Vector3.zero;
            existing.transform.localRotation = Quaternion.identity;
            existing.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(existing, "Add Chatseting");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Chatseting 已添加到 SampleScene/Chat 下，并由 SettingBtn 控制显示。");
    }

    [MenuItem("Tools/Chat/Configure Chatseting Demo")]
    public static void ConfigureChatsettingDemo()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject chat = FindSceneObject("Chat");
        GameObject chatSetting = FindSceneObject("Chatseting");
        GameObject toggleGroup = FindSceneObject("Tog_Group");
        GameObject settingButton = FindSceneObject("SettingBtn");
        GameObject closeButton = FindSceneObject("Btn_guanbi");
        GameObject mainToggle = FindSceneObject("MainTogle");
        GameObject controls = FindSceneObject("ChatDemoControls");

        if (chat == null || chatSetting == null || toggleGroup == null || settingButton == null || closeButton == null || mainToggle == null || controls == null)
        {
            Debug.LogError("无法配置 Chatseting：场景中缺少聊天设置所需对象。");
            return;
        }

        ChatTest.UI.ChatSettingController controller = controls.GetComponent<ChatTest.UI.ChatSettingController>();
        if (controller == null) controller = controls.AddComponent<ChatTest.UI.ChatSettingController>();

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("chatSettingPanel").objectReferenceValue = chatSetting;
        serialized.FindProperty("targetToggleGroup").objectReferenceValue = toggleGroup;
        serialized.FindProperty("settingButton").objectReferenceValue = settingButton.GetComponent<Button>();
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
        serialized.FindProperty("mainToggle").objectReferenceValue = mainToggle.GetComponent<Toggle>();
        serialized.FindProperty("toggleGroupHideType").enumValueIndex = (int)ChatTest.UI.GameObjectHideType.Deactivate;
        serialized.FindProperty("settingPanelHideType").enumValueIndex = (int)ChatTest.UI.GameObjectHideType.Deactivate;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 设置面板由 SettingBtn 打开，因此在保存场景时默认关闭。
        chatSetting.transform.localPosition = new Vector3(650f, 0f, 0f);
        chatSetting.transform.SetAsLastSibling();
        chatSetting.SetActive(false);
        toggleGroup.SetActive(mainToggle.GetComponent<Toggle>().isOn);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Chatseting 已配置完成：SettingBtn 打开面板，MainTogle 控制 Chat/Tog_Group。", controls);
    }

    [MenuItem("Tools/Chat/Convert Chat Prefab Text To TMP")]
    public static void Convert()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // Chat.prefab 包含多个嵌套预制体；先展开它们，才能修改内部 Text 组件。
            UnpackNestedPrefabInstances(root);
            foreach (Text legacyText in root.GetComponentsInChildren<Text>(true).ToArray())
            {
                ConvertText(legacyText);
            }

            foreach (InputField legacyInput in root.GetComponentsInChildren<InputField>(true).ToArray())
            {
                ConvertInputField(legacyInput);
            }

            EnsureTMPInputField(root);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Chat.prefab 已完成 TextMeshPro 迁移。");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Chat/Set Chat Font To Common SDF")]
    public static void SetChatFontToCommonSdf()
    {
        const string fontPath = "Assets/artres/Resources/UI/Font/Common SDF.asset";
        TMP_FontAsset commonFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (commonFont == null)
        {
            Debug.LogError("找不到 Common SDF 字体资源：" + fontPath);
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = commonFont;
                text.SetMaterialDirty();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Chat.prefab 已统一使用 Common SDF 字体。");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Chat/Reduce Chat Font Size")]
    public static void ReduceChatFontSize()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // 统一缩小一档，同时保留自动字号的比例，避免长文本重新撑大。
                text.fontSize = Mathf.Max(10f, text.fontSize * 0.8f);
                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = Mathf.Max(8f, text.fontSizeMin * 0.8f);
                    text.fontSizeMax = Mathf.Max(text.fontSizeMin, text.fontSizeMax * 0.8f);
                }
                text.SetVerticesDirty();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Chat.prefab 字号已整体缩小 20%。");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UnpackNestedPrefabInstances(GameObject root)
    {
        HashSet<GameObject> instanceRoots = new HashSet<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
            if (instanceRoot != null && instanceRoot != root)
            {
                instanceRoots.Add(instanceRoot);
            }
        }

        foreach (GameObject instanceRoot in instanceRoots.ToArray())
        {
            if (instanceRoot != null)
            {
                PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == objectName && transform.gameObject.scene.IsValid()) return transform.gameObject;
        }
        return null;
    }

    private static void RemoveCustomBehaviourScripts(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true).ToArray())
        {
            if (component == null) continue;
            MonoScript script = MonoScript.FromMonoBehaviour(component);
            string typeName = script == null || script.GetClass() == null ? string.Empty : script.GetClass().FullName;
            // 保留 Unity 标准 UI 组件；删除第三方/项目自定义行为脚本。
            if (typeName.StartsWith("UnityEngine.UI.") || typeName.StartsWith("TMPro.") || typeName == "UnityEngine.CanvasScaler" || typeName == "UnityEngine.EventSystems.EventSystem")
            {
                continue;
            }

            Object.DestroyImmediate(component, true);
        }
    }

    private static void RemoveMissingScripts(GameObject root)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            GameObject gameObject = transform.gameObject;
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingCount > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            }
        }
    }

    private static void EnsureStandardToggle(GameObject root, string objectName)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
        if (target == null) return;
        foreach (MonoBehaviour component in target.GetComponents<MonoBehaviour>().ToArray())
        {
            if (component != null && component.GetType() != typeof(Toggle)) Object.DestroyImmediate(component, true);
        }
        Image targetGraphic = EnsureRaycastGraphic(target.gameObject);
        Toggle toggle = target.GetComponent<Toggle>();
        if (toggle == null) toggle = target.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = targetGraphic;
        toggle.graphic = target.GetComponentsInChildren<Graphic>(true).FirstOrDefault(graphic => graphic.gameObject.name == "ON");
        toggle.SetIsOnWithoutNotify(true);
    }

    private static void EnsureStandardButton(GameObject root, string objectName)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
        if (target == null) return;
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = EnsureRaycastGraphic(target.gameObject);
    }

    private static Image EnsureRaycastGraphic(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
        return image;
    }

    private static void ConvertText(Text legacy)
    {
        // 先复制属性，避免 AddComponent 触发 prefab 序列化刷新后 legacy 引用失效。
        string text = legacy.text;
        Color color = legacy.color;
        bool raycastTarget = legacy.raycastTarget;
        int fontSize = legacy.fontSize;
        FontStyle fontStyle = legacy.fontStyle;
        TextAnchor alignment = legacy.alignment;
        bool wordWrapping = legacy.horizontalOverflow == HorizontalWrapMode.Wrap;
        bool bestFit = legacy.resizeTextForBestFit;
        VerticalWrapMode verticalOverflow = legacy.verticalOverflow;
        GameObject go = legacy.gameObject;
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp == null)
        {
            // UGUI Graphic 标记禁止同一节点保留两个 Graphic，先移除旧 Text 再添加 TMP。
            Object.DestroyImmediate(legacy, true);
            tmp = go.AddComponent<TextMeshProUGUI>();
        }
        if (tmp == null)
        {
            Debug.LogError("无法在 " + go.name + " 上创建 TextMeshProUGUI。", go);
            return;
        }

        tmp.text = text;
        tmp.color = color;
        tmp.raycastTarget = raycastTarget;
        tmp.fontSize = fontSize;
        tmp.fontStyle = ConvertFontStyle(fontStyle);
        tmp.alignment = ConvertAlignment(alignment);
        tmp.enableWordWrapping = wordWrapping;
        tmp.overflowMode = verticalOverflow == VerticalWrapMode.Overflow
            ? TextOverflowModes.Overflow
            : TextOverflowModes.Truncate;
        tmp.enableAutoSizing = bestFit;
        if (tmp.font == null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        if (legacy != null)
        {
            Object.DestroyImmediate(legacy, true);
        }
    }

    private static void ConvertInputField(InputField legacy)
    {
        GameObject go = legacy.gameObject;
        TMP_InputField tmp = go.GetComponent<TMP_InputField>();
        if (tmp == null)
        {
            tmp = go.AddComponent<TMP_InputField>();
        }

        tmp.text = legacy.text;
        tmp.characterLimit = legacy.characterLimit;
        tmp.contentType = (TMP_InputField.ContentType)legacy.contentType;
        tmp.lineType = (TMP_InputField.LineType)legacy.lineType;
        tmp.interactable = legacy.interactable;
        tmp.targetGraphic = legacy.targetGraphic;
        // legacy InputField 没有 textViewport 属性，TMP 会使用输入框自身的 RectTransform。
        tmp.textComponent = FindTMPComponent(legacy.textComponent, go.transform, "Text");
        tmp.placeholder = FindTMPComponent(legacy.placeholder, go.transform, "Placeholder");

        Object.DestroyImmediate(legacy, true);
    }

    private static void EnsureTMPInputField(GameObject root)
    {
        Transform inputTransform = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "InputMessage");
        if (inputTransform == null || inputTransform.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        TMP_InputField input = inputTransform.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = inputTransform.GetComponent<Image>();
        input.textComponent = FindTMPByName(inputTransform, "Text");
        input.placeholder = FindTMPByName(inputTransform, "Placeholder");
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit = 200;
    }

    private static TMP_Text FindTMPByName(Transform root, string name)
    {
        return root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == name);
    }

    private static TMP_Text FindTMPComponent(Graphic source, Transform root, string fallbackName)
    {
        if (source != null)
        {
            TMP_Text component = source.GetComponent<TMP_Text>();
            if (component != null) return component;
        }

        foreach (TMP_Text component in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (component.name == fallbackName) return component;
        }

        return root.GetComponent<TMP_Text>();
    }

    private static FontStyles ConvertFontStyle(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold: return FontStyles.Bold;
            case FontStyle.Italic: return FontStyles.Italic;
            case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
            default: return FontStyles.Normal;
        }
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Midline;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            default: return TextAlignmentOptions.BottomRight;
        }
    }
}
