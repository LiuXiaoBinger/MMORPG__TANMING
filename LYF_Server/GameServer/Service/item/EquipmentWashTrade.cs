using System;
using System.Collections.Generic;

/// <summary>由角色逻辑线程串行扣费、创建候选和确认装备洗练。</summary>
public static class EquipmentWashTrade
{
    /// <summary>服务端共用的词条抽取源，仅在抽取一整组时加锁。</summary>
    private static readonly Random Randomizer = new Random();

    /// <summary>核实实例和所见版本，以物品变更通知同步扣费与最终状态。</summary>
    /// <param name="role">网关会话对应的在线角色。</param>
    /// <param name="request">装备编号、萃石和玩家看到的版本。</param>
    /// <param name="reply">操作提示及最新词条快照。</param>
    public static void Execute(OnlineRole role, EquipOperationReq request, EquipOperationRet reply)
    {
        reply.CmdCode = CmdCode.ReqParamError;
        if (!role.IsRoleDataReady()) { reply.WashMessage = "角色背包尚未就绪"; return; }
        // 所有材料和装备均从本角色的容器按实际 UID 查询。
        RoItemComponent items = role.GetComponent<RoItemComponent>();
        EquipBase equip = items.GetItemByUID(request.ItemUid) as EquipBase; // 目标装备实例。
        if (equip == null) { reply.WashMessage = "找不到这件装备"; return; }
        int bag = equip.GetBagType(); // 按背包来源和已穿戴来源处理。
        if (bag != (int)KnapsackType.RoleCurrtEquipPack && bag != (int)KnapsackType.RolePackEquip && bag != (int)KnapsackType.RolePackPlain)
        { reply.WashMessage = "请从背包或穿戴栏选择装备"; return; }
        reply.WashInfo = EquipmentWash.Snapshot(equip);
        if (equip.WashRevision != request.WashRevision) { reply.WashMessage = "洗练结果已更新，请重新查看"; return; }
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_REFINE); // 同一笔物品增量的原因。
        if (request.Operation == EquipOperation.RollWash)
        {
            int price = EquipmentWash.Cost(request.WashStone); // 服务器按正式物品编号定价。
            if (price == 0) { reply.WashMessage = "请选择初级、中级或高级萃石"; return; }
            if (!EquipmentWash.CanLock(reply.WashInfo, request.WashStone, request.WashJade, request.WashLocks))
            { reply.WashMessage = "锁定词条超出萃玉或萃石上限，或所选词条为空"; return; }
            // 预留所有实际堆叠，材料或铜币不足时整笔保持原状。
            List<KeyValuePair<ItemBase, int>> debits = new List<KeyValuePair<ItemBase, int>>();
            ItemSign sign = ItemSign.IsNoSign; // 洗练不改变装备绑定状态。
            if (!GemWorkshopTrade.Reserve(items.GetItemForReduce(request.WashStone, ItemBind.kItemBindIs, -2), EquipmentWash.Count, debits, ref sign))
            { reply.WashMessage = "所选萃石不足 3 个"; return; }
            if (request.WashLocks != 0 && !GemWorkshopTrade.Reserve(items.GetItemForReduce(request.WashJade, ItemBind.kItemBindIs, -2), 1, debits, ref sign))
            { reply.WashMessage = "所选萃玉不足 1 个"; return; }
            if (!GemWorkshopTrade.Reserve(items.GetItemForReduce((int)ItemEnum.ZENY, ItemBind.kItemBindIs,
                (int)KnapsackType.RoleVirtualItemPack), price, debits, ref sign))
            { reply.CmdCode = CmdCode.NotEnoughCurrency; reply.WashMessage = "铜币不足"; return; }
            // 先完成整组抽取，收费后只保存候选，继续洗练覆盖上一候选。
            string candidate;
            lock (Randomizer) candidate = EquipmentWash.Encode(EquipmentWash.Roll(request.WashStone, Randomizer, reply.WashInfo, request.WashJade, request.WashLocks));
            for (int index = 0; index < debits.Count; index++)
            {
                KeyValuePair<ItemBase, int> debit = debits[index]; // 当前材料或铜币的实际扣量。
                if (debit.Key.GetItemCount() == debit.Value) items.DeleteItem(debit.Key.GetItemUID(), reason);
                else items.DeleteItem(debit.Key.GetItemUID(), debit.Value, reason, UpdateType.eUT_Normal);
            }
            equip.WashCandidate = candidate;
            equip.WashStone = request.WashStone;
        }
        else if (request.Operation == EquipOperation.ApplyWash)
        {
            if (string.IsNullOrEmpty(equip.WashCandidate)) { reply.WashMessage = "暂无可替换的洗练结果"; return; }
            EquipmentWash.Replace(equip, EquipmentWash.Decode(equip.WashCandidate));
            equip.WashCandidate = string.Empty;
            role.GetComponent<RoleEquipComponent>().RefreshWash(equip);
            reply.WashMessage = "洗练属性已替换";
        }
        else return;
        equip.WashRevision++;
        items.OnItemChange(equip, reason, UpdateType.eUT_Normal);
        items.NotifyItemChanges();
        reply.WashInfo = EquipmentWash.Snapshot(equip);
        reply.CmdCode = CmdCode.Succeed;
    }
}
