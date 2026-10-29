using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主题副本选择窗口，直接挂在 ThemeDungeonArround 预制体实例的根节点上（该实例挂在 UIRoot / MainView 下），
/// 负责副本书位的配置填充、上下翻页、选中高亮、右侧详情展示与关闭。
/// </summary>
public sealed class ThemeDungeonArroundWindow : WindowBase
{
    /// <summary>配置文件在 Resources 下的路径，指向 theme_dungeon.json。</summary>
    [SerializeField, Header("配置资源路径")] private string _configResourcePath = "UI/Prefabs/ROCharacterLightingDemo/ThemeDungeon/theme_dungeon";

    /// <summary>贴图资源根目录，条目里的 scenePic / painting 都是相对它的路径。</summary>
    [SerializeField, Header("贴图资源根目录")] private string _textureResourceRoot = "UI/Texture/";

    /// <summary>字体资源路径，运行时创建出来的 TMP 文本统一使用 Common SDF。</summary>
    [SerializeField, Header("字体资源路径")] private string _fontResourcePath = "UI/Font/Common SDF";

    /// <summary>当前角色等级，用于判断书位是否解锁（仅本地演示用，未接角色数据）。</summary>
    [SerializeField, Header("当前角色等级")] private int _playerLevel = 50;

    /// <summary>运行时创建文本时使用的默认字号。</summary>
    private const float DefaultFontSize = 18f;

    /// <summary>金色，用于副本名与标题等强调文本。</summary>
    private static readonly Color ColGold = new Color(0.98f, 0.84f, 0.51f, 1f);

    /// <summary>常规正文色。</summary>
    private static readonly Color ColText = new Color(0.93f, 0.94f, 0.96f, 1f);

    /// <summary>次要信息色，用于章节与说明文本。</summary>
    private static readonly Color ColDim = new Color(0.67f, 0.71f, 0.76f, 1f);

    /// <summary>红色，用于"未解锁"提示文本。</summary>
    private static readonly Color ColRed = new Color(1f, 0.42f, 0.38f, 1f);

    /// <summary>书位未选中时的描边色：RO 原版书位没有边框，这里直接透明，只留选中态金边。</summary>
    private static readonly Color ColFrameIdle = new Color(0.36f, 0.29f, 0.18f, 0f);

    /// <summary>书位选中时的高亮边框色。</summary>
    private static readonly Color ColFrameOn = new Color(1f, 0.86f, 0.55f, 1f);

    /// <summary>全部副本条目，由配置文件反序列化得到。</summary>
    private ThemeDungeonEntry[] _items = new ThemeDungeonEntry[0];

    /// <summary>8 个书位的运行时包装，顺序与预制体 ScrollView 下 Item1* 子节点一致。</summary>
    private readonly List<Slot> _slots = new List<Slot>();

    /// <summary>ThemeDungeonArround 预制体实例的根变换，所有节点查找都以它为起点。</summary>
    private RectTransform _root;

    /// <summary>当前页码，从 0 开始。</summary>
    private int _page;

    /// <summary>当前选中的副本在 _items 中的下标，-1 表示未选中。</summary>
    private int _selected = -1;

    /// <summary>运行时创建的文本统一使用的字体资源，InitWindow 时加载一次。</summary>
    private TMP_FontAsset _fontAsset;

    /// <summary>白色 1x1 贴图生成的精灵，用于透明点击区与纯色底。</summary>
    private Sprite _whiteSprite;

    /// <summary>全屏背景图，取当前选中副本的场景大图。</summary>
    private RawImage _splash;

    /// <summary>底部操作反馈文本。</summary>
    private TextMeshProUGUI _statusText;

    /// <summary>翻页按钮上的页码文本。</summary>
    private TextMeshProUGUI _pageText;

    /// <summary>面板顶部的当前副本名文本。</summary>
    private TextMeshProUGUI _nameOfPanel;

    /// <summary>上翻页按钮。</summary>
    private Button _upBtn;

    /// <summary>下翻页按钮。</summary>
    private Button _downBtn;

    /// <summary>关闭按钮。</summary>
    private Button _closeBtn;

    /// <summary>挑战（进入副本）按钮。</summary>
    private Button _challengeBtn;

