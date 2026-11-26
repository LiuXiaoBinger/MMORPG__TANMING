using Google.Protobuf;
using System.Collections.Generic;

/// <summary>业务服发信入口。个人信、全服信、模板和自定义内容都经本区 MS 投递。</summary>
public static class MailAction
{
    /// <summary>按 RO 邮件表编号投递，静态正文由 MS 读取。</summary>
    /// <param name="roleId">收件角色，零表示本区发信时已存在的全部角色。</param>
    /// <param name="mailId">系统邮件表中的稳定模板编号。</param>
    /// <param name="items">本次投递的物品、数量及绑定状态，空集合表示纯文本信。</param>
    /// <returns>本次网络提交是否成功，入库结果由 MS 回执。</returns>
    public static bool SendTemplateMail(int roleId, int mailId, IList<MailItem> items)
    {
        // 每次调用独享正文容器，接收方按模板补上标题和文字。
        MailContent content = new MailContent();
        content.MailItems.Add(items);
        return Post(roleId, new SMailTemplate { MailId = mailId, Content = content });
    }

    /// <summary>通过当前游戏服到 MS 的内部连接提交邮件。</summary>
    /// <param name="roleId">个人或本区全服收件地址。</param><param name="mail">本封信的类型与内容。</param>
    private static bool Post(int roleId, SMailTemplate mail)
    {
        return MasterTeamLink.Instance.SendToMaster(new BasePackage { ProtoCode = MailCode.GsSendMail,
            Data = new GsSendMailArg { RoleId = roleId, Mail = mail }.ToByteString() });
    }

    /// <summary>使用业务现场生成的标题和正文发信，复用模板信的线下投递路径。</summary>
    /// <param name="roleId">收件角色，零表示本区全服。</param><param name="title">显示在收件箱的标题。</param>
    /// <param name="content">正文、署名和可选的附件。</param><param name="expireDays">从投递起的有效天数。</param>
    public static bool SendCustomMail(int roleId, string title, MailContent content, int expireDays)
    {
        return Post(roleId, new SMailTemplate { Title = title, Content = content, ExpireDays = expireDays });
    }
}
