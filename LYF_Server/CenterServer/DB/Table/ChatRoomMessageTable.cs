using SqlSugar;

/// <summary>按群号及递增序号保存的长期聊天正文。</summary>
[SugarTable("chat_room_message")]
public sealed class ChatRoomMessageTable
{
    /// <summary>该条消息所属群号。</summary>
    [SugarColumn(IsPrimaryKey = true)] public long RoomUid { get; set; }
    /// <summary>本群内单调递增的消息号。</summary>
    [SugarColumn(IsPrimaryKey = true)] public int Squence { get; set; }
    /// <summary>真实发件人，用于逐成员统计未读。</summary>
    public int SenderId { get; set; }
    /// <summary>含昵称、时间与文字或语音的正式群消息。</summary>
    [SugarColumn(ColumnDataType = "blob")] public byte[] Content { get; set; }
}