    /// <summary>
    /// 是否保留 RO 原版悬浮在书台中心的「挑战玩法」信息牌。
    /// RO 的 Lua（ThemeDungeonArroundCtrl:RefreshPage）里这块牌子只有在挑战系统开放、
    /// 且当前主题不为空时才显示，所以它本来就是界面的常驻元素，这里保持显示。
    /// </summary>
    private const bool ShowLegacyThemeBadge = true;

    /// <summary>RO 书位名字底条的精灵，从预制体的 NameBg 节点上取，保证和 RO 一致。</summary>
    private Sprite _nameBarSprite;

    /// <summary>进入副本按钮上的副本名文本，随选中的副本实时刷新。</summary>
    private TextMeshProUGUI _challengeName;

    /// <summary>进入副本按钮的透明度分组，未选中任何副本时整体压暗。</summary>
    private CanvasGroup _challengeGroup;

    /// <summary>副本详情窗口（RO 的 Theme_Story），点书位翻开时打开。</summary>
    private ThemeStoryWindow _storyWindow;

    /// <summary>外部注入详情窗口，由主界面在初始化时调用。</summary>
    public void SetStoryWindow(ThemeStoryWindow window)
    {
        _storyWindow = window;
        if (_storyWindow != null)
        {
            _storyWindow.OnEnterDungeon = EnterDungeon;
        }
    }

    /// <summary>详情区的确认按钮。</summary>
    private Button _confirmBtn;

    /// <summary>是否已经完成初始化，避免重复绑定按钮监听。</summary>
    private bool _inited;

    /// <summary>
    /// 单个书位需要的控件引用：整块点击区、选中边框、缩略图、副本名、章节行、锁定遮罩与提示。
    /// </summary>
    private class Slot
    {
        /// <summary>书位根变换，点击事件与子节点创建都挂在它上面。</summary>
        public RectTransform Root;

        /// <summary>本页该书位对应的副本在条目数组中的下标，越界时书位会被隐藏。</summary>
        public int Index;

        /// <summary>选中高亮边框，位于缩略图下层露出金边。</summary>
        public Image Frame;

        /// <summary>副本场景缩略图。</summary>
        public RawImage Thumb;

        /// <summary>副本名文本。</summary>
        public TextMeshProUGUI Name;

        /// <summary>章节与等级文本。</summary>
        public TextMeshProUGUI Chapter;

        /// <summary>未解锁时的半透明黑色遮罩。</summary>
        public Image LockMask;

        /// <summary>遮罩上的"Lv.x 解锁"提示文本。</summary>
        public TextMeshProUGUI LockText;

        /// <summary>用书位根变换构造，Root 直接取该变换。</summary>
        public Slot(Transform t)
        {
            Root = t as RectTransform;
        }
    }

    /// <summary>
    /// 初始化窗口：定位预制体实例、缓存节点、加载配置并填充书位与详情区。
    /// 重复调用时先移除旧监听，避免按钮回调叠加。
    /// </summary>
    public override void InitWindow()
    {
        base.InitWindow();
        if (_inited)
        {
            RemoveButtonListeners();
        }
        _slots.Clear();
        _selected = -1;
        _page = 0;

        _root = FindThemeDungeonRoot();
        if (_root == null)
        {
            Debug.LogError("[主题副本] 窗口根节点上没有 RectTransform");
            return;
        }

        // 重复初始化时先清掉上一轮建的运行时节点，否则会一层层叠加。
        ClearRuntimeNodes();

        // RO 原版把预制体根的 localScale 存成 (0,0,0)，运行时由驱动脚本恢复为 1；
        // 不恢复的话整块界面尺寸为零，任何控件都看不见。
        _root.localScale = Vector3.one;

        _fontAsset = Resources.Load<TMP_FontAsset>(_fontResourcePath);
        _whiteSprite = MakeWhiteSprite();

        CacheNodes();
        SetupChallengeButton();
        FixLostSpriteGraphics();
        LoadConfig();
        CacheLegacySprites();
        BuildCardVisuals();
        BuildDetailPanel();
        ShowPage(0, true);
        _inited = true;
    }

    /// <summary>
    /// 取预制体里 RO 自带的贴图，供运行时新建的节点复用，保证视觉和 RO 一致。
    /// 必须在隐藏那些旧节点之前调用。
    /// </summary>
    private void CacheLegacySprites()
    {
        Transform bar = FindDeep(_root, "NameBg");
        if (bar != null)
        {
            Image img = bar.GetComponent<Image>();
            if (img != null)
            {
                _nameBarSprite = img.sprite;
            }
        }
    }

