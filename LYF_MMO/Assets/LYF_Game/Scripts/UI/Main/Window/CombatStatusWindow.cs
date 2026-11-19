using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>主界面的角色状态列表和增益管理面板。</summary>
public sealed class CombatStatusWindow : WindowBase
{
    /// <summary>永久状态页签。</summary>
    [SerializeField] private Button _permanentTab;
    /// <summary>限时状态页签。</summary>
    [SerializeField] private Button _timedTab;
    /// <summary>展开或收起增益管理。</summary>
    [SerializeField] private Button _manageButton;
    /// <summary>关闭状态窗。</summary>
    [SerializeField] private Button _closeButton;
    /// <summary>永久状态页内容。</summary>
    [SerializeField] private GameObject _permanentPage;
    /// <summary>限时状态页内容。</summary>
    [SerializeField] private GameObject _timedPage;
    /// <summary>右侧增益栏位面板。</summary>
    [SerializeField] private GameObject _management;
    /// <summary>限时状态列表中已有权威数值的护盾条目。</summary>
    [SerializeField] private GameObject _shieldRow;
    /// <summary>限时列表无生效状态时的占位文案。</summary>
    [SerializeField] private GameObject _timedEmpty;
    /// <summary>护盾当前值和满值，单位为吸收伤害点数。</summary>
    [SerializeField] private TMP_Text _shieldAmount;
    /// <summary>状态列表滚动区域，切换页签时回到顶部。</summary>
    [SerializeField] private ScrollRect _scroll;

    /// <summary>绑定常驻预制体上四个按钮，关闭窗口后监听仍有效。</summary>
    public override void InitWindow()
    {
        _permanentTab.onClick.RemoveListener(ShowPermanent);
        _permanentTab.onClick.AddListener(ShowPermanent);
        _timedTab.onClick.RemoveListener(ShowTimed);
        _timedTab.onClick.AddListener(ShowTimed);
        _manageButton.onClick.RemoveListener(ToggleManagement);
        _manageButton.onClick.AddListener(ToggleManagement);
        _closeButton.onClick.RemoveListener(CloseWindow);
        _closeButton.onClick.AddListener(CloseWindow);
    }

    /// <summary>打开时将面板置顶，并优先显示正在生效的护盾。</summary>
    private void OnEnable()
    {
        transform.SetAsLastSibling();
        _management.SetActive(false);
        if (_shieldRow.activeSelf)
        {
            ShowTimed();
        }
        else
        {
            ShowPermanent();
        }
    }

    /// <summary>刷新限时页的护盾显示，归零时移除条目。</summary>
    /// <param name="remaining">当前剩余的护盾吸收点数。</param>
    /// <param name="capacity">护盾生效时的最大吸收点数。</param>
    public void SetShield(int remaining, int capacity)
    {
        // 只有仍能吸收伤害的护盾才出现在限时状态列表里。
        bool active = remaining > 0 && capacity > 0;
        _shieldRow.SetActive(active);
        _timedEmpty.SetActive(!active);
        if (active)
        {
            _shieldAmount.text = $"护盾  {remaining:N0} / {capacity:N0}";
        }
    }

    /// <summary>切换到永久状态列表。</summary>
    private void ShowPermanent()
    {
        ShowPage(false);
    }

    /// <summary>切换到限时状态列表。</summary>
    private void ShowTimed()
    {
        ShowPage(true);
    }

    /// <summary>只显示当前页，并以按钮的选中外观标识当前分页。</summary>
    /// <param name="timed">为真显示限时状态，否则显示永久状态。</param>
    private void ShowPage(bool timed)
    {
        _permanentPage.SetActive(!timed);
        _timedPage.SetActive(timed);
        _permanentTab.interactable = timed;
        _timedTab.interactable = !timed;
        _scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>切换增益管理面板的显隐。</summary>
    private void ToggleManagement()
    {
        _management.SetActive(!_management.activeSelf);
    }
}
