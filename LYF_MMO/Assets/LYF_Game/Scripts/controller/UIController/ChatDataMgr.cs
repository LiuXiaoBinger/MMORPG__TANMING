using System;
using Google.Protobuf;
using UnityEngine;

/// <summary>对应 RO ChatDataMgr，按区、本人、联系人分开保存最近私聊和断线补读位置。</summary>
public sealed class ChatDataMgr
{
    /// <summary>当前区与当前角色的本机存档前缀。</summary>
    private string prefix;
    /// <summary>随主角改变切换存档，角色号不会跨区混用。</summary><param name="realm">登录区号。</param><param name="role">主角数据库编号。</param>
    internal void BindRole(int realm, int role) { prefix = "PrivateChat." + realm + "." + role + "."; }
    /// <summary>读取当前区及角色的本机好友分组和特别关注。</summary>
    internal FriendContactGroups GetGroups()
    {
        // 分组和私聊记录使用不同的键，旧版客户端创建的角色首次从空目录开始。
        string saved = PlayerPrefs.GetString(prefix + "Groups");
        if (saved.Length == 0) return new FriendContactGroups();
        return JsonUtility.FromJson<FriendContactGroups>(saved);
    }

    /// <summary>在玩家确认更改时持久化联系人目录。</summary><param name="groups">本角色当前所有分类和特别关注。</param>
    internal void SaveGroups(FriendContactGroups groups)
    {
        PlayerPrefs.SetString(prefix + "Groups", JsonUtility.ToJson(groups));
        PlayerPrefs.Save();
    }

    /// <summary>从独立存档读取该对端最近的私聊行及最后成功补读的序号。</summary><param name="uid">对端角色号。</param>
    internal ReadPrivateMessageRes Get(ulong uid)
    {
        // 一次保存序号和正文，避免两项分别更新造成丢消息。
        string saved = PlayerPrefs.GetString(prefix + uid);
        if (saved.Length == 0) return new ReadPrivateMessageRes { Uid = uid };
        return ReadPrivateMessageRes.Parser.ParseFrom(Convert.FromBase64String(saved));
    }
    /// <summary>将已收到的单条通知与本机历史合并，仍按原补读位置查询离线消息。</summary><param name="row">服务端已编号的消息。</param>
    internal void AddMessage(PrivateDetailChatInfo row)
    {
        // 实时消息可以先于离线批次到达。
        ReadPrivateMessageRes saved = Get(row.ChatUid);
        Insert(saved, row); Save(saved);
    }
    /// <summary>只推进服务器已返回过的补读位置，合并实时与批次的重合行。</summary><param name="page">该联系人的一个完整补读批次。</param>
    internal void ReadPrivateMessage(ReadPrivateMessageRes page)
    {
        // 选中另一人期间返回的历史仍写入原对端的存档。
        ReadPrivateMessageRes saved = Get(page.Uid);
        // 按返回页内的顺序合并实际记录。
        for (int i = 0; i < page.PrivateChatList.Count; i++) Insert(saved, page.PrivateChatList[i]);
        saved.Squence = Math.Max(saved.Squence, page.Squence); Save(saved);
    }
    /// <summary>按权威序号去重并插入相应位置，界面只保留最近一百行。</summary><param name="saved">该联系人的本地记录。</param><param name="row">待合并的真实消息。</param>
    private static void Insert(ReadPrivateMessageRes saved, PrivateDetailChatInfo row)
    {
        // 从最新端倒查，正常在线消息直接接到末尾。
        int index = saved.PrivateChatList.Count;
        while (index > 0 && saved.PrivateChatList[index - 1].Squence > row.Squence) index--;
        if (index > 0 && saved.PrivateChatList[index - 1].Squence == row.Squence) return;
        saved.PrivateChatList.Insert(index, row);
        if (saved.PrivateChatList.Count > FriendCode.FriendsChatCacheNum) saved.PrivateChatList.RemoveAt(0);
    }
    /// <summary>以同一个键覆盖本次序列化内容。</summary><param name="saved">将正文与已确认游标一起写盘的会话。</param>
    private void Save(ReadPrivateMessageRes saved)
    { PlayerPrefs.SetString(prefix + saved.Uid, Convert.ToBase64String(saved.ToByteArray())); PlayerPrefs.Save(); }
}
