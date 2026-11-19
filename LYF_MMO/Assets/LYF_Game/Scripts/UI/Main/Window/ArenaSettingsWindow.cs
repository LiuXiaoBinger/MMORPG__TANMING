using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 竞技设置窗口，仅展示攻击模式与保护模式的外观选项，不修改任何战斗规则、保护状态或业务数据。
/// </summary>
public sealed class ArenaSettingsWindow : WindowBase
{
    /// <summary>右上角的关闭按钮，点击后只隐藏本窗口。</summary>
    [SerializeField] private Button _closeButton;

    /// <summary>窗口打开时置于主界面最上层，避免被动态创建的聊天窗口遮住。</summary>
    private void OnEnable()
    {
        transform.SetAsLastSibling();
    }

    /// <summary>注册关闭按钮监听，重复初始化时先解绑避免重复订阅。</summary>
    public override void InitWindow()
    {
        if (_closeButton == null)
        {
            return;
        }
        _closeButton.onClick.RemoveListener(CloseWindow);
        _closeButton.onClick.AddListener(CloseWindow);
    }
}
