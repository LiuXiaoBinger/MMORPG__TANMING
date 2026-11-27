using TMPro;
using UnityEngine;

/// <summary>战斗快捷栏的快慢跑按钮，显示已保存的快捷键并把点击交给本地输入控制器。</summary>
public sealed class RunModeButton : MonoBehaviour
{
    /// <summary>按钮左上角的键位文字，由战斗快捷栏预制体引用。</summary>
    [SerializeField] private TMP_Text _keyLabel;

    /// <summary>按钮显示时读取当前键位，并跟随设置页后续保存的改键。</summary>
    private void OnEnable()
    {
        RefreshKeyLabel();
        GameHotkeys.Changed += RefreshKeyLabel;
    }

    /// <summary>按钮隐藏或销毁时结束本次显示期间的改键订阅。</summary>
    private void OnDisable()
    {
        GameHotkeys.Changed -= RefreshKeyLabel;
    }

    /// <summary>在按钮上显示快慢跑实际使用的键帽名称。</summary>
    private void RefreshKeyLabel()
    {
        _keyLabel.SetText(GameHotkeys.Label(GameHotkeys.RunMode.Bound));
    }

    /// <summary>接收按钮的单次点击，从角色当前的快慢跑模式切换到另一档。</summary>
    public void OnClick()
    {
        if (PlayerInputCtr.Instance != null) PlayerInputCtr.Instance.ToggleRunMode();
    }
}
