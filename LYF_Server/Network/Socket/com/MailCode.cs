/// <summary>RO 邮箱 RPC 和通知在本项目 ushort 协议空间中的编号。</summary>
public static class MailCode
{
    /// <summary>请求信封摘要列表。</summary>
    public const ushort GetMailList = 11500;
    /// <summary>按邮件 UID 读取正文和附件。</summary>
    public const ushort GetOneMail = 11501;
    /// <summary>单封或一键领取、删除，回执复用该编号。</summary>
    public const ushort MailOp = 11502;
    /// <summary>登录或收到新信后的未读提示。</summary>
    public const ushort UpdateMailNtf = 11503;
    /// <summary>GS 向 MS 提交已核对角色的邮箱请求。</summary>
    public const ushort RoleRequest = 11504;
    /// <summary>MS 向中心服操作角色信箱，回程保留 GS 地址。</summary>
    public const ushort MailOperation = 11505;
    /// <summary>MS 请求 GS 发放一封或多封信的附件。</summary>
    public const ushort GiveMailPrize = 11506;
    /// <summary>服务内部投递个人信或全服信。</summary>
    public const ushort GsSendMail = 11507;
    /// <summary>MS 向玩家实际宿主投递带角色身份的回包。</summary>
    public const ushort Delivery = 11508;
    /// <summary>GS 连接本区 MS 时订阅新信通知。</summary>
    public const ushort Subscribe = 11509;
    /// <summary>RO 每次展示和一键领取的邮件上限。</summary>
    public const int MaxClientMailCount = 100;
    /// <summary>未领附件允许超过展示容量，达到 RO 的五百封硬上限后淘汰最早一封。</summary>
    public const int MailCountLimit = 500;
}
