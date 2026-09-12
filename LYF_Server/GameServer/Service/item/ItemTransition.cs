

using System.Collections.Generic;

public class ItemTransition
{
     /// <summary>
     /// 按物品配置所属的背包类型对物品进行分组。
     /// </summary>
     /// <param name="items">待分类的物品列表。</param>
     /// <returns>以背包类型为键、物品列表为值的分类结果。</returns>
     public static Dictionary<KnapsackType, List<ItemDesc>> ClassifyItemsByBagType(List<ItemDesc> items)
     {
         Dictionary<KnapsackType, List<ItemDesc>> result =
             new Dictionary<KnapsackType, List<ItemDesc>>();
         if (items == null || items.Count == 0)
         {
             return result;
         }
         foreach (ItemDesc item in items)
         {
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

     public static CmdCode TryTakeItem(OnlineRole role, List<ItemDesc> items)
     {
        if (items.Count == 0) { return CmdCode.Succeed; }
        foreach (var ite in items)
        {
            CmdCode  err = role.
                CheckItem(ite.ItemID, ite.ItemCount, ite.BindOptionForTake);
            if (err != CmdCode.Succeed)
            {
                return err;
            }
        }
        return CmdCode.Succeed;
    }

     public static ItemZhuDongGetResult TryZhuDongGetItem(OnlineRole role, List<ItemDesc> items,KnapsackType type = KnapsackType.RolePackPlain)
     {
         BagZhuDongGetItemTranstion trans = new BagZhuDongGetItemTranstion(role);
         return trans.TryZhuDongGetItem(items, type);
     }
     public static ItemZhuDongGetResult TryZhuDongGetItem(OnlineRole role, ItemDesc item,KnapsackType type = KnapsackType.RolePackPlain)
     {
         BagZhuDongGetItemTranstion trans = new BagZhuDongGetItemTranstion(role);
         return trans.TryZhuDongGetItem(new List<ItemDesc>(){item}, type);
     }
     
     
}
