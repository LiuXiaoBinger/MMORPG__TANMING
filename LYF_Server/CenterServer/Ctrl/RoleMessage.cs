using System;
using System.Collections.Generic;
using Google.Protobuf;
using SqlSugar;

/// <summary>对应 RO RoleMessage，在本区中心库按一对角色的 squence 保存正文及接收方的已读位置。网络框架要求实现 IContainer。</summary>
internal sealed class RoleMessage : IContainer
{
    /// <summary>每次请求独占一个中心库连接，事务使用同一连接。</summary>
    private readonly ConnectionConfig config;
    /// <summary>使用本区角色库。</summary><param name="config">中心服初始化表时的连接配置。</param>
    internal RoleMessage(ConnectionConfig config) { this.config = config; }
    /// <summary>数据表由中心服 CodeFirst 创建。</summary>
    public void OnInit() { }
    /// <summary>中心服只在监听端接收好友请求。</summary><param name="link">框架连接。</param><param name="packet">框架回包。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet) { }
    /// <summary>处理来自 MS 的角色关系、存信或补读并回复原会话。</summary><param name="link">本区 MS。</param><param name="packet">带权威角色号的内部请求。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 角色和逻辑区已由上游据实填写。
        FriendCall call = FriendCall.Parser.ParseFrom(packet.Data);
        using (var db = new SqlSugarClient(config))
        {
            Execute(db, call);
        }
        link.Send(packet, FriendCode.Operation, call.ToByteString());
    }

    /// <summary>查询联系人或在同一数据库事务内修改双方共享的会话。</summary><param name="db">当前操作的连接。</param><param name="call">将写回实际持久化结果的调用。</param>
    private static void Execute(SqlSugarClient db, FriendCall call)
    {
        if (call.Command == FriendCode.AddFriend) FriendStore.AddFriend(db, call);
        if (call.Command == FriendCode.DeleteFriend) FriendStore.DeleteFriend(db, call);
        if (call.Error.Length > 0) return;
        if (call.Command <= FriendCode.DeleteFriend)
        {
            call.Info = new GetFriendInfoRes { Unread = GetUnReadMessageCount(db, call.RoleId) };
            call.Info.Friends.Add(FriendStore.GetFriends(db, call.RoleId, call.RealmId));
            return;
        }
        if (call.Peer == 0 || call.Peer > int.MaxValue) { call.Error = "请选择好友"; return; }
        // 对端角色编号必须属于本区且至少一方已添加另一方。
        int peer = (int)call.Peer;
        if (!FriendStore.IsFriend(db, call.RoleId, peer, call.RealmId)) { call.Error = "对方已不在好友列表"; return; }
        // 两个方向采用相同的索引顺序和事务锁，首次同时互发也只生成一条相同序号。
        int low = Math.Min(call.RoleId, peer);
        // 同一对角色中编号较大的一端。
        int high = Math.Max(call.RoleId, peer);
        db.Ado.BeginTran();
        try
        {
            db.Ado.GetInt("SELECT Id FROM role WHERE Id=@low FOR UPDATE", new SugarParameter("@low", low));
            if (call.Command == FriendCode.SendPrivateChatMsg) OnAddMessage(db, call, low, high);
            else ReadPrivateMessage(db, call, low, high);
            db.Ado.CommitTran();
        }
        finally
        {
            if (db.Ado.Transaction != null) db.Ado.RollbackTran();
        }
        // 新消息只改收件方的未读，补读只改发起方的未读。
        int owner = call.RoleId;
        if (call.Command == FriendCode.SendPrivateChatMsg) owner = peer;
        call.Unread = GetUnReadMessageCount(db, owner);
    }

    /// <summary>按 RO 的每接收方向 100 条上限存储消息，返回发送者视角的正文。</summary>
    /// <param name="db">已取得该对角色锁的连接。</param><param name="call">实际发件人和媒介。</param><param name="low">编号较小的角色。</param><param name="high">编号较大的角色。</param>
    private static void OnAddMessage(SqlSugarClient db, FriendCall call, int low, int high)
    {
        // 在共享序列中分配下一号，首条从 1 起。
        var pair = db.Queryable<RoleChatTable>().Where(m => m.RoleLow == low && m.RoleHigh == high);
        // 双方共享的最近消息序号，空会话为零。
        int last = pair.Clone().OrderBy(m => m.Squence, OrderByType.Desc).Select(m => m.Squence).First();
        // 时间和作者由服务端写入。
        var row = new RoleChatTable { RoleLow = low, RoleHigh = high, Squence = last + 1, SenderId = call.RoleId,
            ChatTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Content = call.Chat.ToByteArray() };
        db.Insertable(row).ExecuteCommand();
        // 第 100 条新的同向消息仍留在数据库中，更早的同向记录可释放。
        var retained = pair.Where(m => m.SenderId == call.RoleId).OrderBy(m => m.Squence, OrderByType.Desc)
            .Skip(FriendCode.FriendsChatCacheNum - 1).Take(1).Select(m => m.Squence).ToList();
        // 只有已达到缓存上限才有可清理的序号边界。
        if (retained.Count > 0)
        {
            // 同一发件方向保留的最早序号。
            int oldest = retained[0];
            db.Deleteable<RoleChatTable>().Where(m => m.RoleLow == low && m.RoleHigh == high && m.SenderId == call.RoleId)
                .Where(m => m.Squence < oldest).ExecuteCommand();
        }
        call.Posted = row.Detail(call.RoleId);
    }

    /// <summary>按客户端上次确认的序号正向补读，只标记这次确实返回的范围。</summary>
    /// <param name="db">本次事务。</param><param name="call">含对端和已同步序号的请求。</param><param name="low">较小角色号。</param><param name="high">较大角色号。</param>
    private static void ReadPrivateMessage(SqlSugarClient db, FriendCall call, int low, int high)
    {
        // 多取一行即可告诉客户端是否需要继续补读。
        var rows = db.Queryable<RoleChatTable>().Where(m => m.RoleLow == low && m.RoleHigh == high && m.Squence > call.Squence)
            .OrderBy(m => m.Squence).Take(FriendCode.FriendsChatCacheNum + 1).ToList();
        // 空批次仍回送联系人，便于切换窗口时准确归属。
        var result = new ReadPrivateMessageRes { Uid = call.Peer, Squence = call.Squence, HasMore = rows.Count > FriendCode.FriendsChatCacheNum };
        // 按序返回一页真实收到的记录。
        for (int i = 0; i < Math.Min(rows.Count, FriendCode.FriendsChatCacheNum); i++)
        {
            result.PrivateChatList.Add(rows[i].Detail(call.RoleId));
            result.Squence = rows[i].Squence;
        }
        if (rows.Count > 0)
            db.Updateable<RoleChatTable>().SetColumns(m => m.IsRead == true).Where(m => m.RoleLow == low && m.RoleHigh == high)
                .Where(m => m.SenderId != call.RoleId && m.Squence <= result.Squence && !m.IsRead).ExecuteCommand();
        call.History = result;
    }

    /// <summary>只统计当前关系可见的会话，即使所有消息已读也保留最近聊天时间。</summary><param name="db">本区角色库。</param><param name="owner">未读状态所属角色。</param>
    private static UnReadMessageCountData GetUnReadMessageCount(SqlSugarClient db, int owner)
    {
        // 单次聚合同时取两种角色顺序，以对端编号作为分组键。
        const string query = @"SELECT IF(c.RoleLow=@owner,c.RoleHigh,c.RoleLow) AS Uid,
SUM(IF(c.SenderId<>@owner AND c.IsRead=0,1,0)) AS Count, MAX(c.ChatTime) AS LastChatTime
FROM role_private_chat c WHERE (c.RoleLow=@owner OR c.RoleHigh=@owner) AND EXISTS
(SELECT 1 FROM role_friend f WHERE (f.RoleId=c.RoleLow AND f.FriendId=c.RoleHigh) OR
(f.RoleId=c.RoleHigh AND f.FriendId=c.RoleLow)) GROUP BY Uid";
        // 每个联系人仅出现一次，按角色号和当前好友列表关联。
        var counts = db.Ado.SqlQuery<UnReadMessageCountInfo>(query, new SugarParameter("@owner", owner));
        // 聚合后的每个会话和总未读数量。
        var result = new UnReadMessageCountData();
        // 逐会话计算本人的总未读。
        for (int i = 0; i < counts.Count; i++)
        {
            result.UnreadCounts.Add(counts[i]);
            result.TotalUnreadCount += counts[i].Count;
        }
        return result;
    }
}
