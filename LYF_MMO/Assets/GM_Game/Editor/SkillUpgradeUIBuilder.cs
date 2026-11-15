using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武学面板「技能升级 / 绝学心得 / 描述滚动」UI 构建器。
/// 用 Unity 自身 API 改写 UIRoot.prefab，避免手改 YAML 造成 fileID 错乱；可重复执行。
/// 「下一级(角色N级可提升)」收进描述滚动区随正文一起滚动，纯色控件统一套 RO 图集九宫格皮肤。
/// </summary>
public static class SkillUpgradeUIBuilder
{
    private const string PrefabPath = "Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab";

    /// <summary>RO 通用 UI 图集路径，皮肤九宫格都从这张图集按 sprite 名取。</summary>
    private const string RoAtlasPath = "Assets/artres/Resources/UI/Atlas/Common1.png";

    private static readonly Color PanelDark = new Color(0.045f, 0.06f, 0.09f, 0.95f);
    private static readonly Color ButtonDark = new Color(0.06f, 0.09f, 0.13f, 0.96f);
    private static readonly Color Gold = new Color(1f, 0.86f, 0.58f, 1f);
    private static readonly Color Green = new Color(0.42f, 0.88f, 0.55f, 1f);
    private static readonly Color Cyan = new Color(0.50f, 0.82f, 0.95f, 1f);
    private static readonly Color White = new Color(0.92f, 0.95f, 1f, 1f);
    private static readonly Color Gray = new Color(0.58f, 0.66f, 0.76f, 1f);
    private static readonly Color BarBack = new Color(0.12f, 0.17f, 0.23f, 1f);

    /// <summary>本次构建缓存的 RO 图集 sprite，键为 sprite 名，避免重复加载整张图集。</summary>
    private static readonly Dictionary<string, Sprite> RoSprites = new Dictionary<string, Sprite>();

    /// <summary>RO 全屏淡蓝白不透明底，与装备详情窗口一致。</summary>
    private static readonly Color RoPageBg = new Color(0.90f, 0.95f, 1f, 1f);

    /// <summary>RO 详情卡片与提示浮层底色。</summary>
    private static readonly Color RoCardBg = new Color(0.965f, 0.982f, 1f, 1f);

    /// <summary>RO 蓝银外框色。</summary>
    private static readonly Color RoFrame = new Color(0.42f, 0.58f, 0.78f, 1f);

    /// <summary>RO 顶部通栏蓝。</summary>
    private static readonly Color RoTopStrip = new Color(0.36f, 0.56f, 0.82f, 1f);

    /// <summary>页签与导航选中蓝底。</summary>
    private static readonly Color RoTabOn = new Color(0.28f, 0.50f, 0.78f, 1f);

    /// <summary>页签与导航未选中浅蓝底、通用按钮底。</summary>
    private static readonly Color RoTabOff = new Color(0.80f, 0.89f, 1f, 1f);

    /// <summary>浅底上的深蓝正文。</summary>
    private static readonly Color RoTextMain = new Color(0.12f, 0.28f, 0.46f, 1f);

    /// <summary>浅蓝按钮上的深蓝字。</summary>
    private static readonly Color RoTextDeep = new Color(0.08f, 0.21f, 0.35f, 1f);

    /// <summary>浅底上的次要灰蓝字。</summary>
    private static readonly Color RoTextSub = new Color(0.35f, 0.47f, 0.60f, 1f);

    /// <summary>章节标题蓝。</summary>
    private static readonly Color RoTextSection = new Color(0.18f, 0.44f, 0.72f, 1f);

    /// <summary>数值与绝学标签的深金色。</summary>
    private static readonly Color RoTextGold = new Color(0.62f, 0.42f, 0.12f, 1f);

    /// <summary>「下一级」与可升级角标的深绿色。</summary>
    private static readonly Color RoTextGreen = new Color(0.16f, 0.52f, 0.30f, 1f);

    /// <summary>滚动条与进度条轨道色。</summary>
    private static readonly Color RoScrollTrack = new Color(0.78f, 0.86f, 0.94f, 1f);

    /// <summary>滚动条滑块深蓝银色。</summary>
    private static readonly Color RoScrollHandle = new Color(0.28f, 0.45f, 0.62f, 1f);

    /// <summary>心得进度填充蓝色。</summary>
    private static readonly Color RoExpFill = new Color(0.25f, 0.55f, 0.85f, 1f);

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
            // 补建「下一级」文本时沿用的描述字体，描述对象缺失时保持空由 TMP 用默认字体。
            TMP_FontAsset font = null;
            if (sample != null)
            {
                font = sample.font;
            }

