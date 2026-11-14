using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武学面板「技能升级 / 绝学心得 / 描述滚动」UI 构建器。
/// 用 Unity 自身 API 改写 UIRoot.prefab，避免手改 YAML 造成 fileID 错乱；可重复执行。
/// </summary>
public static class SkillUpgradeUIBuilder
{
    private const string PrefabPath = "Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab";

    private static readonly Color PanelDark = new Color(0.045f, 0.06f, 0.09f, 0.95f);
    private static readonly Color ButtonDark = new Color(0.06f, 0.09f, 0.13f, 0.96f);
    private static readonly Color Gold = new Color(1f, 0.86f, 0.58f, 1f);
    private static readonly Color Green = new Color(0.42f, 0.88f, 0.55f, 1f);
    private static readonly Color Cyan = new Color(0.50f, 0.82f, 0.95f, 1f);
    private static readonly Color White = new Color(0.92f, 0.95f, 1f, 1f);
    private static readonly Color Gray = new Color(0.58f, 0.66f, 0.76f, 1f);
    private static readonly Color BarBack = new Color(0.12f, 0.17f, 0.23f, 1f);

    [MenuItem("Tools/Skill/Build Upgrade UI")]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform window = FindByName(root.transform, "SkillInfoWindow");
            if (window == null)
            {
                Debug.LogError("[SkillUpgradeUI] SkillInfoWindow not found in UIRoot.prefab");
                return;
            }
            Transform detail = window.Find("DetailPanel");
            if (detail == null)
            {
                Debug.LogError("[SkillUpgradeUI] DetailPanel not found");
                return;
            }
            TMP_Text sample = FindText(detail, "Txt_Desc");
            TMP_FontAsset font = sample != null ? sample.font : null;

            BuildScroll(detail, font);
            BuildNormalUpgrade(detail, font);
            BuildUltimateUpgrade(detail, font);
            BuildCurrencyBar(window, font);
            BuildNodeFlags(window, font);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.ImportAsset(PrefabPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[SkillUpgradeUI] build done: " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    #region 描述滚动区

    /// <summary>把原有描述、分隔线、使用条件收进可滚动内容区，右侧挂细滚动条。</summary>
    private static void BuildScroll(Transform detail, TMP_FontAsset font)
    {
        // 重复执行时先把已收进内容区的原有对象救回 detail，避免随旧滚动区一起被销毁。
        Transform oldScroll = detail.Find("DescScroll");
        if (oldScroll != null)
        {
            Transform oldContent = oldScroll.Find("Viewport/Content");
            if (oldContent != null)
            {
                string[] saved = { "Txt_Sec1", "Txt_Desc", "Divider_2", "Txt_Sec2", "Txt_Cond" };
                for (int i = 0; i < saved.Length; i++)
                {
                    Transform kept = oldContent.Find(saved[i]);
                    if (kept != null)
                    {
                        kept.SetParent(detail, false);
                    }
                }
            }
        }
        RemoveChild(detail, "DescScroll");
        RemoveChild(detail, "DescScrollbar");

        GameObject scrollGo = MakeRect("DescScroll", detail, new Vector2(0f, 0f),
            new Vector2(620f, 460f));
        Image scrollImage = scrollGo.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);
        scrollImage.raycastTarget = true;
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 26f;

        GameObject viewportGo = MakeStretch("Viewport", scrollGo.transform, 0f, 14f, 0f, 0f);
        Image viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = false;
        viewportGo.AddComponent<RectMask2D>();

        GameObject contentGo = MakeRect("Content", viewportGo.transform, Vector2.zero, Vector2.zero);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 12);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 按参考实机的文档流顺序收纳原有对象，引用保持不变。
        MoveInto(contentGo.transform, detail, "Txt_Sec1", -1f);
        MoveInto(contentGo.transform, detail, "Txt_Desc", -1f);
        MoveInto(contentGo.transform, detail, "Divider_2", 10f);
        MoveInto(contentGo.transform, detail, "Txt_Sec2", -1f);
        MoveInto(contentGo.transform, detail, "Txt_Cond", -1f);

        scroll.viewport = viewportGo.GetComponent<RectTransform>();
        scroll.content = contentRect;

