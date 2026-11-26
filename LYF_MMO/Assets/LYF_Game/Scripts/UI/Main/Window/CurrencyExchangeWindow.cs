using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 货币兑换的静态规则表。页签只有「兑铜币 / 兑银两 / 兑银票」三种，
/// 元宝是充值货币，只能作为支付来源，不作为可兑换产出的目标，因此没有「兑元宝」页签。
/// 货币索引全局统一：0 铜币(101)、1 银子/银两(102)、2 银票(106)、3 元宝(103)。
/// </summary>
public static class CurrencyExchangeRule
{
    /// <summary>铜币虚拟物品配置编号。</summary>
    public const int CopperItemId = 101;
    /// <summary>银子（银两）虚拟物品配置编号。</summary>
    public const int SilverItemId = 102;
    /// <summary>银票虚拟物品配置编号。</summary>
    public const int TicketItemId = 106;
    /// <summary>元宝（充值货币）虚拟物品配置编号。</summary>
    public const int YuanbaoItemId = 103;

    /// <summary>三个页签的标题后缀。</summary>
    public static readonly string[] PageName = { "铜币", "银两", "银票" };

    /// <summary>四种货币在界面上的名称，索引与货币索引一致。</summary>
    public static readonly string[] CurrencyName = { "铜币", "银两", "银票", "元宝" };

    /// <summary>每个页签兑换产出的货币索引。</summary>
    public static readonly int[] TargetCurrency = { 0, 1, 2 };

    /// <summary>每个页签可选的支付货币索引；第二项为 -1 表示该页签只有一种支付方式。</summary>
    public static readonly int[,] SourceCurrency = { { 3, 1 }, { 3, 2 }, { 3, -1 } };

    /// <summary>每 1 个支付货币可兑换的产出数量，与 SourceCurrency 同下标。</summary>
    public static readonly long[,] Rate = { { 10000L, 100L }, { 100L, 1L }, { 100L, 0L } };

    /// <summary>左侧「兑换比率」说明，按页签索引。</summary>
    public static readonly string[] RatioText =
    {
        "1元宝 = 10000铜币\n1银两 = 100铜币",
        "1元宝 = 100银两\n1银票 = 1银两",
        "1元宝 = 100银票"
    };

    /// <summary>左侧「获取途径」说明，按页签索引。</summary>
    public static readonly string[] SourceText =
    {
        "福利领取\n每日活跃度奖励\n参与部分日常玩法等",
        "摆摊、集市出售物品\n每日活跃度奖励\n参与部分日常玩法等",
        "使用元宝直接兑换\n使用四象图概率掉落"
    };

    /// <summary>左侧「主要用途」说明，按页签索引；支持 TMP 富文本。</summary>
    public static readonly string[] UsageText =
    {
        "",
        "主要用于 <color=#54E8A6>摆摊交易</color>\n可以购买玩家摆摊出售的物品",
        "主要用于 <color=#54E8A6>集市交易</color>\n可以购买玩家出售的珍贵物品\n交易后卖方得到等价值的银两"
    };

    /// <summary>货币索引转虚拟物品配置编号。</summary>
    /// <param name="currencyIndex">铜、银、票、元宝依次为零至三。</param>
    public static int ItemIdOf(int currencyIndex)
    {
        switch (currencyIndex)
        {
            case 0: return CopperItemId;
            case 1: return SilverItemId;
            case 2: return TicketItemId;
            case 3: return YuanbaoItemId;
            default: return -1;
        }
    }
}

/// <summary>
/// 兑换窗口渲染所需的余额快照 + 打开时要选中的页签。
/// 由 CurrencyExchangeCtrl 从背包虚拟物品组件读取后交给窗口，窗口自身不碰物品数据。
/// </summary>
public sealed class CurrencyExchangeViewData
{
    /// <summary>打开时选中的页签：0 兑铜币、1 兑银两、2 兑银票。</summary>
    public int Page;
    /// <summary>铜币余额。</summary>
    public long Copper;
    /// <summary>银子余额。</summary>
    public long Silver;
    /// <summary>银票余额。</summary>
    public long Ticket;
    /// <summary>元宝余额。</summary>
    public long Yuanbao;

