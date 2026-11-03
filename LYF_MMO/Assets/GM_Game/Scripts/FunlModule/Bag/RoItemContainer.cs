using System;
using System.Collections.Generic;

/// <summary>
/// 物品容器的基础实现。
/// 容器以 ItemUID 索引物品实例，ItemID 只用于同配置物品的统计和排序。
/// </summary>
public class RoItemContainer
{

    #region 字段与属性
    /// <summary>-1 表示未指定格子，由容器选择空位。</summary>
    public const int InvalidItemPos = -1;

    // 按实例 UID 保存容器内物品，外部只能通过事务接口修改。
    private readonly Dictionary<long, ItemBase> _items = new Dictionary<long, ItemBase>();

    // 容器当前已开启的格子数，扩容由 RoItemComponent 统一校验。
    private int _containerSize;

    /// <summary>本容器对应的背包类别，构造后不变。</summary>
    public KnapsackType ContainerType { get; private set; }

    /// <summary>物品归属的客户端角色，放入物品时从中读取角色编号。</summary>
    public PlayerEntity Role { get; private set; }

    /// <summary>只读物品视图，禁止业务模块直接写入容器字典。</summary>
    public IReadOnlyDictionary<long, ItemBase> Items { get { return _items; } }

    /// <summary>当前容量；小于等于 0 在容量判断中表示不设上限。</summary>
    public int ContainerSize { get { return _containerSize; } }
    #endregion

    /// <summary>保存背包类型 containerType 和所属角色 role，容量随后由调用方设置。</summary>
    public RoItemContainer(KnapsackType containerType, PlayerEntity role)
    {
        ContainerType = containerType;
        Role = role;
    }

    /// <summary>进入场景时的扩展入口，基础容器不处理。</summary>
    public virtual void OnEnterScene() { }

    #region 事件回调
    /// <summary>一轮物品移动结束后的扩展入口，基础容器不处理。</summary>
    public virtual void OnSwapEnd() { }

    /// <summary>保留的数据同步回调，基础容器没有同步实现。</summary>
    public virtual void OnSyncLibs() { }
    #endregion

    /// <summary>按背包类型创建对应容器。未覆盖的类型使用通用背包容器。</summary>
    public static RoItemContainer Create(KnapsackType type, PlayerEntity role)
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

    #region 查询与判定
    /// <summary>返回只读物品视图；增删改必须经过 RoItemComponent 的事务入口。</summary>
    public IReadOnlyDictionary<long, ItemBase> GetItemMap() { return _items; }

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
        // 物品缺失或 UID、物品 ID 非法时不可放入。
        if (item == null || item.GetItemUID() <= 0 || item.GetItemID() <= 0)
        {
            return false;
        }
        // 数量必须为正，且该 UID 尚未被容器占用。
        if (item.GetItemCount() <= 0 || _items.ContainsKey(item.GetItemUID()))
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

    /// <summary>返回当前设置的容量值。</summary>
    public virtual int GetContainerSize() { return _containerSize; }

    /// <summary>预留的物品有效期检查入口，当前不执行到期处理。</summary>
    public virtual void CheckItemTimeEffect() { }

    /// <summary>容量为正且物品实例数达到容量时表示已满；容量为 0 表示不限制。</summary>
    public virtual bool IsFull()
    {
        return _containerSize > 0 && _items.Count >= _containerSize;
    }

    /// <summary>预留的容器属性汇总入口，基础容器不计算角色数值。</summary>
    public virtual void CalcContainerAttr() { }

    /// <summary>用实例编号 uid 查找原物品对象，编号无效或不存在时返回 null。</summary>
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

    /// <summary>按格子位置 pos 查找物品；遍历的是引用列表，返回的仍是容器内原对象。</summary>
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

    /// <summary>返回可容纳的新物品实例数；不限容量时用 int.MaxValue 表示。</summary>
    public int GetEmptySize()
    {
        if (_containerSize <= 0)
        {
            return int.MaxValue;
        }
        return Math.Max(0, _containerSize - _items.Count);
    }

    /// <summary>统计 itemId 的总数量；enum_bind 为 kItemBindNo 时排除绑定物品，否则两类都计入。</summary>
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

    /// <summary>查找第一个空格；不限容量时使用当前实例数量作为位置，item 参数目前未用。</summary>
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
    /// <param name="desc">要发放的物品描述，以配置 ID 查找现有未满堆叠。</param>
    /// <returns></returns>
    /// <remarks>只返回可继续堆叠的描述列表，不修改容器内容。</remarks>
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
            // 空槽位或物品配置不匹配时跳过。
            if (item == null || item.GetItemID() != desc.ItemID)
            {
                continue;
            }
            // 数量非法或已达单格堆叠上限时跳过。
            if (item.GetItemCount() <= 0 || item.GetItemCount() >= maxStackCount)
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
    #endregion

    #region 核心逻辑
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

    /// <summary>按物品实例 UID 移出物品，执行移出前后的回调并返回原实例。</summary>
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

    /// <summary>把 size 写为非负容量，不移动或删除已经存在的物品。</summary>
    public virtual void SetContainerSize(int size) { _containerSize = Math.Max(0, size); }

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

    /// <summary>item 因 reason 变化后的扩展回调，基础容器不处理。</summary>
    public virtual void ContainerOnItemChange(ItemBase item, ItemChangeReason reason) { }