    /// <summary>
    /// 显示或隐藏窗口，隐藏时不销毁运行时创建的节点，再次打开直接复用。
    /// </summary>
    /// <param name="isShow">true 显示，false 隐藏。</param>
    /// <param name="obj">外部传入的数据，为空则不刷新。</param>
    public override void Show(bool isShow = true, object obj = null)
    {
        gameObject.Show(isShow);
        if (isShow)
        {
            if (!_inited)
            {
                InitWindow();
            }
            // 主界面下所有窗口都是同级的平铺节点，渲染顺序就是兄弟顺序；
            // 每次打开都提到同级最末，保证不会被聊天、背包、设置等窗口盖住。
            BringToFront();
        }
        if (obj != null)
        {
            ReFreshUI(obj);
        }
    }

    /// <summary>
    /// 把窗口节点移到同级最后一位，使它在 UGUI 里渲染在最上层。
    /// </summary>
    private void BringToFront()
    {
        Transform self = transform;
        if (self.parent == null)
        {
            return;
        }
        self.SetAsLastSibling();
    }

    /// <summary>
    /// 用外部数据刷新窗口：整型参数按新的角色等级重算解锁状态。
    /// </summary>
    /// <param name="obj">期望为 int 类型的角色等级。</param>
    public override void ReFreshUI(object obj)
    {
        if (obj is int)
        {
            _playerLevel = (int)obj;
        }
        ShowPage(_page, false);
    }

    /// <summary>
    /// 删掉本脚本运行时创建的所有 TD_ 前缀节点（详情面板、背景、书位装饰等），
    /// 预制体自带的节点不受影响。重复调用 InitWindow 时用于避免节点叠加。
    /// </summary>
    private void ClearRuntimeNodes()
    {
        if (_root == null)
        {
            return;
        }
        Transform[] all = _root.GetComponentsInChildren<Transform>(true);
        for (int i = all.Length - 1; i >= 0; i--)
        {
            if (all[i] == null || all[i] == _root)
            {
                continue;
            }
            if (all[i].name.StartsWith("TD_"))
            {
                DestroyImmediate(all[i].gameObject);
            }
        }

        // 这些控件都是运行时建的，已经跟着节点一起销毁。引用必须同步清空：
        // Unity 销毁过的对象做 != null 判断仍然为 true，留着会在下一次刷新时报空引用。
        _splash = null;
        _statusText = null;
    }

