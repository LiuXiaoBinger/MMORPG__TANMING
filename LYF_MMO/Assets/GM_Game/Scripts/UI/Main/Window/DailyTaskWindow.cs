using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 日常任务窗口，绑定 DailyTask.prefab（取自 RO 原版日常面板美术）。
/// 只负责窗口呈现与关闭：不接日常任务数据、不驱动列表、不改任何业务状态。
/// </summary>
public sealed class DailyTaskWindow : WindowBase
{
    /// <summary>关闭按钮，绑定预制体 CloseBtn，点击隐藏本窗口。</summary>
    [SerializeField] private Button _closeButton;

    /// <summary>注册关闭按钮点击事件，重复初始化时先移除再添加，避免监听叠加。</summary>
    public override void InitWindow()
    {
        if (_closeButton == null)
        {
            return;
        }
        _closeButton.onClick.RemoveListener(OnCloseClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>关闭按钮回调，只隐藏窗口，不改变日常任务状态。</summary>
    private void OnCloseClicked()
    {
        CloseWindow();
    }
}
