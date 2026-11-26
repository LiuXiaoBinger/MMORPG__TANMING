using System;
using System.Collections.Generic;
using Google.Protobuf;
using SqlSugar;

/// <summary>对应 RO ChatRoomManager。群、成员、邀请、已读和正文都存入本区角色库，离线保留。框架注册要求实现 IContainer。</summary>
internal sealed class ChatRoomManager : IContainer
{
    /// <summary>每次操作独占连接与数据库事务。</summary>
    private readonly ConnectionConfig config;
    /// <summary>使用与私聊相同的角色库。</summary><param name="config">本区数据库部署配置。</param>
    internal ChatRoomManager(ConnectionConfig config) { this.config = config; }
    /// <summary>群表由启动时的 CodeFirst 建立。</summary>
    public void OnInit() { }
    /// <summary>中心服在该协议只接受请求。</summary><param name="link">框架连接。</param><param name="packet">框架入包。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet) { }
    /// <summary>在持久化完成后返回实际成员清单及结果。</summary><param name="link">本区 MS。</param><param name="packet">带可信玩家身份的请求。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 客户端不能提供中心服使用的操作人身份。
        ChatRoomCall call = ChatRoomCall.Parser.ParseFrom(packet.Data);
        using (var db = new SqlSugarClient(config))
        {
            if (call.Command == ChatRoomCode.GetChatRoomInfo) call.List = GetChatRoomInfo(db, call);
            else
            {
                db.Ado.BeginTran();
                try
                {
                    if (call.Command == ChatRoomCode.CreateChatRoom) CreateRoom(db, call);
                    else Operate(db, call);
                    if (call.Error.Length > 0) db.Ado.RollbackTran(); else db.Ado.CommitTran();
                }
                finally { if (db.Ado.Transaction != null) db.Ado.RollbackTran(); }
            }
        }
        link.Send(packet, ChatRoomCode.Operation, call.ToByteString());
    }

    /// <summary>按成员关系查询群目录，邀请中的角色看不到正文或成员名单。</summary><param name="db">本次查询连接。</param><param name="call">所属区和角色。</param>
    private static ChatRoomList GetChatRoomInfo(SqlSugarClient db, ChatRoomCall call)
    {
        // 角色的群目录包括尚未接受的邀请。
        var mine = db.Queryable<ChatRoomMemberTable>().Where(m => m.RoleId == call.RoleId).OrderBy(m => m.RoomUid, OrderByType.Desc).ToList();
        // 本次发回的群目录，只含本角色的关系。
        var result = new ChatRoomList();
        // 逐项读取本角色可见的群。
        for (int i = 0; i < mine.Count; i++)
        {
            // 只向本区角色返回所属群，保留群主和真实昵称。
            ChatRoomMemberTable membership = mine[i];
            // 本区持久群的实际资料。
            ChatRoomTable room = db.Queryable<ChatRoomTable>().Where(r => r.RoomUid == membership.RoomUid && r.RealmId == call.RealmId).First();
            if (room == null) continue;
            // 目录行只携带当前角色能看到的群资料。
            var info = new ChatRoomInfo { RoomUid = (ulong)room.RoomUid, RoomCaptainUid = (ulong)room.Captain, CreateTime = (ulong)room.CreateTime,
                RoomSetting = new RoomSetting { RoomName = room.RoomName, RoomCapacity = room.Capacity }, Invited = !membership.Joined, RoomMember = new RoomMemberInfo() };
            if (membership.Joined)
            {
                // 批量查询群成员编号对应的正式角色名。
                var ids = db.Queryable<ChatRoomMemberTable>().Where(m => m.RoomUid == room.RoomUid && m.Joined).Select(m => m.RoleId).ToList();
                // 按角色编号查询本群成员的当前昵称。
                var roles = db.Queryable<RoleTable>().Where(r => ids.Contains(r.Id)).OrderBy(r => r.Id).ToList();
                // 逐个补全群成员的显示资料。
                for (int j = 0; j < roles.Count; j++) info.RoomMember.Info.Add(new GiftFriendInfo { RoleId = roles[j].Id, Name = roles[j].Nickname });
                info.UnreadCount = db.Queryable<ChatRoomMessageTable>().Where(m => m.RoomUid == room.RoomUid && m.Squence > membership.ReadSquence)
                    .Where(m => m.SenderId != call.RoleId).Count();
            }
            result.Rooms.Add(info);
        }
        return result;
    }

    /// <summary>创建群，并将群主作为第一个已加入的成员写入同一事务。</summary><param name="db">中心角色库。</param><param name="call">名称、容量与实际群主。</param>
    private static void CreateRoom(SqlSugarClient db, ChatRoomCall call)
    {
        // 名称长度与人数选项沿用 RO ChatRoomNameLen 和 ChatRoomSelectNumber。
        RoomSetting setting = call.Setting;
        if (setting == null) { call.Error = "请输入群名称"; return; }
        // 去掉外围空白后的正式群名。
        string name = setting.RoomName.Trim();
        if (name.Length == 0 || FriendCode.TextLength(name) > 36) { call.Error = "群名称限 1 至 36 个计数单位"; return; }
        // RO 提供的四个群容量档位之一。
        int capacity = setting.RoomCapacity;
        if (capacity < 5 || capacity > 20 || capacity % 5 != 0) { call.Error = "群人数可选 5、10、15 或 20"; return; }
        // 中心角色库为本群生成的唯一编号。
        long id = db.Insertable(new ChatRoomTable { RealmId = call.RealmId, Captain = call.RoleId, RoomName = name, Capacity = capacity,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }).ExecuteReturnBigIdentity();
        db.Insertable(new ChatRoomMemberTable { RoomUid = id, RoleId = call.RoleId, Joined = true }).ExecuteCommand();
        call.RoomUid = (ulong)id; call.NotifyRoles.Add(call.RoleId);
    }

    /// <summary>锁定本群资料，使成员变更、群序号、已读游标在同一顺序中生效。</summary><param name="db">本次事务连接。</param><param name="call">已从 MS 接收的操作。</param>
    private static void Operate(SqlSugarClient db, ChatRoomCall call)
    {
        if (call.RoomUid > long.MaxValue) { call.Error = "群不存在"; return; }
        // 按主键锁定单个群，互不影响其他群的操作。
        long id = (long)call.RoomUid;
        db.Ado.GetLong("SELECT RoomUid FROM chat_room WHERE RoomUid=@id FOR UPDATE", new SugarParameter[] { new SugarParameter("@id", id) });
        // 取得事务锁后读取的群主、容量与当前序号。
        ChatRoomTable room = db.Queryable<ChatRoomTable>().Where(r => r.RoomUid == id && r.RealmId == call.RealmId).First();
        if (room == null) { call.Error = "群已解散或不存在"; return; }
        // 含已加入及待接受邀请的当前群关系。
        var members = db.Queryable<ChatRoomMemberTable>().Where(m => m.RoomUid == id).OrderBy(m => m.RoleId).ToList();
        // 发起人在该群的真实关系。
        ChatRoomMemberTable mine = members.Find(m => m.RoleId == call.RoleId);
        if (mine == null) { call.Error = "你不在这个群中"; return; }
        if (call.Command == ChatRoomCode.ApplyJoinRoom)
        {
            if (!mine.Joined)
            {
                if (members.FindAll(m => m.Joined).Count >= room.Capacity) { call.Error = "群人数已满"; return; }
                mine.Joined = true; mine.JoinSquence = room.Squence; mine.ReadSquence = room.Squence; db.Updateable(mine).ExecuteCommand();
            }
        }
        else if (call.Command == ChatRoomCode.LeaveRoom)
        {
            db.Deleteable(mine).ExecuteCommand();
            if (room.Captain == call.RoleId)
            {
                // 群主主动退出时把群交给仍在群中的一名正式成员，空群释放。
                ChatRoomMemberTable next = members.Find(m => m.Joined && m.RoleId != call.RoleId);
                if (next == null) DissolveRoom(db, id);
                else { room.Captain = next.RoleId; db.Updateable(room).ExecuteCommand(); }
            }
        }
        else if (!mine.Joined) { call.Error = "请先接受群邀请"; return; }
        else if (call.Command == ChatRoomCode.DissolveRoom)
        {
            if (call.RoleId != room.Captain) { call.Error = "只有群主可以解散群"; return; }
            DissolveRoom(db, id);
        }
        else if (call.Command == ChatRoomCode.InviteRoomMember)
        {
            // 群邀请只能发给自己的真实好友，可邀请离线角色。
            if (call.Target > int.MaxValue || !FriendStore.IsFriend(db, call.RoleId, (int)call.Target, call.RealmId)) { call.Error = "请选择本区好友"; return; }
            if (members.Exists(m => m.RoleId == (int)call.Target)) { call.Error = "对方已在群中或已受邀"; return; }
            if (members.FindAll(m => m.Joined).Count >= room.Capacity) { call.Error = "群人数已满"; return; }
            db.Insertable(new ChatRoomMemberTable { RoomUid = id, RoleId = (int)call.Target }).ExecuteCommand();
            call.NotifyRoles.Add((int)call.Target);
        }
        else if (call.Command == ChatRoomCode.PullChat) { PullChat(db, call, room, mine); return; }
        else if (call.Command == ChatRoomCode.SendChatMsg)
        {
            // 发件昵称、时间、群号、序号仅由中心服填写。
            RoomChatMsg msg = call.Msg;
            msg.SenderUid = (ulong)call.RoleId; msg.RoomUid = call.RoomUid;
            msg.SenderName = db.Queryable<RoleTable>().Where(r => r.Id == call.RoleId).Select(r => r.Nickname).First();
            msg.Squence = ++room.Squence; msg.ChatTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            db.Updateable(room).ExecuteCommand();
            db.Insertable(new ChatRoomMessageTable { RoomUid = id, Squence = msg.Squence, SenderId = call.RoleId, Content = msg.ToByteArray() }).ExecuteCommand();
        }
        // 分别通知存储事务确定的接收角色。
        for (int i = 0; i < members.Count; i++)
        {
            // 待接受邀请可收关系变化，绝不会收到群消息正文。
            if (call.Command != ChatRoomCode.SendChatMsg || members[i].Joined) call.NotifyRoles.Add(members[i].RoleId);
        }
    }

    /// <summary>按群内可见起点倒查分页，返回时恢复正向顺序。</summary><param name="db">本次事务。</param><param name="call">请求的群和更早页边界。</param><param name="room">锁定的群。</param><param name="mine">当前成员的加入和已读游标。</param>
    private static void PullChat(SqlSugarClient db, ChatRoomCall call, ChatRoomTable room, ChatRoomMemberTable mine)
    {
        // 仅能读取自己加入以后的消息。较早页不重新计算未读。
        var query = db.Queryable<ChatRoomMessageTable>().Where(m => m.RoomUid == room.RoomUid && m.Squence > mine.JoinSquence);
        if (call.BeforeSquence > 0) query = query.Where(m => m.Squence < call.BeforeSquence);
        // 多取一条用于判断是否还有更早记录。
        var rows = query.OrderBy(m => m.Squence, OrderByType.Desc).Take(ChatRoomCode.RoomChatSaveMaxNum + 1).ToList();
        // 带本次查询边界的正向历史页。
        var page = new PullRoomChatRes { RoomUid = call.RoomUid, BeforeSquence = call.BeforeSquence, HasOlder = rows.Count > ChatRoomCode.RoomChatSaveMaxNum };
        // 将倒序查得的页恢复为正向顺序。
        for (int i = Math.Min(rows.Count, ChatRoomCode.RoomChatSaveMaxNum) - 1; i >= 0; i--) page.Messages.Add(RoomChatMsg.Parser.ParseFrom(rows[i].Content));
        if (call.BeforeSquence == 0 && mine.ReadSquence != room.Squence) { mine.ReadSquence = room.Squence; db.Updateable(mine).ExecuteCommand(); }
        call.History = page;
    }

    /// <summary>最后一人主动退出或群主解散时一起删除成员、邀请和该群消息。</summary><param name="db">本次事务。</param><param name="id">已经锁定的群号。</param>
    private static void DissolveRoom(SqlSugarClient db, long id)
    {
        db.Deleteable<ChatRoomMessageTable>().Where(m => m.RoomUid == id).ExecuteCommand();
        db.Deleteable<ChatRoomMemberTable>().Where(m => m.RoomUid == id).ExecuteCommand();
        db.Deleteable<ChatRoomTable>().Where(r => r.RoomUid == id).ExecuteCommand();
    }
}