            LoadRoSprites();
            // 先救出「下一级」控件，避免后面重建 NormalUpgradeRoot 时把它一起销毁。
            RescueNextLevel(detail, font);
            BuildScroll(detail, font);
            BuildNormalUpgrade(detail, font);
            BuildUltimateUpgrade(detail, font);
            BuildCurrencyBar(window, font);
            BuildNodeFlags(window, font);
            ApplyRoPalette(window, detail);
            ApplyRoSkin(window, detail);

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
                // 「下一级」也在滚动文档流里，重建前同样救回 detail。
                string[] saved = { "Txt_Sec1", "Txt_Desc", "Divider_2", "Txt_Sec2", "Txt_Cond", "Txt_NextLevel" };
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
        // 参考实机的绿色「下一级」是描述文档流的最后一行，随滚动条一起滚动。
        MoveInto(contentGo.transform, detail, "Txt_NextLevel", -1f);

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

    #region RO 皮肤

    /// <summary>一次性把 RO 图集内的 sprite 按名字缓存，供皮肤替换按名取用。</summary>
    private static void LoadRoSprites()
    {
        RoSprites.Clear();
        // UnityEngine.Object[] 是 Unity 资源加载 API 的返回边界，不是业务数据载荷。
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RoAtlasPath);
        for (int index = 0; index < assets.Length; index++)
        {
            // 当前图集子资源，非 Sprite 的材质与纹理跳过。
            Sprite sprite = assets[index] as Sprite;
            if (sprite == null)
            {
                continue;
            }
            if (RoSprites.ContainsKey(sprite.name))
            {
                continue;
            }
            RoSprites.Add(sprite.name, sprite);
        }
    }

    /// <summary>把「下一级」控件从旧容器救回详情面板根节点，保证重建滚动区时不丢失。</summary>
    /// <param name="detail">详情面板根节点。</param>
    /// <param name="font">补建控件时沿用的描述字体。</param>
    private static void RescueNextLevel(Transform detail, TMP_FontAsset font)
    {
        // 已救回详情面板根节点时无需重复处理。
        Transform already = detail.Find("Txt_NextLevel");
        if (already != null)
        {
            return;
        }
        // 旧版本把控件放在升级区，新版本放在滚动内容区，两种位置都要能救出。
        Transform old = detail.Find("NormalUpgradeRoot/Txt_NextLevel");
        if (old == null)
        {
            old = detail.Find("DescScroll/Viewport/Content/Txt_NextLevel");
        }
        if (old != null)
        {
            old.SetParent(detail, false);
            return;
        }
        // 控件确实缺失时补建，保证描述文档流末行存在。
        MakeText("Txt_NextLevel", detail, Vector2.zero, new Vector2(360f, 30f),
            "下一级(角色3级可提升)", 20f, Green, font, TextAlignmentOptions.Left);
    }

    /// <summary>把整个面板换成 RO 淡蓝不透明底与深蓝文字，保持原有坐标和层级。</summary>
    /// <param name="window">技能窗口根节点。</param>
    /// <param name="detail">详情面板根节点。</param>
    private static void ApplyRoPalette(Transform window, Transform detail)
    {
        // 全屏底改浅蓝白不透明，顶部改蓝通栏，取代原来的深蓝近黑底。
        SetImageColor(window, "BG_Frame", RoFrame);
        SetImageColor(window, "BG_Inner", RoPageBg);
        SetImageColor(window, "PortraitHalo", new Color(0.25f, 0.42f, 0.65f, 0.22f));
        SetImageColor(window, "RoTopBar", RoTopStrip);
        SetImageColor(window, "TopBar", new Color(0.95f, 0.98f, 1f, 0.55f));
        SetImageColor(window, "Btn_Close", new Color(0.97f, 0.99f, 1f, 0.95f));
        SetTextColor(window, "Txt_Title", new Color(1f, 0.95f, 0.82f, 1f));
        SetTextColor(window, "Txt_Job", new Color(0.88f, 0.94f, 1f, 0.95f));

        // 子页签静态按未选中态，运行时由 RefreshTabs 覆盖。
        for (int index = 0; index < 2; index++)
        {
            // 当前子页签的相对路径。
            string tab = "TabRow/SubTab_" + index;
            SetImageColor(window, tab, RoTabOff);
            SetImageColor(window, tab + "/Icon", new Color(0.20f, 0.40f, 0.64f, 1f));
            SetTextColor(window, tab + "/Txt", RoTextDeep);
        }
        // 竖排导航第一项按选中态，其余浅蓝底深蓝字。
        for (int index = 0; index < 6; index++)
        {
            // 当前导航项的相对路径。
            string nav = "NavRoot/Nav_" + index;
            if (index == 0)
            {
                SetImageColor(window, nav, RoTabOn);
                SetTextColor(window, nav + "/Txt", Color.white);
            }
            else
            {
                SetImageColor(window, nav, RoTabOff);
                SetTextColor(window, nav + "/Txt", RoTextDeep);
            }
        }

        // 详情卡片：淡蓝白底 + 蓝银描边，正文全部改深蓝系。
        Image detailBg = detail.GetComponent<Image>();
        TintImage(detailBg, RoCardBg);
        SetImageColor(window, "DetailBorder", new Color(0.30f, 0.52f, 0.76f, 1f));
        SetImageColor(detail, "DetailIcon", new Color(0.72f, 0.84f, 0.96f, 1f));
        SetImageColor(detail, "Divider_1", new Color(0.33f, 0.52f, 0.72f, 0.75f));
        SetImageColor(detail, "DescScroll/Viewport/Content/Divider_2",
            new Color(0.33f, 0.52f, 0.72f, 0.55f));
        SetTextColor(detail, "Txt_Name", RoTextMain);
        SetTextColor(detail, "Txt_Level", RoTextGold);
        SetTextColor(detail, "Txt_Tag", new Color(0.75f, 0.35f, 0.10f, 1f));
        SetTextColor(detail, "DescScroll/Viewport/Content/Txt_Sec1", RoTextSection);
        SetTextColor(detail, "DescScroll/Viewport/Content/Txt_Sec2", RoTextSection);
        SetTextColor(detail, "DescScroll/Viewport/Content/Txt_Desc", RoTextMain);
        SetTextColor(detail, "DescScroll/Viewport/Content/Txt_Cond", RoTextSub);
        SetTextColor(detail, "DescScroll/Viewport/Content/Txt_NextLevel", RoTextGreen);

        // 两套升级区文字改深蓝/深绿，铜币图标改深金以便在浅底上辨认。
        SetImageColor(detail, "NormalUpgradeRoot/CostIcon", new Color(0.85f, 0.62f, 0.18f, 1f));
        SetTextColor(detail, "NormalUpgradeRoot/Txt_Cost", RoTextMain);
        SetTextColor(detail, "NormalUpgradeRoot/Btn_Upgrade/Txt", RoTextDeep);
        SetTextColor(detail, "UltimateUpgradeRoot/Btn_ExpItem/Txt_ExpItem", RoTextDeep);
        SetTextColor(detail, "UltimateUpgradeRoot/Txt_ExpTip", RoTextSub);
        SetTextColor(detail, "UltimateUpgradeRoot/Txt_Exp", RoTextGreen);
        SetTextColor(detail, "UltimateUpgradeRoot/Btn_UltimateUpgrade/Txt", RoTextDeep);
        SetTextColor(detail, "UltimateUpgradeRoot/ExpItemHint/Txt", RoTextMain);

        // 九个武功节点：内圆与遮罩提亮，绝学保持深金圈与深金字。
        for (int index = 0; index < 9; index++)
        {
            // 当前武功节点的相对路径。
            string node = "NodeRoot/Node_" + index;
            SetImageColor(window, node + "/Glow", new Color(0.35f, 0.60f, 0.88f, 0.14f));
            SetImageColor(window, node + "/Inner", new Color(0.82f, 0.90f, 1f, 1f));
            SetImageColor(window, node + "/IconMask", new Color(0.90f, 0.95f, 1f, 1f));
            SetTextColor(window, node + "/UpgradeFlag", RoTextGreen);
            if (index == 8)
            {
                SetImageColor(window, node + "/Ring", new Color(0.82f, 0.62f, 0.24f, 1f));
                SetTextColor(window, node + "/Tag", RoTextGold);
            }
            else
            {
                SetTextColor(window, node + "/Tag", RoTextMain);
            }
        }
        // 轻功页三个节点与中央大图标同样提亮内圆。
        for (int index = 0; index < 3; index++)
        {
            // 当前轻功节点的相对路径。
            string node = "LightRoot/LightNode_" + index;
            SetImageColor(window, node + "/Glow", new Color(0.35f, 0.60f, 0.88f, 0.14f));
            SetImageColor(window, node + "/Inner", new Color(0.82f, 0.90f, 1f, 1f));
            SetImageColor(window, node + "/IconMask", new Color(0.90f, 0.95f, 1f, 1f));
        }
        SetImageColor(window, "LightRoot/LightBig/Glow", new Color(0.35f, 0.60f, 0.88f, 0.14f));
        SetImageColor(window, "LightRoot/LightBig/Inner", new Color(0.82f, 0.90f, 1f, 1f));
        SetImageColor(window, "LightRoot/LightBig/IconMask", new Color(0.90f, 0.95f, 1f, 1f));

        // 工具行、徽章、秘笈空壳页：浅底深字，徽章保留白字。
        for (int index = 0; index < 3; index++)
        {
            SetImageColor(window, "ToolRow/Tool_" + index + "/Dot",
                new Color(0.28f, 0.52f, 0.80f, 0.90f));
            SetTextColor(window, "ToolRow/Tool_" + index + "/Txt", RoTextMain);
        }
        SetImageColor(window, "BadgeRoot/Badge_1", new Color(0.25f, 0.60f, 0.40f, 0.95f));
        SetImageColor(window, "BadgeRoot/Badge_2", new Color(0.75f, 0.42f, 0.22f, 0.95f));
        SetTextColor(window, "BadgeRoot/Badge_1/Txt", Color.white);
        SetTextColor(window, "BadgeRoot/Badge_2/Txt", Color.white);
        SetImageColor(window, "EmptyPage/Bg", new Color(0.90f, 0.95f, 1f, 0.98f));
        SetTextColor(window, "EmptyPage/Txt_Title", RoTextSection);
        SetTextColor(window, "EmptyPage/Txt_Hint", RoTextSub);
        SetTextColor(window, "EmptyPage/Txt_Foot", RoTextSub);
        for (int index = 0; index < 4; index++)
        {
            SetImageColor(window, "EmptyPage/Slot_" + index, new Color(0.80f, 0.89f, 0.98f, 1f));
            SetTextColor(window, "EmptyPage/Slot_" + index + "/Txt", RoTextSub);
        }

        // 列表条目与底部铜币条：浅蓝卡片 + 深蓝文字。
        SetImageColor(window, "ListRoot/ScrollView/Viewport/Content/ItemTemplate",
            new Color(0.94f, 0.97f, 1f, 1f));
        SetImageColor(window, "ListRoot/ScrollView/Viewport/Content/ItemTemplate/IconMask",
            new Color(0.72f, 0.83f, 0.95f, 1f));
        SetTextColor(window, "ListRoot/ScrollView/Viewport/Content/ItemTemplate/Txt_Name", RoTextMain);
        SetTextColor(window, "ListRoot/ScrollView/Viewport/Content/ItemTemplate/Txt_Meta", RoTextGold);
        SetImageColor(window, "CurrencyBar/CurrencyIcon", new Color(0.85f, 0.62f, 0.18f, 1f));
        SetTextColor(window, "CurrencyBar/Txt_Currency", RoTextMain);
        SetTextColor(window, "CurrencyBar/Btn_UpgradeAll/Txt", RoTextDeep);
    }

    /// <summary>按相对路径给 Image 染色，路径或组件缺失时跳过。</summary>
    /// <param name="root">查找起点。</param>
    /// <param name="path">相对 root 的 Transform 路径。</param>
    /// <param name="color">目标 RO 配色。</param>
    private static void SetImageColor(Transform root, string path, Color color)
    {
        // 命中的图片组件，可能为空。
        Image image = FindImage(root, path);
        if (image == null)
        {
            return;
        }
        image.color = color;
    }

    /// <summary>按相对路径给 TMP 文本染色，路径或组件缺失时跳过。</summary>
    /// <param name="root">查找起点。</param>
    /// <param name="path">相对 root 的 Transform 路径。</param>
    /// <param name="color">目标 RO 文字色。</param>
    private static void SetTextColor(Transform root, string path, Color color)
    {
        // 命中的文本节点，可能为空。
        Transform found = root.Find(path);
        if (found == null)
        {
            return;
        }
        // 节点上的 TMP 文本组件，纯容器节点取不到。
        TMP_Text text = found.GetComponent<TMP_Text>();
        if (text == null)
        {
            return;
        }
        text.color = color;
    }

    /// <summary>给面板内纯色控件套 RO 图集九宫格与蓝银配色，不改任何坐标与层级。</summary>
    /// <param name="window">技能窗口根节点。</param>
    /// <param name="detail">详情面板根节点。</param>
    private static void ApplyRoSkin(Transform window, Transform detail)
    {
        ApplyRoSprite(FindImage(detail, "DescScrollbar"),
            "UI_Common_Bg_progress_01", RoScrollTrack);
        ApplyRoSprite(FindImage(detail, "DescScrollbar/Sliding Area/Handle"),
            "UI_Common_Btn_Scrollbar_01", RoScrollHandle);
        ApplyRoSprite(FindImage(detail, "NormalUpgradeRoot/Btn_Upgrade"),
            "UI_Common_Btn_Normal_01", RoTabOff);
        ApplyRoSprite(FindImage(detail, "UltimateUpgradeRoot/Btn_ExpItem"),
            "UI_Common_Btn_Medium_01", RoTabOff);
        ApplyRoSprite(FindImage(detail, "UltimateUpgradeRoot/ExpBarBg"),
            "UI_Common_Bg_progress_01", RoScrollTrack);
        // 心得填充宽度由代码控制 anchorMax，保持无 sprite 纯色以免九宫格被压缩变形。
        TintImage(FindImage(detail, "UltimateUpgradeRoot/ExpBarBg/ExpBarFill"), RoExpFill);
        ApplyRoSprite(FindImage(detail, "UltimateUpgradeRoot/Btn_UltimateUpgrade"),
            "UI_Common_Btn_Normal_01", RoTabOff);
        ApplyRoSprite(FindImage(detail, "UltimateUpgradeRoot/ExpItemHint"),
            "UI_Common_Bg_Tips_01", RoCardBg);
        ApplyRoSprite(FindImage(window, "CurrencyBar/Btn_UpgradeAll"),
            "UI_Common_Btn_Medium_01", RoTabOff);
        ApplyRoSprite(FindImage(window, "ListRoot/ScrollView/Scrollbar"),
            "UI_Common_Bg_progress_01", RoScrollTrack);
        ApplyRoSprite(FindImage(window, "ListRoot/ScrollView/Scrollbar/Sliding Area/Handle"),
            "UI_Common_Btn_Scrollbar_01", RoScrollHandle);
        ApplyRoSprite(FindImage(window, "TitleLine"),
            "UI_Common_Bg_qujianjianbian_01", new Color(0.33f, 0.52f, 0.72f, 0.70f));
    }

    /// <summary>按相对路径取 Image 组件，路径不存在时返回空引用由调用方跳过。</summary>
    /// <param name="root">查找起点。</param>
    /// <param name="path">相对 root 的 Transform 路径。</param>
    private static Image FindImage(Transform root, string path)
    {
        // 命中的节点，可能没有挂 Image 组件。
        Transform found = root.Find(path);
        if (found == null)
        {
            return null;
        }
        return found.GetComponent<Image>();
    }

    /// <summary>把 RO 九宫格设到 Image 上并改为 Sliced，同时染成传入的 RO 配色。</summary>
    /// <param name="image">待换皮的图片组件。</param>
    /// <param name="spriteName">RO 图集内的 sprite 名。</param>
    /// <param name="color">RO 蓝银或暖金配色。</param>
    private static void ApplyRoSprite(Image image, string spriteName, Color color)
    {
        if (image == null)
        {
            return;
        }
        // 图集里按名字取到的九宫格，未命中时只改颜色不破坏原图。
        Sprite sprite;
        if (RoSprites.TryGetValue(spriteName, out sprite))
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        image.color = color;
    }

    /// <summary>只改颜色不改 sprite，用于按矩形宽度驱动的进度填充。</summary>
    /// <param name="image">待染色的图片组件。</param>
    /// <param name="color">目标 RO 配色。</param>
    private static void TintImage(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }
        image.color = color;
    }

    #endregion

    #region 两套升级区

    /// <summary>普通技能：单级铜币 + 右下角升级箭头；「下一级」提示已移入描述滚动区。</summary>
    private static void BuildNormalUpgrade(Transform detail, TMP_FontAsset font)
    {
        RemoveChild(detail, "NormalUpgradeRoot");
        GameObject group = MakeRect("NormalUpgradeRoot", detail, new Vector2(0f, -330f),
            new Vector2(620f, 100f));

        // 绿色等级门槛由 BuildScroll 收进 DescScroll/Viewport/Content，不再固定在本容器。
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

    /// <summary>在 detail 直属子节点里按名字取 TMP 文本，用作补建控件的字体来源。</summary>
    /// <param name="parent">查找起点。</param>
    /// <param name="name">直属子节点名字。</param>
    private static TMP_Text FindText(Transform parent, string name)
    {
        // 命中的子节点，可能不存在或没有挂 TMP 文本。
        Transform found = parent.Find(name);
        if (found == null)
        {
            return null;
        }
        return found.GetComponent<TMP_Text>();
    }

    #endregion
}