    /// <summary>按货币索引读取余额。</summary>
    /// <param name="currencyIndex">余额快照中的币种索引。</param>
    public long Get(int currencyIndex)
    {
        switch (currencyIndex)
        {
            case 0: return Copper;
            case 1: return Silver;
            case 2: return Ticket;
            case 3: return Yuanbao;
            default: return 0L;
        }
    }
}

/// <summary>
/// 货币兑换窗口：三个页签（兑铜币 / 兑银两 / 兑银票），左侧显示比率与获取途径，
/// 右侧选择支付方式、数量并预览获得量。窗口只做展示与事件派发，实际扣减由 CurrencyExchangeCtrl 完成。
/// 元宝为充值货币，不提供「兑元宝」页签，也不允许用其它货币反向兑换元宝。
/// </summary>
public class CurrencyExchangeWindow : WindowBase
{
    #region Inspector 字段
    [Header("页签（0兑铜币 1兑银两 2兑银票）")]
    /// <summary>三个产出币种页签的点击按钮。</summary>
    [SerializeField] private Button[] _pageTabs = new Button[3];
    /// <summary>选中页签的高亮节点。</summary>
    [SerializeField] private GameObject[] _pageTabOn = new GameObject[3];
    /// <summary>未选中页签的底图节点。</summary>
    [SerializeField] private GameObject[] _pageTabOff = new GameObject[3];

    [Header("标题与关闭")]
    /// <summary>当前兑换币种的窗口标题。</summary>
    [SerializeField] private TMP_Text _titleText;
    /// <summary>关闭兑换窗口的按钮。</summary>
    [SerializeField] private Button _closeButton;

    [Header("左侧说明")]
    /// <summary>当前页签的兑换比率说明。</summary>
    [SerializeField] private TMP_Text _ratioText;
    /// <summary>支付货币的获取途径说明。</summary>
    [SerializeField] private TMP_Text _sourceText;
    /// <summary>目标货币的用途说明。</summary>
    [SerializeField] private TMP_Text _usageText;

    [Header("兑换方式")]
    /// <summary>可切换支付方式时展示的按钮行。</summary>
    [SerializeField] private GameObject _methodRow;
    /// <summary>按配置顺序切换支付币种的按钮。</summary>
    [SerializeField] private Button[] _methodButtons = new Button[2];
    /// <summary>当前支付方式的勾选标记。</summary>
    [SerializeField] private GameObject[] _methodChecks = new GameObject[2];
    /// <summary>各支付方式使用的币种名。</summary>
    [SerializeField] private TMP_Text[] _methodLabels = new TMP_Text[2];

    [Header("右侧数值区")]
    /// <summary>余额旁显示的支付币种图标。</summary>
    [SerializeField] private Image _sourceIcon;
    /// <summary>当前支付货币的可用余额。</summary>
    [SerializeField] private TMP_Text _sourceBalance;
    /// <summary>数量输入区域的支付币种图标。</summary>
    [SerializeField] private Image _amountIcon;
    /// <summary>当前选取的兑换份数。</summary>
    [SerializeField] private TMP_Text _amountText;
    /// <summary>本次支付份数减一。</summary>
    [SerializeField] private Button _minusButton;
    /// <summary>本次支付份数加一。</summary>
    [SerializeField] private Button _plusButton;
    /// <summary>将要获得的币种图标。</summary>
    [SerializeField] private Image _gainIcon;
    /// <summary>产出币种名称。</summary>
    [SerializeField] private TMP_Text _gainLabel;
    /// <summary>按支付份数预览的产出数量。</summary>
    [SerializeField] private TMP_Text _gainValue;
    /// <summary>提交当前兑换的按钮。</summary>
    [SerializeField] private Button _exchangeButton;
    /// <summary>显示余额不足或业务回执的标签。</summary>
    [SerializeField] private TMP_Text _hintText;

