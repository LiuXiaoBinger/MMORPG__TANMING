using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>江湖聊天的一个可选频道，将预制体标签与既有聊天记录频道关联。</summary>
public sealed class YimengChatChannelTab : MonoBehaviour
{
    /// <summary>玩家点击此按钮切换频道。</summary>
    [SerializeField] private Button _button;
    /// <summary>选中时显示的蓝色底纹；发言菜单中的按钮可沿用常驻底纹。</summary>
    [SerializeField] private Image _selection;
    /// <summary>呈现录屏里的频道标题并随选中状态变色。</summary>
    [SerializeField] private TMP_Text _caption;
    /// <summary>发言区不同于左栏标题时使用的频道名，例如互联世界。</summary>
    [SerializeField] private string _postingCaption;
    /// <summary>与已有历史记录对应的频道编号。</summary>
    [SerializeField] private ChatChannel _channel;
    /// <summary>本频道有对应的既有聊天记录来源。</summary>
    [SerializeField] private bool _hasHistory;
    /// <summary>常用频道合并显示既有频道的记录。</summary>
    [SerializeField] private bool _showAllHistory;
    /// <summary>项目已有此频道的文字和语音发言能力。</summary>
    [SerializeField] private bool _canPost;
    /// <summary>系统和招募频道用整列显示内容，底部没有发言区。</summary>
    [SerializeField] private bool _readOnly;
    /// <summary>本频道使用招募工具栏。</summary>
    [SerializeField] private bool _recruit;
    /// <summary>本频道显示录屏里的组队或帮派语音栏。</summary>
    [SerializeField] private bool _groupVoice;

    /// <summary>向所属窗口提供频道切换按钮。</summary>
    public Button Button => _button;
    /// <summary>向所属窗口提供既有频道值。</summary>
    public ChatChannel Channel => _channel;
    /// <summary>输入状态使用的可见频道名。</summary>
    public string Caption
    {
        get
        {
            if (string.IsNullOrEmpty(_postingCaption)) return _caption.text;
            return _postingCaption;
        }
    }
    /// <summary>玩家是否可用项目已有协议在此频道发言。</summary>
    public bool CanPost => _canPost;
    /// <summary>是否收起底部输入区域。</summary>
    public bool ReadOnly => _readOnly;
    /// <summary>是否使用招募专用工具栏。</summary>
    public bool Recruit => _recruit;
    /// <summary>是否显示组队或帮派的语音栏。</summary>
    public bool GroupVoice => _groupVoice;
    /// <summary>是否为聚合记录的常用频道。</summary>
    public bool ShowAllHistory => _showAllHistory;

    /// <summary>切换频道选中底纹，正文侧栏用白字标记当前频道。</summary>
    /// <param name="selected">当前窗口是否正展示这个频道。</param>
    public void SetSelected(bool selected)
    {
        _selection.enabled = selected;
        if (selected)
            _caption.color = new Color32(249, 252, 255, 255);
        else
            _caption.color = new Color32(34, 54, 76, 255);
    }

    /// <summary>确定该频道是否展示一条既有聊天记录。</summary>
    /// <param name="channel">记录的实际所属频道。</param>
    public bool Includes(ChatChannel channel)
    {
        return _showAllHistory || (_hasHistory && _channel == channel);
    }
}
