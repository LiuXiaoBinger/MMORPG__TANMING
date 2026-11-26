using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>保留 UGUI 的输入、输入法和提交事件，把好友及群聊编辑焦点传给现有角色输入控制器。</summary>
public sealed class ChatInputField : InputField
{
    /// <summary>编辑开始时暂停角色快捷键，离开输入框时恢复。</summary>
    internal event Action<bool> FocusChanged;
    /// <summary>记录这个控件是否已向主城报告过选中，窗口隐藏时也能正确释放。</summary>
    private bool editing;
    /// <summary>保留 UGUI 的键盘和移动端输入框后取得角色输入焦点。</summary><param name="eventData">Unity EventSystem 的选中事件。</param>
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        if (!IsInteractable() || editing) return;
        editing = true; FocusChanged?.Invoke(true);
    }
    /// <summary>Unity 结束输入后释放角色快捷键。</summary><param name="eventData">离开输入框的选中事件。</param>
    public override void OnDeselect(BaseEventData eventData) { base.OnDeselect(eventData); ReleaseFocus(); }
    /// <summary>在关闭窗口或切换会话视图时释放仍持有的编辑焦点。</summary>
    protected override void OnDisable() { base.OnDisable(); ReleaseFocus(); }
    /// <summary>仅为这个控件曾经持有的焦点发送一次恢复事件。</summary>
    private void ReleaseFocus() { if (editing) { editing = false; FocusChanged?.Invoke(false); } }
}