    [Header("四枚货币图标：0铜 1银 2票 3元宝")]
    /// <summary>按货币索引排列的四枚图标。</summary>
    [SerializeField] private Sprite[] _currencySprites = new Sprite[4];
    #endregion

    #region 事件与状态
    /// <summary>请求兑换；参数依次为页签、支付方式下标、份数。</summary>
    public event Action<int, int, int> ExchangeRequested;
    /// <summary>窗口需要余额快照；参数为当前页签，控制器据此重新读取四种余额。</summary>
    public event Action<int> BalanceRequested;

    /// <summary>当前打开窗口所用的余额快照。</summary>
    private readonly CurrencyExchangeViewData _data = new CurrencyExchangeViewData();
    /// <summary>当前产出币种页签索引。</summary>
    private int _page;
    /// <summary>当前页签内选择的支付方式。</summary>
    private int _method;
    /// <summary>本次兑换份数，初始为一份。</summary>
    private int _amount = 1;
    /// <summary>当前窗口已经绑定按钮监听。</summary>
    private bool _initialized;
    #endregion

    #region 生命周期
    /// <summary>绑定按钮监听；可在窗口未激活时由 MainView.InitView 提前调用，重复调用安全。</summary>
    public override void InitWindow()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        if (_minusButton != null) _minusButton.onClick.AddListener(OnMinusClicked);
        if (_plusButton != null) _plusButton.onClick.AddListener(OnPlusClicked);
        if (_exchangeButton != null) _exchangeButton.onClick.AddListener(OnExchangeClicked);

