using SqlSugar;

/// <summary>持久保存某群对一个角色的邀请、入群范围与已读位置。</summary>
[SugarTable("chat_room_member")]
public sealed class ChatRoomMemberTable
{
    /// <summary>成员所属群号。</summary>
    [SugarColumn(IsPrimaryKey = true)] public long RoomUid { get; set; }
    /// <summary>受邀或已加入的角色号。</summary>
    [SugarColumn(IsPrimaryKey = true)] public int RoleId { get; set; }
    /// <summary>已接受邀请，可读取入群后的消息。</summary>
    public bool Joined { get; set; }
    /// <summary>入群前最后一条消息号，作为可见范围下界。</summary>
    public int JoinSquence { get; set; }
    /// <summary>该成员打开并读到的最后序号。</summary>
    public int ReadSquence { get; set; }
}
