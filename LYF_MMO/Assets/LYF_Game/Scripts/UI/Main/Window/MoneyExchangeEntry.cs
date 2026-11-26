using UnityEngine;

/// <summary>
/// 背包货币栏「+」角标入口。点击后直接找到 MainView 上的兑换窗口并打开指定页签，
/// 监听以「持久监听」形式序列化在预制体里，不依赖 MainCtrl / MainView.InitView /
/// KnapsackCtrl 的初始化顺序，也不会因为域重载或脚本编译中的时序丢失。
/// 元宝是充值货币，元宝格不挂本组件。
/// </summary>
public sealed class MoneyExchangeEntry : MonoBehaviour
{
    /// <summary>0 兑铜币、1 兑银两、2 兑银票。</summary>
    [SerializeField] private int _page;

    /// <summary>由预制体上的 Button.onClick 持久监听调用。</summary>
    public void OnClick()
    {
        // 从背包加号所在的主城界面获取窗口。
        MainView view = FindObjectOfType<MainView>(true);
        if (view == null)
        {
            return;
        }
        // 该主视图预制体持有的兑换窗口。
        CurrencyExchangeWindow window = view.CurrencyExchangeWindow;
        if (window == null)
        {
            return;
        }
        window.Open(Mathf.Clamp(_page, 0, 2));
    }
}
