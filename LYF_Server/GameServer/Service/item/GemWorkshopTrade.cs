using System;
using System.Collections.Generic;

/// <summary>在角色逻辑线程内结算宝石商会和合成，沿用角色物品、虚拟货币和次数的存档。</summary>
public static class GemWorkshopTrade
{
    /// <summary>为一个品种返回实际额度，同时按请求执行一次购买或合成。</summary>
    /// <param name="role">由网关原始会话取得的在线角色。</param>
    /// <param name="request">目标宝石及按等级排列的投入数量。</param>
    /// <param name="reply">写入实际商品、三档剩余额度和结果，供原请求者刷新。</param>
    public static void Execute(OnlineRole role, EquipOperationReq request, EquipOperationRet reply)
    {
        reply.GemItemId = request.GemItemId;
        reply.CmdCode = CmdCode.ReqParamError;
        // 配置编号同时决定品种和等级，客户端无法指定产物的价格或数量。
        int family;
        int grade;
        if (!role.IsRoleDataReady() || !EquipmentGemRules.TryRead(request.GemItemId, out family, out grade)) return;
        // 额度独立保存到每个角色的每个商品。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        // 该品种一级的编号用于查找连续的低级原料与商会商品。
        int first = EquipmentGemRules.FirstItemId(family);
        if (request.Operation == EquipOperation.GemMarket) reply.CmdCode = CmdCode.Succeed;
        else reply.GemMessage = Settle(role.GetComponent<RoItemComponent>(), counts, request, first, grade, reply);
        // 下标依次对应一级至三级，提交失败也返回最新余额度。
        for (int index = 0; index < EquipmentGemRules.MarketGrades; index++)
            reply.MarketRemaining.Add(Remaining(counts, first + index));
    }

    /// <summary>读取一个商会商品的剩余配额；已购数由角色快照恢复。</summary>
    /// <param name="counts">当前角色的持久次数组件。</param>
    /// <param name="itemId">本品种一级至三级之一的独立配置编号。</param>
    private static int Remaining(RoleCountComponent counts, int itemId)
    {
        return Math.Max(0, EquipmentGemRules.MarketLimit - (int)counts.GetCount(RoleCountAction.GemMarketPurchase, itemId));
    }

