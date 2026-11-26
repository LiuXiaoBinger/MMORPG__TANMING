using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO EmailPanel 的 UGUI 视图。所有正式控件和行模板由邮箱预制体持有。</summary>
public sealed class EmailPanel : WindowBase
{
    /// <summary>按参考分辨率排版的邮箱面板。</summary>
    [SerializeField] private RectTransform frame;
    /// <summary>飞鹰和好友共用的货币栏及侧栏。</summary>
    [SerializeField] internal SocialPanelHeader header;
    /// <summary>通过既有好友赠礼流程发送附物传书。</summary>
    [SerializeField] internal Button sendGift;
    /// <summary>录像中传书下方的既有集市入口。</summary>
    [SerializeField] internal Button market;
    /// <summary>关闭批量删除浮层的右上角按钮。</summary>
    [SerializeField] private Button closeConfirm;
    /// <summary>左栏 ScrollRect 内按顺序排列邮件行的父节点。</summary>
    [SerializeField] private RectTransform emailItemParent;
    /// <summary>左侧列表的隐藏行模板。</summary>
    [SerializeField] private EmailItemTemplate emailItemPrefab;
    /// <summary>右栏整封邮件的展示节点。</summary>
    [SerializeField] private GameObject detailObj;
    /// <summary>列表为空时显示 RO 邮箱空态图和提示。</summary>
    [SerializeField] private GameObject emptyState;
    /// <summary>有来信时展示左侧列表底板和滚动区域。</summary>
    [SerializeField] private GameObject listArea;
    /// <summary>详情抬头中的发件人图像。</summary>
    [SerializeField] private Image senderIcon;
    /// <summary>RO 社交面板已有的卡普拉职员立绘，用于其投递的掉落邮件。</summary>
    [SerializeField] private Sprite kafraIcon;
    /// <summary>未配置发件人头像的邮件使用 RO 邮件页签图标。</summary>
    [SerializeField] private Sprite mailIcon;
    /// <summary>右栏邮件标题。</summary>
    [SerializeField] private Text title;
    /// <summary>详情头像右侧显示的发件人署名。</summary>
    [SerializeField] private Text sender;
    /// <summary>署名下方的发送时间，使用玩家本地时区。</summary>
    [SerializeField] private Text sentTime;
    /// <summary>可滚动正文。</summary>
    [SerializeField] private Text content;
    /// <summary>详情底部的物品列表，无附件时为正文让出空间。</summary>
    [SerializeField] private GameObject attachmentPanel;
    /// <summary>附件领取后叠在物品区右侧的原版领取印章。</summary>
    [SerializeField] private GameObject claimedSeal;
    /// <summary>多件附件横向滚动，切换信件时回到首件。</summary>
    [SerializeField] private ScrollRect attachmentScroll;
    /// <summary>预制体中的隐藏附件卡片原型。</summary>
    [SerializeField] private EmailAttachmentItem attachmentPrefab;
    /// <summary>复用本窗口已经创建的附件卡片。</summary>
    private readonly List<EmailAttachmentItem> attachmentCards = new List<EmailAttachmentItem>();
    /// <summary>切换信件时将正文滚回顶部。</summary>
    [SerializeField] private ScrollRect bodyScroll;
    /// <summary>保留最近一次操作的失败原因。</summary>
    [SerializeField] private Text status;
    /// <summary>单封领取按钮。</summary>
    [SerializeField] internal Button getBtn;
    /// <summary>单封删除按钮。</summary>
    [SerializeField] internal Button delBtn;
    /// <summary>一键领取入口。</summary>
    [SerializeField] internal Button getAllBtn;
    /// <summary>一键删除已读邮件的入口。</summary>
    [SerializeField] internal Button delAllBtn;
    /// <summary>二次确认一键删除。</summary>
    [SerializeField] internal Button confirmBtn;
    /// <summary>取消删除并返回详情。</summary>
    [SerializeField] private Button cancelBtn;
    /// <summary>一键删除确认面板。</summary>
    [SerializeField] private GameObject confirmPanel;
    /// <summary>返回主界面的关闭按钮。</summary>
    [SerializeField] private Button closeBtn;
    /// <summary>已分配的行实例，后续刷新直接复用。</summary>
    private readonly List<EmailItemTemplate> rows = new List<EmailItemTemplate>();
    /// <summary>窗口变为可见时通知控制器取摘要。</summary>
    internal event Action Opened;

    /// <summary>把视图自身的关闭及确认交互绑定到预制体按钮。</summary>
    private void Awake()
    {
        closeBtn.onClick.AddListener(CloseWindow);
        delAllBtn.onClick.AddListener(ShowConfirmation);
        cancelBtn.onClick.AddListener(HideConfirmation);
        closeConfirm.onClick.AddListener(HideConfirmation);
        confirmBtn.onClick.AddListener(HideConfirmation);
    }

    /// <summary>进入收件箱时收起上次的确认面板并请求最新摘要。</summary>
    private void OnEnable()
    {
        OnRectTransformDimensionsChange();
        HideConfirmation();
        SetStatus("");
        Opened?.Invoke();
    }

