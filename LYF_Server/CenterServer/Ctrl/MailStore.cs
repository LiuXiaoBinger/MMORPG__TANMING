using System;
using System.Collections.Generic;
using Google.Protobuf;
using SqlSugar;

/// <summary>对应 RO 的 DB 邮件操作。框架通过 IContainer 把 MS 内网请求交给中心库。</summary>
internal sealed class MailStore : IContainer
{
    /// <summary>复用本中心服部署配置，每个网络调用使用独立数据库连接。</summary>
    private readonly ConnectionConfig config;

    /// <summary>绑定角色库连接配置。</summary><param name="config">中心服已建表的数据库配置。</param>
    internal MailStore(ConnectionConfig config) { this.config = config; }

    /// <summary>表结构由中心服启动时的 CodeFirst 建立。</summary>
    public void OnInit() { }

    /// <summary>处理 MS 的存取并保留原游戏服、网关与玩家会话。</summary>
    /// <param name="link">MS 的内部连接。</param><param name="packet">包含收件地址和操作的内网包。</param>
    public void OnServerCommand(BaseLink link, BasePackage packet)
    {
        // 身份和邮件参数均由上游游戏服务确定。
        MailCall call = MailCall.Parser.ParseFrom(packet.Data);
        using (SqlSugarClient db = new SqlSugarClient(config))
            Execute(db, call);
        link.Send(packet, MailCode.MailOperation, call.ToByteString());
    }

    /// <summary>中心服在该链路只接受 MS 请求。</summary>
    /// <param name="link">框架连接。</param><param name="packet">框架协议包。</param>
    public void OnClientCommand(BaseLink link, BasePackage packet) { }

    /// <summary>按请求只取摘要、单封内容或指定角色的可领取记录。</summary>
    /// <param name="db">本次调用独占的中心库连接。</param><param name="call">原请求，原地填入回执。</param>
    internal static void Execute(SqlSugarClient db, MailCall call)
    {
        // 服务端统一判定有效期，客户端时钟不参与奖励结算。
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        call.Result = new MailOpRes { MailUid = call.MailUid, OpType = call.OpType };
        if (call.Command == MailCode.GsSendMail)
        {
            SendMail(db, call, now);
            return;
        }
        // 可见信件始终限定于服务端会话绑定的收件人。
        var inbox = db.Queryable<RoleMailTable>().Where(m => m.RoleId == call.RoleId && m.ExpireTime > now);
        if (call.Command == MailCode.UpdateMailNtf || call.Command == MailCode.GetMailList)
        {
            call.HasUnread = inbox.Clone().Any(m => !m.IsRead);
            if (call.Command == MailCode.UpdateMailNtf) return;
            // 摘要查询在 SQL 层排除正文大字段，取最近的一百封。
            var rows = inbox.OrderBy(m => m.MailUid, OrderByType.Desc).Take(MailCode.MaxClientMailCount)
                .IgnoreColumns(m => m.Content).ToList();
            for (int i = 0; i < rows.Count; i++)
                call.Mails.Add(new MailInfo { BaseInfo = rows[i].ToBaseInfo() });
            return;
        }
        if (call.Command == MailCode.GetOneMail)
        {
            // 正文按收件角色和指定 UID 定位。
            RoleMailTable row = inbox.Where(m => m.MailUid == call.MailUid).First();
            if (row == null) call.Result.Error = "邮件不存在或已过期";
            else call.Mails.Add(Detail(row));
            return;
        }
        switch (call.OpType)
        {
            case MailOpType.MailRead:
            case MailOpType.MailReadAll:
                // 单封和一键领取共用查询，无附件信在库内即可标记已读。
                var unread = inbox.Where(m => !m.IsRead);
                if (call.OpType == MailOpType.MailRead) unread = unread.Where(m => m.MailUid == call.MailUid);
                // RO 先取最早至多一百封未读信，再按客户端优先级逐封领取。
                var candidates = unread.OrderBy(m => m.MailUid).Take(MailCode.MaxClientMailCount).ToList();
                candidates.Sort(CompareUnread);
                for (int i = 0; i < candidates.Count; i++)
                {
                    // 含附件的信交游戏服和背包一起提交。
                    RoleMailTable candidate = candidates[i];
                    if (candidate.HasItem)
                    {
                        // 发奖 RPC 只需要收件编号、到期时间和附件，批领时不用重复转发整批正文。
                        MailInfo prize = new MailInfo { BaseInfo = new MailBaseInfo { MailUid = candidate.MailUid,
                            ExpireTime = candidate.ExpireTime }, ContentInfo = new MailContent() };
                        prize.ContentInfo.MailItems.Add(MailContent.Parser.ParseFrom(candidate.Content).MailItems);
                        call.Mails.Add(prize);
                    }
                    else call.Result.ReadMailUids.Add(candidate.MailUid);
                }
                if (call.Result.ReadMailUids.Count > 0)
                {
                    // 仅更新当前请求选中的纯文本邮件。
                    var ids = new List<long>(call.Result.ReadMailUids);
                    db.Updateable<RoleMailTable>().SetColumns(m => m.IsRead == true)
                        .Where(m => m.RoleId == call.RoleId && ids.Contains(m.MailUid)).ExecuteCommand();
                }
                if (candidates.Count == 0 && call.OpType == MailOpType.MailRead)
                    call.Result.Error = "邮件已处理或已过期";
                break;
            case MailOpType.MailDel:
                // 有未领附件的信不得单删，已过期的信不会再进入收件箱。
                int removed = db.Deleteable<RoleMailTable>().Where(m => m.RoleId == call.RoleId && m.MailUid == call.MailUid)
                    .Where(m => m.IsRead || !m.HasItem).ExecuteCommand();
                if (removed == 0) call.Result.Error = "邮件不存在或仍有附件待领取";
                break;
            case MailOpType.MailDelAll:
                db.Deleteable<RoleMailTable>().Where(m => m.RoleId == call.RoleId && m.IsRead).ExecuteCommand();
                break;
            default:
                call.Result.Error = "未知邮件操作";
                break;
        }
    }

