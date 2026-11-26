using System;
using Google.Protobuf;

/// <summary>对应 RO ChatRoomMgr。发送建群、邀请、加入、退群、群发和分页查询，并接收群成员定向推送。</summary>
public sealed class ChatRoomMgr
{
    /// <summary>当前角色最新的已加入与受邀目录。</summary>
    internal ChatRoomList Rooms { get; private set; } = new ChatRoomList();
    /// <summary>群列表和本角色未读数已更新。</summary>
    internal event Action Changed;
    /// <summary>群主或成员操作完成，按实际命令恢复选中群。</summary>
    internal event Action<RoomChangeNtf> RoomChange;
    /// <summary>打开群或向前翻页返回的正式记录。</summary>
    internal event Action<PullRoomChatRes> History;
    /// <summary>只发给已入群成员的消息。</summary>
    internal event Action<RoomChatMsg> Chat;
    /// <summary>发送失败或没有群操作权限时的文字。</summary>
    internal event Action<string> Failed;
    /// <summary>注册此主城实例的群回包。</summary>
    internal ChatRoomMgr()
    {
        SocketDispatcher.Instance.AddEventHandler(ChatRoomCode.GetChatRoomInfo, OnGetChatRoomInfo);
        SocketDispatcher.Instance.AddEventHandler(ChatRoomCode.RoomInfoNtf, OnRoomInfoNtf);
        SocketDispatcher.Instance.AddEventHandler(ChatRoomCode.RoomChatNtf, OnRoomChatNtf);
        SocketDispatcher.Instance.AddEventHandler(ChatRoomCode.PullChat, OnPullChat);
        SocketDispatcher.Instance.AddEventHandler(ChatRoomCode.Error, OnError);
    }
    /// <summary>从当前主城注销相同的强类型回调。</summary>
    internal void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(ChatRoomCode.GetChatRoomInfo, OnGetChatRoomInfo);
        SocketDispatcher.Instance.RemoveEventHandler(ChatRoomCode.RoomInfoNtf, OnRoomInfoNtf);
        SocketDispatcher.Instance.RemoveEventHandler(ChatRoomCode.RoomChatNtf, OnRoomChatNtf);
        SocketDispatcher.Instance.RemoveEventHandler(ChatRoomCode.PullChat, OnPullChat);
        SocketDispatcher.Instance.RemoveEventHandler(ChatRoomCode.Error, OnError);
    }
    /// <summary>提交群指令并将断线反馈给输入窗口。</summary><param name="code">公开群命令。</param><param name="body">由该命令定义的消息。</param>
    private bool Send(ushort code, IMessage body)
    {
        if (MNetClient.Instance.Send(code, body.ToByteString())) return true;
        Failed?.Invoke("群聊连接不可用"); return false;
    }
    /// <summary>查询并恢复已加入或受邀的全部群。</summary>
    internal void RequestGetChatRoomInfo() { Send(ChatRoomCode.GetChatRoomInfo, new ChatRoomInfoArg()); }
    /// <summary>使用 RO 原名创建聊天室。</summary><param name="name">群名。</param><param name="capacity">5、10、15 或 20 人。</param>
    internal void RequestCreatChatRoom(string name, int capacity)
    { Send(ChatRoomCode.CreateChatRoom, new CreateChatRoomArg { Setting = new RoomSetting { RoomName = name, RoomCapacity = capacity } }); }
    /// <summary>邀请指定好友，待对方亲自接受。</summary><param name="room">当前群。</param><param name="uid">选中的好友。</param>
    internal void RequestInviteRoomMember(ulong room, ulong uid) { Send(ChatRoomCode.InviteRoomMember, new InviteRoomMemberArg { RoomUid = room, RoleUid = uid }); }
    /// <summary>接受属于自己的群邀请。</summary><param name="room">受邀群号。</param>
    internal void RequestApplyJoinRoom(ulong room) { Send(ChatRoomCode.ApplyJoinRoom, new ApplyJoinRoomArg { RoomUid = room }); }
    /// <summary>退出群聊或拒收该群的邀请。</summary><param name="room">本人群目录内的目标。</param>
    internal void RequestLeaveRoom(ulong room) { Send(ChatRoomCode.LeaveRoom, new LeaveRoomArg { RoomUid = room }); }
    /// <summary>群主主动解散当前群。</summary><param name="room">目标群号。</param>
    internal void RequestDissolveRoom(ulong room) { Send(ChatRoomCode.DissolveRoom, new DissolveRoomArg { RoomUid = room }); }
    /// <summary>读取最新一页，或指定群序号之前的历史。</summary><param name="room">已加入的群。</param><param name="before">零表示最新一页。</param>
    internal void RequestPullChat(ulong room, int before) { Send(ChatRoomCode.PullChat, new ChatRoomInfoArg { RoomUid = room, BeforeSquence = before }); }
    /// <summary>发送群文字、RO 表情标记或已完成上传的语音。</summary><param name="msg">含目标群和媒介的正文。</param>
    internal bool RequestSendChatMsg(RoomChatMsg msg) { return Send(ChatRoomCode.SendChatMsg, msg); }
    /// <summary>接收本人目录后刷新。</summary><param name="data">ChatRoomList 字节。</param>
    private void OnGetChatRoomInfo(ByteString data) { Rooms = ChatRoomList.Parser.ParseFrom(data); Changed?.Invoke(); }
    /// <summary>成员关系变更后重新查询权威目录。</summary><param name="data">群号及本人刚完成的命令。</param>
    private void OnRoomInfoNtf(ByteString data) { RoomChange?.Invoke(RoomChangeNtf.Parser.ParseFrom(data)); RequestGetChatRoomInfo(); }
    /// <summary>将在线群消息交给当前窗口处理。</summary><param name="data">含真实昵称及群内序号的消息。</param>
    private void OnRoomChatNtf(ByteString data) { Chat?.Invoke(RoomChatMsg.Parser.ParseFrom(data)); }
    /// <summary>返回按时间正序排列的一页。</summary><param name="data">群历史分页。</param>
    private void OnPullChat(ByteString data) { History?.Invoke(PullRoomChatRes.Parser.ParseFrom(data)); }
    /// <summary>返回本人操作失败原因。</summary><param name="data">FriendError 字节。</param>
    private void OnError(ByteString data) { Failed?.Invoke(FriendError.Parser.ParseFrom(data).Error); }
}
