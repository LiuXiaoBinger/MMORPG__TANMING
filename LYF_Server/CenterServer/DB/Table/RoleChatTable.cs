using SqlSugar;

/// <summary>一对角色共享的私聊正文，双方按同一递增序号恢复顺序；未读状态只属于接收者。</summary>
[SugarTable("role_private_chat")]
public sealed class RoleChatTable
{
    /// <summary>双方编号较小者，与另一端和序号组成主键。</summary>
    [SugarColumn(IsPrimaryKey = true)] public int RoleLow { get; set; }
    /// <summary>双方编号较大者。</summary>
    [SugarColumn(IsPrimaryKey = true)] public int RoleHigh { get; set; }
    /// <summary>该对联系人从一开始递增的 RO 会话序号。</summary>
    [SugarColumn(IsPrimaryKey = true)] public int Squence { get; set; }
    /// <summary>本条消息实际发件角色，由 GS 会话决定。</summary>
    public int SenderId { get; set; }
    /// <summary>中心库存入本条消息的 UTC Unix 秒。</summary>
    public long ChatTime { get; set; }
    /// <summary>接收者是否已经打开这段对话并补读至该条。</summary>
    public bool IsRead { get; set; }
    /// <summary>原始 PrivateChatInfo 正文，语音只保留文件标识和时长。</summary>
    [SugarColumn(ColumnDataType = "blob")] public byte[] Content { get; set; }

    /// <summary>按本人或对方的视角生成 RO 私聊行。</summary><param name="owner">看这行记录的角色编号。</param>
    internal PrivateDetailChatInfo Detail(int owner)
    {
        // 正文内的目标和表中的发件人同属一段对话。
        PrivateChatInfo body = PrivateChatInfo.Parser.ParseFrom(Content);
        // 默认是对方写给本人的消息。
        var row = new PrivateDetailChatInfo { ChatUid = (ulong)SenderId, Squence = Squence, WhoType = 2,
            ChatTime = (ulong)ChatTime, ChatType = (int)body.MediumType, Content = body.Content, Audio = body.Audio };
        if (SenderId == owner) { row.ChatUid = body.Uid; row.WhoType = 1; }
        return row;
    }
}
