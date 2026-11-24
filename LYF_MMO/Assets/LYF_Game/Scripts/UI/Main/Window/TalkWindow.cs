using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>在场景人物前展示逐句对白和该 NPC 可用的服务，管理对话期间的界面显隐。</summary>
public class TalkWindow : WindowBase
{
    /// <summary>从 NPC 表逐句显示的对白。</summary>
    [SerializeField] private TMP_Text _talkContent;
    /// <summary>对白区左侧显示的说话者。</summary>
    [SerializeField] private TMP_Text _talkTitle;
    /// <summary>按服务数量显隐的右侧滚动区域。</summary>
    [SerializeField] private ScrollRect _options;
    /// <summary>在正式预制体中排版的服务按钮，首个子节点保留为隐藏模板。</summary>
    [SerializeField] private Button _optionTemplate;
    /// <summary>剧情选项使用浅色底图，商店服务使用按钮模板的蓝色底图。</summary>
    [SerializeField] private Sprite _storyOptionBackground;
    /// <summary>与主界面隔离显示的常驻 HUD 和聊天层。</summary>
    [SerializeField] private CanvasGroup[] _hudLayers;
    /// <summary>以 NPC 配置编号为键的服务资源，交给控制器选取当前货架。</summary>
    [SerializeField] private NpcDialogueCatalog _catalog;
    /// <summary>主控制器从正式窗口引用读取服务配置。</summary>
    public NpcDialogueCatalog Catalog => _catalog;
    /// <summary>窗口当前正在显示的逐句对白。</summary>
    private string[] _lines;
    /// <summary>当前对白的零基序号。</summary>
    private int _lineIndex;

    /// <summary>玩家选择一个具体货架，请求商店控制器展示对应商品。</summary>
    public event Action<NpcDialogueCatalog.Service> ShopRequested;
    /// <summary>玩家选择帮助魔神塔剧情 NPC。</summary>
    public event Action MoshentaBattleRequested;
    /// <summary>选择离开、点完最后一句或按 Esc 时请求关闭。</summary>
    public event Action TalkLeaveRequested;
    /// <summary>窗口经任何入口隐藏时通知控制器归还世界镜头。</summary>
    public event Action Closed;

    /// <summary>通过主视图的框架边界接收一次真实 NPC 对话并重建本次选项。</summary>
    /// <param name="obj">项目 WindowBase 规定的参数，窗口使用具名的 NPC 展示数据。</param>
    public override void ReFreshUI(object obj)
    {
        // 当前说话者的强类型数据来自主控制器。
        if (!(obj is NpcTalkViewData dialogue)) return;
        _talkTitle.text = dialogue.Name;
        _lines = dialogue.Lines;
        _lineIndex = 0;
        _talkContent.text = string.Empty;
        if (_lines.Length > 0) _talkContent.text = _lines[0];

        // 逆序移除上次的实例，索引零保留给正式模板。
        for (int index = _options.content.childCount - 1; index >= 1; index--)
        {
            // 清掉上一位说话者的按钮，模板始终保留在首位。
            GameObject previous = _options.content.GetChild(index).gameObject;
            previous.SetActive(false);
            Destroy(previous);
        }
        // 按配置序号决定服务的显示顺序。
        for (int index = 0; index < dialogue.Services.Length; index++)
        {
            // 点击时需携带对应选项的商店与商品清单。
            NpcDialogueCatalog.Service service = dialogue.Services[index];
            AddOption(service.Label, () => ShopRequested?.Invoke(service));
        }
        if (dialogue.ShowMoshentaBossChoices)
        {
            AddOption("帮助他（进入战斗）", () => MoshentaBattleRequested?.Invoke(), _storyOptionBackground);
            AddOption("暂时离开", OnTalkLeaveClick, _storyOptionBackground);
        }
        _options.gameObject.SetActive(dialogue.Services.Length > 0 || dialogue.ShowMoshentaBossChoices);
        ScrollToFirstOption();
    }

    /// <summary>框架先在隐藏状态填充对白，打开窗口、激活全部按钮后再定位列表。</summary>
    /// <param name="isShow">显示对话时需让首个服务进入可视区。</param>
    /// <param name="obj">窗口基类的刷新参数，传入对白时由 ReFreshUI 完成列表定位。</param>
    public override void Show(bool isShow = true, object obj = null)
    {
        base.Show(isShow, obj);
        if (isShow && obj == null) ScrollToFirstOption();
    }

    /// <summary>窗口可见后按实际选项数排版，让超出可视区的列表从第一个服务开始显示。</summary>
    private void ScrollToFirstOption()
    {
        if (!_options.isActiveAndEnabled) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_options.content);
        _options.verticalNormalizedPosition = 1f;
    }

    /// <summary>从实装按钮模板生成一行，将点击绑定到本次选项。</summary>
    /// <param name="label">货架名称或剧情选项的正文。</param>
    /// <param name="onClick">控制器应接收的单次选择。</param>
    /// <param name="background">剧情选项的底图，留空时使用服务按钮的预制体样式。</param>
    private void AddOption(string label, UnityAction onClick, Sprite background = null)
    {
        // 新行继承服务模板的排版，剧情选项替换底图。
        Button row = Instantiate(_optionTemplate, _options.content, false);
        if (background != null) row.image.sprite = background;
        row.GetComponentInChildren<TMP_Text>(true).text = label;
        row.onClick.AddListener(onClick);
        row.gameObject.SetActive(true);
    }

    /// <summary>点击底部对白区域或翻页箭头时进入下一句，末句结束本次对话。</summary>
    public void OnAdvanceClick()
    {
        if (_lines != null && _lineIndex + 1 < _lines.Length)
        {
            _talkContent.text = _lines[++_lineIndex];
            return;
        }
        OnTalkLeaveClick();
    }

    /// <summary>关闭按钮和剧情暂离选项沿主控制器离开对话。</summary>
    public void OnTalkLeaveClick() => TalkLeaveRequested?.Invoke();

    /// <summary>进入人物特写时隐藏世界 HUD，保留各子窗口自己的显隐状态。</summary>
    private void OnEnable() => SetHudVisible(false);

    /// <summary>外部关窗、切场景和正常退出都归还 HUD 和镜头。</summary>
    private void OnDisable()
    {
        SetHudVisible(true);
        Closed?.Invoke();
    }

    /// <summary>仅切换预制体内为对话设置的两组透明度与鼠标穿透。</summary>
    /// <param name="visible">是否恢复常驻世界和聊天界面。</param>
    private void SetHudVisible(bool visible)
    {
        // 两个专用透明组使用相同的显隐值，画布内部状态继续更新。
        float alpha = 0f;
        if (visible) alpha = 1f;
        for (int index = 0; index < _hudLayers.Length; index++)
        {
            // 同层原本关闭的子窗口不会因为恢复透明度而被打开。
            CanvasGroup layer = _hudLayers[index];
            if (layer == null) continue;
            layer.alpha = alpha;
            layer.blocksRaycasts = visible;
            layer.interactable = visible;
        }
    }

    /// <summary>PC 对话接受 Esc 退出，鼠标可操作全部对白与服务。</summary>
    private void Update()
    {
        // 当前键盘可能在设备热拔插后为空。
        Keyboard keys = Keyboard.current;
        if (keys != null && keys.escapeKey.wasPressedThisFrame) OnTalkLeaveClick();
    }
}
