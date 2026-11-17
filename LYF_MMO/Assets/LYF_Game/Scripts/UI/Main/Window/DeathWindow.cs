using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>死亡后的正式提示窗口，沿用RO死亡标题及返回记录点的单按钮交互。</summary>
public class DeathWindow : WindowBase
{
    /// <summary>返回副本出生点复活的按钮，引用保存在正式预制体。</summary>
    [SerializeField] private Button reviveButton;
    /// <summary>窗口只转发点击，由控制器请求服务端复活。</summary>
    public event Action ReviveRequested;

    /// <summary>由MainView初始化一次按钮监听。</summary>
    public override void InitWindow()
    {
        reviveButton.onClick.AddListener(OnReviveClicked);
    }

    /// <summary>点击不修改生命与坐标，等待服务端确认。</summary>
    private void OnReviveClicked()
    {
        ReviveRequested?.Invoke();
    }
}