        if (_pageTabs != null)
        {
            for (int i = 0; i < _pageTabs.Length; i++)
            {
                // 当前准备绑定的页签按钮。
                Button tab = _pageTabs[i];
                if (tab == null) continue;
                // 将本按钮对应的页签保存在点击回调中。
                int page = i;
                tab.onClick.AddListener(delegate { OnPageClicked(page); });
            }
        }
        if (_methodButtons != null)
        {
            for (int i = 0; i < _methodButtons.Length; i++)
            {
                // 当前准备绑定的支付方式按钮。
                Button btn = _methodButtons[i];
                if (btn == null) continue;
                // 将本按钮对应的支付方式保存在点击回调中。
                int method = i;
                btn.onClick.AddListener(delegate { OnMethodClicked(method); });
            }
        }
    }

    /// <summary>注销本窗口添加的监听，清空外部事件引用。</summary>
    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        if (_minusButton != null) _minusButton.onClick.RemoveListener(OnMinusClicked);
        if (_plusButton != null) _plusButton.onClick.RemoveListener(OnPlusClicked);
        if (_exchangeButton != null) _exchangeButton.onClick.RemoveListener(OnExchangeClicked);
        ExchangeRequested = null;
        BalanceRequested = null;
    }

    /// <summary>
    /// 幂等打开窗口并切到指定页签。刻意不走 UIBase.ShowMainWindow 的「再点一次就关闭」语义，
    /// 也不依赖 windowDic 是否已初始化，保证货币栏加号在任何时序下都能把窗口点出来。
    /// </summary>
    /// <param name="page">0 兑铜币、1 兑银两、2 兑银票。</param>
    public void Open(int page)
    {
        InitWindow();
        transform.SetAsLastSibling();   // 并入主画布，靠兄弟顺序压在同层 UI 之上
        gameObject.SetActive(true);
        ReFreshUI(Mathf.Clamp(page, 0, 2));
    }

    /// <summary>
    /// 刷新数据。obj 可以是 CurrencyExchangeViewData（完整余额）或 int（仅指定页签，随后请求余额）；
    /// 传 null 时沿用上次数据重绘。
    /// </summary>
    /// <param name="obj">WindowBase 规定的框架入口，接收余额快照或页签。</param>
    public override void ReFreshUI(object obj)
    {
        InitWindow();
        // 从基类刷新入口取得强类型余额。
        CurrencyExchangeViewData data = obj as CurrencyExchangeViewData;
        if (data != null)
        {
            _data.Page = data.Page;
            _data.Copper = data.Copper;
            _data.Silver = data.Silver;
            _data.Ticket = data.Ticket;
            _data.Yuanbao = data.Yuanbao;
            _page = Mathf.Clamp(data.Page, 0, 2);
            _method = 0;
            _amount = 1;
            Render();
            return;
        }
        if (obj is int)
        {
            _page = Mathf.Clamp((int)obj, 0, 2);
            _method = 0;
            _amount = 1;
            Render();
            if (BalanceRequested != null)
            {
                BalanceRequested.Invoke(_page);
            }
            return;
        }
        Render();
    }
    #endregion

    #region 按钮回调
    /// <summary>关闭当前兑换窗口。</summary>
    private void OnCloseClicked()
    {
        CloseWindow();
    }

    /// <summary>切换产出币种并重新请求余额。</summary><param name="page">零基页签。</param>
    private void OnPageClicked(int page)
    {
        if (page == _page)
        {
            return;
        }
        _page = Mathf.Clamp(page, 0, 2);
        _method = 0;
        _amount = 1;
        Render();
        if (BalanceRequested != null)
        {
            BalanceRequested.Invoke(_page);
        }
    }

    /// <summary>切换支付币种并重置份数。</summary><param name="method">本页支付方式索引。</param>
    private void OnMethodClicked(int method)
    {
        if (CurrencyExchangeRule.SourceCurrency[_page, method] < 0)
        {
            return;
        }
        _method = method;
        _amount = 1;
        Render();
    }

    /// <summary>将支付份数减少至最小一份。</summary>
    private void OnMinusClicked()
    {
        if (_amount > 1)
        {
            _amount--;
            Render();
        }
    }

    /// <summary>在余额和单次上限内增加一份支付。</summary>
    private void OnPlusClicked()
    {
        // 余额与单次份数共同决定的上限。
        long max = MaxAmount();
        if (_amount < max)
        {
            _amount++;
            Render();
        }
    }

    /// <summary>按当前页签和份数向控制器提交兑换。</summary>
    private void OnExchangeClicked()
    {
        // 本次支付所用币种的索引。
        int source = CurrentSource();
        // 当前支付币种可用的背包余额。
        long balance = _data.Get(source);
        if (balance < _amount)
        {
            SetHint(CurrencyExchangeRule.CurrencyName[source] + "余额不足");
            return;
        }
        SetHint("");
        if (ExchangeRequested != null)
        {
            ExchangeRequested.Invoke(_page, _method, _amount);
        }
    }
    #endregion

    #region 渲染
    /// <summary>取得当前页签的支付货币索引。</summary>
    private int CurrentSource()
    {
        // 本次支付所用币种的索引。
        int source = CurrencyExchangeRule.SourceCurrency[_page, _method];
        if (source < 0) return 3;
        return source;
    }

    /// <summary>取得余额允许的兑换份数，单次最多 9999 份。</summary>
    private long MaxAmount()
    {
        // 当前支付币种可用的背包余额。
        long balance = _data.Get(CurrentSource());
        if (balance <= 0L)
        {
            return 1L;
        }
        if (balance > 9999L) return 9999L;
        return balance;
    }

    /// <summary>设置窗口内的结算提示。</summary><param name="text">展示文本，空串清除。</param>
    private void SetHint(string text)
    {
        if (_hintText != null)
        {
            _hintText.text = text;
        }
    }

    /// <summary>供控制器在兑换失败时写入提示文字，不改动其它展示数据。</summary>
    /// <param name="text">提示内容，传空串清除。</param>
    public void ShowHint(string text)
    {
        SetHint(text);
    }

    /// <summary>设置指定币种的展示图标。</summary><param name="target">接收图标的控件。</param><param name="currencyIndex">四种币种的索引。</param>
    private void SetSprite(Image target, int currencyIndex)
    {
        if (target == null || _currencySprites == null) return;
        if (currencyIndex < 0 || currencyIndex >= _currencySprites.Length)
        {
            return;
        }
        // 图集中对应当前币种的图标。
        Sprite sp = _currencySprites[currencyIndex];
        if (sp != null)
        {
            target.sprite = sp;
        }
    }

    /// <summary>按当前页签、支付方式、份数重绘整个窗口。</summary>
    private void Render()
    {
        // 本次绘制的页签。
        int page = Mathf.Clamp(_page, 0, 2);
        // 本次支付所用币种的索引。
        int source = CurrentSource();
        // 当前页签产出的货币索引。
        int target = CurrencyExchangeRule.TargetCurrency[page];
        // 单份支付可获得的数量。
        long rate = CurrencyExchangeRule.Rate[page, _method];
        // 当前支付币种可用的背包余额。
        long balance = _data.Get(source);

        if (_titleText != null) _titleText.text = "兑" + CurrencyExchangeRule.PageName[page];
        if (_ratioText != null) _ratioText.text = CurrencyExchangeRule.RatioText[page];
        if (_sourceText != null) _sourceText.text = CurrencyExchangeRule.SourceText[page];
        if (_usageText != null) _usageText.text = CurrencyExchangeRule.UsageText[page];

        // 页签选中态
        for (int i = 0; i < 3; i++)
        {
            // 是否显示此页签的高亮。
            bool on = i == page;
            if (_pageTabOn != null && i < _pageTabOn.Length && _pageTabOn[i] != null) _pageTabOn[i].SetActive(on);
            if (_pageTabOff != null && i < _pageTabOff.Length && _pageTabOff[i] != null) _pageTabOff[i].SetActive(!on);
        }

        // 兑换方式：兑银票只有元宝一种，整行隐藏
        // 本页是否提供第二种支付方式。
        bool hasSecond = CurrencyExchangeRule.SourceCurrency[page, 1] >= 0;
        if (_methodRow != null) _methodRow.SetActive(hasSecond);
        for (int i = 0; i < 2; i++)
        {
            // 当前方式所需的支付币种。
            int src = CurrencyExchangeRule.SourceCurrency[page, i];
            // 规则中配置了该支付币种时显示选项。
            bool visible = src >= 0;
            if (_methodButtons != null && i < _methodButtons.Length && _methodButtons[i] != null)
            {
                _methodButtons[i].gameObject.SetActive(visible);
            }
            if (_methodChecks != null && i < _methodChecks.Length && _methodChecks[i] != null)
            {
                _methodChecks[i].SetActive(visible && i == _method);
            }
            if (!visible) continue;
            if (_methodLabels != null && i < _methodLabels.Length && _methodLabels[i] != null)
            {
                _methodLabels[i].text = CurrencyExchangeRule.CurrencyName[src];
            }
        }

        SetSprite(_sourceIcon, source);
        SetSprite(_amountIcon, source);
        SetSprite(_gainIcon, target);

        if (_sourceBalance != null) _sourceBalance.text = balance.ToString();
        if (_amountText != null) _amountText.text = _amount.ToString();
        if (_gainLabel != null) _gainLabel.text = "获得" + CurrencyExchangeRule.CurrencyName[target];
        if (_gainValue != null) _gainValue.text = (rate * _amount).ToString();
        if (_exchangeButton != null) _exchangeButton.interactable = balance >= _amount;
        if (balance < _amount) SetHint(CurrencyExchangeRule.CurrencyName[source] + "余额不足");
        else SetHint("");
    }
    #endregion
}
