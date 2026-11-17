using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>副本附近操作的正式子窗口；控件和字体引用均保存在预制体。</summary>
public class DungeonFlowWindow : WindowBase
{
    /// <summary>附近门户或采集物的唯一操作按钮。</summary>
    [SerializeField] private Button interactButton;
    /// <summary>操作按钮标题，门户固定显示“传送”。</summary>
    [SerializeField] private Text interactTitle;
    /// <summary>按钮点击交给控制器发请求。</summary>
    public event Action InteractionRequested;

    /// <summary>主视图初始化时绑定控件点击。</summary>
    public override void InitWindow()
    {
        interactButton.onClick.AddListener(OnInteract);
        SetInteraction(null);
    }

    /// <summary>仅转发点击，不在窗口里决定任务或传送结果。</summary>
    private void OnInteract()
    {
        InteractionRequested?.Invoke();
    }

    /// <summary>附近没有交互时隐藏按钮，靠近时显示正式操作名称。</summary>
    /// <param name="title">操作名；空值表示隐藏。</param>
    public void SetInteraction(string title)
    {
        interactButton.gameObject.SetActive(!string.IsNullOrEmpty(title));
        interactTitle.text = title;
    }
}
