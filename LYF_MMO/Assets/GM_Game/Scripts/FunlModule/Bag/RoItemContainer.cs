using System;
using System.Collections.Generic;

/// <summary>
/// 物品容器的基础实现。
/// 容器以 ItemUID 索引物品实例，ItemID 只用于同配置物品的统计和排序。
/// </summary>
public class RoItemContainer
{
    public const int InvalidItemPos = -1;

    // 按实例 UID 保存容器内物品，外部只能通过事务接口修改。
    private readonly Dictionary<long, ItemBase> _items = new Dictionary<long, ItemBase>();
    // 容器当前已开启的格子数，扩容由 RoItemComponent 统一校验。
    private int _containerSize;

    /// <summary>返回只读物品视图；增删改必须经过 RoItemComponent 的事务入口。</summary>
    public IReadOnlyDictionary<long, ItemBase> GetItemMap() { return _items; }
    public RoItemContainer(KnapsackType containerType, ClientRole role)
    {
        ContainerType = containerType;
        Role = role;
    }

    /// <summary>按背包类型创建对应容器。未覆盖的类型使用通用背包容器。</summary>
    public static RoItemContainer Create(KnapsackType type, ClientRole role)
    {
        switch (type)
        {
            case KnapsackType.RolePackEquip:
                return new EquipBag(type, role);
            case KnapsackType.RolePackConsume:
                return new ConsumeBag(type, role);
            case KnapsackType.RolePackMaterial:
                return new MaterialBag(type, role);
            case KnapsackType.RoleCurrtEquipPack:
                return new BodyBag(type, role);
            case KnapsackType.RoleVirtualItemPack:
                // 虚拟物品没有格子容量限制，但仍复用标准物品容器。
                return new PlainBag(type, role);
            case KnapsackType.RolePackPlain:
            default:
                return new PlainBag(type, role);
        }
    }

    public KnapsackType ContainerType { get; private set; }
    public ClientRole Role { get; private set; }
    /// <summary>只读物品视图，禁止业务模块直接写入容器字典。</summary>
    public IReadOnlyDictionary<long, ItemBase> Items { get { return _items; } }
    public int ContainerSize { get { return _containerSize; } }

    /// <summary>按位置查找物品实例索引；找不到时返回 0。</summary>
    public virtual long GetItemUIDByPos(int pos)
    {
        ItemBase item = GetItemByPos(pos);
        if (item == null)
        {
            return 0L;
        }
        return item.GetItemUID();
    }

    /// <summary>按位置查找物品配置 ID；找不到时返回 0。</summary>
    public virtual long GetItemItemIDByPos(int pos)
    {
        ItemBase item = GetItemByPos(pos);
        if (item == null)
        {
            return 0L;
        }
        return item.GetItemID();
    }

    /// <summary>检查物品是否可以放入指定位置。</summary>
    public virtual bool CheckIn(ItemBase item, int pos = InvalidItemPos)
    {
        if (item == null || item.GetItemUID() <= 0 || item.GetItemID() <= 0 ||
            item.GetItemCount() <= 0 || _items.ContainsKey(item.GetItemUID()))
        {
            return false;
        }

        if (pos != InvalidItemPos && pos < 0)
        {
            return false;
        }

        if (pos != InvalidItemPos && GetItemByPos(pos) != null)
        {
            return false;
        }

        return !IsFull() || (pos != InvalidItemPos && pos < _containerSize);
    }

    /// <summary>检查物品是否可以取出。子容器可覆盖实现特殊限制。</summary>
    public virtual bool CheckOut(ItemBase item)
    {
        return item != null && _items.ContainsKey(item.GetItemUID());
    }

    /// <summary>将物品放入容器并更新容器类型和位置。</summary>
    public virtual bool SwapIn(ItemBase item, int pos = InvalidItemPos)
    {
        if (!CheckIn(item, pos))
        {
            return false;
        }

        int targetPos;
        if (pos == InvalidItemPos)
        {
            targetPos = GetEmptyPos(item);
        }
        else
        {
            targetPos = pos;
        }
        if (targetPos == InvalidItemPos)
        {
            return false;
        }

        int roleId = 0;
        if (Role != null && Role.RoleInfo != null && Role.RoleInfo.BaseInfo != null)
        {
            roleId = Role.RoleInfo.BaseInfo.RoleId;
        }
        item.SetLocation(roleId, (int)ContainerType, targetPos);
        _items[item.GetItemUID()] = item;
        AfterSwapIn(item);
        return true;
    }

    /// <summary>按物品 ID 移出物品。</summary>
    public virtual ItemBase SwapOutByUID(long itemUid)
    {
        if (itemUid <= 0 || !_items.TryGetValue(itemUid, out ItemBase item) || !CheckOut(item))
        {
            return null;
        }

        BeforeSwapOut(item);
        _items.Remove(itemUid);
        AfterSwapOut(item);
        return item;
    }

    /// <summary>按容器位置移出物品。</summary>
    public virtual ItemBase SwapOutByPos(int pos)
    {
        ItemBase item = GetItemByPos(pos);
        if (item == null)
        {
            return null;
        }
        return SwapOutByUID(item.GetItemUID());
    }

