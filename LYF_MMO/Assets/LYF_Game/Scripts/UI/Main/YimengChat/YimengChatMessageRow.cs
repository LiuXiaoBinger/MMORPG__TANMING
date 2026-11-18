using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>用头像、频道标签和气泡展示一条既有聊天消息，系统消息使用传音图标样式。</summary>
public sealed class YimengChatMessageRow : MonoBehaviour
{
    /// <summary>正常宽度下的头像边长，过窄的行会按行宽缩小头像。</summary>
    private const float AvatarSize = 127f;
    /// <summary>气泡顶部距离行顶部的 Canvas 像素数，给频道和昵称留出空间。</summary>
    private const float BubbleTop = 48f;
    /// <summary>气泡左右正文留空之和，和预制体文字矩形的左右偏移一致。</summary>
    private const float TextPadding = 23f;

    /// <summary>由列表布局读取的行高。</summary>
    [SerializeField] private LayoutElement _layout;
    /// <summary>玩家消息的头像和正文容器。</summary>
    [SerializeField] private GameObject _player;
    /// <summary>系统消息的图标和正文容器。</summary>
    [SerializeField] private GameObject _system;
    /// <summary>头像容器，时间徽标随该容器移到消息作者对应的一侧。</summary>
    [SerializeField] private RectTransform _avatar;
    /// <summary>昵称左侧的频道名，使用频道对应颜色。</summary>
    [SerializeField] private TMP_Text _channel;
    /// <summary>由既有聊天记录提供的发言者昵称。</summary>
    [SerializeField] private TMP_Text _sender;
    /// <summary>头像下方用实际消息时间显示的小徽标。</summary>
    [SerializeField] private TMP_Text _sentAt;
    /// <summary>气泡内容边界，可按文本或图片实际宽度缩短。</summary>
    [SerializeField] private RectTransform _bubble;
    /// <summary>单独的气泡底图，翻转尾部时不翻转文字、按钮或表情。</summary>
    [SerializeField] private Image _bubbleImage;
    /// <summary>普通文字或已有的语音转写。</summary>
    [SerializeField] private TMP_Text _message;
    /// <summary>普通聊天正文完全匹配内置表情标记时显示的图集区域。</summary>
    [SerializeField] private RawImage _emoji;
    /// <summary>系统消息通栏的正文。</summary>
    [SerializeField] private TMP_Text _systemMessage;
    /// <summary>旧语音播放控件，显示录音时长并提供点击回放。</summary>
    [SerializeField] private ChatVoiceItem _voice;
    /// <summary>上次参与排版的消息行宽度，避免高度刷新时重复测量文字。</summary>
    private float _layoutWidth;
    /// <summary>当前实例绑定到旧聊天记录里的哪一条消息。</summary>
    public ChatMessage Message { get; private set; }

    /// <summary>绑定真实消息并按当前行宽排版。</summary>
    /// <param name="message">旧聊天记录提供的完整消息对象。</param>
    /// <param name="emoji">由完整正文找到的表情格，普通文字消息传空。</param>
    /// <param name="playVoice">经聊天控制器转到现有语音管理器的回放请求。</param>
    public void Bind(ChatMessage message, YimengChatEmojiTile emoji, Action<AudioChat> playVoice)
    {
        Message = message;
        // 只有系统频道用传音图标行。
        bool isSystem = message.Channel == ChatChannel.System;
        _player.SetActive(!isSystem);
        _system.SetActive(isSystem);
        if (isSystem)
        {
            _systemMessage.text = message.Text;
            UpdateLayout(((RectTransform)transform).rect.width);
            return;
        }

        _channel.text = "[" + ChannelCaption(message.Channel) + "]";
        _channel.color = ChannelColor(message.Channel);
        _sender.text = message.Sender;
        _sentAt.text = message.SentAt.ToString("HH:mm");

        // 有实际录音引用时显示原语音播放控件。
        bool hasVoice = message.HasVoice && message.Audio != null;
        _voice.gameObject.SetActive(hasVoice);
        if (hasVoice) _voice.Bind(message.Audio, playVoice);
        // 语音保留转写正文；普通表情复用选择面板的图集区域。
        bool hasSticker = !hasVoice && emoji != null;
        _emoji.gameObject.SetActive(hasSticker);
        if (hasSticker) emoji.CopyIconTo(_emoji);
        _message.gameObject.SetActive(!hasSticker && !string.IsNullOrEmpty(message.Text));
        _message.text = message.Text;
        if (message.IsLocalPlayer)
            _bubbleImage.color = new Color32(45, 89, 93, 205);
        else
            _bubbleImage.color = new Color32(12, 18, 19, 132);
        UpdateLayout(((RectTransform)transform).rect.width);
    }

