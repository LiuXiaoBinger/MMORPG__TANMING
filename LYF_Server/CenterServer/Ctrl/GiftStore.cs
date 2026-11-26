using System.Collections.Generic;
using SqlSugar;

/// <summary>中心角色库的赠礼联系人，对应 RO 的好友赠礼列表和收件角色查询。</summary>
internal static class GiftStore
{
    /// <summary>从同区角色名添加联系人，或获取玩家已添加以及添加过自己的好友。</summary>
    /// <param name="db">当前区角色库连接。</param><param name="call">游戏服从发起人会话构造的内部请求。</param>
    internal static void GetFriends(SqlSugarClient db, MailCall call)
    {
        // 背包的添加入口和好友面板维护同一条角色关系。
        string name = call.FriendName.Trim();
        if (name.Length > 0)
        {
            // 保留赠礼 RPC 原有的成功和错误回执。
            var add = new FriendCall { RoleId = call.RoleId, RealmId = call.RealmId, Name = name };
            FriendStore.AddFriend(db, add);
            if (add.Error.Length > 0) { call.Result.Error = add.Error; return; }
        }
        call.Friends.Add(FriendStore.GetFriends(db, call.RoleId, call.RealmId));
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
