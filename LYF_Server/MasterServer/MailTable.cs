using System.Collections.Generic;
using System.IO;
using System.Xml;

/// <summary>按 RO 邮件模板编号解析标题、署名、正文和有效期，供本区 MS 投递。</summary>
internal sealed class MailTable
{
    /// <summary>MS 启动时加载的模板原件，多次投递共用。</summary>
    private readonly Dictionary<int, SMailTemplate> rows = new Dictionary<int, SMailTemplate>();

    /// <summary>读取随 MS 一起发布的系统邮件表。</summary>
    internal MailTable()
    {
        // 程序集资源与当前区服进程的部署目录无关。
        using (Stream stream = typeof(MailTable).Assembly.GetManifestResourceStream("MailTable.xml"))
        {
            // 表中每个 Mail 节点定义一个可发放的模板编号。
            XmlDocument table = new XmlDocument();
            table.Load(stream);
            // 遍历配置行，排除根节点下的说明注释。
            XmlNodeList entries = table.DocumentElement.SelectNodes("Mail");
            for (int i = 0; i < entries.Count; i++)
            {
                // 单行配置只提供静态内容，附件由业务发信时决定。
                XmlElement row = (XmlElement)entries[i];
                // 稳定模板编号与 RO 表一致。
                int id = int.Parse(row.GetAttribute("Id"));
                rows.Add(id, new SMailTemplate { MailId = id, Title = row.GetAttribute("Title"),
                    ExpireDays = int.Parse(row.GetAttribute("ExpireTime")), Content = new MailContent {
                        SendName = row.GetAttribute("SendName"), Content = row.InnerText } });
            }
        }
    }

    /// <summary>将赠礼模板的标题、正文和署名填入同一角色名。</summary><param name="sender">中心角色库中的发件人昵称。</param>
    internal SMailTemplate ResolveGift(string sender)
    {
        // 每封礼物独占一个附件列表。
        SMailTemplate mail = Resolve(new SMailTemplate { MailId = 4001 });
        mail.Title = mail.Title.Replace("{0}", sender);
        mail.Content.Content = mail.Content.Content.Replace("{0}", sender);
        mail.Content.SendName = mail.Content.SendName.Replace("{0}", sender);
        return mail;
    }

    /// <summary>按编号生成本封信的快照，自定义信直接使用业务提交的内容。</summary>
    /// <param name="request">GS 提交的模板编号或自定义正文及实际附件。</param>
    internal SMailTemplate Resolve(SMailTemplate request)
    {
        if (request == null || request.MailId == 0) return request;
        // 已配置的静态模板由 MS 决定，未知编号无法投递。
        if (!rows.TryGetValue(request.MailId, out SMailTemplate source)) return null;
        // 每封信拥有独立的附件列表。
        SMailTemplate mail = source.Clone();
        if (request.Content != null) mail.Content.MailItems.Add(request.Content.MailItems);
        return mail;
    }
}