    /// <summary>分辨率或列表布局改变宽度时，按实际行宽重新计算换行。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (Message == null) return;
        // 只对宽度变化重新排版，LayoutElement 调整高度时无需重复计算。
        float width = ((RectTransform)transform).rect.width;
        if (!Mathf.Approximately(width, _layoutWidth)) UpdateLayout(width);
    }

    /// <summary>使用一套布局对齐两侧玩家消息，系统正文仍使用原通栏样式。</summary>
    /// <param name="width">列表布局分配给本行的实际宽度，单位为 Canvas 像素。</param>
    private void UpdateLayout(float width)
    {
        _layoutWidth = width;
        if (Message.Channel == ChatChannel.System)
        {
            _layout.preferredHeight = Mathf.Max(54, _systemMessage.GetPreferredValues(Message.Text, Mathf.Max(1, width - 105), Mathf.Infinity).y + 18);
            return;
        }

        // 0 为行左边缘，1 为行右边缘，负方向表示从右边向内留距。
        float edge = 0f;
        float direction = 1f;
        if (Message.IsLocalPlayer)
        {
            edge = 1f;
            direction = -1f;
        }
        // 保留足够的正文宽度，窄画面下同时缩小头像与时间徽标。
        float portrait = Mathf.Min(AvatarSize, width * .26f);
        // 正文从对应一侧向内跨过头像列和头像间距。
        float bodyOffset = portrait + 34f;
        SetTopBox(_avatar, edge, direction * 15f, 2f, portrait, portrait);
        LayoutHeader(width, edge, direction, bodyOffset);

        // 坐标按照行真实宽度，扣除头像列、气泡内边距和另一侧外边距。
        float maxTextWidth = Mathf.Max(1f, width - bodyOffset - TextPadding - 12f);
        // 气泡可用内宽由正文、表情或语音按钮中最大的一项确定。
        float innerWidth = 1f;
        // 实际在本行绘制的内置贴图尺寸，不占用普通正文的空间。
        float stickerSize = Mathf.Min(72f, maxTextWidth);
        // 这些状态在绑定真实消息时设置，行宽变化只重排可见节点。
        bool hasText = _message.gameObject.activeSelf;
        bool hasVoice = _voice.gameObject.activeSelf;
        bool hasSticker = _emoji.gameObject.activeSelf;
        if (hasText)
            innerWidth = Mathf.Min(maxTextWidth, _message.GetPreferredValues(Message.Text, maxTextWidth, Mathf.Infinity).x + 1f);
        if (hasSticker) innerWidth = stickerSize;
        if (hasVoice) innerWidth = Mathf.Max(innerWidth, Mathf.Min(165f, maxTextWidth));

        // 按最终气泡内宽再次测量高度，避免中英文实际换行后尾行未计入。
        float bodyContentHeight = 0f;
        if (hasText)
            bodyContentHeight = Mathf.Ceil(_message.GetPreferredValues(Message.Text, innerWidth, Mathf.Infinity).y) + 1f;
        _message.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyContentHeight);
        if (hasSticker)
        {
            bodyContentHeight = stickerSize;
            SetTopBox(_emoji.rectTransform, edge, direction * 12f, 7f, stickerSize, stickerSize);
        }

        // 文字与贴图上下留距各 7 像素，语音按钮在正文下方独占一行。
        float bodyHeight = bodyContentHeight + 14f;
        if (hasVoice)
        {
            bodyHeight += 45f;
            SetTopBox((RectTransform)_voice.transform, edge, direction * 8f, bodyHeight - 47f, Mathf.Min(166f, innerWidth + 7f), 42f);
        }
        SetTopBox(_bubble, edge, direction * bodyOffset, BubbleTop, innerWidth + TextPadding, bodyHeight);
        _bubbleImage.rectTransform.localScale = new Vector3(direction, 1, 1);
        _layout.preferredHeight = Mathf.Max(portrait + 10f, BubbleTop + bodyHeight + 5f);
    }

    /// <summary>让频道和昵称按阅读顺序相邻，在作者对应一侧整体对齐。</summary>
    /// <param name="width">玩家消息行的宽度，单位为 Canvas 像素。</param>
    /// <param name="edge">消息锚定的横向边缘，0 为左，1 为右。</param>
    /// <param name="direction">从锚点向行内偏移的符号，左为 1，右为 -1。</param>
    /// <param name="bodyOffset">消息区域相对作者一侧的头像列留距。</param>
    private void LayoutHeader(float width, float edge, float direction, float bodyOffset)
    {
        // 标签和昵称一行合用的最大宽度，给对侧留出安全边距。
        float headerWidth = Mathf.Max(1f, width - bodyOffset - 19f);
        // 先给固定频道标签分配宽度，再让昵称按剩余宽度省略。
        float channelWidth = Mathf.Min(headerWidth, _channel.GetPreferredValues().x + 6f);
        float senderWidth = Mathf.Min(headerWidth - channelWidth, _sender.GetPreferredValues().x + 1f);
        // 摆放坐标随方向切换，文字的书写顺序始终保持频道在昵称之前。
        float channelOffset = bodyOffset + 7f;
        float senderOffset = bodyOffset + 7f + channelWidth;
        if (Message.IsLocalPlayer)
        {
            senderOffset = bodyOffset + 7f;
            channelOffset = senderOffset + senderWidth;
        }
        SetTopBox(_channel.rectTransform, edge, direction * channelOffset, 4f, channelWidth, 42f);
        SetTopBox(_sender.rectTransform, edge, direction * senderOffset, 4f, senderWidth, 42f);
    }

    /// <summary>把已序列化的节点从父区域的一侧顶部定位到指定矩形。</summary>
    /// <param name="rect">要摆放的现有头像、文字、气泡等矩形。</param>
    /// <param name="edge">横向锚点，0 为左边缘，1 为右边缘。</param>
    /// <param name="horizontal">自横向锚点起算的偏移，单位为 Canvas 像素。</param>
    /// <param name="top">自父区域顶边向下的距离，单位为 Canvas 像素。</param>
    /// <param name="width">控件在 Canvas 上的实际宽度。</param>
    /// <param name="height">控件在 Canvas 上的实际高度。</param>
    private static void SetTopBox(RectTransform rect, float edge, float horizontal, float top, float width, float height)
    {
        // 锚点与枢轴保持在同一侧顶部，调整宽度时边缘位置稳定。
        Vector2 anchor = new Vector2(edge, 1);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = new Vector2(horizontal, -top);
        rect.sizeDelta = new Vector2(width, height);
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
