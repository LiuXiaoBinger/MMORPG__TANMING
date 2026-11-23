using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

/// <summary>读取秘笈目录，在角色主循环内结算出战、培养及角色属性。</summary>
internal static class MartialManualSystem
{
    /// <summary>由游戏逻辑线程用于命中被动的百分比判定。</summary>
    private static readonly Random HitRoll = new Random();

    /// <summary>随游戏服加载的秘笈目录，配置顺序也是客户端目录顺序。</summary>
    private static readonly ManualCatalog Catalog = JsonParser.Default.Parse<ManualCatalog>(
        File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "MartialManuals.json")));

    /// <summary>从角色存档读取等级，首次领悟的起点由目录配置。</summary>
    /// <param name="counts">角色永久记录。</param><param name="book">当前书籍。</param>
    private static int Level(RoleCountComponent counts, ManualBook book)
    {
        return Math.Max(book.StartLevel, (int)counts.GetCount(RoleCountAction.ManualLevel, book.Id));
    }

    /// <summary>选中的方案从一开始编号。</summary>
    /// <param name="counts">当前角色记录。</param>
    private static int Scheme(RoleCountComponent counts)
    {
        return Math.Max(1, (int)counts.GetCount(RoleCountAction.ManualScheme, 1));
    }

    /// <summary>将方案与槽位组成持久化键，每个方案预留五个展示位置。</summary>
    /// <param name="scheme">从一开始的方案号。</param><param name="index">从零开始的槽序。</param>
    private static int SlotKey(int scheme, int index) { return (scheme - 1) * 5 + index + 1; }

    /// <summary>按稳定配置编号查找书籍。</summary><param name="id">目录编号。</param>
    private static ManualBook Find(int id)
    {
        for (int i = 0; i < Catalog.Books.Count; i++)
            if (Catalog.Books[i].Id == id) return Catalog.Books[i];
        return null;
    }

    /// <summary>执行一次读取或写入，再从背包与持久记录产生回包。</summary>
    /// <param name="role">已完成载入的角色。</param><param name="request">玩家操作。</param>
    internal static ManualReply Execute(OnlineRole role, ManualRequest request)
    {
        // 等级、阶数和方案随角色计数模块一起保存。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        // 培养的材料与铜币均读取既有背包。
        RoItemComponent items = role.GetComponent<RoItemComponent>();
        // 一次请求只走一个写入入口，空错误消息表示成功。
        string error = "";
        if (request.Action == ManualAction.ManualSave) error = SaveSlots(counts, request);
        else if (request.Action == ManualAction.ManualSelectScheme)
        {
            if (request.Scheme < 1 || request.Scheme > Catalog.Schemes.Count) error = "出战方案无效";
            else counts.SetCount(RoleCountAction.ManualScheme, 1, request.Scheme);
        }
        else if (request.Action != ManualAction.ManualRead) error = Train(counts, items, request);
        if (error.Length == 0 && request.Action != ManualAction.ManualRead)
            role.GetComponent<RoleAttributeComponent>().RefreshManualAttributes();
        // 返回成功与失败后的真实数值，客户端可以据此纠正预览。
        ManualReply reply = new ManualReply { RoleId = role.mainRoleInfo.BaseInfo.RoleId, Catalog = Catalog,
            Scheme = Scheme(counts), Action = request.Action, Success = error.Length == 0, Message = error,
            Copper = items.GetVirtualItemCount((int)ItemEnum.ZENY) };
        for (int i = 0; i < Catalog.OpenSlots; i++)
            reply.Slots.Add((int)counts.GetCount(RoleCountAction.ManualSlot, SlotKey(reply.Scheme, i)));
        for (int i = 0; i < Catalog.Books.Count; i++)
        {
            // 收集一本书的进度与本目录涉及的材料余量。
            ManualBook book = Catalog.Books[i];
            reply.Progress.Add(new ManualProgress { Book = book.Id, Level = Level(counts, book),
                Rank = (int)counts.GetCount(RoleCountAction.ManualRank, book.Id),
                Insights = (int)counts.GetCount(RoleCountAction.ManualInsight, book.Id) });
            AddMaterial(reply, items, book.Learn);
            for (int step = 0; step < book.Upgrades.Count; step++) AddMaterial(reply, items, book.Upgrades[step]);
            for (int step = 0; step < book.Ranks.Count; step++) AddMaterial(reply, items, book.Ranks[step]);
            for (int step = 0; step < book.Insights.Count; step++) AddMaterial(reply, items, book.Insights[step]);
        }
        return reply;
    }

    /// <summary>相同材料只查询一次。</summary>
    /// <param name="reply">正在生成的回包。</param><param name="items">角色背包。</param><param name="cost">一步消耗。</param>
    private static void AddMaterial(ManualReply reply, RoItemComponent items, ManualCost cost)
    {
        if (cost == null || cost.Item <= 0 || reply.Materials.ContainsKey(cost.Item)) return;
        reply.Materials.Add(cost.Item, Math.Max(0, items.GetItemNum(cost.Item)));
    }

    /// <summary>先验证所有槽，再一次性替换方案，零表示空槽。</summary>
    /// <param name="counts">角色永久记录。</param><param name="request">方案编号与排好序的槽。</param>
    private static string SaveSlots(RoleCountComponent counts, ManualRequest request)
    {
        if (request.Scheme < 1 || request.Scheme > Catalog.Schemes.Count || request.Slots.Count != Catalog.OpenSlots)
            return "出战方案无效";
        // 单个方案内同一本书只能出现一次。
        HashSet<int> seen = new HashSet<int>();
        for (int i = 0; i < request.Slots.Count; i++)
        {
            if (request.Slots[i] == 0) continue;
            // 检查书籍存在、已领悟且没有重复。
            ManualBook book = Find(request.Slots[i]);
            if (book == null || Level(counts, book) < 1 || !seen.Add(book.Id)) return "请装备已领悟且未重复的秘笈";
        }
        for (int i = 0; i < request.Slots.Count; i++)
            counts.SetCount(RoleCountAction.ManualSlot, SlotKey(request.Scheme, i), request.Slots[i]);
        counts.SetCount(RoleCountAction.ManualScheme, 1, request.Scheme);
        return "";
    }

    /// <summary>先核对当前进度、材料和铜币，再提升一档。</summary>
    /// <param name="counts">负责持久化的角色记录。</param><param name="items">权威背包。</param><param name="request">所选培养操作。</param>
    private static string Train(RoleCountComponent counts, RoItemComponent items, ManualRequest request)
    {
        // 配置决定所需材料、等级与费用。
        ManualBook book = Find(request.Book);
        if (book == null) return "未收录这本秘笈";
        // 与点击时界面数据比对，避免连续点击重复购买同一级。
        int level = Level(counts, book);
        int rank = (int)counts.GetCount(RoleCountAction.ManualRank, book.Id);
        int insight = (int)counts.GetCount(RoleCountAction.ManualInsight, book.Id);
        if (request.Level != level || request.Rank != rank || request.Insights != insight) return "秘笈进度已更新";
        // 写入哪种永久记录，以及该记录的目标值。
        RoleCountAction field = RoleCountAction.ManualLevel;
        int next = level + 1;
        // 缺少下一档时不收取资源。
        ManualCost cost = null;
        if (level == 0 && request.Action == ManualAction.ManualLearn) cost = book.Learn;
        if (level > 0)
        {
            switch (request.Action)
            {
                case ManualAction.ManualUpgrade:
                    if (level <= book.Upgrades.Count) cost = book.Upgrades[level - 1];
                    break;
                case ManualAction.ManualRank:
                    field = RoleCountAction.ManualRank;
                    next = rank + 1;
                    if (rank < book.Ranks.Count) cost = book.Ranks[rank];
                    break;
                case ManualAction.ManualInsight:
                    field = RoleCountAction.ManualInsight;
                    next = insight + 1;
                    if (insight < book.Insights.Count) cost = book.Insights[insight];
                    break;
            }
        }
        if (cost == null) return "当前阶段尚未开放";
        if (level < cost.Level) return "需要秘笈达到" + cost.Level + "级";
        if (cost.Amount > 0 && items.GetItemNum(cost.Item) < cost.Amount) return "材料不足";
        if (items.GetVirtualItemCount((int)ItemEnum.ZENY) < cost.Copper) return "铜币不足";
        if (cost.Amount > 0 && items.ReduceItem(cost.Item, cost.Amount, ItemChangeReason.ITEM_REASON_USE, ItemBind.kItemBindNo) != CmdCode.Succeed)
            return "材料不足";
        // 角色主循环内扣除预检过的虚拟铜币，不跨线程修改背包。
        if (cost.Copper > 0) items.TakeVirtualItem((int)ItemEnum.ZENY, cost.Copper);
        counts.SetCount(field, book.Id, next);
        return "";
    }

    /// <summary>出战秘笈在一次实际攻击命中时，按当前阶位判定目标流血。</summary>
    /// <param name="role">攻击者。</param><param name="target">已命中目标的效果容器。</param>
    internal static void OnAttackHit(OnlineRole role, GeneRuntime target)
    {
        // 仅遍历当前方案的开放槽，同一本书不会重复判定。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        int scheme = Scheme(counts);
        for (int slot = 0; slot < Catalog.OpenSlots; slot++)
        {
            // 出战书、被动和当前阶都来自服务端配置与存档。
            ManualBook book = Find((int)counts.GetCount(RoleCountAction.ManualSlot, SlotKey(scheme, slot)));
            if (book == null || book.Bleed == null) continue;
            ManualBleed bleed = book.Bleed;
            int rank = (int)counts.GetCount(RoleCountAction.ManualRank, book.Id);
            if (rank >= bleed.ChancePercent.Count || HitRoll.Next(100) >= bleed.ChancePercent[rank]) continue;
            // 未提供跳伤数值时只施加限时标记，不推算录屏未展示的伤害。
            GeneBase effect = new StateGene();
            if (bleed.TickMs > 0 && bleed.TickDamage > 0)
                effect = new PeriodicDamageGene { IntervalMilliseconds = bleed.TickMs, Value = bleed.TickDamage };
            effect.GeneID = bleed.EffectId;
            effect.DisplayName = "流血";
            effect.DurationSeconds = bleed.DurationSeconds;
            target.AddGene(effect, role.GetID(), 0, role.GetID());
        }
    }

    /// <summary>合计永久领悟属性与当前方案装备属性，仅在进度发生变化时重算。</summary>
    /// <param name="counts">已加载的角色存档。</param><param name="def">外功防御点数。</param><param name="hp">气血上限。</param><param name="attack">外功最大值。</param>
    internal static void Bonuses(RoleCountComponent counts, out int def, out int hp, out int attack)
    {
        def = hp = attack = 0;
        // 当前方案的一组开放槽。
        int scheme = Scheme(counts);
        for (int i = 0; i < Catalog.Books.Count; i++)
        {
            // 同一本书的基础值由领悟与装备两个来源累加。
            ManualBook book = Catalog.Books[i];
            int level = Level(counts, book);
            if (level <= 0) continue;
            def += book.CollectDef + (level - 1) * book.CollectDefStep;
            hp += book.CollectHp + (level - 1) * book.CollectHpStep;
            for (int slot = 0; slot < Catalog.OpenSlots; slot++)
            {
                if (counts.GetCount(RoleCountAction.ManualSlot, SlotKey(scheme, slot)) != book.Id) continue;
                def += book.EquipDef + (level - 1) * book.EquipDefStep;
                hp += book.EquipHp + (level - 1) * book.EquipHpStep;
                if (counts.GetCount(RoleCountAction.ManualRank, book.Id) > 0) attack += book.FirstRankAttack;
                break;
            }
        }
    }
}
