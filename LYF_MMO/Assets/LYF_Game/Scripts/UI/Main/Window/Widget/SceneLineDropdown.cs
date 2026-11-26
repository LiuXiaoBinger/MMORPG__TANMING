using System;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>打开小地图线路菜单前请求本区最新目录，在答复到达后沿用 TMP 的原生选项和键盘导航。</summary>
public sealed class SceneLineDropdown : TMP_Dropdown
{
    /// <summary>玩家点击或用键盘展开菜单时通知所属窗口刷新目录。</summary>
    public event Action LinesRequested;
    /// <summary>本控件可见期间是否有一次尚未展示的展开操作。</summary>
    private bool openRequested;

    /// <summary>鼠标或触屏点击先查询真实线路。</summary>
    /// <param name="eventData">来自 Unity 输入模块的指针事件。</param>
    public override void OnPointerClick(PointerEventData eventData) { RequestLines(); }

    /// <summary>键盘及手柄确认也通过同一个目录入口。</summary>
    /// <param name="eventData">当前选中控件的提交事件。</param>
    public override void OnSubmit(BaseEventData eventData) { RequestLines(); }

    /// <summary>仅在菜单可交互且收起时发送查询。</summary>
    private void RequestLines()
    {
        if (!IsActive() || !IsInteractable() || IsExpanded) return;
        openRequested = true;
        LinesRequested?.Invoke();
    }

    /// <summary>窗口使用最新回包填充选项后，只响应玩家主动发起的展开。</summary>
    public void ShowRequestedLines()
    {
        if (!openRequested) return;
        openRequested = false;
        Show();
    }

    /// <summary>取消展开时同时取消仍在途的展示意图。</summary>
    /// <param name="eventData">输入模块发出的取消事件。</param>
    public override void OnCancel(BaseEventData eventData)
    {
        openRequested = false;
        base.OnCancel(eventData);
    }

    /// <summary>窗口关闭或进副本时清除本次展开意图。</summary>
    protected override void OnDisable()
    {
        openRequested = false;
        base.OnDisable();
    }
}
