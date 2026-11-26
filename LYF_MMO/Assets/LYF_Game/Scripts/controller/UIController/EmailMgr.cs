using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>对应 RO EmailMgr，负责邮箱协议、摘要排序和单封正文缓存。</summary>
public sealed class EmailMgr
{
    /// <summary>当前角色最近一百封信的有序摘要。</summary>
    public readonly List<MailBaseInfo> Datas = new List<MailBaseInfo>();
    /// <summary>本角色已经读到的正文，以邮件编号索引。</summary>
    private readonly Dictionary<long, MailContent> details = new Dictionary<long, MailContent>();
    /// <summary>摘要列表已经替换，窗口可恢复选中项。</summary>
    public event Action Init;
    /// <summary>指定邮件的详情已返回。</summary>
    public event Action<long> Reset;
    /// <summary>全邮箱未读状态发生变化。</summary>
    public event Action<bool> UnreadChanged;
    /// <summary>收到在线新信提示，已打开的窗口需要重新取列表。</summary>
    public event Action NewMail;
    /// <summary>请求失败时向窗口传递服务端提示。</summary>
    public event Action<string> Failed;

    /// <summary>随主城控制器注册邮箱回包。</summary>
    public EmailMgr()
    {
        SocketDispatcher.Instance.AddEventHandler(MailCode.GetMailList, OnGetMailList);
        SocketDispatcher.Instance.AddEventHandler(MailCode.GetOneMail, OnGetOneMail);
        SocketDispatcher.Instance.AddEventHandler(MailCode.MailOp, OnMailOp);
        SocketDispatcher.Instance.AddEventHandler(MailCode.UpdateMailNtf, OnUpdateMailNtf);
    }

    /// <summary>主城离场时解除四个邮箱网络订阅。</summary>
    public void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(MailCode.GetMailList, OnGetMailList);
        SocketDispatcher.Instance.RemoveEventHandler(MailCode.GetOneMail, OnGetOneMail);
        SocketDispatcher.Instance.RemoveEventHandler(MailCode.MailOp, OnMailOp);
        SocketDispatcher.Instance.RemoveEventHandler(MailCode.UpdateMailNtf, OnUpdateMailNtf);
    }

    /// <summary>切换角色时释放前一个收件箱和红点状态。</summary>
    public void ClearData()
    {
        Datas.Clear();
        details.Clear();
        UnreadChanged?.Invoke(false);
        Init?.Invoke();
    }

    /// <summary>打开邮箱时只请求信封摘要。</summary>
    public void RequestGetMailList()
    {
        MNetClient.Instance.Send(MailCode.GetMailList, new GetMailListArg().ToByteString());
    }

    /// <summary>选中后取单封正文。</summary><param name="uid">当前选择的信件编号。</param>
    public void RequestGetOneMail(long uid)
    {
        MNetClient.Instance.Send(MailCode.GetOneMail, new GetOneMailArg { MailUid = uid }.ToByteString());
    }

    /// <summary>提交领取或删除意图。</summary><param name="uid">信件编号，批量操作为零。</param><param name="op">具体邮件操作。</param>
    public void RequestMailOp(long uid, MailOpType op)
    {
        MNetClient.Instance.Send(MailCode.MailOp, new MailOpArg { MailUid = uid, OpType = op }.ToByteString());
    }

    /// <summary>返回当前角色名下指定 UID 的摘要。</summary><param name="uid">待查邮件编号。</param>
    public MailBaseInfo GetMail(long uid)
    {
        for (int i = 0; i < Datas.Count; i++)
            if (Datas[i].MailUid == uid) return Datas[i];
        return null;
    }

    /// <summary>尚未拉过的信件正文为空。</summary><param name="uid">当前选择的邮件。</param>
    public MailContent GetDetail(long uid)
    {
        // 读缓存时不创建空正文。
        details.TryGetValue(uid, out MailContent detail);
        return detail;
    }

    /// <summary>采用服务端最新信封，按 RO 显示顺序重排。</summary><param name="data">GetMailListRes 字节。</param>
    private void OnGetMailList(ByteString data)
    {
        // 一次查询的权威摘要与全邮箱未读状态。
        GetMailListRes result = GetMailListRes.Parser.ParseFrom(data);
        if (result.Error.Length > 0) { Failed?.Invoke(result.Error); return; }
        Datas.Clear();
        Datas.AddRange(result.Mails);
        Datas.Sort(Compare);
        UnreadChanged?.Invoke(result.HasUnread);
        Init?.Invoke();
    }

    /// <summary>未读优先，未读间重要优先，最后按发送时间和编号倒排。</summary>
    /// <param name="a">左侧候选信封。</param><param name="b">右侧候选信封。</param>
    private static int Compare(MailBaseInfo a, MailBaseInfo b)
    {
        if (a.IsRead != b.IsRead) return a.IsRead.CompareTo(b.IsRead);
        if (!a.IsRead && a.Important != b.Important) return b.Important.CompareTo(a.Important);
        if (a.CreateTime != b.CreateTime) return b.CreateTime.CompareTo(a.CreateTime);
        return b.MailUid.CompareTo(a.MailUid);
    }

    /// <summary>详情按 UID 返回，切换选择后迟到的正文仍只进入自己的缓存。</summary><param name="data">GetOneMailRes 字节。</param>
    private void OnGetOneMail(ByteString data)
    {
        // 正文回执来自角色当前宿主。
        GetOneMailRes result = GetOneMailRes.Parser.ParseFrom(data);
        if (result.Error.Length > 0) { Failed?.Invoke(result.Error); return; }
        // 一键删除期间返回的旧信可能已移除。
        long uid = result.MailInfo.BaseInfo.MailUid;
        if (GetMail(uid) == null) return;
        details[uid] = result.MailInfo.ContentInfo;
        Reset?.Invoke(uid);
    }

    /// <summary>操作后重新取列表，使部分领取、红点和删除结果均以中心库为准。</summary><param name="data">MailOpRes 字节。</param>
    private void OnMailOp(ByteString data)
    {
        // 部分领取可以同时返回成功邮件和错误信息。
        MailOpRes result = MailOpRes.Parser.ParseFrom(data);
        if (result.Error.Length > 0) Failed?.Invoke(result.Error);
        if (result.OpType == MailOpType.MailDel) details.Remove(result.MailUid);
        else if (result.OpType == MailOpType.MailDelAll) details.Clear();
        RequestGetMailList();
    }

    /// <summary>关闭窗口期间收到新信时也更新入口红点。</summary><param name="data">UpdateMailNtf 字节。</param>
    private void OnUpdateMailNtf(ByteString data)
    {
        // 登录补发和在线投递共用同一提示。
        UpdateMailNtf notice = UpdateMailNtf.Parser.ParseFrom(data);
        UnreadChanged?.Invoke(notice.HasUnread);
        if (notice.HasUnread) NewMail?.Invoke();
    }
}