    /// <summary>先定位整笔订单的真实堆叠、价格和产物空间，检查通过后在同一逻辑线程提交。</summary>
    /// <param name="items">当前角色的全部物品与铜币容器。</param>
    /// <param name="counts">按商品记录已用商会额度。</param>
    /// <param name="req">客户端选择的同品种原料颗数。</param>
    /// <param name="first">当前品种的一级编号。</param>
    /// <param name="target">将要产出的宝石等级。</param>
    /// <param name="reply">成功或背包空间、铜币不足等具体回包码。</param>
    private static string Settle(RoItemComponent items, RoleCountComponent counts, EquipOperationReq req,
        int first, int target, EquipOperationRet reply)
    {
        // 工坊旧版直购不传数量，仍然只买一颗；合成每次也只产出一颗。
        bool buy = req.Operation == EquipOperation.BuyGem;
        // 本次入包的颗数，同时决定直购扣款额及占用的商品配额。
        int outputCount = 1;
        if (buy && req.HasBuyQuantity) outputCount = req.BuyQuantity;
        if (buy && target > EquipmentGemRules.MarketGrades) return "商会只出售1至3级宝石";
        if (outputCount < 1 || outputCount > EquipmentGemRules.MarketLimit) return "购买数量无效";
        if (!buy && (target < 2 || target > EquipmentGemRules.ComposeCap)) return "该等级尚未开放合成";
        if (!buy && (req.BagGemCounts.Count != EquipmentGemRules.ComposeCap || req.MarketGemCounts.Count != EquipmentGemRules.MarketGrades))
            return "材料清单不完整";
        // 每个计划项保存此次实际扣除的堆叠引用与数量，材料可横跨背包格。
        List<KeyValuePair<ItemBase, int>> debits = new List<KeyValuePair<ItemBase, int>>();
        // 三档商会材料单独占用各自的商品额度。
        int[] purchases = new int[EquipmentGemRules.MarketGrades];
        // 以一级份数累计材料总量，并使用服务器价格计算总铜币。
        int units = 0;
        long copper = 0;
        // 使用绑定材料时继承绑定，防止合成洗掉物品标记。
        ItemSign sign = ItemSign.IsNoSign;
        // 品种相同，不接受与目标同级或更高的材料。
        for (int index = 0; index < EquipmentGemRules.ComposeCap; index++)
        {
            // 一级在零下标，直购只在目标商品的档位计入所选颗数。
            int grade = index + 1;
            // 本档从玩家库存中投入的合成材料数量。
            int bag = 0;
            // 本档从商会购买的颗数。
            int shop = 0;
            if (buy) { if (grade == target) shop = outputCount; }
            else
            {
                bag = req.BagGemCounts[index];
                if (index < purchases.Length) shop = req.MarketGemCounts[index];
                if (grade >= target && (bag != 0 || shop != 0)) return "材料等级须低于合成目标";
            }
            // 单笔最高目标七级仅需64份，在乘法前限定不可信数量。
            int max = EquipmentGemRules.Units(EquipmentGemRules.ComposeCap);
            if (bag < 0 || bag > max) return "背包材料数量无效";
            if (!buy && (shop < 0 || shop > max)) return "商会材料数量无效";
            if (shop > 0)
            {
                if (shop > Remaining(counts, first + index)) return "该等级商会宝石已达限购";
                purchases[index] = shop;
                copper += (long)shop * EquipmentGemRules.MarketPrice(grade);
            }
            units += (bag + shop) * EquipmentGemRules.Units(grade);
            if (bag > 0 && !Reserve(items.GetItemForReduce(first + index, ItemBind.kItemBindIs, -2), bag, debits, ref sign))
                return "背包中的宝石不足";
        }
        if (units != EquipmentGemRules.Units(target) * outputCount) return "选中材料与目标所需份数不符";
        // 只从虚拟货币容器扣铜币；货币上的绑定标记不传递给商品。
        ItemSign walletSign = ItemSign.IsNoSign;
        if (copper > 0 && !Reserve(items.GetItemForReduce((int)ItemEnum.ZENY, ItemBind.kItemBindIs,
            (int)KnapsackType.RoleVirtualItemPack), (int)copper, debits, ref walletSign))
        {
            reply.CmdCode = CmdCode.NotEnoughCurrency;
            return "铜币不足";
        }
        // 允许用本次恰好耗尽的材料格放产物，背包满且合成后仍无格时整笔拒绝。
        if (!FitsOutput(items, req.GemItemId, outputCount, sign, debits))
        {
            reply.CmdCode = CmdCode.BagFull;
            return "背包空间不足";
        }
        // 所有物品引用和空间已经按真实容器预检，角色命令串行执行至通知完成。
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_REFINE);
        for (int index = 0; index < debits.Count; index++)
        {
            // 整叠删除采用含 DeleteList 的通知入口，部分消耗走堆叠数量增量。
            KeyValuePair<ItemBase, int> debit = debits[index];
            if (debit.Key.GetItemCount() == debit.Value) items.DeleteItem(debit.Key.GetItemUID(), reason);
            else items.DeleteItem(debit.Key.GetItemUID(), debit.Value, reason, UpdateType.eUT_Normal);
        }
        items.AddItem(req.GemItemId, outputCount, reason, sign, 0, 0L, UpdateType.eUT_Normal, new IdipParam());
        for (int index = 0; index < purchases.Length; index++)
        {
            if (purchases[index] == 0) continue;
            // 写入角色已有的次数存档，查询窗口或重登均读取同一额度。
            int id = first + index;
            counts.SetCount(RoleCountAction.GemMarketPurchase, id,
                counts.GetCount(RoleCountAction.GemMarketPurchase, id) + purchases[index]);
        }
        items.NotifyItemChanges();
        reply.CmdCode = CmdCode.Succeed;
        if (buy) return "购买成功";
        return "合成成功";
    }

    /// <summary>预留指定数量的真实堆叠；不足时调用者直接丢弃尚未写入的整份计划。</summary>
    /// <param name="stacks">绑定优先的匹配物品实例。</param>
    /// <param name="needed">该等级需要扣除的颗数或铜币。</param>
    /// <param name="debits">收集本单精确扣量的列表。</param>
    /// <param name="sign">跟随已选宝石的绑定属性。</param>
    internal static bool Reserve(List<ItemBase> stacks, int needed, List<KeyValuePair<ItemBase, int>> debits, ref ItemSign sign)
    {
        for (int index = 0; index < stacks.Count && needed > 0; index++)
        {
            // 只选同一物品编号中仍持有的颗数。
            ItemBase stack = stacks[index];
            int take = Math.Min(needed, (int)stack.GetItemCount());
            debits.Add(new KeyValuePair<ItemBase, int>(stack, take));
            if ((stack.GetItemSign() & (int)ItemSign.IsItemSignBind) != 0) sign = ItemSign.IsItemSignBind;
            needed -= take;
        }
        return needed == 0;
    }

    /// <summary>按 AddItem 的容器和绑定规则核对整单空间，包含部分空闲的堆叠和将被耗尽的材料格。</summary>
    /// <param name="items">当前角色物品组件。</param>
    /// <param name="id">真实目标宝石编号。</param>
    /// <param name="quantity">整笔直购或合成需入包的颗数。</param>
    /// <param name="sign">产物绑定状态。</param>
    /// <param name="debits">扣款前已定位的消耗，用于识别即将腾出的格位。</param>
    private static bool FitsOutput(RoItemComponent items, int id, int quantity, ItemSign sign, List<KeyValuePair<ItemBase, int>> debits)
    {
        // 从正式物品配置确定产物的容器和每格堆叠上限。
        ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(id);
        if (config == null) return false;
        // 入包时只会在该容器寻找空格及可合并的堆叠。
        RoItemContainer container = items.GetContainer(config.PackType);
        if (container == null) return false;
        // 先计算空格能容纳的颗数，再计入尚能叠加的库存。
        long capacity = (long)container.GetEmptySize() * config.MaxStackCount;
        if (capacity >= quantity) return true;
        // 只累计实际入包容器中同物品、同绑定属性的堆叠。
        List<ItemBase> stacks = new List<ItemBase>(container.GetItemMap().Values);
        for (int index = 0; index < stacks.Count; index++)
        {
            // 当前堆叠可接收的颗数与 AddItem 的合并条件相同。
            ItemBase stack = stacks[index];
            if (stack.GetItemID() == id && stack.GetItemSign() == (int)sign)
                capacity += Math.Max(0L, config.MaxStackCount - stack.GetItemCount());
        }
        // 同容器且整堆用尽的材料会空出一格。
        for (int index = 0; index < debits.Count; index++)
        {
            // 此项保存已预检的堆叠及该堆实际扣量。
            KeyValuePair<ItemBase, int> debit = debits[index];
            if (debit.Key.GetBagType() == (int)container.ContainerType && debit.Key.GetItemCount() == debit.Value)
                capacity += config.MaxStackCount;
        }
        return capacity >= quantity;
    }
}
