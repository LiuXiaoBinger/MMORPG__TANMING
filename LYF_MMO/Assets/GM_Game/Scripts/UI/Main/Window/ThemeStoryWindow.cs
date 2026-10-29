using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主题副本详情窗口，移植自 RO 的 Theme_Story 预制体。
/// 全屏展示首领立绘、首领/特性/档案三个页签、通关奖励与「开始挑战」入口。
/// 本类只负责展示与交互，真正的进入副本协议通过 <see cref="OnEnterDungeon"/> 交给外部发起。
/// </summary>
public sealed class ThemeStoryWindow : WindowBase
{
    /// <summary>首领页签下标。</summary>
    private const int TabBoss = 0;

    /// <summary>特性页签下标。</summary>
    private const int TabSpecial = 1;

    /// <summary>档案页签下标。</summary>
    private const int TabRecord = 2;

    /// <summary>RO 原始设计分辨率，窗口按这个尺寸居中摆放，不再拉伸铺满。</summary>
    private static readonly Vector2 DesignSize = new Vector2(1704f, 852f);

    /// <summary>窗口根节点。</summary>
    private RectTransform _root;

    /// <summary>首领立绘。</summary>
    private RawImage _bossPic;

    /// <summary>左上角窗口标题。</summary>
    private TextMeshProUGUI _titleText;

    /// <summary>页签内容区的标题，三个页签共用。</summary>
    private TextMeshProUGUI _bossNameText;

    /// <summary>页签正文，三个页签共用。</summary>
    private TextMeshProUGUI _descText;

    /// <summary>左侧的副本等级文字。</summary>
    private TextMeshProUGUI _lvText;

    /// <summary>左侧的副本名称文字。</summary>
    private TextMeshProUGUI _nameText;

    /// <summary>奖励区的标题。</summary>
    private TextMeshProUGUI _rewardTitle;

    /// <summary>右上角关闭按钮。</summary>
    private Button _closeBtn;

    /// <summary>「开始挑战」按钮。</summary>
    private Button _beginBtn;

    /// <summary>「公开情报」按钮。</summary>
    private Button _infoBtn;

    /// <summary>奖励区根节点，只在首领页签显示。</summary>
    private GameObject _awardRoot;

    /// <summary>描述滚动区，只在首领页签显示。</summary>
    private GameObject _descScroll;

    /// <summary>正文的滚动组件，用来判断还有没有没读完的内容。</summary>
    private ScrollRect _infoScroll;

    /// <summary>正文下方的下翻箭头，内容没滚到底时才显示，点一下翻一页。</summary>
    private GameObject _downArrow;

    /// <summary>已通关角标。</summary>
    private GameObject _passedImg;

    /// <summary>RO 里的两张特效贴图，原版由动画控制，这里默认隐藏。</summary>
    private GameObject _effect;

    /// <summary>右下角那张写着大段文字的特效贴图，默认隐藏。</summary>
    private GameObject _effect2;

    /// <summary>三个页签的选中态节点。</summary>
    private readonly GameObject[] _tabOn = new GameObject[3];

    /// <summary>三个页签的未选中态节点。</summary>
    private readonly GameObject[] _tabOff = new GameObject[3];

    /// <summary>缓存是否已完成，重复 InitWindow 时用来兜底。</summary>
    private bool _inited;

    /// <summary>当前页签下标。</summary>
    private int _tab = TabBoss;

    /// <summary>当前展示的副本数据。</summary>
    private ThemeDungeonEntry _entry;

    /// <summary>点击「开始挑战」时触发，参数为当前展示的副本。</summary>
    public System.Action<ThemeDungeonEntry> OnEnterDungeon;

    /// <summary>缓存控件、绑定事件，并把窗口按 RO 设计尺寸居中。重复调用安全。</summary>
    public override void InitWindow()
    {
        if (_inited)
        {
            return;
        }
        _root = transform as RectTransform;
        if (_root == null)
        {
            return;
        }
        ApplyDesignRect();
        NormalizeTextAlignment();
        CacheNodes();
        BindTabs();
        AddButtonListeners();
        _inited = true;
        gameObject.Show(false);
    }