    public virtual void OnSwapEnd() { }
    public virtual void SetContainerSize(int size) { _containerSize = Math.Max(0, size); }
    public virtual int GetContainerSize() { return _containerSize; }

    /// <summary>仅用于登录快照重载，调用方负责确保当前没有正在执行的背包事务。</summary>
    public void Clear()
    {
        _items.Clear();
    }

    /// <summary>按配置 ID 和位置重新整理物品，保持容器位置连续。</summary>
    public virtual void SortOutBag()
    {
        List<ItemBase> sorted = new List<ItemBase>(_items.Values);
        sorted.Sort((left, right) =>
        {
            int result = left.GetItemID().CompareTo(right.GetItemID());
            if (result != 0)
            {
                return result;
            }
            return left.GetBagIndex().CompareTo(right.GetBagIndex());
        });

        for (int index = 0; index < sorted.Count; index++)
        {
            sorted[index].SetBagIndex(index);
        }
    }

    public virtual void ContainerOnItemChange(ItemBase item, ItemChangeReason reason) { }
    public virtual void OnSyncLibs() { }
    public virtual void AfterInitGameRole() { }
    public virtual void OnEnterScene() { }
    public virtual void CheckItemTimeEffect() { }
    public virtual void Reconnect() { }

    public virtual bool IsFull()
    {
        return _containerSize > 0 && _items.Count >= _containerSize;
    }

    public virtual void CalcContainerAttr() { }
    public virtual ItemBase GetItemByUid(long uid)
    {
        if (uid <= 0)
        {
            return null;
        }
        ItemBase item;
        if (_items.TryGetValue(uid, out item))
        {
            return item;
        }
        return null;
    }

    public ItemBase GetItemByPos(int pos)
    {
        List<ItemBase> snapshot = new List<ItemBase>(_items.Values);
        for (int index = 0; index < snapshot.Count; index++)
        {
            ItemBase item = snapshot[index];
            if (item.GetBagIndex() == pos)
            {
                return item;
            }
        }

        return null;
    }

    public int GetEmptySize()
    {
        if (_containerSize <= 0)
        {
            return int.MaxValue;
        }
        return Math.Max(0, _containerSize - _items.Count);
    }

    /// <summary>统计指定配置的数量。当前 ItemBase 没有绑定字段，因此忽略绑定筛选。</summary>
    public virtual int CountItem(int itemId, ItemBind enum_bind)
    {
        if (_items == null) return 0;
        int count = 0;
        List<ItemBase> snapshot = new List<ItemBase>(_items.Values);
        for (int index = 0; index < snapshot.Count; index++)
        {
            ItemBase item = snapshot[index];
            if (item.GetItemID() == itemId)
            {
                //>> 如果找非绑定的就找非绑定的，如果找绑定的所有的都可以
                if (enum_bind == ItemBind.kItemBindNo 
                    && item.IsBind() == ItemBind.kItemBindIs)
                {
                    continue;
                }
                count += item.GetItemCount();
            }
        }

        return count;
    }

    /// <summary>修改物品堆叠数量，数量小于等于零时移除物品。</summary>
    public virtual bool ModifyCount(long itemUid, long addCount, int averagePrice = 0)
    {
        ItemBase item = GetItemByUid(itemUid);
        if (item == null || item.GetItemCount() + addCount < 0 || item.GetItemCount() + addCount > int.MaxValue)
        {
            return false;
        }

        if (!item.TryChangeCount(addCount))
        {
            return false;
        }
        if (item.GetItemCount() == 0)
        {
            _items.Remove(itemUid);
        }

        ContainerOnItemChange(item, ItemChangeReason.ITEM_REASON_MOVE_ITEM);
        return true;
    }

    protected virtual int GetEmptyPos(ItemBase item)
    {
        if (_containerSize <= 0)
        {
            return _items.Count;
        }

        for (int index = 0; index < _containerSize; index++)
        {
            if (GetItemByPos(index) == null)
            {
                return index;
            }
        }

        return InvalidItemPos;
    }

    protected virtual void AfterSwapIn(ItemBase item) { }
    protected virtual void AfterSwapOut(ItemBase item) { }
    protected virtual void BeforeSwapOut(ItemBase item) { }

