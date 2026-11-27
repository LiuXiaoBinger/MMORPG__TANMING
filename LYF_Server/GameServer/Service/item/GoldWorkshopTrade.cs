using System.Collections.Generic;

/// <summary>在角色逻辑线程按共享配方结算金装，装备成长只修改原有实例。</summary>
public static class GoldWorkshopTrade
{
    /// <summary>对同一装备 UID 结算一次强化、非金装进阶或重铸。</summary>
    /// <param name="role">操作人及其已挂载的装备基因容器。</param>
    /// <param name="items">本角色的装备、材料与钱包。</param>
    /// <param name="equip">已经核实归属与物品编号的实例。</param>
    /// <param name="request">玩家选择的养成动作和目标档。</param>
    /// <param name="reply">回写操作结果与扣费提示。</param>
    private static void ApplyGrowth(OnlineRole role, RoItemComponent items, EquipBase equip, EquipOperationReq request, EquipOperationRet reply)
    {
        // 当前物品模板决定可强化状态和每个随机槽的范围。
        cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equip.GetItemID());
        // 重铸无等级目标，提升时只接受玩家看到的下一档。
        int target = 0;
        if (request.Operation == EquipOperation.Strengthen)
        {
            target = equip.StrengthenLevel + 1;
            if (config.CanStrengthen == 0 || target > EquipmentGrowth.MaxStrength) { reply.GoldMessage = "当前装备无法继续强化"; return; }
        }
        if (request.Operation == EquipOperation.Advance)
        {
            target = equip.GoldTierSteps + 2;
            if (GoldWorkshopRecipe.IsGold(equip.GetItemID())) { reply.GoldMessage = "金装请使用金装进阶配方"; return; }
            if (target > EquipmentGrowth.MaxTier) { reply.GoldMessage = "已达十阶"; return; }
        }
        if (request.GoldTarget != target) { reply.GoldMessage = "装备进度已变化"; return; }
        // 材料和铜币全部核对充足后统一扣除，失败不改装备。
        List<KeyValuePair<ItemBase, int>> debits = new List<KeyValuePair<ItemBase, int>>();
        // 养成不会改变原有绑定状态。
        ItemSign sign = ItemSign.IsNoSign;
        if (request.Operation == EquipOperation.Strengthen)
        {
            // 每十级使用下一种强化石，每次一颗。
            int stone = 8101 + (target - 1) / 10;
            if (!GemWorkshopTrade.Reserve(items.GetItemForReduce(stone, ItemBind.kItemBindIs, -2), 1, debits, ref sign))
            { reply.GoldMessage = LubanMgr.Instance.GetItemInfoById(stone).Name + "不足"; return; }
        }
        if (!GemWorkshopTrade.Reserve(items.GetItemForReduce((int)ItemEnum.ZENY, ItemBind.kItemBindIs, (int)KnapsackType.RoleVirtualItemPack),
            EquipmentGrowth.Cost(request.Operation, target), debits, ref sign))
        { reply.CmdCode = CmdCode.NotEnoughCurrency; reply.GoldMessage = "铜币不足"; return; }
        // 使用与金装作坊相同的存档和增量通知原因。
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_REFINE);
        if (request.Operation == EquipOperation.Strengthen) { equip.StrengthenLevel = target; reply.GoldMessage = "强化成功，当前 +" + target; }
        else if (request.Operation == EquipOperation.Advance) { equip.GoldTierSteps++; reply.GoldMessage = "进阶成功，当前 " + target + " 阶"; }
        else { EquipmentGrowth.Roll(equip, config); reply.GoldMessage = "重铸完成"; }
        for (int index = 0; index < debits.Count; index++)
        {
            // 保留未耗尽的原物品堆叠。
            KeyValuePair<ItemBase, int> debit = debits[index];
            if (debit.Key.GetItemCount() == debit.Value) items.DeleteItem(debit.Key.GetItemUID(), reason);
            else items.DeleteItem(debit.Key.GetItemUID(), debit.Value, reason, UpdateType.eUT_Normal);
        }
        items.OnItemChange(equip, reason, UpdateType.eUT_Normal);
        role.GetComponent<RoleEquipComponent>().RefreshBaseAttributes(equip);
        items.NotifyItemChanges();
        reply.CmdCode = CmdCode.Succeed;
    }

    /// <summary>按服务器持有的实例进度和库存决定本次能否提交。</summary>
    /// <param name="role">请求会话所绑定的角色。</param>
    /// <param name="request">选中的物品、实例和玩家看到的目标档。</param>
    /// <param name="reply">返回结果码、真实实例 UID 和操作提示。</param>
    public static void Execute(OnlineRole role, EquipOperationReq request, EquipOperationRet reply)
    {
        reply.CmdCode = CmdCode.ReqParamError;
        if (!role.IsRoleDataReady()) { reply.GoldMessage = "角色背包尚未就绪"; return; }
        // 后续查找及扣除都限制在此角色的容器中。
        RoItemComponent items = role.GetComponent<RoItemComponent>();
        // 合成的起点是一阶60级，成长请求从现有装备取得真实档数。
        int levels = 0;
        // 已完成的进阶次数，初始为零。
        int tiers = 0;
        // 装备由真实 UID 指定，同名的其他实例不会被修改。
        EquipBase equip = null;
        if (request.Operation != EquipOperation.ForgeGold)
        {
            equip = items.GetItemByUID(request.ItemUid) as EquipBase;
            if (equip == null || equip.GetItemID() != request.GoldItemId) { reply.GoldMessage = "所选装备已不在背包或穿戴栏"; return; }
            // 普通包也可能留有旧版装备实例，沿用原养成窗口的背包来源。
            int bag = equip.GetBagType();
            if (bag != (int)KnapsackType.RolePackEquip && bag != (int)KnapsackType.RoleCurrtEquipPack && bag != (int)KnapsackType.RolePackPlain)
            { reply.GoldMessage = "请从背包或穿戴栏选择装备"; return; }
            levels = equip.GoldLevelSteps;
            tiers = equip.GoldTierSteps;
        }
        // 通用养成与金装共用扣料、实例通知和存档事务。
        if (request.Operation == EquipOperation.Strengthen || request.Operation == EquipOperation.Advance || request.Operation == EquipOperation.Reforge)
        {
            ApplyGrowth(role, items, equip, request, reply);
            return;
        }
        // 逐级、逐阶计算下一步，过期的目标不能被再次扣料。
        GoldWorkshopRecipe recipe = GoldWorkshopRecipe.Get(request.Operation, request.GoldItemId, levels, tiers);
        if (recipe.Unavailable.Length > 0) { reply.GoldMessage = recipe.Unavailable; return; }
        if (request.GoldTarget != recipe.Target) { reply.GoldMessage = "装备进度已变化，请查看当前配方"; return; }
        // 每项对应一个真实材料或钱包堆叠，齐备前不动任何物品。
        List<KeyValuePair<ItemBase, int>> debits = new List<KeyValuePair<ItemBase, int>>();
        // 新产物继承实际花费材料中的绑定标记。
        ItemSign sign = ItemSign.IsNoSign;
        // index 逐项核对背包中的材料。
        for (int index = 0; index < recipe.Materials.Count; index++)
        {
            // 当前行的具体物品和本次所需数量。
            ItemDesc mat = recipe.Materials[index];
            if (!GemWorkshopTrade.Reserve(items.GetItemForReduce(mat.ItemID, ItemBind.kItemBindIs, -2),
                (int)mat.ItemCount, debits, ref sign))
            { reply.GoldMessage = LubanMgr.Instance.GetItemInfoById(mat.ItemID).Name + "不足"; return; }
        }
        // 钱包上的标志不参与判定成品绑定。
        ItemSign wallet = ItemSign.IsNoSign;
        if (recipe.Copper > 0 && !GemWorkshopTrade.Reserve(items.GetItemForReduce((int)ItemEnum.ZENY,
            ItemBind.kItemBindIs, (int)KnapsackType.RoleVirtualItemPack), recipe.Copper, debits, ref wallet))
        { reply.CmdCode = CmdCode.NotEnoughCurrency; reply.GoldMessage = "铜钱不足"; return; }
        // 已核定的这一笔变动统一使用现有物品养成原因。
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_REFINE);
        if (request.Operation == EquipOperation.ForgeGold)
        {
            // 单件金装进入独立装备包，满包时发放返回空，不会动预留材料。
            equip = items.AddItem(request.GoldItemId, 1, reason, sign, 0, 0, UpdateType.eUT_Normal, new IdipParam()) as EquipBase;
            if (equip == null) { reply.CmdCode = CmdCode.BagFull; reply.GoldMessage = "装备背包空间不足"; return; }
            reply.GoldMessage = "合成成功，金装已放入装备背包";
        }
        else
        {
            if (request.Operation == EquipOperation.AdvanceGold)
            { equip.GoldTierSteps++; role.GetComponent<RoleEquipComponent>().RefreshBaseAttributes(equip); reply.GoldMessage = "进阶成功，装备已达" + recipe.Target + "阶"; }
            else { equip.GoldLevelSteps++; reply.GoldMessage = "升级成功，装备已达" + recipe.Target + "级"; }
            items.OnItemChange(equip, reason, UpdateType.eUT_Normal);
        }
        // index 遍历已核定的扣料清单，整叠移除沿用背包的删除通知。
        for (int index = 0; index < debits.Count; index++)
        {
            // 当前堆叠及其精确扣除量，部分堆叠保留 UID。
            KeyValuePair<ItemBase, int> debit = debits[index];
            if (debit.Key.GetItemCount() == debit.Value) items.DeleteItem(debit.Key.GetItemUID(), reason);
            else items.DeleteItem(debit.Key.GetItemUID(), debit.Value, reason, UpdateType.eUT_Normal);
        }
        items.NotifyItemChanges();
        reply.ItemUid = equip.GetItemUID();
        reply.CmdCode = CmdCode.Succeed;
    }
}
