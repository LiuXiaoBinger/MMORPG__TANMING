using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>从实例怪物的实际死亡事件结算每位在场成员的首领进度及掉落。</summary>
public sealed partial class SceneWaveSpawner
{
    /// <summary>实例选定副本和难度的首领清单，顺序与策划表一致。</summary>
    private readonly List<AwardTable> bossAwards = new List<AwardTable>();

    /// <summary>绑定实例时选出当前难度的奖励行，终绝各层共用同一组首领资格。</summary>
    private void LoadBossAwards()
    {
        bossAwards.Clear();
        // 同一个首领模板可以在不同副本或难度使用不同的永久进度键。
        List<AwardTable> rows = LubanMgr.Instance.ConfigTables.TbAwardTable.DataList;
        // 遍历工作簿中的奖励配置。
        for (int index = 0; index < rows.Count; index++)
        {
            // 只有副本与难度同时匹配的行才参与本实例结算。
            AwardTable row = rows[index];
            if (row.DungeonId == Difficulty.DungeonId && row.DifficultyId == Difficulty.DifficultyId)
                bossAwards.Add(row);
        }
    }

    /// <summary>将服务器 UTC 时刻转换为可存进角色次数的周期起始日序号。</summary>
    /// <param name="utcNow">同一次击杀对全队使用的结算时刻。</param>
    private int BossPeriod(DateTime utcNow)
    {
        // 只有难度表 1（新秀）每日刷新，其余难度分别在每周起点刷新。
        int refreshType = 2;
        if (Difficulty.DifficultyId == 1) refreshType = 1;
        return (int)(RoleCountComponent.PeriodStart(utcNow, refreshType).Ticks / TimeSpan.TicksPerDay);
    }

    /// <summary>匹配本次死亡的首领，依次结算本实例当时在场的队员。</summary>
    /// <param name="entityId">刚刚从存活变为死亡的怪物模板号。</param>
    private void SettleBossDeath(int entityId)
    {
        // 遍历本难度的首领数，普通怪物和流程终结器不会命中奖励行。
        for (int index = 0; index < bossAwards.Count; index++)
        {
            // 此行的永久 ID 标识一个副本、难度和首领。
            AwardTable award = bossAwards[index];
            if (award.BossId != entityId) continue;
            // 同次击杀即使恰逢刷新边界，全队也使用同一个周期。
            int period = BossPeriod(DateTime.UtcNow);
            // 实例已有成员筛选同时核对地图与实例号。
            List<OnlineRole> players = Players;
            // 每人的背包和击杀历史单独结算。
            for (int member = 0; member < players.Count; member++)
                AwardBossToRole(players[member], award, period);
            return;
        }
    }

    /// <summary>先记下击杀进度，实际入包成功后再消耗该首领本周期的掉落资格。</summary>
    /// <param name="role">击杀时在场的角色。</param><param name="award">匹配的首领难度行。</param><param name="period">本次周期起始日序号。</param>
    private void AwardBossToRole(OnlineRole role, AwardTable award, int period)
    {
        // 进度与入包记录共用角色原有持久化保存链路。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        if (!counts.SetCount(RoleCountAction.DungeonBossKill, award.Id, period)) return;
        // 本次单播先填入击杀后的完整进度，重复击杀也只显示已有进度。
        DungeonBossProgressNtf notice = BossProgress(counts, period);
        notice.BossName = award.BossName;
        if (counts.GetCount(RoleCountAction.DungeonBossDrop, award.Id) != period)
        {
            // 一个包对应一次完整的 AddItem，空间不足时不会先发半包。
            AwardPackTable pack = LubanMgr.Instance.ConfigTables.TbAwardPackTable.Get(award.PackId);
            // 正式角色背包负责堆叠、容量、增量通知及保存。
            RoItemComponent items = role.GetComponent<RoItemComponent>();
            // 掉落沿用项目现有副本结算变更原因。
            ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_DUNGEONS_RESULT);
            if (items.AddItem(pack.ItemId, pack.ItemCount, reason, ItemSign.IsNoSign, 0, 0,
                UpdateType.eUT_Update_All, default(IdipParam)) != null)
            {
                counts.SetCount(RoleCountAction.DungeonBossDrop, award.Id, period);
                notice.ItemId = pack.ItemId;
                notice.ItemCount = pack.ItemCount;
                items.NotifyItemChanges();
            }
            else notice.BagFull = true;
        }
        role.SendToClient(NetDefine.CMD_DungeonBossProgress, notice.ToByteString());
    }

    /// <summary>根据该角色持久进度重建本周期已击杀首领，供入场和击杀通知共用。</summary>
    /// <param name="counts">接收者自己的角色次数组件。</param><param name="period">当前难度周期起始日序号。</param>
    private DungeonBossProgressNtf BossProgress(RoleCountComponent counts, int period)
    {
        // 完整快照中的总数取决于登记的战斗首领数。
        DungeonBossProgressNtf notice = new DungeonBossProgressNtf { InstanceId = instanceId, BossTotal = bossAwards.Count };
        // 按表中顺序列出本周期已完成的首领。
        for (int index = 0; index < bossAwards.Count; index++)
        {
            // 资格键跨实例和角色存档稳定。
            AwardTable award = bossAwards[index];
            if (counts.GetCount(RoleCountAction.DungeonBossKill, award.Id) == period)
                notice.KilledBosses.Add(award.BossName);
        }
        return notice;
    }

    /// <summary>在入场通知后给本人恢复本周期已击杀的首领。</summary>
    /// <param name="role">刚成功进入或重新进入该实例的成员。</param>
    internal void SendBossProgress(OnlineRole role)
    {
        if (bossAwards.Count == 0) return;
        // 入场时从角色记录取当前周期，不能使用队长的进度。
        DungeonBossProgressNtf notice = BossProgress(role.GetComponent<RoleCountComponent>(), BossPeriod(DateTime.UtcNow));
        role.SendToClient(NetDefine.CMD_DungeonBossProgress, notice.ToByteString());
    }
}