    /// <summary>角色初始化完成后的扩展入口，当前为空。</summary>
    public virtual void AfterInitGameRole() { }

    /// <summary>重连后的扩展入口，当前为空。</summary>
    public virtual void Reconnect() { }

    /// <summary>给指定实例增加 addCount；变成负数时拒绝，恰好为 0 时移除；averagePrice 当前未使用。</summary>
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

    /// <summary>item 成功放入之后调用，基础容器不处理。</summary>
    protected virtual void AfterSwapIn(ItemBase item) { }

    /// <summary>item 成功移出之后调用，基础容器不处理。</summary>
    protected virtual void AfterSwapOut(ItemBase item) { }

    /// <summary>item 从字典移除之前调用，基础容器不处理。</summary>
    protected virtual void BeforeSwapOut(ItemBase item) { }
    #endregion
}

// ============================================================
// 背包容器类
// ============================================================

/// <summary>普通背包容器。</summary>
public class PlainBag : RoItemContainer
{
    public PlainBag(KnapsackType type, PlayerEntity role) : base(type, role) { }
}

/// <summary>装备背包容器。</summary>
public class EquipBag : RoItemContainer
{
    public EquipBag(KnapsackType type, PlayerEntity role) : base(type, role) { }
}

/// <summary>材料背包容器。</summary>
public class MaterialBag : RoItemContainer
{
    public MaterialBag(KnapsackType type, PlayerEntity role) : base(type, role) { }
    public void CalculateContainerSize() { }
}

/// <summary>消耗品背包容器。</summary>
public class ConsumeBag : RoItemContainer
{
    public ConsumeBag(KnapsackType type, PlayerEntity role) : base(type, role) { }
    public override bool CheckIn(ItemBase item, int pos = InvalidItemPos) { return base.CheckIn(item, pos); }
}

/// <summary>保存角色当前穿戴的装备，格子位置代表装备部位。</summary>
public class BodyBag : RoItemContainer
{
    public BodyBag(KnapsackType type, PlayerEntity role) : base(type, role) { }
    /// <summary>预留的装备页切换标志，当前容器没有读取它的逻辑。</summary>
    public bool IsChangeEquipPage { get; set; }
    public override void CalcContainerAttr() { }
}

/// <summary>仓库容器。</summary>
public class Warehouse : RoItemContainer
{
    public Warehouse(KnapsackType type, PlayerEntity role) : base(type, role) { }
    public override bool CheckIn(ItemBase item, int pos = InvalidItemPos) { return base.CheckIn(item, pos); }
}

/// <summary>快捷栏容器。当前仅保存快捷栏物品，映射关系待协议模型接入。</summary>
public class ShortCutBag : RoItemContainer
{
    public ShortCutBag(KnapsackType type, PlayerEntity role) :
        base(type, role) { }

    // 快捷栏槽位到物品实例 UID 的映射。
    private readonly Dictionary<ulong, ulong> _shortcutMap = new Dictionary<ulong, ulong>();

    /// <summary>把快捷槽编号 uid 映射为物品 UID 再查询；映射未登记时返回 null。</summary>
    public ItemBase GetShortCutItemByUid(long uid)
    {
        if (uid <= 0 || !_shortcutMap.TryGetValue((ulong)uid, out ulong targetUid))
        {
            return null;
        }

        return GetItemByUid((long)targetUid);
    }
}

/// <summary>预留的货币专用容器，目前回调均为空；创建工厂实际为虚拟物品使用 PlainBag。</summary>
class MoneyBag : RoItemContainer
{
   
    /// <summary>预留的元气提示红点编号，当前类内尚未使用。</summary>
    public  static readonly int kVigourRedPointId = 92;

    public MoneyBag(KnapsackType type, PlayerEntity role) :
base(type, role) { }

    #region 事件回调
    // 经验变化的预留回调，当前无实现。
    private void OnExpChange(ItemBase item, ItemChangeReason reason)
    {
        
    }

    // 职业经验变化的预留回调，当前无实现。
    private void OnJobExpChange(ItemBase item, ItemChangeReason reason)
    {
        
    }

    // 元气变化的预留回调，当前无实现。
    private void OnVitalityChange(ItemBase item, ItemChangeReason reason)
    {
        
    }

    // 通行证经验变化的预留回调，当前无实现。
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

    void OnResearchLevelChange(int old_level, int new_level)
    {
        
    }
    #endregion

    void SendMonsterResearchPointsToRank(int score)
    {
        
    }

    /// <summary>货币容器的保留统计入口；item_id、enum_bind 当前均未使用，固定返回 0。</summary>
    public virtual Int64 CountItem(int item_id, ItemBind enum_bind)
    {
        return 0;
    }

    #region 核心逻辑
    // 物品进入货币容器的预留回调，当前无实现。
    public virtual void AfterSwapIn(ItemBase item, ITEM_SWAP_REASON swapin_reason = ITEM_SWAP_REASON.ISR_None)
    {
        
    }

    // 物品移出货币容器的预留回调，当前无实现。
    public virtual void AfterSwapOut(ItemBase item, ITEM_SWAP_REASON swapin_reason = ITEM_SWAP_REASON.ISR_None)
    {
        
    }

    // 货币物品变化的预留回调，当前无实现。
    public virtual void ContainerOnItemChange(ItemBase item, ItemChangeReason reason)
    {
        
    }
    #endregion
};
