/// <summary>好友长期群使用 RO 聊天室协议名称，在本项目预留的连续区间注册。</summary>
public static class ChatRoomCode
{
    /// <summary>查询本角色已加入和受邀的群。</summary>
    public const ushort GetChatRoomInfo = 11540;
    /// <summary>创建长期好友群。</summary>
    public const ushort CreateChatRoom = 11541;
    /// <summary>邀请本区好友加入群。</summary>
    public const ushort InviteRoomMember = 11542;
    /// <summary>接受发给本人的邀请。</summary>
    public const ushort ApplyJoinRoom = 11543;
    /// <summary>退出群或拒绝邀请。</summary>
    public const ushort LeaveRoom = 11544;
    /// <summary>群主解散群。</summary>
    public const ushort DissolveRoom = 11545;
    /// <summary>查询最近或更早一页实际群消息。</summary>
    public const ushort PullChat = 11546;
    /// <summary>向当前群成员发送文字、表情或语音。</summary>
    public const ushort SendChatMsg = 11547;
    /// <summary>成员或本人邀请发生变化后重新查询列表。</summary>
    public const ushort RoomInfoNtf = 11548;
    /// <summary>仅推送给已加入群的角色。</summary>
    public const ushort RoomChatNtf = 11549;
    /// <summary>中心服持久化群操作的内部协议。</summary>
    public const ushort Operation = 11550;
    /// <summary>GS 向 MS 转交实际登录角色的群请求。</summary>
    public const ushort RoleRequest = 11551;
    /// <summary>仅发给群操作人的失败原因。</summary>
    public const ushort Error = 11552;
    /// <summary>RO RoomChatSaveMaxNum，长期历史每次分页条数。</summary>
    public const int RoomChatSaveMaxNum = 30;
    /// <summary>RO RoomChatMaxNum，以 ASCII 一单位、其他码点两单位计。</summary>
    public const int RoomChatMaxNum = 60;
}
