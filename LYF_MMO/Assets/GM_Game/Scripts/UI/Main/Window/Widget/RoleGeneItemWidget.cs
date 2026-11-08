using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>角色窗口中的基因行，只显示名称、层数、事件/次数与服务器寿命。</summary>
public sealed class RoleGeneItemWidget : MonoBehaviour
{
    /// <summary>正式行模板中的名称与层数文本。</summary>
    [SerializeField] private TMP_Text _nameText;
    /// <summary>正式行模板中的类型、事件和次数文本。</summary>
    [SerializeField] private TMP_Text _detailsText;
    /// <summary>正式行模板中的剩余时间文本。</summary>
    [SerializeField] private TMP_Text _timeText;
    /// <summary>当前显示的实际节点引用，隐藏或角色替换时释放。</summary>
    private GeneBase node;
    /// <summary>已经显示的秒数，避免同一秒内反复写入文本。</summary>
    private long displayedSeconds = long.MinValue;
    /// <summary>只有有限寿命行需要窗口按间隔刷新时间。</summary>
    public bool HasCountdown { get { return node.RemainingMilliseconds >= 0; } }

    /// <summary>列表数据变化时更新一次名称、层数和详情。</summary>
    /// <param name="data">控制器从当前实际节点生成的强类型展示输入。</param>
    public void Bind(RoleGeneViewData data)
    {
        node = data.Node;
        // 参考配置术语仅在文字展示边界转换，保留节点名称、配置和业务身份原值。
        string displayName = Regex.Replace(data.Name, "buff", "基因", RegexOptions.IgnoreCase);
        // 层数放在名称前，长名称省略时仍能看到实际层数。
        _nameText.text = "×" + node.StackCount + "  " + displayName;
        _detailsText.text = data.Details;
        displayedSeconds = long.MinValue;
        RefreshTime();
    }

    /// <summary>只刷新整数倒计时，不重建列表，不触碰角色属性或节点生命周期。</summary>
    public void RefreshTime()
    {
        if (node == null) return;
        // 显示向上取整的秒数，零只表示等待权威移除，不提前撤销效果。
        long seconds = -1;
        if (node.RemainingMilliseconds >= 0) seconds = (long)Math.Ceiling(node.RemainingMilliseconds / 1000.0);
        if (displayedSeconds == seconds) return;
        displayedSeconds = seconds;
        if (seconds < 0) _timeText.text = "永久";
        else if (seconds == 0) _timeText.text = "0 秒";
        else if (seconds < 60) _timeText.text = seconds + " 秒";
        else _timeText.text = (seconds / 60) + "分" + (seconds % 60).ToString("00") + "秒";
    }

    /// <summary>窗口隐藏时解除旧角色节点引用，实例留作下次同窗口显示复用。</summary>
    public void Clear() { node = null; }
}