    /// <summary>从存储字节还原单封信，附件来源与持久化内容一致。</summary><param name="row">数据库收件记录。</param>
    private static MailInfo Detail(RoleMailTable row)
    {
        return new MailInfo { BaseInfo = row.ToBaseInfo(), ContentInfo = MailContent.Parser.ParseFrom(row.Content) };
    }

    /// <summary>未读信中的重要邮件优先，相同优先级从新到旧领取。</summary>
    /// <param name="left">参与排序的一封信。</param><param name="right">与其比较的另一封信。</param>
    private static int CompareUnread(RoleMailTable left, RoleMailTable right)
    {
        // 邮件 UID 的产生顺序可区分同秒投递。
        int priority = right.Important.CompareTo(left.Important);
        if (priority != 0) return priority;
        return right.MailUid.CompareTo(left.MailUid);
    }

    /// <summary>向指定角色或本区发信时已存在的角色投递，离线角色同样获得持久记录。</summary>
    /// <param name="db">本次投递独占的数据库连接。</param><param name="call">来自业务系统的收件范围和信件。</param>
    /// <param name="now">本次投递的 UTC Unix 秒。</param>
    private static void SendMail(SqlSugarClient db, MailCall call, long now)
    {
        // 模板与自定义信共用投递和附件持久化。
        SMailTemplate template = call.SendMail;
        if (template == null || template.Content == null || template.ExpireDays <= 0)
        {
            call.Result.Error = "邮件缺少正文或有效期";
            return;
        }
        // 个人信同样校验区服，避免共享中心向其他区的角色投递。
        var roles = db.Queryable<RoleTable>().Where(r => r.ServerId == call.RealmId);
        if (call.RoleId != 0) roles = roles.Where(r => r.Id == call.RoleId);
        // 这批角色在投递时已经存在，之后新建的角色不会收到历史全服信。
        var ids = roles.OrderBy(r => r.Id).Select(r => r.Id).ToList();
        if (ids.Count == 0 && call.RoleId != 0)
        {
            call.Result.Error = "收件角色不存在";
            return;
        }
        // 相同正文只序列化一次，每个角色独立维护阅读状态。
        byte[] content = template.Content.ToByteArray();
        // 批量插入由同一事务提交，避免全服信只投递一部分。
        var rows = new List<RoleMailTable>(ids.Count);
        for (int i = 0; i < ids.Count; i++)
            rows.Add(new RoleMailTable { MailUid = UIDHelper.CreateUID(UIDType.UID_Mail), RoleId = ids[i],
                MailId = template.MailId, Title = template.Title, Important = template.Important,
                Content = content, HasItem = template.Content.MailItems.Count > 0, CreateTime = now,
                ExpireTime = now + 86400L * template.ExpireDays });
        if (rows.Count > 0)
        {
            // 多批写入由一个事务覆盖，失败原因沿原发信回执返回。
            var result = db.Ado.UseTran(() =>
            {
                for (int i = 0; i < ids.Count; i++) MakeRoom(db, ids[i], now);
                db.Insertable(rows).ExecuteCommand();
            });
            if (!result.IsSuccess) call.Result.Error = "邮件投递入库失败：" + result.ErrorMessage;
        }
    }

