

using System.Collections.Generic;

/// <summary>
/// 物品事务的静态帮助类：按背包类型分组物品、预检能否扣除、主动领奖空间预检。
/// </summary>
public class ItemTransition
{
     /// <summary>
     /// 按物品配置所属的背包类型对物品进行分组。
     /// </summary>
     /// <param name="items">待分类的物品列表。</param>
     /// <returns>以背包类型为键、物品列表为值的分类结果。</returns>
     public static Dictionary<KnapsackType, List<ItemDesc>> ClassifyItemsByBagType(List<ItemDesc> items)
     {
         // 按配置背包类型收集原物品描述，不复制物品对象。
         Dictionary<KnapsackType, List<ItemDesc>> result =
             new Dictionary<KnapsackType, List<ItemDesc>>();
         if (items == null || items.Count == 0)
         {
             return result;
         }
         for (int index = 0; index < items.Count; index++)
         {
             // 本次要分类的物品描述，非法编号会跳过。
             ItemDesc item = items[index];
             if (item == null || item.ItemID <= 0) { continue; }

             // 配置查询同时覆盖普通物品、装备和虚拟物品；未配置的物品返回空值并跳过。
             KnapsackType? bagType = LubanMgr.Instance.GetItemPackTypeById(item.ItemID);
             if (!bagType.HasValue) { continue; }

             if (!result.TryGetValue(bagType.Value, out List<ItemDesc> bagItems))
             {
                 bagItems = new List<ItemDesc>();
                 result.Add(bagType.Value, bagItems);
             }

             bagItems.Add(item);
         }

         return result;
     }

     /// <summary>逐项检查 role 的物品是否够扣；遇到首个失败即返回错误码，不扣除物品。</summary>
     public static CmdCode TryTakeItem(PlayerEntity role, List<ItemDesc> items)
     {
        if (items.Count == 0) { return CmdCode.Succeed; }
        for (int index = 0; index < items.Count; index++)
        {
            // 当前待扣物品，携带数量和绑定物品的使用规则。
            ItemDesc item = items[index];
            CmdCode  err = role.
                CheckItem(item.ItemID, item.ItemCount, item.BindOptionForTake);
            if (err != CmdCode.Succeed)
            {
                return err;
            }
        }
        return CmdCode.Succeed;
    }

     /// <summary>检查 items 能否放入 role 的指定背包 type，只返回预检结果。</summary>
     public static ItemZhuDongGetResult TryZhuDongGetItem(PlayerEntity role, List<ItemDesc> items,KnapsackType type = KnapsackType.RolePackPlain)
     {
         BagZhuDongGetItemTranstion trans = new BagZhuDongGetItemTranstion(role);
         return trans.TryZhuDongGetItem(items, type);
     }
     /// <summary>将单个 item 包装为列表，再检查 role 的背包 type 是否放得下。</summary>
     public static ItemZhuDongGetResult TryZhuDongGetItem(PlayerEntity role, ItemDesc item,KnapsackType type = KnapsackType.RolePackPlain)
     {
         BagZhuDongGetItemTranstion trans = new BagZhuDongGetItemTranstion(role);
         return trans.TryZhuDongGetItem(new List<ItemDesc>(){item}, type);
     }
     
     
}