    /// <summary>
    /// 获取容器还有多少空格子
    /// </summary>
    /// <returns></returns>
    public int GetLeftSpace()
    {
        return _containerSize - _items.Count;
    }

  
    /// <summary>
    /// 查找同 ID 且仍有空余堆叠空间的物品。
    /// </summary>
    /// <param name="desc"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public List<ItemDesc> FindItemForGive(ItemDesc desc)
    {
        List<ItemDesc> result = new List<ItemDesc>();
        if (desc == null || desc.ItemID <= 0)
        {
            return null;
        }

        int maxStackCount = 1;
        if (LubanMgr.Instance != null)
        {
            ItemConfigBase itemConfig = LubanMgr.Instance.GetItemConfigById(desc.ItemID);
            if (itemConfig != null)
            {
                maxStackCount = Math.Max(1, itemConfig.MaxStackCount);
            }
        }

        // 只返回同一配置且未达到单格堆叠上限的物品，供发放逻辑优先填充已有堆叠。
        List<ItemBase> snapshot = new List<ItemBase>(_items.Values);
        for (int index = 0; index < snapshot.Count; index++)
        {
            ItemBase item = snapshot[index];
            if (item == null || item.GetItemID() != desc.ItemID ||
                item.GetItemCount() <= 0 || item.GetItemCount() >= maxStackCount)
            {
                continue;
            }

            ItemDesc itemDesc = new ItemDesc(item.GetItemID(), item.GetItemCount());
            itemDesc.ItemUID = (ulong)item.GetItemUID();
            itemDesc.ItemSign = item.GetItemSign();
            result.Add(itemDesc);
        }

        return result;
    }
}


// ============================================================
// 背包容器类
// ============================================================

/// <summary>普通背包容器。</summary>
public class PlainBag : RoItemContainer
{
    public PlainBag(KnapsackType type, ClientRole role) : base(type, role) { }
}

/// <summary>装备背包容器。</summary>
public class EquipBag : RoItemContainer
{
    public EquipBag(KnapsackType type, ClientRole role) : base(type, role) { }
}

/// <summary>材料背包容器。</summary>
public class MaterialBag : RoItemContainer
{
    public MaterialBag(KnapsackType type, ClientRole role) : base(type, role) { }
    public void CalculateContainerSize() { }
}

/// <summary>消耗品背包容器。</summary>
public class ConsumeBag : RoItemContainer
{
    public ConsumeBag(KnapsackType type, ClientRole role) : base(type, role) { }
    public override bool CheckIn(ItemBase item, int pos = InvalidItemPos) { return base.CheckIn(item, pos); }
}

/// <summary>角色已穿戴装备容器，对应 C++ BodyBag。</summary>
public class BodyBag : RoItemContainer
{
    public BodyBag(KnapsackType type, ClientRole role) : base(type, role) { }
    public bool IsChangeEquipPage { get; set; }
    public override void CalcContainerAttr() { }
}

/// <summary>仓库容器。</summary>
public class Warehouse : RoItemContainer
{
    public Warehouse(KnapsackType type, ClientRole role) : base(type, role) { }
    public override bool CheckIn(ItemBase item, int pos = InvalidItemPos) { return base.CheckIn(item, pos); }
}

/// <summary>快捷栏容器。当前仅保存快捷栏物品，映射关系待协议模型接入。</summary>
public class ShortCutBag : RoItemContainer
{
    // 快捷栏槽位到物品实例 UID 的映射。
    private readonly Dictionary<ulong, ulong> _shortcutMap = new Dictionary<ulong, ulong>();

    public ShortCutBag(KnapsackType type, ClientRole role) : 
        base(type, role) { }

    public ItemBase GetShortCutItemByUid(long uid)
    {
        if (uid <= 0 || !_shortcutMap.TryGetValue((ulong)uid, out ulong targetUid))
        {
            return null;
        }

        return GetItemByUid((long)targetUid);
    }
}

class MoneyBag : RoItemContainer
{
   
    public MoneyBag(KnapsackType type, ClientRole role) :
base(type, role) { }


    //当道具进入容器
    public virtual void AfterSwapIn(ItemBase item, ITEM_SWAP_REASON swapin_reason = ITEM_SWAP_REASON.ISR_None)
    {
        
    }
    //当道具移除容器
    public virtual void AfterSwapOut(ItemBase item, ITEM_SWAP_REASON swapin_reason = ITEM_SWAP_REASON.ISR_None)
    {
        
    }
    //当道具发生改变
    public virtual void ContainerOnItemChange(ItemBase item, ItemChangeReason reason)
    {
        
    }

    public virtual Int64 CountItem(int item_id, ItemBind enum_bind)
    {
        return 0;
    }
    
    //当经验值发生变化
    private void OnExpChange(ItemBase item, ItemChangeReason reason)
    {
        
    }
    //当职业经验值发生变化
    private void OnJobExpChange(ItemBase item, ItemChangeReason reason)
    {
        
    }
    //当元气值发生变化
    private void OnVitalityChange(ItemBase item, ItemChangeReason reason)
    {
        
    }
    //当通信证经验值发生变化
    private void OnPassCheckExpChange(ItemBase item, ItemChangeReason reason)
    {
        
    }

    /*************************************************
    // @Method: OnMonsterResearchPointsChange
    // @Description: 怪物研究点数变化
    // @Date: 2020/06/03
    // @Returns: void
    // @Parameter: item
    // @Parameter: reason
    // @Auther: kaili@123u.cn
    *************************************************/
   public void OnMonsterResearchPointsChange(ItemBase item, ItemChangeReason reason)
    {
        
    }

    void SendMonsterResearchPointsToRank(int score)
    {
        
    }

    void OnResearchLevelChange(int old_level, int new_level)
    {
        
    }
    
    public  static readonly int kVigourRedPointId = 92;
};