    /// <summary>按 RO 的 1704×852 设计尺寸居中摆放，避免铺满 1920×1080 时元素被拉伸错位。</summary>
    private void ApplyDesignRect()
    {
        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.sizeDelta = DesignSize;
        _root.anchoredPosition = Vector2.zero;
        _root.localScale = Vector3.one;
    }

    /// <summary>
    /// 从 RO 的 UGUI Text 转过来的 TMP 里，垂直对齐曾被写成 25612 / 51212 这种非法值（正确值是 256 / 512）。
    /// 非法值不会报错，但会让文字整体横向偏移半个框宽，所以初始化时统一归一化一次。
    /// </summary>
    private void NormalizeTextAlignment()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        int i = 0;
        while (i < texts.Length)
        {
            int value = (int)texts[i].verticalAlignment;
            if (value > 8192)
            {
                texts[i].verticalAlignment = (VerticalAlignmentOptions)(value / 100);
            }
            i++;
        }
    }

    /// <summary>按名字缓存控件，找不到的留空并由调用处判空跳过。</summary>
    private void CacheNodes()
    {
        Transform boss = FindDeep(_root, "BossImg");
        if (boss != null)
        {
            _bossPic = boss.GetComponent<RawImage>();
        }
        Transform title = FindDeep(_root, "Title");
        if (title != null)
        {
            Transform t = FindDeep(title, "Text");
            if (t != null)
            {
                _titleText = t.GetComponent<TextMeshProUGUI>();
            }
            Transform close = FindDeep(title, "Btn_Close");
            if (close != null)
            {
                _closeBtn = close.GetComponent<Button>();
            }
        }
        _bossNameText = FindText("BossName");
        if (_bossNameText != null)
        {
            // RO 的原图是长条底图，文字跟着底条居中才不会偏到右端。
            _bossNameText.alignment = TextAlignmentOptions.Center;
        }
        _descText = FindText("DesText");
        _lvText = FindText("DungeonLv");
        _nameText = FindText("DungeonName");
        // 奖励标题是 Award 下的 Text，不能直接按名字找，否则会撞到标题栏里的 Text
        Transform award = FindDeep(_root, "Award");
        if (award != null)
        {
            _awardRoot = award.gameObject;
            Transform at = FindDeep(award, "Text");
            if (at != null)
            {
                _rewardTitle = at.GetComponent<TextMeshProUGUI>();
            }
        }
        Transform begin = FindDeep(_root, "BeginBtn");
        if (begin != null)
        {
            _beginBtn = begin.GetComponent<Button>();
        }
        Transform info = FindDeep(_root, "InformationBtn");
        if (info != null)
        {
            _infoBtn = info.GetComponent<Button>();
        }
        Transform scroll = FindDeep(_root, "InformationScroll");
        if (scroll != null)
        {
            _descScroll = scroll.gameObject;
            _infoScroll = scroll.GetComponent<ScrollRect>();
        }
        _downArrow = FindNode("DownArrow");
        SetupDownArrow();
        _passedImg = FindNode("PassedImg");
        _effect = FindNode("Effect");
        _effect2 = FindNode("Effect2");
        // RO 的两张特效图在预制体里是常显的，放进我们的界面会盖住正文，默认关掉。
        if (_effect != null)
        {
            _effect.SetActive(false);
        }
        if (_effect2 != null)
        {
            _effect2.SetActive(false);
        }
        SetupOpaqueBackdrop();
        SetupDescText();
    }

    /// <summary>
    /// 背景大图本身是半透明的，叠在 LYF 的主界面上会把下面的聊天窗口透出来。
    /// 在 Bg 之前插一层不透明深色底，既保留原图的氛围又不让下层内容漏出来。
    /// </summary>
    private void SetupOpaqueBackdrop()
    {
        Transform bg = FindDeep(_root, "Bg");
        if (bg == null)
        {
            return;
        }
        Transform old = _root.Find("TD_StoryBackdrop");
        if (old != null)
        {
            return;
        }
        GameObject go = new GameObject("TD_StoryBackdrop", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(_root, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.07f, 1f);
        img.raycastTarget = true;
        // 画布是 1704×852 居中的，四边会露出主界面，所以底色要外扩到盖满 1920×1080。
        rt.offsetMin = new Vector2(-400f, -400f);
        rt.offsetMax = new Vector2(400f, 400f);
        // 排到 Bg 前面，这样它在下层，Bg 仍然可见。
        go.transform.SetSiblingIndex(bg.GetSiblingIndex());
    }

    /// <summary>
    /// 正文字号固定、高度由内容决定。RO 原版给的是 106 高的固定框加裁切，
    /// 中文长段落会被切掉底部，这里改成按内容撑高并且不裁切。
    /// </summary>
    private void SetupDescText()
    {
        if (_descText == null)
        {
            return;
        }
        _descText.overflowMode = TextOverflowModes.Overflow;
        _descText.textWrappingMode = TextWrappingModes.Normal;
        // 左对齐才能保证长句正常换行，不会被视口左右各切一半。
        _descText.alignment = TextAlignmentOptions.TopLeft;
        _descText.enableAutoSizing = false;
        _descText.margin = new Vector4(2f, 0f, 2f, 2f);
        Transform content = _descText.transform.parent;
        if (content == null)
        {
            return;
        }
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            // TMP 的行顶会略微高出 rect 顶边，靠 margin 留白会连同字头一起被视口的 Mask 裁掉，
            // 所以改由内容容器留上边距，让整行落在视口内。
            vlg.padding = new RectOffset(5, 5, 12, 5);
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
        }
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        // 正文宽度跟随视口，避免预制体里写死的窄框把长句挤在一起。
        RectTransform vp = content.parent as RectTransform;
        if (vp != null)
        {
            RectTransform rt = _descText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y);
            rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y);
        }
    }

    /// <summary>缓存三个页签的选中态与未选中态节点。</summary>
    private void BindTabs()
    {
        BindTab(0, "BossTog");
        BindTab(1, "SpecialTog");
        BindTab(2, "RecordTog");
    }

    /// <summary>缓存单个页签的两个状态节点，并把它的按钮点击接到页签切换上。</summary>
    private void BindTab(int index, string name)
    {
        Transform tog = FindDeep(_root, name);
        if (tog == null)
        {
            return;
        }
        Transform on = FindDeep(tog, "ON");
        Transform off = FindDeep(tog, "OFF");
        _tabOn[index] = on == null ? null : on.gameObject;
        _tabOff[index] = off == null ? null : off.gameObject;
        HideBlankSprite(on);
        HideBlankSprite(off);
        // ON/OFF 两个状态节点里各带一个 Image，把点击挂到它们身上，比挂 Toggle 稳定。
        Transform hit = on != null ? on : off;
        if (hit == null)
        {
            return;
        }
        Image img = hit.GetComponent<Image>();
        if (img == null)
        {
            return;
        }
        img.raycastTarget = true;
        Button btn = hit.GetComponent<Button>();
        if (btn == null)
        {
            btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
        }
        int captured = index;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(delegate { SwitchTab(captured); });
    }

    /// <summary>
    /// RO 的页签状态节点上挂了一个没有 sprite 的 Image，Unity 会把它画成纯白方块。
    /// 这里只保留它的点击热区，颜色置全透明，避免白板盖住页签文字。
    /// </summary>
    private static void HideBlankSprite(Transform t)
    {
        if (t == null)
        {
            return;
        }
        Image img = t.GetComponent<Image>();
        if (img == null)
        {
            return;
        }
        if (img.sprite != null)
        {
            return;
        }
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = true;
    }

    /// <summary>给关闭、开始挑战、公开情报三个按钮注册回调。</summary>
    private void AddButtonListeners()
    {
        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(OnCloseClicked);
            _closeBtn.onClick.AddListener(OnCloseClicked);
        }
        if (_beginBtn != null)
        {
            _beginBtn.onClick.RemoveListener(OnBeginClicked);
            _beginBtn.onClick.AddListener(OnBeginClicked);
        }
        if (_infoBtn != null)
        {
            _infoBtn.onClick.RemoveListener(OnInfoClicked);
            _infoBtn.onClick.AddListener(OnInfoClicked);
        }
    }

    /// <summary>
    /// RO 里正文下方那个白色小箭头只是「下面还有内容」的提示：正文没溢出时隐藏，
    /// 滚到底也隐藏，点一下往下翻一页。移植过来时它是一张常显的死图，这里补上行为。
    /// </summary>
    private void SetupDownArrow()
    {
        if (_downArrow == null)
        {
            return;
        }
        Button btn = _downArrow.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnDownArrowClicked);
            btn.onClick.AddListener(OnDownArrowClicked);
        }
        if (_infoScroll != null)
        {
            _infoScroll.onValueChanged.RemoveListener(OnDescScrolled);
            _infoScroll.onValueChanged.AddListener(OnDescScrolled);
        }
        _downArrow.SetActive(false);
    }

    /// <summary>滚动时刷新箭头显隐。</summary>
    private void OnDescScrolled(Vector2 position)
    {
        RefreshDownArrow();
    }

    /// <summary>点箭头往下翻一页；已经到底就直接隐藏。</summary>
    private void OnDownArrowClicked()
    {
        if (_infoScroll == null || _infoScroll.content == null || _infoScroll.viewport == null)
        {
            return;
        }
        float contentHeight = _infoScroll.content.rect.height;
        float viewHeight = _infoScroll.viewport.rect.height;
        float range = contentHeight - viewHeight;
        if (range <= 1f)
        {
            _downArrow.SetActive(false);
            return;
        }
        float page = viewHeight / range;
        float target = _infoScroll.verticalNormalizedPosition - page;
        if (target < 0f)
        {
            target = 0f;
        }
        _infoScroll.verticalNormalizedPosition = target;
        RefreshDownArrow();
    }

    /// <summary>正文换行后布局要到帧末才重建，这里先强制重建再判断需不需要显示箭头。</summary>
    private void RefreshDownArrowNow()
    {
        if (_infoScroll != null && _infoScroll.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_infoScroll.content);
        }
        Canvas.ForceUpdateCanvases();
        RefreshDownArrow();
    }

    /// <summary>按滚动区实际尺寸决定箭头显隐：内容不溢出或已到底都隐藏。</summary>
    private void RefreshDownArrow()
    {
        if (_downArrow == null)
        {
            return;
        }
        if (_infoScroll == null || _infoScroll.content == null || _infoScroll.viewport == null)
        {
            _downArrow.SetActive(false);
            return;
        }
        float contentHeight = _infoScroll.content.rect.height;
        float viewHeight = _infoScroll.viewport.rect.height;
        if (contentHeight <= viewHeight + 1f)
        {
            _downArrow.SetActive(false);
            return;
        }
        // verticalNormalizedPosition：1 表示顶部，0 表示底部。
        _downArrow.SetActive(_infoScroll.verticalNormalizedPosition > 0.01f);
    }

    /// <summary>显示窗口并刷新为指定副本；传入空数据时只显示不刷新。</summary>
    public override void Show(bool isShow = true, object obj = null)
    {
        if (!_inited)
        {
            InitWindow();
        }
        if (isShow)
        {
            ThemeDungeonEntry entry = obj as ThemeDungeonEntry;
            if (entry != null)
            {
                ReFreshUI(entry);
            }
            transform.SetAsLastSibling();
        }
        gameObject.Show(isShow);
        if (isShow)
        {
            // 窗口刚激活时滚动区的 rect 才是有效值，这时才能判断正文有没有溢出。
            RefreshDownArrowNow();
        }
    }

    /// <summary>用副本数据刷新界面，并把页签重置到首领页。</summary>
    public override void ReFreshUI(object obj)
    {
        ThemeDungeonEntry entry = obj as ThemeDungeonEntry;
        if (entry == null)
        {
            return;
        }
        _entry = entry;
        if (_titleText != null)
        {
            _titleText.text = entry.themeName;
        }
        if (_nameText != null)
        {
            _nameText.text = entry.themeName;
        }
        if (_lvText != null)
        {
            _lvText.text = "Lv." + entry.chapterLevel;
        }
        ApplyBossPic(entry.painting);
        ApplyPassedState();
        SwitchTab(TabBoss);
    }

    /// <summary>加载首领立绘，路径为空或加载失败时保留预制体自带的图。</summary>
    private void ApplyBossPic(string path)
    {
        if (_bossPic == null || string.IsNullOrEmpty(path))
        {
            return;
        }
        string realPath = path;
        if (realPath.EndsWith(".png"))
        {
            realPath = realPath.Substring(0, realPath.Length - 4);
        }
        Texture2D tex = Resources.Load<Texture2D>("UI/Texture/" + realPath);
        if (tex != null)
        {
            _bossPic.texture = tex;
        }
    }

    /// <summary>按通关状态显示角标。</summary>
    private void ApplyPassedState()
    {
        if (_passedImg != null)
        {
            _passedImg.SetActive(false);
        }
    }

    /// <summary>切换页签：更新两个状态节点的显隐，并换上对应正文。</summary>
    private void SwitchTab(int index)
    {
        if (index < 0 || index > 2)
        {
            return;
        }
        _tab = index;
        int i = 0;
        while (i < 3)
        {
            bool active = i == index;
            if (_tabOn[i] != null)
            {
                _tabOn[i].SetActive(active);
            }
            if (_tabOff[i] != null)
            {
                _tabOff[i].SetActive(!active);
            }
            i++;
        }
        if (_awardRoot != null)
        {
            _awardRoot.SetActive(index == TabBoss);
        }
        if (_descScroll != null)
        {
            _descScroll.SetActive(true);
        }
        RefreshTabContent();
    }

    /// <summary>按当前页签与副本数据刷新正文标题和内容。</summary>
    private void RefreshTabContent()
    {
        if (_entry == null)
        {
            return;
        }
        if (_bossNameText != null)
        {
            if (_tab == TabBoss)
            {
                _bossNameText.text = "首领：" + _entry.leaderName;
            }
            else if (_tab == TabSpecial)
            {
                _bossNameText.text = "副本特性";
            }
            else
            {
                _bossNameText.text = "背景档案";
            }
        }
        if (_descText != null)
        {
            _descText.text = BuildTabText();
        }
        if (_rewardTitle != null)
        {
            _rewardTitle.text = "通关奖励：";
        }
        // 切页签换了正文，滚动位置要回到顶部，箭头按新内容的长度重新判断。
        if (_infoScroll != null)
        {
            _infoScroll.verticalNormalizedPosition = 1f;
        }
        RefreshDownArrowNow();
    }

    /// <summary>拼出当前页签要显示的正文。真实文案等配置表接入后替换这里。</summary>
    private string BuildTabText()
    {
        string name = _entry.themeName;
        if (_tab == TabBoss)
        {
            return "镇守「" + name + "」的首领是" + _entry.leaderName + "。\n"
                + "它盘踞在副本最深处，只有在清理掉沿途的守卫之后才会现身。\n"
                + "击败它可以获得该主题的专属掉落，是本主题剧情的最后一战。";
        }
        if (_tab == TabSpecial)
        {
            return "「" + name + "」的特性\n"
                + "· 首领拥有阶段性护盾，护盾存在时受到的伤害大幅降低。\n"
                + "· 战斗场地会周期性出现陷阱区域，站在其中会持续掉血。\n"
                + "· 建议携带能够驱散增益的职业组队挑战。";
        }
        return "「" + name + "」的档案\n"
            + "这一主题的剧情围绕该地区发生的异变展开。\n"
            + "玩家需要依次清理章节内的封锁，才能解锁首领所在的区域。\n"
            + "通关后可以在奖励界面领取对应的主题材料与外观。";
    }

    /// <summary>关闭按钮回调。</summary>
    private void OnCloseClicked()
    {
        CloseWindow();
    }

    /// <summary>「开始挑战」回调：等级校验通过后交给外部发起进入流程。</summary>
    private void OnBeginClicked()
    {
        if (_entry == null)
        {
            return;
        }
        if (OnEnterDungeon != null)
        {
            OnEnterDungeon(_entry);
        }
    }

    /// <summary>「公开情报」回调，对应 RO 里的 Theme_Information 弹窗，当前只做占位提示。</summary>
    private void OnInfoClicked()
    {
        Debug.Log("[ThemeStory] 公开情报按钮点击，等待接入 Theme_Information");
    }

    /// <summary>按名字找深层子节点。</summary>
    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        int i = 0;
        while (i < all.Length)
        {
            if (all[i].name == name)
            {
                return all[i];
            }
            i++;
        }
        return null;
    }

    /// <summary>按名字找节点并返回 GameObject。</summary>
    private GameObject FindNode(string name)
    {
        Transform t = FindDeep(_root, name);
        return t == null ? null : t.gameObject;
    }

    /// <summary>按名字找 TMP 文本。</summary>
    private TextMeshProUGUI FindText(string name)
    {
        Transform t = FindDeep(_root, name);
        return t == null ? null : t.GetComponent<TextMeshProUGUI>();
    }
}
