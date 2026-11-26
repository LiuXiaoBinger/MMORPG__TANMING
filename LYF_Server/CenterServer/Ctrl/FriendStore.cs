using System.Collections.Generic;
using SqlSugar;

/// <summary>RO 好友关系在本区的存取，赠礼与私聊共用 role_friend 的两个方向。</summary>
internal static class FriendStore
{
    /// <summary>主动添加本区角色或按 UID 回加对方。</summary><param name="db">角色库连接。</param><param name="call">实际操作人及输入的昵称或对端 UID。</param>
    internal static void AddFriend(SqlSugarClient db, FriendCall call)
    {
        // 角色选择按本区正式资料，支持当前离线的好友。
        var query = db.Queryable<RoleTable>().Where(r => r.ServerId == call.RealmId);
        if (call.Peer > int.MaxValue) { call.Error = "本区没有这个角色"; return; }
        // 按区服和输入昵称或回加 UID 找到的角色。
        RoleTable target;
        if (call.Peer > 0) target = query.Where(r => r.Id == (int)call.Peer).First();
        else target = query.Where(r => r.Nickname == call.Name).First();
        if (target == null) { call.Error = "本区没有这个角色"; return; }
        if (target.Id == call.RoleId) { call.Error = "不能添加自己"; return; }
        db.Ado.ExecuteCommand("INSERT INTO role_friend (RoleId,FriendId) VALUES (@sender,@friend) ON DUPLICATE KEY UPDATE FriendId=VALUES(FriendId)",
            new SugarParameter("@sender", call.RoleId), new SugarParameter("@friend", target.Id));
        call.Peer = (ulong)target.Id;
    }

    /// <summary>仅取消自己主动添加的边，保留对方独立的添加记录。</summary><param name="db">角色库。</param><param name="call">发起人和要删除的联系人。</param>
    internal static void DeleteFriend(SqlSugarClient db, FriendCall call)
    {
        if (call.Peer == 0 || call.Peer > int.MaxValue) { call.Error = "请选择好友"; return; }
        // 单向关系的移除只影响当前角色。
        int peer = (int)call.Peer;
        db.Deleteable<RoleFriendTable>().Where(f => f.RoleId == call.RoleId && f.FriendId == peer).ExecuteCommand();
    }

    /// <summary>双方向任一好友关系均可发起或回复同区对话。</summary><param name="db">角色库。</param><param name="owner">发件角色。</param><param name="peer">对端角色。</param><param name="realm">当前区号。</param>
    internal static bool IsFriend(SqlSugarClient db, int owner, int peer, int realm)
    {
        if (owner == peer || !db.Queryable<RoleTable>().Any(r => r.Id == peer && r.ServerId == realm)) return false;
        return db.Queryable<RoleFriendTable>().Where(f => f.RoleId == owner || f.FriendId == owner)
            .Any(f => f.RoleId == peer || f.FriendId == peer);
    }

    /// <summary>将主动和被添加的关系合并成按角色名排列的列表。</summary><param name="db">角色库。</param><param name="owner">查列表的角色。</param><param name="realm">该角色所在区。</param>
    internal static List<GiftFriendInfo> GetFriends(SqlSugarClient db, int owner, int realm)
    {
        // 收礼列表和私聊使用相同的联系人结构。
        var result = new List<GiftFriendInfo>();
        // 正反向记录分别表示已添加和可回加，双向关系只显示一行。
        var edges = db.Queryable<RoleFriendTable>().Where(f => f.RoleId == owner || f.FriendId == owner).ToList();
        // 同一角色正反两条关系合并为一个选择项。
        var contacts = new Dictionary<int, GiftFriendInfo>();
        // 逐条读取当前角色作为主动方或被添加方的关系。
        for (int index = 0; index < edges.Count; index++)
        {
            // 取关系的另一端作为联系人。
            RoleFriendTable edge = edges[index];
            // 初始视为被添加方，若本角色是主动方则改取对端编号。
            int id = edge.RoleId;
            if (id == owner) id = edge.FriendId;
            // 首次遇到此角色时建立可附加两个方向标志的行。
            if (!contacts.TryGetValue(id, out GiftFriendInfo info)) { info = new GiftFriendInfo { RoleId = id }; contacts.Add(id, info); }
            if (edge.RoleId == owner) info.Added = true;
            else info.AddedMe = true;
        }
        if (contacts.Count == 0) return result;
        // 本区可见联系人的显示名以数据库当前值为准。
        var ids = new List<int>(contacts.Keys);
        // 只取同区仍存在的联系人，按角色名顺序展示。
        var roles = db.Queryable<RoleTable>().Where(r => r.ServerId == realm && ids.Contains(r.Id)).OrderBy(r => r.Nickname).ToList();
        // 按当前角色库顺序返回已补全昵称的好友。
        for (int index = 0; index < roles.Count; index++)
        {
            // 用真实角色信息补全列表项。
            GiftFriendInfo info = contacts[roles[index].Id];
            info.Name = roles[index].Nickname;
            result.Add(info);
        }
        return result;
    }
}