    /// <summary>
    /// 取得界面根变换。脚本直接挂在 ThemeDungeonArround 预制体根上，根就是自身；
    /// 若被挂到别的容器上，则向下找名为 ThemeDungeonArround 的子节点。
    /// </summary>
    private RectTransform FindThemeDungeonRoot()
    {
        RectTransform self = transform as RectTransform;
        if (self != null && transform.name == "ThemeDungeonArround")
        {
            return self;
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "ThemeDungeonArround")
            {
                return child as RectTransform;
            }
        }
        return self;
    }

    /// <summary>
    /// 缓存预制体里已有的按钮与文本节点，并注册翻页、关闭、挑战、确认的点击事件。
    /// </summary>
    private void CacheNodes()
    {
        Transform up = FindDeep(_root, "UpBtn");
        if (up != null)
        {
            _upBtn = up.GetComponent<Button>();
        }
        Transform down = FindDeep(_root, "DownBtn");
        if (down != null)
        {
            _downBtn = down.GetComponent<Button>();
        }
        Transform close = FindDeep(_root, "CloseBtn");
        if (close != null)
        {
            _closeBtn = close.GetComponent<Button>();
        }
        Transform challenge = FindDeep(_root, "ChallengeDungeonBtn");
        if (challenge != null)
        {
            _challengeBtn = challenge.GetComponent<Button>();
        }
        Transform challengeName = FindDeep(_root, "ChallengeName");
        if (challengeName != null)
        {
            _challengeName = challengeName.GetComponent<TextMeshProUGUI>();
        }
        Transform confirm = FindDeep(_root, "Button");
        if (confirm != null)
        {
            _confirmBtn = confirm.GetComponent<Button>();
        }

        Transform pageNode = FindDeep(_root, "PageText");
        if (pageNode != null)
        {
            _pageText = pageNode.GetComponent<TextMeshProUGUI>();
        }
        Transform nameNode = FindDeep(_root, "Name");
        if (nameNode != null)
        {
            _nameOfPanel = nameNode.GetComponent<TextMeshProUGUI>();
        }

        Transform scroll = FindDeep(_root, "ScrollView");
        if (scroll == null)
        {
            scroll = FindDeep(_root, "ActivePanel");
        }
        if (scroll != null)
        {
            for (int i = 0; i < scroll.childCount; i++)
            {
                Transform c = scroll.GetChild(i);
                if (c.name.StartsWith("Item1"))
                {
                    _slots.Add(new Slot(c));
                }
            }
        }

        AddButtonListeners();
    }

    /// <summary>
    /// 配置「进入副本」按钮：解锁主题名的自动缩放，并挂一个透明度分组，
    /// 供未选中任何副本时把整个按钮压暗。重复调用是安全的。
    /// </summary>
    private void SetupChallengeButton()
    {
        if (_challengeName != null)
        {
            // RO 原版把 overflow 设成裁切，长主题名会被直接切掉；改成按框自动缩放。
            _challengeName.overflowMode = TextOverflowModes.Overflow;
            _challengeName.enableAutoSizing = true;
            _challengeName.fontSizeMin = 12f;
            _challengeName.fontSizeMax = 28f;
        }
        if (_challengeBtn != null)
        {
            // RO 里这块牌子是「挑战模式」入口（Theme_Challenge），不是进入当前副本的按钮。
            // 挑战模式界面还没移植，所以只保留可点与提示，不做静默失效。
            _challengeBtn.interactable = true;
            _challengeGroup = _challengeBtn.GetComponent<CanvasGroup>();
            if (_challengeGroup == null)
            {
                _challengeGroup = _challengeBtn.gameObject.AddComponent<CanvasGroup>();
            }
            _challengeBtn.gameObject.SetActive(ShowLegacyThemeBadge);
            // RO 的牌子挂在 y=73.4，正好落在 3D 书台的空心位置；LYF 的封面是 2D 矩形，
            // 往上占得更多，实测把牌子抬到 100 才刚好卡在上下两本书之间（上下各留 ~2px）。
            RectTransform badgeRt = _challengeBtn.transform as RectTransform;
            if (badgeRt != null)
            {
                badgeRt.anchoredPosition = new Vector2(3.2f, 100f);
            }
            // 底下是明亮的场景大图，白字会糊，所以在副本名背后垫一层半透明暗色。
            if (_challengeName != null)
            {
                Transform info = _challengeName.transform.parent;
                if (info != null && info.Find("TD_BadgePlate") == null)
                {
                    Image plate = NewImage("TD_BadgePlate", info, 0f, -8f, 214f, 90f, new Color(0f, 0f, 0f, 0.42f));
                    plate.raycastTarget = false;
                    plate.transform.SetAsFirstSibling();
                }
            }
        }
        RefreshChallengeButton();
    }

    /// <summary>
    /// 按当前选中状态刷新「进入副本」按钮：有选中就把副本名写上去并恢复正常亮度，
    /// 没选中则显示占位提示并压暗。按钮始终可点，点了会给提示而不是静默无反应。
    /// </summary>
    private void RefreshChallengeButton()
    {
        bool ready = _selected >= 0 && _selected < _items.Length;

        // 中心的信息牌：只显示当前选中的副本名，不响应点击。
        if (_challengeName != null)
        {
            if (ready)
            {
                _challengeName.text = _items[_selected].themeName;
                _challengeName.color = Color.white;
            }
            else
            {
                _challengeName.text = "请选择副本";
                _challengeName.color = ColDim;
            }
        }
        if (_challengeGroup != null)
        {
            if (ready)
            {
                _challengeGroup.alpha = 1f;
            }
            else
            {
                _challengeGroup.alpha = 0.4f;
            }
        }
    }

    /// <summary>给已缓存的按钮注册点击回调，未找到的按钮直接跳过。</summary>
    private void AddButtonListeners()
    {
        if (_upBtn != null)
        {
            _upBtn.onClick.AddListener(PrevPage);
        }
        if (_downBtn != null)
        {
            _downBtn.onClick.AddListener(NextPage);
        }
        if (_closeBtn != null)
        {
            _closeBtn.onClick.AddListener(OnCloseClicked);
        }
        if (_challengeBtn != null)
        {
            _challengeBtn.onClick.AddListener(OnChallengeClicked);
        }
        if (_confirmBtn != null)
        {
            _confirmBtn.onClick.AddListener(OnConfirmClicked);
        }
    }

    /// <summary>移除已注册的点击回调，供重复初始化时调用。</summary>
    private void RemoveButtonListeners()
    {
        if (_upBtn != null)
        {
            _upBtn.onClick.RemoveListener(PrevPage);
        }
        if (_downBtn != null)
        {
            _downBtn.onClick.RemoveListener(NextPage);
        }
        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(OnCloseClicked);
        }
        if (_challengeBtn != null)
        {
            _challengeBtn.onClick.RemoveListener(OnChallengeClicked);
        }
        if (_confirmBtn != null)
        {
            _confirmBtn.onClick.RemoveListener(OnConfirmClicked);
        }
    }

    /// <summary>
    /// 读取并解析主题副本配置，失败时置为空数组并打印错误。
    /// </summary>
    private void LoadConfig()
    {
        TextAsset asset = Resources.Load<TextAsset>(_configResourcePath);
        if (asset == null)
        {
            Debug.LogError("[主题副本] 找不到配置文件: " + _configResourcePath);
            _items = new ThemeDungeonEntry[0];
            return;
        }
        ThemeDungeonConfig config = JsonUtility.FromJson<ThemeDungeonConfig>(asset.text);
        if (config == null || config.items == null)
        {
            _items = new ThemeDungeonEntry[0];
            return;
        }
        _items = config.items;
    }

    /// <summary>
    /// 清掉预制体里"sprite 引用丢失的白板"。
    /// RO 原工程转过来时部分 Image 的 sprite 变成 null，Unity 遇到这种 Image 会画一整块纯白不透明矩形；
    /// ScrollView 上那张 0x0 拉伸铺满全屏的白板就是它，本身还带 -28° 的 X 轴旋转，
    /// 在主画布（Screen Space - Camera，透视投影）里会被投成斜着的梯形，把整个界面盖白。
    /// 这里统一改成全透明，但保留 raycastTarget，滚动与点击区域不受影响。
    /// </summary>
    private void FixLostSpriteGraphics()
    {
        Image[] images = _root.GetComponentsInChildren<Image>(true);
        int fixedCount = 0;
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img.sprite != null)
            {
                continue;
            }
            Color c = img.color;
            if (c.a <= 0f)
            {
                continue;
            }
            c.a = 0f;
            img.color = c;
            fixedCount++;
        }
        if (fixedCount > 0)
        {
            Debug.Log("[主题副本] 已透明化丢图白板数量: " + fixedCount);
        }
    }

    /// <summary>
    /// 为每个书位补上点击区、选中边框、缩略图、名称、章节与锁定遮罩，
    /// 并隐藏 RO 原版里位置错位的旧文字与碰撞体子节点。
    /// </summary>
    private void BuildCardVisuals()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            Slot s = _slots[i];
            RectTransform rt = s.Root;

            // 整块 155x155 都可点：原预制体只有 Collider 子节点可点，偏移对不上。
            Image hit = rt.GetComponent<Image>();
            if (hit == null)
            {
                hit = rt.gameObject.AddComponent<Image>();
            }
            hit.sprite = _whiteSprite;
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            Button btn = rt.GetComponent<Button>();
            if (btn == null)
            {
                btn = rt.gameObject.AddComponent<Button>();
            }
            btn.transition = Selectable.Transition.None;
            int captured = i;
            btn.onClick.AddListener(delegate { OnSlotClicked(captured); });

            // 选中描边画在封面下层，只有选中时才上色，未选中完全透明，避免 RO 原版没有的金框。
            s.Frame = NewImage("TD_Frame", rt, 0f, 12f, 116f, 116f, ColFrameIdle);
            s.Frame.raycastTarget = false;

            // RO 的书是 3D 立在书台上的，2D 缩略图不能太大，否则环心会被占满、中心信息牌没地方放。
            RectTransform thumbRt = NewRect("TD_Thumb", rt, 0f, 12f, 108f, 108f);
            s.Thumb = thumbRt.gameObject.AddComponent<RawImage>();
            s.Thumb.raycastTarget = false;
            s.Thumb.color = Color.white;

            // RO 的书位是「压条写章节 + 下面写副本名」（NameBg / ChapterName / DungeonName 三层），
            // 这里照抄这套层级，只是把 3D 书皮换成了场景缩略图。
            Image bar = NewImage("TD_NameBar", rt, 0f, -56f, 146f, 24f, Color.white);
            bar.raycastTarget = false;
            if (_nameBarSprite != null)
            {
                bar.sprite = _nameBarSprite;
                bar.type = Image.Type.Sliced;
            }
            else
            {
                bar.color = new Color(0.06f, 0.05f, 0.03f, 0.85f);
            }
            s.Chapter = NewText("TD_Chapter", rt, 0f, -56f, 142f, 22f, 14f, ColText, TextAlignmentOptions.Center);
            s.Name = NewText("TD_Name", rt, 0f, -80f, 164f, 26f, 18f, ColGold, TextAlignmentOptions.Center);

            s.LockMask = NewImage("TD_LockMask", rt, 0f, 12f, 108f, 108f, new Color(0f, 0f, 0f, 0.62f));
            s.LockMask.raycastTarget = false;
            s.LockText = NewText("TD_LockText", s.LockMask.transform, 0f, 0f, 108f, 30f, 16f, ColRed, TextAlignmentOptions.Center);

            Transform col = FindDirect(rt, "Collider");
            if (col != null)
            {
                col.gameObject.SetActive(false);
            }
            string[] legacy = { "DungeonName", "ChapterName", "NotOpen", "Lock", "NameBg" };
            for (int k = 0; k < legacy.Length; k++)
            {
                Transform old = FindDirect(rt, legacy[k]);
                if (old != null)
                {
                    old.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 创建背景层与操作反馈文本。
    /// RO 这个界面自己没有详情板：书位下方的章节条 + 副本名、中心的挑战信息牌、以及
    /// 翻开后的 Theme_Story 才是它的信息结构，所以这里只补一张全屏背景和一个反馈行。
    /// </summary>
    private void BuildDetailPanel()
    {
        RectTransform splashRt = NewRect("TD_Splash", _root, 0f, 0f, 0f, 0f);
        Stretch(splashRt);
        _splash = splashRt.gameObject.AddComponent<RawImage>();
        _splash.raycastTarget = false;
        // RawImage 在 texture 为空时会画出一个实心白块，这里先设为全透明，选中后再按贴图恢复。
        _splash.color = new Color(1f, 1f, 1f, 0f);
        _splash.uvRect = new Rect(0f, 0f, 1f, 1f);
        splashRt.SetSiblingIndex(1);

        Image dim = NewImage("TD_SplashDim", _root, 0f, 0f, 0f, 0f, new Color(0f, 0f, 0f, 0.30f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = false;
        dim.rectTransform.SetSiblingIndex(2);

        // 反馈行放在页签圆点上方居中，和 RO 的底部页码区同一区域，不会压到主界面 HUD。
        _statusText = NewText("TD_Status", _root, 0f, -252f, 720f, 30f, 16f, ColGold, TextAlignmentOptions.Center);
        _statusText.text = string.Empty;
    }

    /// <summary>总页数，书位为 0 时按 1 页处理。</summary>
    private int PageCount
    {
        get
        {
            if (_slots.Count <= 0)
            {
                return 1;
            }
            return Mathf.Max(1, Mathf.CeilToInt(_items.Length / (float)_slots.Count));
        }
    }

    /// <summary>
    /// 显示指定页并刷新书位内容。
    /// </summary>
    /// <param name="page">目标页码，会被夹在合法范围内。</param>
    /// <param name="selectFirst">true 时自动选中本页第一个书位。</param>
    private void ShowPage(int page, bool selectFirst)
    {
        if (_items.Length == 0)
        {
            RefreshChallengeButton();
            return;
        }
        _page = Mathf.Clamp(page, 0, PageCount - 1);

        for (int i = 0; i < _slots.Count; i++)
        {
            int index = _page * _slots.Count + i;
            Slot s = _slots[i];
            if (index >= _items.Length)
            {
                s.Root.gameObject.SetActive(false);
                continue;
            }
            s.Root.gameObject.SetActive(true);
            s.Index = index;
            ApplySlot(s, _items[index]);
        }

        UpdateFrames();
        if (_pageText != null)
        {
            _pageText.text = (_page + 1) + " / " + PageCount;
        }
        if (selectFirst)
        {
            Select(_page * _slots.Count, false);
        }
        else
        {
            // 翻页后原来选中的副本可能已经不在本页，继续留着会让「进入副本」按钮
            // 显示上一页的副本名，所以自动改选本页第一个。
            int first = _page * _slots.Count;
            int last = first + _slots.Count - 1;
            bool inPage = _selected >= first && _selected <= last && _selected < _items.Length;
            if (!inPage)
            {
                Select(first, false);
            }
        }
        RefreshChallengeButton();
    }

    /// <summary>
    /// 把一个副本条目的数据填到书位上：缩略图、名称、章节等级与锁定状态。
    /// </summary>
    private void ApplySlot(Slot s, ThemeDungeonEntry d)
    {
        bool locked = _playerLevel < d.chapterLevel;
        s.Thumb.texture = LoadTexture(d.scenePic);
        ApplyCoverUv(s.Thumb, 0f, 0f);
        if (locked)
        {
            s.Thumb.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        }
        else
        {
            s.Thumb.color = Color.white;
        }
        s.Name.text = d.themeName;
        s.Chapter.text = d.chapterText + " · Lv." + d.chapterLevel;
        s.LockMask.gameObject.SetActive(locked);
        if (locked)
        {
            s.LockText.text = "Lv." + d.chapterLevel + " 解锁";
        }
    }

    /// <summary>刷新所有书位的边框颜色，选中项为高亮金边，其余为暗金。</summary>
    private void UpdateFrames()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            Slot s = _slots[i];
            bool on = s.Index == _selected;
            if (on)
            {
                s.Frame.color = ColFrameOn;
            }
            else
            {
                s.Frame.color = ColFrameIdle;
            }
        }
    }

    /// <summary>
    /// 选中指定下标的副本并刷新右侧详情。
    /// </summary>
    /// <param name="index">副本在 _items 中的下标。</param>
    /// <param name="fromClick">true 表示由玩家点击触发，会给出操作反馈。</param>
    private void Select(int index, bool fromClick)
    {
        if (index < 0 || index >= _items.Length)
        {
            return;
        }
        _selected = index;
        UpdateFrames();

        ThemeDungeonEntry d = _items[index];
        _splash.texture = LoadTexture(d.scenePic);
        ApplySplashCover();
        // 贴图加载成功才显示，否则保持透明，避免整屏白块。
        if (_splash.texture != null)
        {
            _splash.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            _splash.color = new Color(1f, 1f, 1f, 0f);
        }

        if (_nameOfPanel != null)
        {
            _nameOfPanel.text = d.themeName;
        }
        // 进入副本按钮上的名字跟着选中项走。
        RefreshChallengeButton();
        if (fromClick)
        {
            SetStatus("已选中：" + d.themeName);
        }
    }

    /// <summary>
    /// 书位点击回调。RO 里点书位是「把这本书转到正前方」，转到正前方之后再点才翻开；
    /// LYF 没有 3D 转盘，点一次就把选中与翻开一起做掉，避免玩家点第二下以为失灵。
    /// </summary>
    private void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return;
        }
        int index = _slots[slotIndex].Index;
        Select(index, true);
        if (_selected != index)
        {
            return;
        }
        EnterSelected();
    }

    /// <summary>上一页，已在第一页时给出提示。</summary>
    private void PrevPage()
    {
        if (_page <= 0)
        {
            SetStatus("已经是第一页");
            return;
        }
        ShowPage(_page - 1, false);
    }

    /// <summary>下一页，已在最后一页时给出提示。</summary>
    private void NextPage()
    {
        if (_page >= PageCount - 1)
        {
            SetStatus("已经是最后一页");
            return;
        }
        ShowPage(_page + 1, false);
    }

    /// <summary>
    /// 翻开选中的书：按 RO 的流程先打开 Theme_Story 详情页，
    /// 由详情页右下角的「开始挑战」再真正发起进入。
    /// 重复点击已选中的书位走这里，对应 RO 里点书翻开书的动作。
    /// </summary>
    private void EnterSelected()
    {
        if (_selected < 0 || _selected >= _items.Length)
        {
            SetStatus("请先选择一个副本");
            return;
        }
        ThemeDungeonEntry d = _items[_selected];
        if (_storyWindow == null)
        {
            SetStatus("详情界面未接入，" + d.themeName + " 无法打开");
            return;
        }
        _storyWindow.Show(true, d);
        SetStatus("翻开：" + d.themeName);
    }

    /// <summary>
    /// 详情页确认进入副本。当前是空壳：只做等级校验并给出反馈，
    /// 真正的进入副本协议等服务器接口就绪后在这里发起。
    /// </summary>
    private void EnterDungeon(ThemeDungeonEntry d)
    {
        if (d == null)
        {
            return;
        }
        if (_playerLevel < d.chapterLevel)
        {
            SetStatus("等级不足，" + d.themeName + " 需要 Lv." + d.chapterLevel);
            return;
        }
        SetStatus("进入副本：" + d.themeName + "（协议待接入）");
    }

    /// <summary>确认按钮回调，目前只做反馈，不发起进入副本的协议。</summary>
    private void OnConfirmClicked()
    {
        if (_selected < 0)
        {
            SetStatus("请先选择一个副本");
            return;
        }
        SetStatus("确认：" + _items[_selected].themeName);
    }

    /// <summary>
    /// 中心「挑战玩法」信息牌回调。RO 里它打开的是 Theme_Challenge（主题挑战模式），
    /// 该界面还没移植，这里只给提示，避免点了没有任何反应。
    /// </summary>
    private void OnChallengeClicked()
    {
        if (_selected < 0 || _selected >= _items.Length)
        {
            SetStatus("请先选择一个副本");
            return;
        }
        SetStatus("挑战模式：" + _items[_selected].themeName + "（界面待移植）");
    }

    /// <summary>关闭按钮回调，隐藏本窗口。</summary>
    private void OnCloseClicked()
    {
        CloseWindow();
    }

    /// <summary>在右下角显示一行操作反馈文本。</summary>
    private void SetStatus(string msg)
    {
        if (_statusText != null)
        {
            _statusText.text = msg;
        }
    }

    /// <summary>
    /// 按 Resources 相对路径加载贴图，自动去掉扩展名。
    /// </summary>
    private Texture LoadTexture(string relative)
    {
        if (string.IsNullOrEmpty(relative))
        {
            return null;
        }
        string p = relative.Replace("\\", "/");
        int dot = p.LastIndexOf('.');
        if (dot > p.LastIndexOf('/'))
        {
            p = p.Substring(0, dot);
        }
        return Resources.Load<Texture2D>(_textureResourceRoot + p);
    }

    /// <summary>
    /// 让 RawImage 按"短边铺满、长边裁切"的方式显示贴图，避免宽幅场景图被压扁。
    /// </summary>
    private static void ApplyCoverUv(RawImage raw, float targetW, float targetH)
    {
        if (raw == null || raw.texture == null)
        {
            return;
        }
        raw.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    /// <summary>按当前屏幕宽高比重算背景大图的裁切区域。</summary>
    private void ApplySplashCover()
    {
        if (_splash == null)
        {
            return;
        }
        ApplyCoverUv(_splash, (float)Screen.width, (float)Screen.height);
    }

    /// <summary>
    /// 深度优先查找指定名称的子节点。
    /// </summary>
    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name)
            {
                return c;
            }
            Transform r = FindDeep(c, name);
            if (r != null)
            {
                return r;
            }
        }
        return null;
    }

    /// <summary>只在直接子节点里查找指定名称的节点。</summary>
    private static Transform FindDirect(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name)
            {
                return c;
            }
        }
        return null;
    }

    /// <summary>创建一个居中锚点的 RectTransform 子节点。</summary>
    private RectTransform NewRect(string name, Transform parent, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.transform as RectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    /// <summary>创建一个纯色 Image 子节点。</summary>
    private Image NewImage(string name, Transform parent, float x, float y, float w, float h, Color c)
    {
        RectTransform rt = NewRect(name, parent, x, y, w, h);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = _whiteSprite;
        img.color = c;
        return img;
    }

    /// <summary>创建一个 TextMeshProUGUI 子节点，字体统一使用 Common SDF。</summary>
    private TextMeshProUGUI NewText(string name, Transform parent, float x, float y, float w, float h,
        float fontSize, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent, x, y, w, h);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null)
        {
            t.font = _fontAsset;
        }
        if (fontSize > 0f)
        {
            t.fontSize = fontSize;
        }
        else
        {
            t.fontSize = DefaultFontSize;
        }
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = string.Empty;
        return t;
    }

    /// <summary>把 RectTransform 拉伸到铺满父节点。</summary>
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    /// <summary>生成一张 1x1 白色贴图对应的精灵，供纯色图与透明点击区使用。</summary>
    private Sprite MakeWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
    }
}