    /// <summary>依照 RO 的收件箱容量优先淘汰已读和普通信，保留未领附件至硬上限。</summary>
    /// <param name="db">该次投递事务。</param><param name="roleId">收件角色主键。</param>
    /// <param name="now">当前 UTC Unix 秒，用于判定可见信件。</param>
    private static void MakeRoom(SqlSugarClient db, int roleId, long now)
    {
        // 同角色的并发投递按角色行串行，整区投递以角色主键顺序取锁。
        db.Ado.GetInt("SELECT Id FROM role WHERE Id=@id FOR UPDATE", new SugarParameter("@id", roleId));
        // 过期的普通信和已结算信已无待处理附件，可随投递回收。
        db.Deleteable<RoleMailTable>().Where(m => m.RoleId == roleId && m.ExpireTime <= now)
            .Where(m => m.IsRead || !m.HasItem).ExecuteCommand();
        // 只计算仍可见的邮件，已过期的信自动让出位置。
        var active = db.Queryable<RoleMailTable>().Where(m => m.RoleId == roleId && m.ExpireTime > now);
        // 大多数邮箱在软容量以内，无需查找或更新旧信。
        int count = active.Clone().Count();
        if (count < MailCode.MaxClientMailCount) return;
        // 优先淘汰最早的已读信，包含附件已到账的邮件。
        long uid = active.Clone().Where(m => m.IsRead).OrderBy(m => m.MailUid).Select(m => m.MailUid).First();
        if (uid == 0)
        {
            // RO 只在最早的一百封中寻找无附件邮件。
            var oldest = active.OrderBy(m => m.MailUid).Take(MailCode.MaxClientMailCount)
                .Select(m => new { m.MailUid, m.HasItem }).ToList();
            for (int i = 0; i < oldest.Count; i++)
            {
                if (oldest[i].HasItem) continue;
                uid = oldest[i].MailUid;
                break;
            }
            if (uid == 0 && count >= MailCode.MailCountLimit) uid = oldest[0].MailUid;
        }
        if (uid == 0) return;
        // 普通信或已结算的行可直接删除。
        if (db.Deleteable<RoleMailTable>().Where(m => m.MailUid == uid).Where(m => m.IsRead || !m.HasItem).ExecuteCommand() > 0) return;
        // 硬上限淘汰未领附件时立即移出收件箱，保留原行供已送达 GS 的领取回执使用。
        db.Updateable<RoleMailTable>().SetColumns(m => m.ExpireTime == now).Where(m => m.MailUid == uid).ExecuteCommand();
    }

    /// <summary>在角色背包已开启的同一事务内确认附件，失败时由原存档事务整体回滚。</summary>
    /// <param name="db">当前角色存档事务。</param><param name="request">背包快照和本批实际到账的邮件 UID。</param>
    internal static void CommitClaims(SqlSugarClient db, SaveRoleDataReq request)
    {
        if (request.ClaimedMailUids.Count == 0) return;
        // 仅处理本角色的未领附件，条件更新阻止重发产生第二份奖励。
        var ids = new List<long>(request.ClaimedMailUids);
        // 每份附件必须与一条可领取的信对应。
        int changed = db.Updateable<RoleMailTable>().SetColumns(m => m.IsRead == true)
            .Where(m => m.RoleId == request.RoleId && !m.IsRead && m.HasItem)
            .Where(m => ids.Contains(m.MailUid)).ExecuteCommand();
        if (changed != ids.Count) throw new InvalidOperationException("邮件附件与待提交背包不一致");
    }

    /// <summary>同版本背包重发时确认这批信没有仍待领取的记录，玩家可在成功后正常删信。</summary>
    /// <param name="db">核对权威版本的角色存档事务。</param><param name="request">重放的存档和邮件编号。</param>
    internal static bool ClaimsCommitted(SqlSugarClient db, SaveRoleDataReq request)
    {
        if (request.ClaimedMailUids.Count == 0) return true;
        // 对该版本实际声称已入包的邮件逐批核对。
        var ids = new List<long>(request.ClaimedMailUids);
        return !db.Queryable<RoleMailTable>().Any(m => m.RoleId == request.RoleId && !m.IsRead && ids.Contains(m.MailUid));
    }
}
