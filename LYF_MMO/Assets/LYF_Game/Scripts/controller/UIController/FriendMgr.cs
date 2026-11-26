using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>对应 RO FriendMgr，收发好友操作、私聊实时通知和补读协议。</summary>
public sealed class FriendMgr
{
    /// <summary>服务端最近一次确认的双向联系人信息。</summary>
    internal readonly List<GiftFriendInfo> Friends = new List<GiftFriendInfo>();
    /// <summary>本角色的各联系人未读条数与最后聊天时间。</summary>
    internal UnReadMessageCountData Unread { get; private set; } = new UnReadMessageCountData();
    /// <summary>关系列表或未读概要已更新。</summary>
    internal event Action Changed;
    /// <summary>收到已入库的本人回显或对方的消息。</summary>
    internal event Action<PrivateDetailChatInfo> PrivateChat;
    /// <summary>服务端已返回指定联系人的一个历史批次。</summary>
    internal event Action<ReadPrivateMessageRes> History;
    /// <summary>将服务端业务失败或断线提示交给界面。</summary>
    internal event Action<string> Failed;

    /// <summary>注册当前主城生命周期的六种好友回执。</summary>
    internal FriendMgr()
    {
        SocketDispatcher.Instance.AddEventHandler(FriendCode.GetFriendInfo, ReceiveFriendInfo);
        SocketDispatcher.Instance.AddEventHandler(FriendCode.PrivateChatNtf, ReceivePrivateChatNtf);
        SocketDispatcher.Instance.AddEventHandler(FriendCode.ReadPrivateMessage, ReceiveReadPrivateMessage);
        SocketDispatcher.Instance.AddEventHandler(FriendCode.UnReadMessageCountNtf, ReceiveUnReadMessageCountNtf);
        SocketDispatcher.Instance.AddEventHandler(FriendCode.FriendChangedNtf, ReceiveFriendChanged);
        SocketDispatcher.Instance.AddEventHandler(FriendCode.Error, ReceiveError);
    }

    /// <summary>随主城退出注销原委托。</summary>
    internal void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(FriendCode.GetFriendInfo, ReceiveFriendInfo);
        SocketDispatcher.Instance.RemoveEventHandler(FriendCode.PrivateChatNtf, ReceivePrivateChatNtf);
        SocketDispatcher.Instance.RemoveEventHandler(FriendCode.ReadPrivateMessage, ReceiveReadPrivateMessage);
        SocketDispatcher.Instance.RemoveEventHandler(FriendCode.UnReadMessageCountNtf, ReceiveUnReadMessageCountNtf);
        SocketDispatcher.Instance.RemoveEventHandler(FriendCode.FriendChangedNtf, ReceiveFriendChanged);
        SocketDispatcher.Instance.RemoveEventHandler(FriendCode.Error, ReceiveError);
    }

    /// <summary>角色切换后重新加载关系和红点。</summary>
    internal void ClearData() { Friends.Clear(); Unread = new UnReadMessageCountData(); }

    /// <summary>获取两个方向的联系人及各段对话的未读数。</summary>
    internal void RequestGetFriendInfo() { Send(FriendCode.GetFriendInfo, new GetFriendInfoArg()); }
    /// <summary>通过名字添加或通过非零 UID 回加好友。</summary><param name="name">输入的同区角色名。</param><param name="uid">回加的真实角色号。</param>
    internal void RequestAddFriend(string name, ulong uid) { Send(FriendCode.AddFriend, new AddFriendArg { Uid = uid, Name = name }); }
    /// <summary>移除自己的单向关系。</summary><param name="uid">当前选中的好友。</param>
    internal void RequestDeleteFriend(ulong uid) { Send(FriendCode.DeleteFriend, new DeleteFriendArg { Uid = uid }); }
    /// <summary>发送 RO 私聊，不进入公共频道。</summary><param name="info">目标、媒介与消息内容。</param>
    internal bool RequestSendPrivateChatMsg(PrivateChatInfo info) { return Send(FriendCode.SendPrivateChatMsg, info); }
    /// <summary>从指定联系人上次写入本地的序号之后补读。</summary><param name="uid">对端角色。</param><param name="squence">最后已确认的本地序号。</param>
    internal void RequestReadPrivateMessage(ulong uid, int squence)
    { Send(FriendCode.ReadPrivateMessage, new ReadPrivateMessageArg { Uid = uid, Squence = squence }); }

    /// <summary>沿用已有 Protobuf 消息接口向当前网关发包。</summary><param name="code">好友公开命令。</param><param name="body">该命令规定的协议对象。</param>
    private bool Send(ushort code, IMessage body)
    {
        if (MNetClient.Instance.Send(code, body.ToByteString())) return true;
        Failed?.Invoke("好友连接不可用"); return false;
    }

    /// <summary>替换完整的好友列表及未读摘要。</summary><param name="data">GetFriendInfoRes 字节。</param>
    private void ReceiveFriendInfo(ByteString data)
    {
        // 服务端已合并两个方向的好友关系。
        GetFriendInfoRes info = GetFriendInfoRes.Parser.ParseFrom(data);
        Friends.Clear(); Friends.AddRange(info.Friends); Unread = info.Unread; Changed?.Invoke();
    }
    /// <summary>当前联系人有新的实际消息。</summary><param name="data">从当前玩家视角组装的正文。</param>
    private void ReceivePrivateChatNtf(ByteString data) { PrivateChat?.Invoke(PrivateDetailChatInfo.Parser.ParseFrom(data)); }
    /// <summary>传递服务端按序返回的历史。</summary><param name="data">含序号和继续标志的历史页。</param>
    private void ReceiveReadPrivateMessage(ByteString data) { History?.Invoke(ReadPrivateMessageRes.Parser.ParseFrom(data)); }
    /// <summary>更新红点及最近联系人，不接触正在编辑的文字。</summary><param name="data">本角色所有可见联系人的未读概要。</param>
    private void ReceiveUnReadMessageCountNtf(ByteString data)
    { Unread = UnReadMessageCountData.Parser.ParseFrom(data); Changed?.Invoke(); }
    /// <summary>对端添加或删除关系后，重新查询自己的两个关系方向。</summary><param name="data">无字段的关系变动通知。</param>
    private void ReceiveFriendChanged(ByteString data) { RequestGetFriendInfo(); }
    /// <summary>将业务错误展现给发起人。</summary><param name="data">FriendError 原始字节。</param>
    private void ReceiveError(ByteString data) { Failed?.Invoke(FriendError.Parser.ParseFrom(data).Error); }
}
