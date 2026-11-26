using System.Collections.Generic;
using SqlSugar;

/// <summary>中心角色库的赠礼联系人，对应 RO 的好友赠礼列表和收件角色查询。</summary>
internal static class GiftStore
{
    /// <summary>从同区角色名添加联系人，或获取玩家已添加以及添加过自己的好友。</summary>
    /// <param name="db">当前区角色库连接。</param><param name="call">游戏服从发起人会话构造的内部请求。</param>
    internal static void GetFriends(SqlSugarClient db, MailCall call)
    {
        // 空昵称表示只刷新列表。
        string name = call.FriendName.Trim();
        if (name.Length > 0)
        {
            // 角色名按本区的正式角色表查找，支持离线角色。
            RoleTable target = db.Queryable<RoleTable>().Where(r => r.ServerId == call.RealmId && r.Nickname == name).First();
            if (target == null) { call.Result.Error = "本区没有这个角色"; return; }
            if (target.Id == call.RoleId) { call.Result.Error = "不能添加自己"; return; }
            db.Ado.ExecuteCommand("INSERT INTO role_friend (RoleId,FriendId) VALUES (@sender,@friend) ON DUPLICATE KEY UPDATE FriendId=VALUES(FriendId)",
                new SugarParameter("@sender", call.RoleId), new SugarParameter("@friend", target.Id));
        }
        // 正反向记录分别表示已添加和可回加，双向关系只显示一行。
        var edges = db.Queryable<RoleFriendTable>().Where(f => f.RoleId == call.RoleId || f.FriendId == call.RoleId).ToList();
        // 同一角色正反两条关系合并为一个选择项。
        var contacts = new Dictionary<int, GiftFriendInfo>();
        // 逐条读取当前角色作为主动方或被添加方的关系。
        for (int index = 0; index < edges.Count; index++)
        {
            // 取关系的另一端作为联系人。
            RoleFriendTable edge = edges[index];
            // 初始视为被添加方，若本角色是主动方则改取对端编号。
            int id = edge.RoleId;
            if (id == call.RoleId) id = edge.FriendId;
            // 首次遇到此角色时建立可附加两个方向标志的行。
            if (!contacts.TryGetValue(id, out GiftFriendInfo info)) { info = new GiftFriendInfo { RoleId = id }; contacts.Add(id, info); }
            if (edge.RoleId == call.RoleId) info.Added = true;
            else info.AddedMe = true;
        }
        if (contacts.Count == 0) return;
        // 本区可见联系人的显示名以数据库当前值为准。
        var ids = new List<int>(contacts.Keys);
        // 只取同区仍存在的联系人，按角色名顺序展示。
        var roles = db.Queryable<RoleTable>().Where(r => r.ServerId == call.RealmId && ids.Contains(r.Id)).OrderBy(r => r.Nickname).ToList();
        // 按当前角色库顺序返回已补全昵称的好友。
        for (int index = 0; index < roles.Count; index++)
        {
            // 用真实角色信息补全列表项。
            GiftFriendInfo info = contacts[roles[index].Id];
            info.Name = roles[index].Nickname;
            call.Friends.Add(info);
        }
    }

    /// <summary>为本区已添加的好友确认收件地址和发件人署名。</summary>
    /// <param name="db">收发双方的角色库。</param><param name="call">来自已登录角色的赠礼请求。</param>
    internal static void PrepareGift(SqlSugarClient db, MailCall call)
    {
        // 好友列表和提交动作使用同一条主动添加关系。
        int target = call.Gift.RoleUid;
        if (target == call.RoleId || !db.Queryable<RoleFriendTable>().Any(f => f.RoleId == call.RoleId && f.FriendId == target))
        { call.Result.Error = "请从已添加的好友中选择收礼人"; return; }
        if (!db.Queryable<RoleTable>().Any(r => r.Id == target && r.ServerId == call.RealmId))
        { call.Result.Error = "收礼角色不存在"; return; }
        // 署名来自服务端角色资料，客户端只提交好友编号和物品实例。
        RoleTable sender = db.Queryable<RoleTable>().Where(r => r.Id == call.RoleId && r.ServerId == call.RealmId).First();
        if (sender == null) { call.Result.Error = "发件角色不存在"; return; }
        call.FriendName = sender.Nickname;
    }
}
