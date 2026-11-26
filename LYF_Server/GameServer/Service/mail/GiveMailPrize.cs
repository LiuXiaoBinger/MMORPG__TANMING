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

    /// <summary>按类型、绑定状态和配置堆叠上限合计全部格数，再沿现有 AddItem 发放。</summary>
    /// <param name="bag">已装载的在线角色背包。</param><param name="content">一封信的完整附件列表。</param>
    private static bool GiveItems(RoItemComponent bag, MailContent content)
    {
        // 同物品与绑定状态共享旧堆和新堆，避免不同附件重复使用同一空位。
        var quantities = new Dictionary<(int Id, ItemSign Sign), long>();
        // 保留第一次出现的附件顺序供容量核算。
        var groups = new List<(int Id, ItemSign Sign)>();
        for (int i = 0; i < content.MailItems.Count; i++)
        {
            // 业务系统写入的单项奖励。
            MailItem item = content.MailItems[i];
            if (item.ItemId <= 0 || item.Count <= 0) return false;
            // 绑定状态参与现有背包的堆叠判定。
            ItemSign sign = ItemSign.IsNoSign;
            if (item.IsBind) sign = ItemSign.IsItemSignBind;
            // 同配置编号的绑定和非绑定奖励分别计算容量。
            var key = (item.ItemId, sign);
            if (!quantities.ContainsKey(key)) { groups.Add(key); quantities.Add(key, 0); }
            quantities[key] += item.Count;
        }
        // 不同种类也可能进入同类背包，累加它们占用的新格。
        var slots = new Dictionary<KnapsackType, long>();
        for (int i = 0; i < groups.Count; i++)
        {
            // 本组共享堆叠条件。
            var group = groups[i];
            // 通过本项目物品表决定容器和单格容量。
            ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(group.Id);
            if (config == null) return false;
            // 与 AddItem 的 GetDefineContiner 使用同一背包分类。
            RoItemContainer container = bag.GetContainer(config.PackType);
            if (container == null) return false;
            // 每格至少一件。
            int stack = Math.Max(1, config.MaxStackCount);
            // 扣除同类型旧堆还能容纳的件数。
            long remaining = quantities[group];
            // 快照只复制当前容器内物品的引用。
            var items = new List<ItemBase>(container.GetItemMap().Values);
            for (int j = 0; j < items.Count; j++)
                if (items[j].GetItemID() == group.Id && items[j].GetItemSign() == (int)group.Sign)
                    remaining -= Math.Max(0, stack - items[j].GetItemCount());
            // 此容器此前各组已占用的新格数。
            slots.TryGetValue(config.PackType, out long used);
            used += (Math.Max(0L, remaining) + stack - 1) / stack;
            if (used > container.GetEmptySize()) return false;
            slots[config.PackType] = used;
        }
        for (int i = 0; i < content.MailItems.Count; i++)
        {
            // 容量已在同一逻辑线程整封核对，沿正常通知、脏标记及装备实例生成路径发放。
            MailItem item = content.MailItems[i];
            // 保留该附件的绑定状态。
            ItemSign sign = ItemSign.IsNoSign;
            if (item.IsBind) sign = ItemSign.IsItemSignBind;
            bag.AddItem(item.ItemId, item.Count, new ChangeReason(), sign, 0, 0, UpdateType.eUT_Normal, new IdipParam());
        }
        return true;
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
