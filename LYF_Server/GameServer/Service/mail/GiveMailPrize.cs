using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>对应 RO 的 GS GiveMailPrize。每封信完整入包后，随角色背包版本提交已领状态。</summary>
public sealed class GiveMailPrize : RoleComponentBase
{
    /// <summary>本次在线期间已实际入包的邮件，覆盖重复点击产生的在途回包。</summary>
    private readonly HashSet<long> received = new HashSet<long>();
    /// <summary>已经入包、等待随下一版背包送入角色库的信件编号。</summary>
    private readonly List<long> pendingIds = new List<long>();
    /// <summary>下一版背包成功保存后才应送达客户端的领取回执。</summary>
    private readonly List<MailOpRes> pendingReplies = new List<MailOpRes>();
    /// <summary>唯一在途存档对应的领取回执。</summary>
    private readonly List<MailOpRes> savingReplies = new List<MailOpRes>();

    /// <summary>将附件发给拥有此组件的在线角色。</summary><param name="owner">该游戏服当前持有的角色。</param>
    public GiveMailPrize(OnlineRole owner) : base(owner) { }

    /// <summary>按信逐封核对容量；一键领取回执仅列出实际成功的邮件。</summary>
    /// <param name="call">MS 从收件人数据库记录读取的附件和已处理的纯文本信。</param>
    public void Give(MailCall call)
    {
        // 文本信已由中心服标记；有附件的信须等背包事务完成。
        MailOpRes reply = call.Result;
        // 当前角色唯一的权威背包组件。
        RoItemComponent bag = Owner.GetComponent<RoItemComponent>();
        // 本批回执是否需要等待新的背包写入。
        bool added = false;
        for (int i = 0; i < call.Mails.Count; i++)
        {
            // 每封信是最小领取单位，单封内不能部分到账。
            MailInfo mail = call.Mails[i];
            // 领取和失效两个时点均按服务端 UTC 秒判定。
            long uid = mail.BaseInfo.MailUid;
            if (received.Contains(uid)) continue;
            if (mail.BaseInfo.ExpireTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                reply.Error = "邮件已过期";
                continue;
            }
            if (!GiveItems(bag, mail.ContentInfo))
            {
                reply.Error = "附件配置无效或背包空间不足";
                continue;
            }
            received.Add(uid);
            pendingIds.Add(uid);
            reply.ReadMailUids.Add(uid);
            added = true;
        }
        if (added)
        {
            pendingReplies.Add(reply);
            Owner.SaveMailAssets();
        }
        else Owner.SendToClient(MailCode.MailOp, reply.ToByteString());
    }

    /// <summary>将一封信的附件交给与副本共用的整批入包检查，保留原有绑定与更新方式。</summary>
    /// <param name="bag">已装载的收件人背包。</param><param name="content">中心服读取的完整附件。</param>
    private static bool GiveItems(RoItemComponent bag, MailContent content)
    {
        // 原附件顺序和绑定信息随同传给背包。
        List<RewardItemAmount> rewards = new List<RewardItemAmount>(content.MailItems.Count);
        for (int index = 0; index < content.MailItems.Count; index++)
        {
            // 单条邮件附件对应同一条背包发放内容。
            MailItem item = content.MailItems[index];
            rewards.Add(new RewardItemAmount { ItemId = item.ItemId, Count = item.Count, IsBind = item.IsBind });
        }
        return bag.TryAddRewards(rewards, new ChangeReason(), UpdateType.eUT_Normal);
    }

    /// <summary>至少一封已入包的信需要与背包一同持久化。</summary>
    public override bool NeedSave() { return pendingIds.Count > 0; }

    /// <summary>把本批邮件编号附到同版本背包快照，并保留操作回执至提交确认。</summary>
    /// <param name="fieldManager">角色所有组件共用的存档请求。</param><param name="saveVersion">这批背包的存档版本。</param>
    public override bool Save(RoleDataFieldManager fieldManager, long saveVersion)
    {
        fieldManager.GetRequest().ClaimedMailUids.Add(pendingIds);
        savingReplies.AddRange(pendingReplies);
        pendingIds.Clear();
        pendingReplies.Clear();
        return true;
    }

    /// <summary>中心服确认物品和信件均已入库后，向玩家交付各次领取结果。</summary>
    /// <param name="saveVersion">角色发送器已核对的存档版本。</param>
    public override void OnSaveAcknowledged(long saveVersion)
    {
        if (Owner.State == OnlineRoleState.Online)
            for (int i = 0; i < savingReplies.Count; i++)
                Owner.SendToClient(MailCode.MailOp, savingReplies[i].ToByteString());
        savingReplies.Clear();
    }
}
