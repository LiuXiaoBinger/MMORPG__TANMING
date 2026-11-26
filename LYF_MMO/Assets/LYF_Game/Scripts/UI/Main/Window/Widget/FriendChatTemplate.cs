using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO FriendChatTemplate，按 who_type 左右排列文字或可播放的语音气泡。</summary>
public sealed class FriendChatTemplate : MonoBehaviour
{
    /// <summary>本行在纵向会话列表中占用的高度。</summary>
    [SerializeField] private LayoutElement layout;
    /// <summary>RO 的九切气泡背景，用同一张原图镜像另一端。</summary>
    [SerializeField] private Image bubble;
    /// <summary>正文或语音时长，宽度跟随实际字数。</summary>
    [SerializeField] private Text body;
    /// <summary>当前行的本地日期和时间。</summary>
    [SerializeField] private Text time;
    /// <summary>仅语音气泡接受点击。</summary>
    [SerializeField] private Button play;
    /// <summary>与表情选择格共用原图的消息图片区。</summary>
    [SerializeField] private Image expression;
    /// <summary>当前复用行的语音引用。</summary>
    private AudioChat voice;
    /// <summary>共享的项目语音播放器入口。</summary>
    private Action<AudioChat> playAudio;
    /// <summary>每个复用行只注册一次播放动作。</summary>
    private void Awake() { play.onClick.AddListener(OnPlay); }
    /// <summary>播放当前行的真实录音。</summary>
    private void OnPlay() { if (voice != null) playAudio?.Invoke(voice); }
    /// <summary>以消息真实长度安排气泡并显示服务端时间。</summary><param name="row">服务器消息及当前接收者视角。</param><param name="onPlay">语音点击回调。</param>
    /// <param name="emojis">当前窗基础表情包。</param><param name="speaker">群聊行的真实发件人昵称。</param>
    internal void Bind(PrivateDetailChatInfo row, Action<AudioChat> onPlay, EmojiPackageTemplate emojis, string speaker = "")
    {
        voice = row.Audio; playAudio = onPlay; play.enabled = voice != null;
        // 图像来自预制体的正式表情格，普通正文不会误识别为图片。
        Sprite icon = emojis.GetChatExpression(row.Content);
        expression.gameObject.SetActive(icon != null); expression.sprite = icon;
        body.gameObject.SetActive(icon == null);
        if (voice == null) body.text = row.Content;
        else body.text = "语音  " + Mathf.CeilToInt(voice.Duration) + " 秒  ▶";
        time.text = DateTimeOffset.FromUnixTimeSeconds((long)row.ChatTime).ToLocalTime().ToString("MM-dd  HH:mm");
        if (speaker.Length > 0) time.text = speaker + "  ·  " + time.text;
        // 气泡内容限宽后按字体实际换行高度增长。
        float width = Mathf.Clamp(body.preferredWidth + 40, 108, 406);
        if (icon != null) width = 112;
        body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 40);
        // 正文完成换行后的可见气泡高度。
        float height = Mathf.Max(49, Mathf.Ceil(body.preferredHeight) + 20);
        if (icon != null) height = 78;
        // 行矩形两侧提供相同边距，自己靠右，对方靠左。
        float side = 0;
        if (row.WhoType == 1) side = 1;
        // 本人消息锚定行右端，对方锚定行左端。
        Vector2 anchor = new Vector2(side, 1);
        // 这条消息的背景矩形。
        RectTransform box = bubble.rectTransform;
        box.anchorMin = anchor; box.anchorMax = anchor;
        // 气泡尖角与滚动边缘间留出的画布单位。
        float inset = 10;
        if (side == 1) inset = -10;
        box.anchoredPosition = new Vector2(inset, -23);
        box.sizeDelta = new Vector2(width, height);
        body.rectTransform.sizeDelta = new Vector2(width - 40, height - 12);
        // 背景朝向说话者，子元素做逆向缩放以保持正文和图片正向。
        float facing = 1;
        if (side == 1) facing = -1;
        box.pivot = new Vector2(0, 1);
        box.localScale = new Vector3(facing, 1, 1);
        body.rectTransform.localScale = new Vector3(facing, 1, 1);
        expression.rectTransform.localScale = body.rectTransform.localScale;
        if (side == 1) bubble.color = new Color32(190, 214, 201, 255);
        else bubble.color = Color.white;
        time.alignment = TextAnchor.MiddleLeft;
        if (side == 1) time.alignment = TextAnchor.MiddleRight;
        layout.preferredHeight = height + 28;
    }
}
