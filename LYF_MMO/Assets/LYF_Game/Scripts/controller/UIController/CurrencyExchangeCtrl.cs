using System;

/// <summary>
/// 货币兑换控制器：读取四种虚拟货币余额交给窗口，并在点击「兑换」时
/// 通过 KnapsackCtrl 原子地完成「扣支付货币 + 发产出货币」。
/// 不直接持有物品组件，避免与背包控制器的角色绑定流程重复。
/// 元宝是充值货币，本控制器只把它当支付来源，不提供任何反向产出元宝的兑换。
/// </summary>
public sealed class CurrencyExchangeCtrl : IDisposable
{
    /// <summary>主界面视图，用于定位兑换窗口。</summary>
    private readonly MainView _mainView;
    /// <summary>背包控制器，提供余额读取与虚拟货币增减。</summary>
    private readonly KnapsackCtrl _knapsackCtrl;
    /// <summary>兑换窗口视图，只负责展示与事件派发。</summary>
    private readonly CurrencyExchangeWindow _window;

    /// <summary>创建控制器并订阅窗口事件。</summary>
    /// <param name="mainView">持有兑换窗口引用的主界面。</param>
    /// <param name="knapsackCtrl">已创建好的背包控制器，必须早于本控制器构造。</param>
    public CurrencyExchangeCtrl(MainView mainView, KnapsackCtrl knapsackCtrl)
    {
        _mainView = mainView;
        _knapsackCtrl = knapsackCtrl;
        if (mainView != null) _window = mainView.CurrencyExchangeWindow;
        if (_window != null)
        {
            _window.BalanceRequested += OnBalanceRequested;
            _window.ExchangeRequested += OnExchangeRequested;
        }
    }

    /// <summary>打开指定页签；走窗口自身的幂等 Open，避免 ShowMainWindow 的 toggle 语义把窗口又关掉。</summary>
    /// <param name="page">0 兑铜币、1 兑银两、2 兑银票。</param>
    public void Open(int page)
    {
        if (_window == null)
        {
            return;
        }
        if (page < 0 || page > 2) page = 0;
        _window.Open(page);
    }

    /// <summary>重新读取四种余额并刷新窗口，不改动窗口显隐。</summary>
    /// <param name="page">刷新后保持选中的页签。</param>
    public void Refresh(int page)
    {
        if (_window == null || _knapsackCtrl == null)
        {
            return;
        }
        // 本次刷新使用的四种余额和页签。
        CurrencyExchangeViewData data = new CurrencyExchangeViewData();
        if (page < 0 || page > 2) page = 0;
        data.Page = page;
        _knapsackCtrl.ReadMoney(out data.Copper, out data.Silver,
            out data.Ticket, out data.Yuanbao);
        _window.ReFreshUI(data);
    }

    /// <summary>窗口切页或首次打开时请求余额，这里补一次完整数据。</summary>
    /// <param name="page">要刷新的零基页签。</param>
    private void OnBalanceRequested(int page)
    {
        Refresh(page);
    }

    /// <summary>执行兑换：校验页签与方式 → 扣来源货币 → 发产出货币 → 刷新两个窗口的余额。</summary>
    /// <param name="page">目标币种页签。</param><param name="method">支付方式索引。</param><param name="amount">本次支付份数。</param>
    private void OnExchangeRequested(int page, int method, int amount)
    {
        if (_window == null || _knapsackCtrl == null)
        {
            return;
        }
        if (page < 0 || page > 2) return;
        if (method < 0 || method > 1 || amount <= 0)
        {
            return;
        }
        // 本兑换方式消耗的货币索引。
        int source = CurrencyExchangeRule.SourceCurrency[page, method];
        if (source < 0)
        {
            return;
        }
        // 每份支付货币可兑换的产出数量。
        long rate = CurrencyExchangeRule.Rate[page, method];
        // 本页签的产出货币索引。
        int target = CurrencyExchangeRule.TargetCurrency[page];
        if (rate <= 0L || target == 3)
        {
            // 兜底：任何规则都不允许把元宝作为产出。
            return;
        }

        // 支付指定份数可获得的总数量。

        long gain = rate * amount;
        if (gain <= 0L || gain > int.MaxValue)
        {
            _window.ShowHint("单次兑换数量过大");
            return;
        }

        // 背包中用于扣款的物品配置编号。

        int fromId = CurrencyExchangeRule.ItemIdOf(source);
        // 背包中用于入账的物品配置编号。
        int toId = CurrencyExchangeRule.ItemIdOf(target);
        if (!_knapsackCtrl.ApplyCurrencyExchange(fromId, amount, toId, gain))
        {
            _window.ShowHint(CurrencyExchangeRule.CurrencyName[source] + "余额不足或兑换失败");
            return;
        }
        Refresh(page);
    }

    /// <summary>注销窗口事件引用。</summary>
    public void Dispose()
    {
        if (_window != null)
        {
            _window.BalanceRequested -= OnBalanceRequested;
            _window.ExchangeRequested -= OnExchangeRequested;
        }
    }
}
