using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

/// <summary>
/// 聊天界面的手动迁移工具：把旧 Text/InputField 换成 TextMeshPro，并提供演示场景和字体设置菜单。
/// 这些菜单会保存指定预制体或场景；游戏运行时不会自动执行。
/// </summary>
public static class ConvertChatPrefabToTMP
{
    // 要迁移的聊天主预制体路径。
    private const string PrefabPath = "Assets/artres/Resources/UI/Prefabs/Chat.prefab";

    // 要迁移的聊天设置预制体路径，保留资源原有拼写。
    private const string ChatSettingPrefabPath = "Assets/artres/Resources/UI/Prefabs/Chatseting.prefab";

    // 打开 SampleScene，在 Chat 下补建 Chatseting 实例，然后保存场景；已有实例不会重复创建。
    [MenuItem("Tools/Chat/Add Chatseting To Sample Scene")]
    public static void AddChatsettingToSampleScene()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        // 本次以 Single 模式打开的场景，用于标记修改和保存。
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject chat = GameObject.Find("Chat");
        if (chat == null)
        {
            Debug.LogError("SampleScene 中找不到 Chat 对象。");
            return;
        }

        // 场景中已有的设置面板；只有不存在时才实例化预制体。
        GameObject existing = GameObject.Find("Chatseting");
        if (existing == null)
        {
            // 设置面板的预制体资源，用来创建演示场景中的实例。
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

    #region 移除与回收
    // 遍历 root 下的脚本（包含隐藏节点），仅保留列出的 Unity UI/TMP 类型，删除其他行为脚本。
    private static void RemoveCustomBehaviourScripts(GameObject root)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true).ToArray())
        {
            if (component == null) continue;
            // 当前组件对应的脚本资源，用它查询完整类型名。
            MonoScript script = MonoScript.FromMonoBehaviour(component);
            // 组件的完整类型名；无法解析脚本时用空字符串参与保留名单判断。
            string typeName = script == null || script.GetClass() == null ? string.Empty : script.GetClass().FullName;
            // 是否属于 Unity 原生 UGUI 或 TextMeshPro 命名空间。
            bool isKnownUguiNamespace = typeName.StartsWith("UnityEngine.UI.") || typeName.StartsWith("TMPro.");
            // 是否属于事件系统或画布缩放的原生组件类型。
            bool isKnownNativeType = typeName == "UnityEngine.CanvasScaler" || typeName == "UnityEngine.EventSystems.EventSystem";
            // 保留 Unity 标准 UI 组件；删除第三方/项目自定义行为脚本。
            if (isKnownUguiNamespace || isKnownNativeType)
            {
                continue;
            }

            Object.DestroyImmediate(component, true);
        }
    }

    // 移除 root 及子节点上已经找不到脚本文件的组件，不改仍有效的脚本。
    private static void RemoveMissingScripts(GameObject root)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            // 当前检查缺失脚本的场景节点。
            GameObject gameObject = transform.gameObject;
            // 当前节点已经失去脚本引用的组件数量。
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingCount > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            }
        }
    }
    #endregion

    #region 查询与判定
    // 迁移设置预制体：更换文本和字体、清除旧脚本、补齐标准按钮与开关，然后保存。
    [MenuItem("Tools/Chat/Convert Chatseting To TMP")]
    public static void ConvertChatsettingToTMP()
    {
        // 项目统一的 TMP 字体资源，加载失败就停止迁移。
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

    // 展开聊天预制体中的嵌套实例，替换文本和输入框后保存；finally 负责卸载临时编辑副本。
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

    // 在已加载场景中按 objectName 找对象，包含隐藏节点；找不到时返回 null。
    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == objectName && transform.gameObject.scene.IsValid()) return transform.gameObject;
        }
        return null;
    }

    // 把旧文本 legacy 的文字、颜色、字号和排版设置复制到同节点的 TMP 文本，再移除旧组件。
    private static void ConvertText(Text legacy)
    {
        // 先复制属性，避免 AddComponent 触发 prefab 序列化刷新后 legacy 引用失效。
        // 删除旧组件前保存的文字内容。
        string text = legacy.text;
        // 旧文本的颜色，包含透明度。
        Color color = legacy.color;
        // 旧文本是否接收鼠标或触摸射线。
        bool raycastTarget = legacy.raycastTarget;
        // 旧文本字号，迁移后作为 TMP 的字号。
        int fontSize = legacy.fontSize;
        // 旧文本的粗体和斜体设置。
        FontStyle fontStyle = legacy.fontStyle;
        // 旧文本在矩形内的水平和垂直对齐方式。
        TextAnchor alignment = legacy.alignment;
        // 旧文本是否在宽度不足时自动换行。
        bool wordWrapping = legacy.horizontalOverflow == HorizontalWrapMode.Wrap;
        // 旧文本是否自动缩放字号以适应矩形。
        bool bestFit = legacy.resizeTextForBestFit;
        // 旧文本超出矩形高度时的溢出方式。
        VerticalWrapMode verticalOverflow = legacy.verticalOverflow;
        GameObject go = legacy.gameObject;
        // 同节点上已有或即将创建的 TMP 文本。
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

    // 为旧输入框 legacy 建立 TMP 输入框，迁移输入限制并重新绑定文字和占位提示。
    private static void ConvertInputField(InputField legacy)
    {
        GameObject go = legacy.gameObject;
        // 同节点上已有或即将创建的 TMP 输入框。
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

    // 从 root 的子节点中找名字等于 name 的 TMP 文本，包含隐藏节点；找不到时返回 null。
    private static TMP_Text FindTMPByName(Transform root, string name)
    {
        return root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == name);
    }

    // 先找 source 同节点的 TMP，再到 root 下按 fallbackName 查找，最后尝试 root 自己的 TMP。
    private static TMP_Text FindTMPComponent(Graphic source, Transform root, string fallbackName)
    {
        if (source != null)
        {
            // 原引用所指节点上的 TMP 文本，优先复用它。
            TMP_Text component = source.GetComponent<TMP_Text>();
            if (component != null) return component;
        }

        foreach (TMP_Text component in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (component.name == fallbackName) return component;
        }

        return root.GetComponent<TMP_Text>();
    }

    // 把旧文本的 style（普通、粗体、斜体或粗斜体）转换成对应的 TMP 字体样式。
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

    // 把旧文本九宫格对齐方式 anchor 转换成 TMP 对齐枚举。
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
    #endregion

    #region 核心逻辑
    // 给 SampleScene 的演示控制器绑定设置面板和按钮，设置默认显隐后保存场景。
    [MenuItem("Tools/Chat/Configure Chatseting Demo")]
    public static void ConfigureChatsettingDemo()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject chat = FindSceneObject("Chat");
        // 演示场景中的聊天设置面板。
        GameObject chatSetting = FindSceneObject("Chatseting");
        // 受主开关控制显示的聊天频道分组。
        GameObject toggleGroup = FindSceneObject("Tog_Group");
        // 打开设置面板的按钮节点。
        GameObject settingButton = FindSceneObject("SettingBtn");
        // 关闭设置面板的按钮节点。
        GameObject closeButton = FindSceneObject("Btn_guanbi");
        // 控制聊天分组显隐的主开关节点。
        GameObject mainToggle = FindSceneObject("MainTogle");
        // 演示控制脚本所在节点。
        GameObject controls = FindSceneObject("ChatDemoControls");

        // 聊天面板与聊天设置面板节点是否缺失。
        bool isChatOrSettingMissing = chat == null || chatSetting == null;
        // 开关分组与打开设置按钮节点是否缺失。
        bool isToggleGroupOrSettingBtnMissing = toggleGroup == null || settingButton == null;
        // 关闭按钮、主开关与演示控件节点是否缺失。
        bool isCloseOrMainToggleOrControlsMissing = closeButton == null || mainToggle == null || controls == null;
        if (isChatOrSettingMissing || isToggleGroupOrSettingBtnMissing || isCloseOrMainToggleOrControlsMissing)
        {
            Debug.LogError("无法配置 Chatseting：场景中缺少聊天设置所需对象。");
            return;
        }

        // 当前演示场景的设置控制器，缺失时在演示节点补建。
        ChatTest.UI.ChatSettingController controller = controls.GetComponent<ChatTest.UI.ChatSettingController>();
        if (controller == null) controller = controls.AddComponent<ChatTest.UI.ChatSettingController>();

        // 控制器的 Unity 序列化视图，用字段名绑定场景引用。
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

    // 把聊天预制体的全部 TMP 文本设为 Common SDF 字体并保存。
    [MenuItem("Tools/Chat/Set Chat Font To Common SDF")]
    public static void SetChatFontToCommonSdf()
    {
        // 统一字体资源的固定路径。
        const string fontPath = "Assets/artres/Resources/UI/Font/Common SDF.asset";
        // 本次要赋给全部聊天文字的 TMP 字体。
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

    // 把聊天预制体字号缩至当前的 80%；同时调整自动字号范围，每次执行都会再次缩小。
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

    // 收集 root 内的嵌套预制体实例并解除它们的连接，使内部控件能直接修改；不解包 root 自身。
    private static void UnpackNestedPrefabInstances(GameObject root)
    {
        // 去重保存嵌套预制体的根节点，避免一个实例被反复解包。
        HashSet<GameObject> instanceRoots = new HashSet<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            // 当前子节点最近的预制体实例根对象。
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

    // 在 root 下按 objectName 找开关节点，清除其他行为组件并补标准 Toggle，默认设为开启但不触发回调。
    private static void EnsureStandardToggle(GameObject root, string objectName)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
        if (target == null) return;
        foreach (MonoBehaviour component in target.GetComponents<MonoBehaviour>().ToArray())
        {
            if (component != null && component.GetType() != typeof(Toggle)) Object.DestroyImmediate(component, true);
        }
        // 开关接收点击的透明背景，不是勾选标记。
        Image targetGraphic = EnsureRaycastGraphic(target.gameObject);
        // 当前节点的标准开关组件，缺失时创建。
        Toggle toggle = target.GetComponent<Toggle>();
        if (toggle == null) toggle = target.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = targetGraphic;
        toggle.graphic = target.GetComponentsInChildren<Graphic>(true).FirstOrDefault(graphic => graphic.gameObject.name == "ON");
        toggle.SetIsOnWithoutNotify(true);
    }

    // 在 root 下按 objectName 找按钮节点，补 Button 并指定可接收点击的透明背景。
    private static void EnsureStandardButton(GameObject root, string objectName)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
        if (target == null) return;
        // 当前节点的标准按钮组件，缺失时创建。
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = EnsureRaycastGraphic(target.gameObject);
    }

    // 为 target 补一个透明 Image，允许它接收 UI 射线检测，并返回这个 Image。
    private static Image EnsureRaycastGraphic(GameObject target)
    {
        // 用于点击检测的背景图像；没有时新增。
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
        return image;
    }

    // 为 root 下的 InputMessage 补 TMP 输入框，绑定文字和占位节点，设置为多行且最多 200 字。
    private static void EnsureTMPInputField(GameObject root)
    {
        Transform inputTransform = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "InputMessage");
        if (inputTransform == null || inputTransform.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        // 为输入消息节点新建的 TMP 输入框。
        TMP_InputField input = inputTransform.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = inputTransform.GetComponent<Image>();
        input.textComponent = FindTMPByName(inputTransform, "Text");
        input.placeholder = FindTMPByName(inputTransform, "Placeholder");
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit = 200;
    }
    #endregion
}
