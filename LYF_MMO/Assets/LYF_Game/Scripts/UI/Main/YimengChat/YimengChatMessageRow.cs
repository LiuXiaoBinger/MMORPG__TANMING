using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>用头像、频道标签和气泡展示一条既有聊天消息，系统消息使用传音图标样式。</summary>
public sealed class YimengChatMessageRow : MonoBehaviour
{
    /// <summary>用于限制文本折行宽度的头像、边距与头像右间距总和，单位为 Canvas 像素。</summary>
    private const float AvatarColumns = 161f;
    /// <summary>包含头像和时间徽标的行的最小布局高度，单位为 Canvas 像素。</summary>
    private const float MinPlayerHeight = 124f;

    /// <summary>由列表布局读取的行高。</summary>
    [SerializeField] private LayoutElement _layout;
    /// <summary>玩家消息的头像和正文容器。</summary>
    [SerializeField] private GameObject _player;
    /// <summary>系统消息的图标和正文容器。</summary>
    [SerializeField] private GameObject _system;
    /// <summary>昵称左侧的频道名，使用频道对应颜色。</summary>
    [SerializeField] private TMP_Text _channel;
    /// <summary>由既有聊天记录提供的发言者昵称。</summary>
    [SerializeField] private TMP_Text _sender;
    /// <summary>头像下方用实际消息时间显示的小徽标。</summary>
    [SerializeField] private TMP_Text _sentAt;
    /// <summary>气泡和其边界，可按文本实际宽度缩短。</summary>
    [SerializeField] private Image _bubble;
    /// <summary>普通文字或已有的语音转写。</summary>
    [SerializeField] private TMP_Text _message;
    /// <summary>系统消息通栏的正文。</summary>
    [SerializeField] private TMP_Text _systemMessage;
    /// <summary>旧语音播放控件，显示录音时长并提供点击回放。</summary>
    [SerializeField] private ChatVoiceItem _voice;
    /// <summary>当前实例绑定到旧聊天记录里的哪一条消息。</summary>
    public ChatMessage Message { get; private set; }

    /// <summary>按当前列表宽度绑定真实消息并测量折行后的高度。</summary>
    /// <param name="message">旧聊天记录提供的完整消息对象。</param>
    /// <param name="width">窗口当前消息视口宽度，单位为 Canvas 像素。</param>
    /// <param name="playVoice">经聊天控制器转到现有语音管理器的回放请求。</param>
    public void Bind(ChatMessage message, float width, Action<AudioChat> playVoice)
    {
        Message = message;
        // 只有系统频道用传音图标行。
        bool isSystem = message.Channel == ChatChannel.System;
        _player.SetActive(!isSystem);
        _system.SetActive(isSystem);
        if (isSystem)
        {
            _systemMessage.text = message.Text;
            _layout.preferredHeight = Mathf.Max(54, _systemMessage.GetPreferredValues(message.Text, Mathf.Max(1, width - 105), Mathf.Infinity).y + 18);
            return;
        }

        _channel.text = "[" + ChannelCaption(message.Channel) + "]";
        _channel.color = ChannelColor(message.Channel);
        // 标签和昵称各自采用纯文本，按实际字号相邻放置。
        float channelWidth = _channel.GetPreferredValues().x + 6f;
        _channel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, channelWidth);
        _sender.rectTransform.offsetMin = new Vector2(AvatarColumns + 7 + channelWidth, _sender.rectTransform.offsetMin.y);
        _sender.text = message.Sender;
        _sentAt.text = message.SentAt.ToString("HH:mm");

        // 声音在正文为空时独占气泡；已有转写时追加到正文下方。
        bool hasVoice = message.HasVoice && message.Audio != null;
        _voice.gameObject.SetActive(hasVoice);
        // 源正文为空时折叠文字区。
        bool hasText = !string.IsNullOrEmpty(message.Text);
        _message.gameObject.SetActive(hasText);
        _message.text = message.Text;
        // 气泡留出头像列和两侧内边距，保证长句能换行。
        float available = Mathf.Max(1, width - AvatarColumns - 37);
        // TMP 计算指定宽度下的准确文本高度。
        Vector2 textSize = Vector2.zero;
        if (hasText) textSize = _message.GetPreferredValues(message.Text, available, Mathf.Infinity);
        // 气泡的可见宽度取正文或语音按钮较宽者。
        float bodyWidth = textSize.x + 23f;
        // 语音控件位于正文结束后的下一行。
        float bodyHeight = textSize.y + 14f;
        if (hasVoice)
        {
            bodyWidth = Mathf.Max(188, bodyWidth);
            bodyHeight += 45f;
            _voice.Bind(message.Audio, playVoice);
        }
        _bubble.rectTransform.sizeDelta = new Vector2(Mathf.Min(available + 23, bodyWidth), bodyHeight);
        _message.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textSize.y);
        if (message.IsLocalPlayer)
            _bubble.color = new Color32(45, 89, 93, 205);
        else
            _bubble.color = new Color32(12, 18, 19, 132);
        _layout.preferredHeight = Mathf.Max(MinPlayerHeight, bodyHeight + 53f);
    }

    /// <summary>给既有项目频道使用江湖界面显示的名称。</summary>
    /// <param name="channel">既有记录使用的频道值。</param>
    public static string ChannelCaption(ChatChannel channel)
    {
        switch (channel)
        {
            case ChatChannel.Nearby: return "当前";
            case ChatChannel.Team: return "队伍";
            case ChatChannel.Guild: return "帮派";
            case ChatChannel.Private: return "私聊";
            case ChatChannel.Watch: return "观战";
            case ChatChannel.Profession: return "门派";
            case ChatChannel.System: return "系统";
            default: return "世界";
        }
    }

    /// <summary>给频道标签和当前发言目标选择统一的可读颜色。</summary>
    /// <param name="channel">要上色的既有聊天频道。</param>
    public static Color32 ChannelColor(ChatChannel channel)
    {
        switch (channel)
        {
            case ChatChannel.Nearby: return new Color32(177, 212, 221, 255);
            case ChatChannel.Team: return new Color32(111, 217, 163, 255);
            case ChatChannel.Guild: return new Color32(212, 164, 224, 255);
            case ChatChannel.Profession: return new Color32(214, 177, 145, 255);
            case ChatChannel.System: return new Color32(228, 194, 123, 255);
            default: return new Color32(166, 217, 123, 255);
        }
    }
}
