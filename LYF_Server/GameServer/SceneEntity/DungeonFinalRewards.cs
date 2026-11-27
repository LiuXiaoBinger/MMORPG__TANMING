using System;
using System.Collections.Generic;
using System.IO;

/// <summary>终绝在完整胜利时登记成绩。北京周五跨周后，由玩家从上周页打开该角色唯一的宝箱。</summary>
internal static class DungeonFinalRewards
{
    /// <summary>正式周箱配置内嵌在游戏服中，内容与客户端预览资源相同。</summary>
    private static readonly DungeonFinalRewardTier[] Tiers = Load();

    /// <summary>读取编译时收录的周箱配置。</summary>
    private static DungeonFinalRewardTier[] Load()
    {
        using (StreamReader reader = new StreamReader(typeof(DungeonFinalRewards).Assembly.GetManifestResourceStream("Dungeons.FinalWeeklyRewards.xml")))
            return DungeonFinalRewardTier.Read(reader.ReadToEnd());
    }

    /// <summary>把真实整层胜利记给当时在同一实例内的角色。相同基础副本只占一种。</summary>
    /// <param name="roles">实例过滤后的在场角色。</param><param name="dungeon">基础副本编号。</param>
    /// <param name="floor">实际通关层数。</param><param name="utc">服务端确认胜利时间。</param>
    internal static void Record(List<OnlineRole> roles, int dungeon, int floor, DateTime utc)
    {
        // 同场所有成员采用同一周界。
        int week = DungeonFinalWeek.Period(utc);
        for (int index = 0; index < roles.Count; index++)
        {
            // 按个人存档更新最高层与不同副本的种数。
            OnlineRole role = roles[index];
            RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
            bool improved = floor > counts.GetCount((RoleCountAction)DungeonFinalWeek.Highest, week);
            bool firstDungeon = counts.GetCount((RoleCountAction)DungeonFinalWeek.DungeonSeen, dungeon) != week;
            if (!improved && !firstDungeon) continue;
            if (improved) counts.SetCount((RoleCountAction)DungeonFinalWeek.Highest, week, floor);
            if (firstDungeon)
            {
                counts.SetCount((RoleCountAction)DungeonFinalWeek.DungeonSeen, dungeon, week);
                long previous = counts.GetCount((RoleCountAction)DungeonFinalWeek.Distinct, week);
                counts.SetCount((RoleCountAction)DungeonFinalWeek.Distinct, week, previous + 1);
            }
            role.SaveDungeonFinalAssets();
        }
    }

    /// <summary>用游戏服当前周、最高层和已领记录决定一次实际入包，旧期成绩可离线保留。</summary>
    /// <param name="role">由网关会话解析的领取角色。</param><param name="week">玩家当前查看的已结束周键。</param>
    /// <param name="utc">游戏服受理领取的时间。</param>
    internal static DungeonFinalClaimRet Claim(OnlineRole role, int week, DateTime utc)
    {
        // 结果始终回到发起角色；客户端无法指定奖励档位或件数。
        DungeonFinalClaimRet reply = new DungeonFinalClaimRet { Week = week };
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        if (week <= 0 || week >= DungeonFinalWeek.Period(utc)) { reply.Message = "本期尚未结算"; return reply; }
        if (counts.GetCount((RoleCountAction)DungeonFinalWeek.Claimed, week) > 0) { reply.Message = "该周宝箱已领取"; return reply; }
        DungeonFinalRewardTier tier = DungeonFinalRewardTier.ForFloor(Tiers, (int)counts.GetCount((RoleCountAction)DungeonFinalWeek.Highest, week));
        if (tier == null) { reply.Message = "该周没有终绝通关记录"; return reply; }
        // 固定角色与周期的抽取序列，背包空间不足后重试不会改变内容。
        Random random = new Random(unchecked(role.mainRoleInfo.BaseInfo.RoleId * 397 ^ week));
        MailContent contents = new MailContent();
        for (int index = 0; index < tier.Fixed.Length; index++) contents.MailItems.Add(tier.Fixed[index].Clone());
        for (int index = 0; index < tier.Draws; index++)
        {
            // 同种材料合并数量，开箱结果与背包变更一一对应。
            MailItem drawn = tier.Pool[random.Next(tier.Pool.Length)];
            int at = 0;
            while (at < contents.MailItems.Count && contents.MailItems[at].ItemId != drawn.ItemId) at++;
            if (at == contents.MailItems.Count) contents.MailItems.Add(drawn.Clone());
            else contents.MailItems[at].Count += drawn.Count;
        }
        // 沿用附件发放已有的整批容量检查，背包和次数通过同一份角色保存提交。
        RoItemComponent bag = role.GetComponent<RoItemComponent>();
        // 将抽中的物品换成背包的整批发放参数，使用副本来源记录这次变动。
        List<RewardItemAmount> rewards = new List<RewardItemAmount>(contents.MailItems.Count);
        for (int index = 0; index < contents.MailItems.Count; index++)
        {
            MailItem item = contents.MailItems[index];
            rewards.Add(new RewardItemAmount { ItemId = item.ItemId, Count = item.Count, IsBind = item.IsBind });
        }
        if (!bag.TryAddRewards(rewards, new ChangeReason(ItemChangeReason.ITEM_REASON_DUNGEONS_RESULT), UpdateType.eUT_Update_All)) { reply.Message = "背包空间不足，腾出空间后可重新领取"; return reply; }
        counts.SetCount((RoleCountAction)DungeonFinalWeek.Claimed, week, 1);
        bag.NotifyItemChanges();
        role.SaveDungeonFinalAssets();
        reply.Success = true;
        reply.Message = "已开启终绝" + tier.Name + "宝箱";
        reply.Items.AddRange(contents.MailItems);
        return reply;
    }
}
