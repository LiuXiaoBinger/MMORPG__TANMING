using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO EmailItemTemplate，绑定左栏的一封邮件和选中效果。</summary>
public sealed class EmailItemTemplate : MonoBehaviour
{
    /// <summary>整行点击热区。</summary>
    [SerializeField] private Button button;
    /// <summary>当前信封的标题。</summary>
    [SerializeField] private Text title;
    /// <summary>列表中当前发件人的头像，与详情使用同一张图。</summary>
    [SerializeField] private Image senderIcon;
    /// <summary>标题下方显示的来信日期。</summary>
    [SerializeField] private Text date;
    /// <summary>与来信日期并列显示的到期日。</summary>
    [SerializeField] private Text expires;
    /// <summary>未领取附件对应的关闭宝箱。</summary>
    [SerializeField] private GameObject giftClosed;
    /// <summary>已领取附件对应的打开宝箱。</summary>
    [SerializeField] private GameObject giftOpen;
    /// <summary>已读邮件叠加 RO 半透明遮罩。</summary>
    [SerializeField] private GameObject readMask;
    /// <summary>当前选中的行底图。</summary>
    [SerializeField] private GameObject selected;
    /// <summary>未读重要信件的角标。</summary>
    [SerializeField] private GameObject important;
    /// <summary>当前绑定的邮件编号。</summary>
    private long uid;
    /// <summary>行点击后通知交互控制器。</summary>
    private Action<long> onClick;

    /// <summary>实例化的行只绑定一次点击，刷新时读取当前 UID。</summary>
    private void Awake() { button.onClick.AddListener(Select); }

    /// <summary>以最新摘要和选中状态刷新行。</summary>
    /// <param name="mail">本行对应的信封。</param><param name="active">右侧是否显示该信。</param>
    /// <param name="clicked">点击信封后的回调。</param>
    /// <param name="portrait">当前邮件模板对应的 RO 发件人图标。</param>
    public void SetData(MailBaseInfo mail, bool active, Action<long> clicked, Sprite portrait)
    {
        uid = mail.MailUid;
        onClick = clicked;
        title.text = mail.Title;
        SetSenderIcon(senderIcon, portrait);
        // 发送和到期按玩家本地日历显示，跨自然日按 RO 列表切换为天数。
        DateTimeOffset now = DateTimeOffset.Now;
        // 发送日与当前本地日期相差的天数。
        int daysSince = (now.Date - DateTimeOffset.FromUnixTimeSeconds(mail.CreateTime).LocalDateTime.Date).Days;
        date.text = "今天";
        if (daysSince > 0) date.text = daysSince + "天前";
        // 使用服务器给出的到期秒戳，临近失效时显示剩余小时。
        TimeSpan remaining = DateTimeOffset.FromUnixTimeSeconds(mail.ExpireTime) - now;
        expires.text = "即将到期";
        if (remaining.Days >= 1) expires.text = remaining.Days + "天后到期";
        else if (remaining.TotalHours >= 1) expires.text = (int)remaining.TotalHours + "小时后到期";
        giftClosed.SetActive(mail.IsHasItem && !mail.IsRead);
        giftOpen.SetActive(mail.IsHasItem && mail.IsRead);
        readMask.SetActive(mail.IsRead);
        selected.SetActive(active);
        important.SetActive(mail.Important && !mail.IsRead);
    }

    /// <summary>将发件人图像居中放入头像底板，小尺寸邮件图标使用切片原宽。</summary>
    /// <param name="target">列表或抬头的头像显示控件。</param>
    /// <param name="portrait">该邮件模板使用的头像或默认邮件切片。</param>
    internal static void SetSenderIcon(Image target, Sprite portrait)
    {
        target.sprite = portrait;
        // 留出底板的五像素内边距，卡普拉头像缩入格内，普通信封按原分辨率显示。
        float available = ((RectTransform)target.transform.parent).rect.width - 10f;
        target.rectTransform.sizeDelta = Vector2.one * Mathf.Min(available, portrait.rect.width);
    }

    /// <summary>报告当前行绑定的邮件 UID。</summary>
    private void Select() { onClick?.Invoke(uid); }
}