    /// <summary>在小分辨率下等比缩放 RO 双栏布局。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (frame == null) return;
        // 当前主城功能层的可用 UI 尺寸。
        Rect area = ((RectTransform)transform).rect;
        // 小屏幕按窗口比例缩放，宽屏两侧仍由确认遮罩完整覆盖。
        float scale = Mathf.Min(area.width / frame.sizeDelta.x, area.height / frame.sizeDelta.y);
        frame.localScale = Vector3.one * scale;
    }

    /// <summary>显示批量删除确认，对应 RO 的 DelAll 提示。</summary>
    private void ShowConfirmation() { confirmPanel.SetActive(true); }
    /// <summary>收起批量操作确认。</summary>
    private void HideConfirmation() { confirmPanel.SetActive(false); }

    /// <summary>显示服务器操作结果，空串清除原提示。</summary><param name="message">当前提示文本。</param>
    internal void SetStatus(string message) { status.text = message; }

    /// <summary>刷新左侧行与批量操作的可用状态。</summary>
    /// <param name="mails">当前有序摘要。</param><param name="uid">当前选择的邮件编号。</param>
    /// <param name="clicked">点击某封信时通知 EmailHandler。</param>
    internal void ShowList(List<MailBaseInfo> mails, long uid, Action<long> clicked)
    {
        // 参照 RO 的行模板池，仅在已有行不够时实例化。
        while (rows.Count < mails.Count) rows.Add(Instantiate(emailItemPrefab, emailItemParent));
        // 本页存在未读时允许一键领取。
        bool unread = false;
        // 至少一封已读时允许批删。
        bool read = false;
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].gameObject.SetActive(i < mails.Count);
            if (i >= mails.Count) continue;
            rows[i].SetData(mails[i], mails[i].MailUid == uid, clicked, GetSenderIcon(mails[i].MailId));
            if (mails[i].IsRead) read = true;
            else unread = true;
        }
        // 一键服务作用于整个信箱，即使最新一百封都已读，仍可领取更早的未读信。
        getAllBtn.interactable = unread || mails.Count == MailCode.MaxClientMailCount;
        delAllBtn.interactable = read || mails.Count == MailCode.MaxClientMailCount;
        // 与 RO 空信箱相同，收起列表底板及两个批量按钮。
        listArea.SetActive(mails.Count > 0);
        getAllBtn.gameObject.SetActive(mails.Count > 0);
        delAllBtn.gameObject.SetActive(mails.Count > 0);
    }

    /// <summary>右栏显示当前邮件、附件或空态，正文较长时由原生滚动区域承载。</summary>
    /// <param name="mail">当前信封，空表示没有邮件。</param><param name="body">已缓存的正文，尚未返回时为空。</param>
    /// <param name="resetScroll">切换选中项时滚到顶部。</param>
    internal void ShowDetail(MailBaseInfo mail, MailContent body, bool resetScroll)
    {
        emptyState.SetActive(mail == null);
        detailObj.SetActive(mail != null);
        getBtn.gameObject.SetActive(mail != null && mail.IsHasItem && !mail.IsRead);
        delBtn.gameObject.SetActive(mail != null && mail.IsRead);
        if (mail == null) return;
        title.text = mail.Title;
        EmailItemTemplate.SetSenderIcon(senderIcon, GetSenderIcon(mail.MailId));
        // 正文到达后显示该信的真实署名。
        sender.text = body?.SendName ?? "";
        sentTime.text = DateTimeOffset.FromUnixTimeSeconds(mail.CreateTime).LocalDateTime.ToString("HH:mm   yyyy-MM-dd");
        content.text = "正在读取正文…";
        if (body != null) content.text = body.Content.Replace("\\n", "\n");
        ShowAttachments(body, mail.IsRead);
        if (resetScroll)
        {
            bodyScroll.verticalNormalizedPosition = 1f;
            attachmentScroll.horizontalNormalizedPosition = 0f;
        }
    }

    /// <summary>按 RO 邮件模板的发件人选择图标，当前 20、21 均由 NPC 1021 卡普拉职员投递。</summary>
    /// <param name="mailId">服务器发送的邮件模板编号，自定义信为零。</param>
    private Sprite GetSenderIcon(int mailId)
    {
        if (mailId == 20 || mailId == 21) return kafraIcon;
        return mailIcon;
    }

    /// <summary>依本封信实际附件数复用物品格，并按领取状态显示印章。</summary>
    /// <param name="body">当前正文，尚在读取时为空。</param><param name="claimed">附件是否已入包。</param>
    private void ShowAttachments(MailContent body, bool claimed)
    {
        // 每条附件包含独立的物品编号、数量与绑定状态。
        int count = body?.MailItems.Count ?? 0;
        attachmentPanel.SetActive(count > 0);
        // 保持正文顶边固定，空出的附件区交给长信滚动阅读。
        float height = 206f;
        if (count == 0) height = 324f;
        RectTransform textArea = (RectTransform)bodyScroll.transform;
        textArea.sizeDelta = new Vector2(textArea.sizeDelta.x, height);
        textArea.anchoredPosition = new Vector2(textArea.anchoredPosition.x, 129.5f - height * .5f);
        claimedSeal.SetActive(claimed && count > 0);
        if (count == 0) return;
        while (attachmentCards.Count < count)
            attachmentCards.Add(Instantiate(attachmentPrefab, attachmentScroll.content));
        for (int i = 0; i < attachmentCards.Count; i++)
        {
            attachmentCards[i].gameObject.SetActive(i < count);
            if (i < count) attachmentCards[i].SetData(body.MailItems[i]);
        }
    }
}
