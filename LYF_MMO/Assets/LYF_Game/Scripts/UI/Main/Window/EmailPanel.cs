using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO EmailPanel 的 UGUI 视图。所有正式控件和行模板由邮箱预制体持有。</summary>
public sealed class EmailPanel : WindowBase
{
    /// <summary>按参考分辨率排版的邮箱面板。</summary>
    [SerializeField] private RectTransform frame;
    /// <summary>左栏 ScrollRect 内按顺序排列邮件行的父节点。</summary>
    [SerializeField] private RectTransform emailItemParent;
    /// <summary>左侧列表的隐藏行模板。</summary>
    [SerializeField] private EmailItemTemplate emailItemPrefab;
    /// <summary>右栏整封邮件的展示节点。</summary>
    [SerializeField] private GameObject detailObj;
    /// <summary>空信箱或请求中的提示。</summary>
    [SerializeField] private Text empty;
    /// <summary>右栏邮件标题。</summary>
    [SerializeField] private Text title;
    /// <summary>发件人、发送日期和过期日期。</summary>
    [SerializeField] private Text senderTime;
    /// <summary>可滚动正文。</summary>
    [SerializeField] private Text content;
    /// <summary>正文下方的附件名称及数量清单。</summary>
    [SerializeField] private Text items;
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
        frame.localScale = Vector3.one * Mathf.Min(1f, area.width / frame.sizeDelta.x, area.height / frame.sizeDelta.y);
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
        // 本页含有可领取或可删除的已读邮件。
        bool unread = false;
        // 至少一封已读时允许批删。
        bool read = false;
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].gameObject.SetActive(i < mails.Count);
            if (i >= mails.Count) continue;
            rows[i].SetData(mails[i], mails[i].MailUid == uid, clicked);
            if (mails[i].IsRead) read = true;
            else unread = true;
        }
        // 一键服务作用于整个信箱，即使最新一百封都已读，仍可领取更早的未读信。
        getAllBtn.interactable = unread || mails.Count == MailCode.MaxClientMailCount;
        delAllBtn.interactable = read || mails.Count == MailCode.MaxClientMailCount;
    }

    /// <summary>右栏显示当前邮件、附件或空态，正文较长时由原生滚动区域承载。</summary>
    /// <param name="mail">当前信封，空表示没有邮件。</param><param name="body">已缓存的正文，尚未返回时为空。</param>
    /// <param name="resetScroll">切换选中项时滚到顶部。</param>
    internal void ShowDetail(MailBaseInfo mail, MailContent body, bool resetScroll)
    {
        empty.gameObject.SetActive(mail == null);
        detailObj.SetActive(mail != null);
        getBtn.gameObject.SetActive(mail != null && mail.IsHasItem && !mail.IsRead);
        delBtn.gameObject.SetActive(mail != null && mail.IsRead);
        if (mail == null) return;
        title.text = mail.Title;
        // 发送和过期时间按玩家本机时区显示。
        string sender = "";
        if (body != null) sender = body.SendName;
        senderTime.text = sender + "    " + DateTimeOffset.FromUnixTimeSeconds(mail.CreateTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm")
            + "    到期 " + DateTimeOffset.FromUnixTimeSeconds(mail.ExpireTime).LocalDateTime.ToString("MM-dd HH:mm");
        content.text = "正在读取正文…";
        items.text = "";
        if (body != null)
        {
            content.text = body.Content.Replace("\\n", "\n");
            // 使用本项目物品表显示名字；旧配置缺失时仍能辨认道具编号。
            var lines = new StringBuilder();
            if (body.MailItems.Count > 0)
            {
                lines.Append("附件");
                if (mail.IsRead) lines.Append("  ·  已领取");
            }
            for (int i = 0; i < body.MailItems.Count; i++)
            {
                // 原信保留附件清单，领取后依然可查看。
                MailItem item = body.MailItems[i];
                // 客户端本地配置将物品编号转换成可读名称。
                ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(item.ItemId);
                lines.Append("\n").Append(config?.ItemName ?? item.ItemId.ToString()).Append("  × ").Append(item.Count);
                if (item.IsBind) lines.Append("  (绑定)");
            }
            items.text = lines.ToString();
        }
        if (resetScroll) bodyScroll.verticalNormalizedPosition = 1f;
    }
}
