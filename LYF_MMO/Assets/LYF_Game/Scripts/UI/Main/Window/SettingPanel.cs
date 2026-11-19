using UnityEngine;
using UnityEngine.UI;

/// <summary>控制设置窗口的画幅、八个页签和画质页的展示状态。</summary>
public sealed class SettingPanel : WindowBase
{
    /// <summary>保留八页原始布局的窗体，按主界面可用宽高缩放到全屏。</summary>
    [SerializeField] private RectTransform _frame;

    /// <summary>右侧页签列表，可在基础、镜头、画质、偏好、热键、观战、状态和跳字之间滚动。</summary>
    [SerializeField] private ScrollRect _tabScroll;

    /// <summary>按右侧页签顺序排列的八个页面根节点。</summary>
    [SerializeField] private GameObject[] _pages;

    /// <summary>各页签选中时显示的浅色底板与深色字图标，与页面下标一一对应。</summary>
    [SerializeField] private GameObject[] _tabOn;

    /// <summary>各页签未选中时显示的浅色文字与图标，与页面下标一一对应。</summary>
    [SerializeField] private GameObject[] _tabOff;

    /// <summary>选择自定义画质时显示的细项滚动区域。</summary>
    [SerializeField] private GameObject _customGraphics;

    /// <summary>选择预设画质时提示如何打开细项的说明。</summary>
    [SerializeField] private GameObject _graphicsHint;

    /// <summary>打开时置于聊天等主界面窗口上方，回到基础页和页签列表顶部。</summary>
    private void OnEnable()
    {
        transform.SetAsLastSibling();
        OnRectTransformDimensionsChange();
        ScrollTabs(1);
        SelectOneHandler(0);
    }

    /// <summary>窗口可用面积变化后，让原始窗体的四边与主界面可用区域重合。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_frame == null)
        {
            return;
        }

        // 根节点随主界面拉伸，宽高以 Canvas 单位表示。
        Rect available = ((RectTransform)transform).rect;
        // 窗体原始宽高以 Canvas 单位表示，内部控件保留预制体中的布局。
        Vector2 frameSize = _frame.rect.size;
        _frame.localScale = new Vector3(available.width / frameSize.x, available.height / frameSize.y, 1f);
        _frame.anchoredPosition = Vector2.zero;
    }

    /// <summary>切换内容与对应页签外观，同时仅显示一个主页面。</summary>
    /// <param name="index">预制体传入的页签下标，从 0 基础到 7 跳字。</param>
    public void SelectOneHandler(int index)
    {
        // 使用同一个下标依次更新每一页内容及其亮、暗两种页签外观。
        for (int pageIndex = 0; pageIndex < _pages.Length; pageIndex++)
        {
            _pages[pageIndex].SetActive(pageIndex == index);
            _tabOn[pageIndex].SetActive(pageIndex == index);
            _tabOff[pageIndex].SetActive(pageIndex != index);
        }
    }

    /// <summary>停止当前页签滑动，并把列表定位到指定端。</summary>
    /// <param name="position">0 为底部，1 为顶部，由右侧箭头传入。</param>
    public void ScrollTabs(int position)
    {
        _tabScroll.StopMovement();
        _tabScroll.verticalNormalizedPosition = position;
    }

    /// <summary>切换画质页的细项面板和预设提示，仅更新窗口显示。</summary>
    /// <param name="isCustom">自定义档位选中时为 true，其他档位选中时为 false。</param>
    public void ShowCustomGraphics(bool isCustom)
    {
        _customGraphics.SetActive(isCustom);
        _graphicsHint.SetActive(!isCustom);
    }
}
