using UnityEngine;
using UnityEngine.UI;

/// <summary>控制设置窗口的画幅、八个页签和画质页的展示状态。</summary>
public sealed class SettingPanel : WindowBase
{
    /// <summary>按 1920×1080 主界面画布设计的窗体，等比缩放后仍保持居中。</summary>
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

    /// <summary>每次打开窗口时回到基础页和页签列表顶部。</summary>
    private void OnEnable()
    {
        OnRectTransformDimensionsChange();
        ScrollTabs(1);
        SelectOneHandler(0);
    }

    /// <summary>窗口可用面积变化后，按短边比例缩放，保证标题、导航和关闭按钮都留在画面内。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_frame == null)
        {
            return;
        }

        // 根节点随主界面拉伸，宽高以 Canvas 单位表示。
        Rect available = ((RectTransform)transform).rect;
        // 以 1920×1080 为原始画幅，选取可完整容纳窗体的缩放比例。
        float scale = Mathf.Min(available.width / 1920f, available.height / 1080f);
        _frame.localScale = Vector3.one * scale;
        _frame.anchoredPosition = new Vector2(0f, 18f * scale);
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
