using SqlSugar;

/// <summary>一个角色收到的一封信。领取与该角色背包共用中心库事务。</summary>
[SugarTable("role_mail")]
[SugarIndex("ix_role_mail_inbox", nameof(RoleId), OrderByType.Asc, nameof(MailUid), OrderByType.Desc)]
public sealed class RoleMailTable
{
    /// <summary>中心服分配的全库唯一邮件编号。</summary>
    [SugarColumn(IsPrimaryKey = true)]
    public long MailUid { get; set; }
    /// <summary>收件角色的数据库主键。</summary>
    public int RoleId { get; set; }
    /// <summary>模板编号，零表示自定义邮件。</summary>
    public int MailId { get; set; }
    /// <summary>发出时的 UTC Unix 秒。</summary>
    public long CreateTime { get; set; }
    /// <summary>达到此 UTC Unix 秒后邮件失效。</summary>
    public long ExpireTime { get; set; }
    /// <summary>文本已阅读或全部附件已到账。</summary>
    public bool IsRead { get; set; }
    /// <summary>原信是否携带附件，用于列表和删除规则。</summary>
    public bool HasItem { get; set; }
    /// <summary>在同为未读的信中优先展示。</summary>
    public bool Important { get; set; }
    /// <summary>收件箱显示的标题。</summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Title { get; set; }
    /// <summary>MailContent 序列化的正文、署名和奖励明细。</summary>
    [SugarColumn(ColumnDataType = "longblob")]
    public byte[] Content { get; set; }

    /// <summary>生成不含正文或物品明细的列表信封。</summary>
    public MailBaseInfo ToBaseInfo()
    {
        return new MailBaseInfo { MailUid = MailUid, MailId = MailId, CreateTime = CreateTime,
            ExpireTime = ExpireTime, IsRead = IsRead, IsHasItem = HasItem, Title = Title, Important = Important };
    }
}
