/// <summary>RO 好友、私聊和离线补读在本项目网关上的协议编号。</summary>
public static class FriendCode
{
    /// <summary>获取本角色的好友关系与最近联系人。</summary>
    public const ushort GetFriendInfo = 11520;
    /// <summary>主动添加或回加同区角色。</summary>
    public const ushort AddFriend = 11521;
    /// <summary>取消自己主动添加的好友关系。</summary>
    public const ushort DeleteFriend = 11522;
    /// <summary>提交仅向一个联系人发送的文字或语音。</summary>
    public const ushort SendPrivateChatMsg = 11523;
    /// <summary>按对端 UID 和会话序号补读历史。</summary>
    public const ushort ReadPrivateMessage = 11524;
    /// <summary>向会话两端定向投递带序号的私聊正文。</summary>
    public const ushort PrivateChatNtf = 11525;
    /// <summary>推送本角色各会话未读数和最近聊天时间。</summary>
    public const ushort UnReadMessageCountNtf = 11526;
    /// <summary>对端关系变动，通知在线角色重新查询自己的列表。</summary>
    public const ushort FriendChangedNtf = 11527;
    /// <summary>告诉操作发起人本次动作未成功及原因。</summary>
    public const ushort Error = 11528;
    /// <summary>GS 向 MS 提交已确认发件角色的请求。</summary>
    public const ushort RoleRequest = 11529;
    /// <summary>MS 与 Center 收发绑定逻辑区号的角色消息存取调用。</summary>
    public const ushort Operation = 11530;
    /// <summary>RO 的 FriendsChatCacheNum，每位联系人每个接收方向保存的消息上限。</summary>
    public const int FriendsChatCacheNum = 100;
    /// <summary>按 ASCII 计 1、非 ASCII 码点计 2 的频道长度单位计数。</summary>
    /// <param name="text">文字或转写。</param><returns>消息占用的频道长度单位。</returns>
    public static int TextLength(string text)
    {
        // 多字节码点按一个字符处理，包括补充平面的表情。
        int units = 0;
        // 逐码点计数，成对代理项只占一个非 ASCII 码点。
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] <= 127) units++;
            else { units += 2; if (char.IsSurrogatePair(text, i)) i++; }
        }
        return units;
    }

    /// <summary>RO 的 FriendMaxNum，私聊输入长度上限。</summary>
    public const int FriendMaxNum = 100;
}
