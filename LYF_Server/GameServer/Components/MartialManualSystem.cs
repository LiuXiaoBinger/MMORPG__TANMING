using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

/// <summary>读取秘笈目录，在角色主循环内结算出战、培养及角色属性。</summary>
internal static class MartialManualSystem
{
    /// <summary>随角色首次使用装载的展示目录，培养价格和战斗基因均来自 Luban。</summary>
    private static readonly ManualCatalog Catalog = LoadCatalog();

    /// <summary>从书目、阶位、消耗表组装下发给客户端的只读目录。</summary>
    private static ManualCatalog LoadCatalog()
    {
        // 方案名称及开放槽位沿用原服配置。
        ManualCatalog catalog = JsonParser.Default.Parse<ManualCatalog>(File.ReadAllText(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "MartialManuals.json")));
        // 书目顺序用于客户端分组与选中定位。
        cfg.Tables tables = LubanMgr.Instance.ConfigTables;
        for (int index = 0; index < tables.TbMartialManualInfo.DataList.Count; index++)
        {
            // 负防御表示内防，传给客户端的数值始终为正。
            cfg.MartialManualInfo row = tables.TbMartialManualInfo.DataList[index];
            ManualBook book = new ManualBook { Id = row.Id, Name = row.Name, School = row.School,
                Description = row.Description, StartLevel = row.StartLevel, CollectInner = row.CollectDef < 0,
                CollectDef = Math.Abs(row.CollectDef), CollectHp = row.CollectHp, EquipInner = row.EquipDef < 0,
                EquipDef = Math.Abs(row.EquipDef), EquipHp = row.EquipHp, EquipExtraInner = row.EquipInnerDef,
                CollectDefStep = row.CollectDefStep, CollectHpStep = row.CollectHpStep,
                EquipDefStep = row.EquipDefStep, EquipHpStep = row.EquipHpStep };
            if (row.FragmentCount > 0) book.Learn = new ManualCost { Item = row.FragmentId, Amount = row.FragmentCount };
            for (int rank = 0; ; rank++)
            {
                // 阶数从零起连续排列；后续未展示的阶位可以独立补表。
                cfg.MartialManualTier tier = tables.TbMartialManualTier.GetOrDefault(row.Id * 10 + rank);
                if (tier == null) break;
                // 使用同一组基因参数填入说明，改触发概率后服务端和预览同步变化。
                string passive = FormatPassive(tier);
                if (rank == 0) book.Passive = passive;
                book.TierPassive.Add(passive);
                if (rank > 0)
                {
                    string stat = string.Format(System.Globalization.CultureInfo.InvariantCulture, tier.RankStat, GeneValue(tier.RankGene1));
                    if (passive.Length > 0) stat += "；" + passive;
                    book.RankText.Add(stat);
                }
            }
            for (int indexCost = 0; indexCost < tables.TbMartialManualCost.DataList.Count; indexCost++)
            {
                // 显式配置某一步费用才开放扣费，操作顺序按目标档次写入。
                cfg.MartialManualCost price = tables.TbMartialManualCost.DataList[indexCost];
                if (price.BookId != row.Id) continue;
                ManualCost cost = new ManualCost { Item = price.Item, Amount = price.Amount, Level = price.Level, Copper = price.Copper };
                Google.Protobuf.Collections.RepeatedField<ManualCost> costs = book.Upgrades;
                if (price.Kind == 2) costs = book.Ranks;
                else if (price.Kind == 3) costs = book.Insights;
                if (price.Step != costs.Count + 1) throw new InvalidDataException("秘笈培养费用档次不连续：" + price.Id);
                costs.Add(cost);
            }
            catalog.Books.Add(book);
        }
        return catalog;
    }

    /// <summary>读取基因百分数或点数；事件行的万分比换算成界面的百分数。</summary>
    /// <param name="id">被动或进阶属性的基因编号，空槽为零。</param>
    private static float GeneValue(int id)
    {
        if (id == 0) return 0;
        cfg.Gene gene = LubanMgr.Instance.ConfigTables.TbGene.Get(id);
        if (gene.EffectType == (int)GeneEffectType.Event) return gene.TriggerChance / 100f;
        return gene.Value;
    }

    /// <summary>用装备、未装备及事件子效果的实际参数生成被动说明。</summary>
    /// <param name="tier">当前书籍的单个阶位，展示模板使用从零开始的参数占位符。</param>
    private static string FormatPassive(cfg.MartialManualTier tier)
    {
        // 前四项对应三个装备根和一个未装备根。
        float[] values = { GeneValue(tier.EquipGene1), GeneValue(tier.EquipGene2), GeneValue(tier.EquipGene3),
            GeneValue(tier.UnequippedGene), 0, 0, 0, 0 };
        cfg.TbGene genes = LubanMgr.Instance.ConfigTables.TbGene;
        cfg.Gene root = genes.GetOrDefault(tier.EquipGene1);
        if (root != null && root.EffectType == (int)GeneEffectType.Event)
        {
            // 后四项对应命中效果时长、施毒次数、满层惩罚的数值和时长。
            cfg.Gene child = genes.Get(root.AttachGeneId0);
            values[4] = child.GeneTime;
            values[5] = child.Condition;
            cfg.Gene penalty = genes.GetOrDefault(child.AttachGeneId0);
            if (penalty != null) { values[6] = penalty.Value; values[7] = penalty.GeneTime; }
        }
        string text = tier.Passive;
        for (int index = 0; index < values.Length; index++)
            text = text.Replace("{" + index + "}", values[index].ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        return text;
    }

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

    /// <summary>从当前领悟、出战、阶数恢复对应来源的基因，保留仍生效的同一节点。</summary>
    /// <param name="role">已载入计数存档的在线角色。</param>
    internal static void ApplyGenes(OnlineRole role)
    {
        // 负槽位专用于角色秘笈根，不匹配装备和命中后独立施加的状态。
        GeneRuntime runtime = role.Gene.Runtime;
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        int scheme = Scheme(counts);
        HashSet<int> usedSlots = new HashSet<int>();
        for (int index = 0; index < Catalog.Books.Count; index++)
        {
            // 同一本书领悟属性永久保留，其余按当前方案取值。
            ManualBook book = Catalog.Books[index];
            int level = Level(counts, book);
            if (level == 0) continue;
            bool equipped = false;
            for (int slot = 0; slot < Catalog.OpenSlots; slot++)
                if (counts.GetCount(RoleCountAction.ManualSlot, SlotKey(scheme, slot)) == book.Id) equipped = true;
            // 每书预留一百个独立来源槽，普通属性使用现有同编号基础基因。
            int first = -10000 - book.Id * 100;
            int defense = 7;
            if (book.CollectInner) defense = 9;
            SetGene(role, usedSlots, first, defense, book.CollectDef + (level - 1) * book.CollectDefStep);
            SetGene(role, usedSlots, first - 1, 1, book.CollectHp + (level - 1) * book.CollectHpStep);
            if (equipped)
            {
                defense = 7;
                if (book.EquipInner) defense = 9;
                SetGene(role, usedSlots, first - 2, defense, book.EquipDef + (level - 1) * book.EquipDefStep);
                SetGene(role, usedSlots, first - 3, 1, book.EquipHp + (level - 1) * book.EquipHpStep);
                SetGene(role, usedSlots, first - 4, 9, book.EquipExtraInner);
            }
            // 被动只取当前阶；进阶新增属性从一阶起累计。
            int rank = (int)counts.GetCount(RoleCountAction.ManualRank, book.Id);
            for (int step = 0; step <= rank; step++)
            {
                cfg.MartialManualTier tier = LubanMgr.Instance.ConfigTables.TbMartialManualTier.GetOrDefault(book.Id * 10 + step);
                if (tier == null) continue;
                if (equipped)
                {
                    SetGene(role, usedSlots, first - 10 - step * 2, tier.RankGene1);
                    SetGene(role, usedSlots, first - 11 - step * 2, tier.RankGene2);
                }
                if (step != rank) continue;
                if (equipped)
                {
                    SetGene(role, usedSlots, first - 5, tier.EquipGene1);
                    SetGene(role, usedSlots, first - 6, tier.EquipGene2);
                    SetGene(role, usedSlots, first - 7, tier.EquipGene3);
                }
                else SetGene(role, usedSlots, first - 8, tier.UnequippedGene);
            }
        }
        for (int index = runtime.ActiveGenes.Count - 1; index >= 0; index--)
        {
            // 撤下本角色已失效的根，命中施加给目标的计时效果仍自行到期。
            GeneBase gene = runtime.ActiveGenes[index];
            if (gene.SourceSlot <= -10000 && gene.SourceId == role.GetID() && !usedSlots.Contains(gene.SourceSlot))
                runtime.RemoveGene(gene);
        }
    }

    /// <summary>将表行绑定到稳定来源槽；进阶或升级时替换旧节点。</summary>
    /// <param name="role">持有该秘笈的角色。</param><param name="used">本次仍有效的槽。</param>
    /// <param name="slot">书号派生的负槽。</param><param name="id">对应 Luban 基因。</param>
    /// <param name="value">普通面板属性的当前等级值；被动直接用基因表。</param>
    private static void SetGene(OnlineRole role, HashSet<int> used, int slot, int id, float? value = null)
    {
        if (id == 0 || value == 0) return;
        used.Add(slot);
        // 同一位置稳定复用事件树，以免无关培养刷新触发计时。
        GeneRuntime runtime = role.Gene.Runtime;
        for (int index = 0; index < runtime.ActiveGenes.Count; index++)
        {
            GeneBase previous = runtime.ActiveGenes[index];
            if (previous.SourceId != role.GetID() || previous.SourceSlot != slot) continue;
            if (previous.GeneID == id && (!value.HasValue || previous.Value == value.Value)) return;
            runtime.RemoveGene(previous);
            break;
        }
        runtime.AddGene(id, role.GetID(), value, slot, role.GetID());
    }
}
