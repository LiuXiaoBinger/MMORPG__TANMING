using SqlSugar;

/// <summary>RO ChatRoom 的长期群资料，最后一个人主动离开时释放。</summary>
[SugarTable("chat_room")]
public sealed class ChatRoomTable
{
    /// <summary>中心库生成的唯一群号。</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)] public long RoomUid { get; set; }
    /// <summary>所属逻辑区。</summary>
    public int RealmId { get; set; }
    /// <summary>当前群主的角色号。</summary>
    public int Captain { get; set; }
    /// <summary>玩家输入的群名。</summary>
    [SugarColumn(Length = 64)] public string RoomName { get; set; }
    /// <summary>本群已选的 RO 人数档位。</summary>
    public int Capacity { get; set; }
    /// <summary>创建时的 Unix 秒。</summary>
    public long CreateTime { get; set; }
    /// <summary>群内最后一条权威消息的连续序号。</summary>
    public int Squence { get; set; }
}