        GameObject barGo = MakeRect("DescScrollbar", detail, new Vector2(305f, 0f),
            new Vector2(6f, 460f));
        Image barImage = barGo.AddComponent<Image>();
        barImage.color = new Color(0.20f, 0.28f, 0.36f, 0.35f);
        barImage.raycastTarget = true;
        GameObject areaGo = MakeStretch("Sliding Area", barGo.transform, 0f, 0f, 2f, 2f);
        GameObject handleGo = MakeStretch("Handle", areaGo.transform, 0f, 0f, 0f, 0f);
        Image handleImage = handleGo.AddComponent<Image>();
        handleImage.color = new Color(0.72f, 0.82f, 0.92f, 0.65f);
        handleImage.raycastTarget = true;
        Scrollbar scrollbar = barGo.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleGo.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbar.size = 0.3f;
        scroll.verticalScrollbar = scrollbar;
    }

    /// <summary>把 detail 下已有的对象移到滚动内容下，并按需要补一个高度约束。</summary>
    private static void MoveInto(Transform content, Transform detail, string name, float preferredHeight)
    {
        Transform found = detail.Find(name);
        if (found == null)
        {
            // 重复执行时对象已经在内容区里。
            found = content.Find(name);
        }
        if (found == null)
        {
            return;
        }
        found.SetParent(content, false);
        found.SetAsLastSibling();
        RectTransform rt = found as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, Mathf.Max(10f, rt.sizeDelta.y));
        }
        TMP_Text text = found.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.enableAutoSizing = false;
            text.raycastTarget = false;
        }
        if (preferredHeight > 0f)
        {
            LayoutElement element = found.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = found.gameObject.AddComponent<LayoutElement>();
            }
            element.preferredHeight = preferredHeight;
            element.minHeight = preferredHeight;
        }
    }

    #endregion

    #region 两套升级区

    /// <summary>普通技能：角色等级门槛 + 单级铜币 + 右下角升级箭头。</summary>
    private static void BuildNormalUpgrade(Transform detail, TMP_FontAsset font)
    {
        RemoveChild(detail, "NormalUpgradeRoot");
        GameObject group = MakeRect("NormalUpgradeRoot", detail, new Vector2(0f, -330f),
            new Vector2(620f, 100f));

        MakeText("Txt_NextLevel", group.transform, new Vector2(-120f, 28f),
            new Vector2(360f, 30f), "下一级(角色3级可提升)", 20f, Green, font,
            TextAlignmentOptions.Left);
        MakeIcon("CostIcon", group.transform, new Vector2(150f, -22f), 24f);
        MakeText("Txt_Cost", group.transform, new Vector2(205f, -22f),
            new Vector2(80f, 34f), "40", 22f, White, font, TextAlignmentOptions.Left);
        GameObject button = MakeButton("Btn_Upgrade", group.transform,
            new Vector2(272f, -22f), new Vector2(58f, 58f), ButtonDark);
        MakeText("Txt", button.transform, Vector2.zero, new Vector2(58f, 58f),
            "↑", 34f, White, font, TextAlignmentOptions.Center);
    }

    /// <summary>绝学：心得进度条 + 心得丹道具 + 心得满后的升级按钮，与铜币升级互斥。</summary>
    private static void BuildUltimateUpgrade(Transform detail, TMP_FontAsset font)
    {
        RemoveChild(detail, "UltimateUpgradeRoot");
        GameObject group = MakeRect("UltimateUpgradeRoot", detail, new Vector2(0f, -330f),
            new Vector2(620f, 100f));

        MakeText("Txt_ExpTip", group.transform, new Vector2(0f, 40f),
            new Vector2(600f, 24f), "释放绝学，心得值+1/次 · 使用心得丹也可以增加心得",
            16f, Gray, font, TextAlignmentOptions.Left);

        GameObject item = MakeButton("Btn_ExpItem", group.transform,
            new Vector2(-215f, 0f), new Vector2(160f, 42f), new Color(0.10f, 0.14f, 0.20f, 0.96f));
        MakeText("Txt_ExpItem", item.transform, Vector2.zero, new Vector2(154f, 38f),
            "心得丹 ×10", 18f, Gold, font, TextAlignmentOptions.Center);

        MakeText("Txt_Exp", group.transform, new Vector2(55f, 8f),
            new Vector2(330f, 28f), "绝学升级：心得 0/100", 19f, Green, font,
            TextAlignmentOptions.Left);
        GameObject barBg = MakeRect("ExpBarBg", group.transform, new Vector2(55f, -22f),
            new Vector2(330f, 10f));
        Image bgImage = barBg.AddComponent<Image>();
        bgImage.color = BarBack;
        bgImage.raycastTarget = false;
        GameObject fillGo = MakeStretch("ExpBarFill", barBg.transform, 0f, 0f, 0f, 0f);
        Image fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.42f, 0.82f, 0.66f, 1f);
        fillImage.type = Image.Type.Simple;
        fillImage.raycastTarget = false;
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMax = new Vector2(0f, 1f);

        GameObject upgrade = MakeButton("Btn_UltimateUpgrade", group.transform,
            new Vector2(270f, -2f), new Vector2(54f, 54f), ButtonDark);
        MakeText("Txt", upgrade.transform, Vector2.zero, new Vector2(54f, 54f),
            "↑", 32f, White, font, TextAlignmentOptions.Center);

        GameObject hint = MakeRect("ExpItemHint", group.transform, new Vector2(0f, 108f),
            new Vector2(280f, 84f));
        Image hintImage = hint.AddComponent<Image>();
        hintImage.color = PanelDark;
        hintImage.raycastTarget = false;
        MakeText("Txt", hint.transform, Vector2.zero, new Vector2(252f, 76f),
            "点击或长按可以使用道具：\n心得丹  +10心得/个", 17f, White, font,
            TextAlignmentOptions.MidlineLeft);
        hint.SetActive(false);
        group.SetActive(false);
    }

    /// <summary>底部铜币总数 + 升级全部按钮，与人物区下沿对齐。</summary>
    private static void BuildCurrencyBar(Transform window, TMP_FontAsset font)
    {
        RemoveChild(window, "CurrencyBar");
        GameObject group = MakeRect("CurrencyBar", window, new Vector2(-430f, -470f),
            new Vector2(460f, 60f));
        MakeIcon("CurrencyIcon", group.transform, new Vector2(-152f, 0f), 28f);
        MakeText("Txt_Currency", group.transform, new Vector2(-62f, 0f),
            new Vector2(180f, 40f), "224516", 26f, White, font, TextAlignmentOptions.Left);
        GameObject button = MakeButton("Btn_UpgradeAll", group.transform,
            new Vector2(112f, 0f), new Vector2(200f, 56f),
            new Color(0.80f, 0.84f, 0.88f, 0.95f));
        MakeText("Txt", button.transform, Vector2.zero, new Vector2(200f, 56f),
            "升级全部", 24f, new Color(0.10f, 0.13f, 0.18f, 1f), font,
            TextAlignmentOptions.Center);
    }

    /// <summary>九个武功节点右上角的绿色可升级三角。</summary>
    private static void BuildNodeFlags(Transform window, TMP_FontAsset font)
    {
        Transform nodeRoot = window.Find("NodeRoot");
        if (nodeRoot == null)
        {
            return;
        }
        for (int index = 0; index < 9; index++)
        {
            Transform node = nodeRoot.Find("Node_" + index);
            if (node == null)
            {
                continue;
            }
            RemoveChild(node, "UpgradeFlag");
            GameObject flag = MakeText("UpgradeFlag", node, new Vector2(34f, 34f),
                new Vector2(28f, 28f), "▲", 20f, Green, font, TextAlignmentOptions.Center);
            flag.SetActive(false);
        }
    }

    #endregion

    #region 通用构建辅助

    private static GameObject MakeRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static GameObject MakeStretch(string name, Transform parent,
        float left, float right, float top, float bottom)
    {
        GameObject go = MakeRect(name, parent, Vector2.zero, Vector2.zero);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return go;
    }

    private static GameObject MakeText(string name, Transform parent, Vector2 pos, Vector2 size,
        string text, float fontSize, Color color, TMP_FontAsset font,
        TextAlignmentOptions alignment)
    {
        GameObject go = MakeRect(name, parent, pos, size);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return go;
    }

    /// <summary>金色圆形铜币图标，避免用字体符号出现缺字方框。</summary>
    private static GameObject MakeIcon(string name, Transform parent, Vector2 pos, float size)
    {
        GameObject go = MakeRect(name, parent, pos, new Vector2(size, size));
        Image image = go.AddComponent<Image>();
        image.sprite = GetCoinSprite();
        image.color = Gold;
        image.raycastTarget = false;
        return go;
    }

    /// <summary>在项目内生成并缓存一张 64x64 圆形白色贴图，供铜币图标复用。</summary>
    private static Sprite GetCoinSprite()
    {
        const string path = "Assets/GM_Game/UISprites/Skill/CoinIcon.png";
        Sprite cached = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (cached != null)
        {
            return cached;
        }
        string folder = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        const int n = 64;
        Texture2D texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - 31.5f;
                float dy = y - 31.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(31f - dist);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject MakeButton(string name, Transform parent, Vector2 pos,
        Vector2 size, Color background)
    {
        GameObject go = MakeRect(name, parent, pos, size);
        Image image = go.AddComponent<Image>();
        image.color = background;
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        return go;
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null)
        {
            Object.DestroyImmediate(found.gameObject);
        }
    }

    private static Transform FindByName(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == name)
            {
                return all[i];
            }
        }
        return null;
    }

    private static TMP_Text FindText(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    #endregion
}
