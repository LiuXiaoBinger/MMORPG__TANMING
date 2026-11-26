using UnityEngine;

/// <summary>对应 RO EmailHandler。负责列表选择、邮件按钮及打开后的第一封信，协议收发交 EmailMgr。</summary>
public sealed class EmailHandler
{
    /// <summary>本主城窗口对应的邮箱数据管理器。</summary>
    private readonly EmailMgr mgr = new EmailMgr();
    /// <summary>当前主城预制体持有的正式邮箱窗口。</summary>
    private readonly EmailPanel panel;
    /// <summary>邮箱入口的未读红点。</summary>
    private readonly GameObject hint;
    /// <summary>保持当前选择，删除后按原位置选择下一封。</summary>
    private long selected;
    /// <summary>当前选中行的上一次排序位置。</summary>
    private int selectedIndex;
    /// <summary>本次主城窗口显示的角色编号。</summary>
    private int roleId;
    /// <summary>服务器未读变化时通知所有可见的社交侧栏。</summary>
    internal event System.Action HintChanged;
    /// <summary>以同一份未读状态更新菜单及侧栏。</summary><param name="unread">本角色邮箱是否有未读来信。</param>
    private void SetHint(bool unread)
    {
        hint.SetActive(unread);
        HintChanged?.Invoke();
    }

    /// <summary>接入窗口、行模板和网络事件。</summary><param name="panel">主城邮箱窗口。</param><param name="hint">入口未读红点。</param>
    public EmailHandler(EmailPanel panel, GameObject hint)
    {
        this.panel = panel;
        this.hint = hint;
        mgr.Init += RefreshList;
        mgr.Reset += RefreshDetail;
        mgr.UnreadChanged += SetHint;
        mgr.NewMail += RefreshIfOpen;
        mgr.Failed += panel.SetStatus;
        panel.Opened += RefreshIfOpen;
        panel.getBtn.onClick.AddListener(OnBtnGet);
        panel.delBtn.onClick.AddListener(OnBtnDel);
        panel.getAllBtn.onClick.AddListener(OnBtnGetAll);
        panel.confirmBtn.onClick.AddListener(OnBtnDelAll);
    }

    /// <summary>切角色后清掉前一角色信件，并向服务器查询新角色的未读状态。</summary><param name="id">新角色编号，零表示离场。</param>
    public void BindRole(int id)
    {
        if (roleId == id) return;
        roleId = id;
        selected = 0;
        selectedIndex = 0;
        mgr.ClearData();
        if (id > 0) mgr.RequestGetMailList();
    }

    /// <summary>可见窗口在打开及新信抵达时重新读取摘要。</summary>
    private void RefreshIfOpen()
    {
        if (panel.isActiveAndEnabled) mgr.RequestGetMailList();
    }

    /// <summary>列表变化后保留当前信；删除当前信时沿相邻行继续。</summary>
    private void RefreshList()
    {
        if (!panel.isActiveAndEnabled) return;
        // 数据刷新后原选中项可能已被删除。
        MailBaseInfo mail = mgr.GetMail(selected);
        if (mail == null && mgr.Datas.Count > 0)
            mail = mgr.Datas[Mathf.Clamp(selectedIndex, 0, mgr.Datas.Count - 1)];
        if (mail != null) OnClickEmail(mail.MailUid);
        else
        {
            selected = 0;
            panel.ShowList(mgr.Datas, 0, OnClickEmail);
            panel.ShowDetail(null, null, true);
        }
    }

    /// <summary>仅无附件信在选中时自动已读，带附件的信必须点击领取。</summary><param name="uid">玩家选中的信封编号。</param>
    private void OnClickEmail(long uid)
    {
        // 查找当前排序中仍存在的信件。
        MailBaseInfo mail = mgr.GetMail(uid);
        if (mail == null) return;
        // 只在选择切换时重置正文滚动位置。
        bool changed = selected != uid;
        selected = uid;
        selectedIndex = mgr.Datas.IndexOf(mail);
        panel.ShowList(mgr.Datas, uid, OnClickEmail);
        // 已看过的详情直接从角色缓存恢复。
        MailContent body = mgr.GetDetail(uid);
        panel.ShowDetail(mail, body, changed);
        if (body == null) mgr.RequestGetOneMail(uid);
        if (!mail.IsRead && !mail.IsHasItem) mgr.RequestMailOp(uid, MailOpType.MailRead);
    }

    /// <summary>迟到的详情仅在对应选中项仍可见时刷新。</summary><param name="uid">正文所属邮件。</param>
    private void RefreshDetail(long uid)
    {
        if (selected == uid && panel.isActiveAndEnabled) panel.ShowDetail(mgr.GetMail(uid), mgr.GetDetail(uid), false);
    }

    /// <summary>领取当前信的全部附件。</summary>
    private void OnBtnGet() { panel.SetStatus(""); mgr.RequestMailOp(selected, MailOpType.MailRead); }
    /// <summary>删除当前已读信。</summary>
    private void OnBtnDel() { panel.SetStatus(""); mgr.RequestMailOp(selected, MailOpType.MailDel); }
    /// <summary>读取纯文本信并领取本次可领取的全部附件。</summary>
    private void OnBtnGetAll() { panel.SetStatus(""); mgr.RequestMailOp(0, MailOpType.MailReadAll); }
    /// <summary>确认后仅删除已读邮件。</summary>
    private void OnBtnDelAll() { panel.SetStatus(""); mgr.RequestMailOp(0, MailOpType.MailDelAll); }

    /// <summary>随主城控制器释放网络订阅和窗口事件。</summary>
    public void Dispose()
    {
        panel.Opened -= RefreshIfOpen;
        panel.getBtn.onClick.RemoveListener(OnBtnGet);
        panel.delBtn.onClick.RemoveListener(OnBtnDel);
        panel.getAllBtn.onClick.RemoveListener(OnBtnGetAll);
        panel.confirmBtn.onClick.RemoveListener(OnBtnDelAll);
        mgr.UnreadChanged -= SetHint;
        mgr.Dispose();
    }
}
