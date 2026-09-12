using System;
using System.Collections.Generic;

/// <summary>
/// 客户端角色物品组件，管理背包容量、物品快照和本地变更通知。
/// </summary>
public class RoItemComponent : RoleComponentBase
{
    // 四类可扩容背包的最小初始容量。
    private const int MinOpenedGridCount = 81;
    // 数据库存储为 byte 时允许的最大容量。
    private const int MaxOpenedGridCount = byte.MaxValue;
    // 仅保存四类可扩容背包的权威容量。
    private readonly Dictionary<KnapsackType, int> _openedGridCounts =
        new Dictionary<KnapsackType, int>();
    /// <summary>物品模型发生变化时通知控制器刷新界面。</summary>
    public event Action<ItemBase, ChangeReason, UpdateType> ItemChanged;
    /// <summary>背包物品或容量发生变化时通知背包控制器。</summary>
    public event Action KnapsackChanged;
    /// <summary>背包发生细粒度变化时通知控制器，参数是不可变快照。</summary>
    public event Action<KnapsackChangedEventArgs> KnapsackChangedDetailed;
    /// <summary>
    /// 构造角色物品组件。
    /// </summary>
    /// <param name="owner">所属客户端角色，不能为空。</param>
    public RoItemComponent(ClientRole owner)
        : base(owner)
    {
    }

    // 背包容器按 KnapsackType 分类保存；容器本身负责物品位置和容量校验。
    private Dictionary<KnapsackType, RoItemContainer> _itemContainers;

    /// <summary>
    /// 初始化全部背包容器，并为四类可扩容背包设置默认 81 格容量。
    /// </summary>
    public override void Initialize()
    {
        _itemContainers = new Dictionary<KnapsackType, RoItemContainer>();
        _openedGridCounts.Clear();

        // 五类配置背包在角色组件初始化时一次性创建，避免首次访问时出现空容器。
        KnapsackType[] containerTypes =
        {
            KnapsackType.RolePackPlain,
            KnapsackType.RolePackEquip,
            KnapsackType.RolePackConsume,
            KnapsackType.RolePackMaterial,
            KnapsackType.RoleCurrtEquipPack,
            KnapsackType.RoleVirtualItemPack
        };

        for (int index = 0; index < containerTypes.Length; index++)
        {
            KnapsackType type = containerTypes[index];
            _itemContainers[type] = RoItemContainer.Create(type, Owner);
        }

        // 四类可扩容背包统一从最小 81 格初始化，穿戴栏不参与扩容。
        KnapsackType[] expandableTypes = GetExpandableTypes();
        for (int index = 0; index < expandableTypes.Length; index++)
        {
            KnapsackType type = expandableTypes[index];
            bool changed;
            ApplyOpenedGridCount(type, MinOpenedGridCount, out changed);
        }
    }

    /// <summary>获取虚拟物品背包中的指定物品数量。</summary>
    /// <param name="itemId">虚拟物品配置 ID，也就是商城配置中的 CurrencyItemId 值。</param>
    /// <returns>虚拟物品总数量。</returns>
    public long GetVirtualItemCount(int itemId)
    {
        RoItemContainer container = GetContainer(KnapsackType.RoleVirtualItemPack);
        if (container == null || !IsConfiguredVirtualItem(itemId))
        {
            return 0L;
        }

        return container.CountItem(itemId, ItemBind.kItemBindNo);
    }

    /// <summary>增加虚拟物品，虚拟物品没有格子容量限制。</summary>
    /// <param name="itemId">虚拟物品配置 ID。</param>
    /// <param name="count">增加数量。</param>
    /// <returns>成功返回 true。</returns>
    public bool GiveVirtualItem(int itemId, long count)
    {
        if (!IsConfiguredVirtualItem(itemId) || count <= 0 || count > int.MaxValue)
        {
            return false;
        }

        ItemBase item = AddItem(itemId, (int)count, new ChangeReason(ItemChangeReason.ITEM_REASON_NONE),
            ItemSign.IsNoSign, 0, 0L, UpdateType.eUT_Normal, new IdipParam());
        return item != null;
    }

    /// <summary>扣除虚拟物品；余额不足时不修改任何物品实例。</summary>
    /// <param name="itemId">虚拟物品配置 ID。</param>
    /// <param name="count">扣除数量。</param>
    /// <returns>成功返回 true，余额不足或参数非法返回 false。</returns>
    public bool TakeVirtualItem(int itemId, long count)
    {
        if (!IsConfiguredVirtualItem(itemId) || count <= 0)
        {
            return false;
        }

        long remaining = count;
        RoItemContainer container = GetContainer(KnapsackType.RoleVirtualItemPack);
        if (container == null || GetVirtualItemCount(itemId) < count)
        {
            return false;
        }

        List<ItemBase> stacks = new List<ItemBase>();
        List<ItemBase> containerItems = new List<ItemBase>(container.GetItemMap().Values);
        for (int index = 0; index < containerItems.Count; index++)
        {
            ItemBase item = containerItems[index];
            if (item.GetItemID() == itemId)
            {
                stacks.Add(item);
            }
        }

        for (int index = 0; index < stacks.Count; index++)
        {
            ItemBase item = stacks[index];
            if (remaining <= 0)
            {
                break;
            }

            int takeCount = (int)Math.Min((long)item.GetItemCount(), remaining);
            if (DeleteItem(item.GetItemUID(), takeCount,
                new ChangeReason(ItemChangeReason.ITEM_REASON_NONE), UpdateType.eUT_Normal))
            {
                remaining -= takeCount;
            }
        }

        return remaining == 0;
    }

    /// <summary>按背包类型获取容器；组件尚未初始化或类型不存在时返回 null。</summary>
    /// <param name="type">背包类型。</param>
    /// <returns>对应容器实例，未初始化或未知类型返回 null。</returns>
    public RoItemContainer GetContainer(KnapsackType type)
    {
        if (_itemContainers == null)
        {
            return null;
        }

        RoItemContainer container;
        if (_itemContainers.TryGetValue(type, out container))
        {
            return container;
        }
        return null;
    }

    /// <summary>获取指定可扩容背包的已开启格子数。</summary>
    /// <param name="type">背包类型，应为四类可扩容背包之一。</param>
    /// <returns>已开启格子数；未记录时回退到容器当前容量。</returns>
    public int GetOpenedGridCount(KnapsackType type)
    {
        int count;
        if (_openedGridCounts.TryGetValue(type, out count))
        {
            return count;
        }

        RoItemContainer container = GetContainer(type);
        if (container == null)
        {
            return 0;
        }
        return container.GetContainerSize();
    }

    /// <summary>更新扩容容量，统一校验类型和 81-255 范围。</summary>
    /// <param name="type">背包类型，必须为可扩容类型。</param>
    /// <param name="count">目标格子数，范围 [81, 255]。</param>
    /// <returns>设置成功返回 true，类型不合法或越界返回 false。</returns>
    public bool SetOpenedGridCount(KnapsackType type, int count)
    {
        if (!IsExpandableType(type) || count < MinOpenedGridCount || count > MaxOpenedGridCount)
        {
            return false;
        }
        bool changed;
        bool updated = ApplyOpenedGridCount(type, count, out changed);
        if (updated && changed)
        {
            NotifyKnapsackChanged(new KnapsackChangedEventArgs(
                KnapsackChangeType.CapacityChanged, type, 0L, -1, 0, 0,
                count, string.Empty, type, -1));
        }
        return updated;
    }

    /// <summary>加载中心服容量，低于最小值时按 81 格兼容。</summary>
    /// <param name="type">背包类型。</param>
    /// <param name="count">中心服存储的格子数，0 或负数视为无效。</param>
    /// <returns>加载成功返回 true。</returns>
    public bool LoadOpenedGridCount(KnapsackType type, int count)
    {
        if (!IsExpandableType(type) || count <= 0 || count > MaxOpenedGridCount)
        {
            return false;
        }
        bool changed;
        return ApplyOpenedGridCount(type, Math.Max(MinOpenedGridCount, count), out changed);
    }

    /// <summary>统一写入容量字典并同步对应容器。</summary>
    /// <param name="type">背包类型。</param>
    /// <param name="count">目标格子数。</param>
    /// <returns>成功返回 true；类型不可扩容、容器为空或越界返回 false。</returns>
    private bool ApplyOpenedGridCount(KnapsackType type, int count, out bool changed)
    {
        changed = false;
        RoItemContainer container = GetContainer(type);
        if (!IsExpandableType(type) || container == null || count < MinOpenedGridCount || count > MaxOpenedGridCount)
        {
            return false;
        }
        int currentCount = GetOpenedGridCount(type);
        if (currentCount == count)
        {
            return true;
        }
        container.SetContainerSize(count);
        _openedGridCounts[type] = count;
        changed = true;
        return true;
    }

    /// <summary>判断是否为四类可扩容背包。</summary>
    /// <param name="type">待判断的背包类型。</param>
    /// <returns>可扩容返回 true，穿戴栏等返回 false。</returns>
    private static bool IsExpandableType(KnapsackType type)
    {
        return type == KnapsackType.RolePackPlain || type == KnapsackType.RolePackEquip ||
            type == KnapsackType.RolePackConsume || type == KnapsackType.RolePackMaterial;
    }

    /// <summary>获取四类可扩容背包的类型数组，用于批量初始化与遍历。</summary>
    /// <returns>包含四类可扩容背包类型的新数组。</returns>
    private static KnapsackType[] GetExpandableTypes()
    {
        return new[]
        {
            KnapsackType.RolePackPlain,
            KnapsackType.RolePackEquip,
            KnapsackType.RolePackConsume,
            KnapsackType.RolePackMaterial
        };
    }

    /// <summary>返回容器只读视图，供客户端业务查询。</summary>
    /// <returns>背包类型到容器的只读字典。</returns>
    public IReadOnlyDictionary<KnapsackType, RoItemContainer> GetContainers()
    {
        return _itemContainers;
    }

    /// <summary>
    /// 从角色快照装载全部背包数据：先恢复容量，再清空并按 UID 去重装入物品。
    /// </summary>
    /// <param name="ret">中心服返回的角色背包信息回包。</param>
    public void LoadKnapsack(RoleKanpsackInfoRet ret)
    {
        if (ret == null || ret.CmdCode != CmdCode.Succeed || ret.RoleKanpsackInfo == null || _itemContainers == null)
        {
            return;
        }

        RoleKanpsackInfo info = ret.RoleKanpsackInfo;

        // 先恢复每类背包的已开启容量。
        for (int index = 0; index < info.KanpsackTypeCountLst.Count; index++)
        {
            Kanpsacktypecount packCount = info.KanpsackTypeCountLst[index];
            LoadOpenedGridCount((KnapsackType)packCount.Type, packCount.Count);
        }

        // 清空所有容器内存状态，准备全量装载。
        List<RoItemContainer> containers = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containers.Count; index++)
        {
            RoItemContainer container = containers[index];
            container.Clear();
        }

        // role_pack_plain 仅包含普通背包；各分类列表独立装入对应真实容器，UID 去重用于防御异常重复数据。
        HashSet<long> loadedItemUids = new HashSet<long>();
        LoadItems(info.RolePackPlain, loadedItemUids);
        LoadItems(info.RolePackEquip, loadedItemUids);
        LoadItems(info.RolePackConsume, loadedItemUids);
        LoadItems(info.RolePackMaterial, loadedItemUids);
        LoadItems(info.RoleCurrtEquipPack, loadedItemUids);
        LoadItems(info.RoleVirtualItemPack, loadedItemUids);
        NotifyKnapsackChanged(new KnapsackChangedEventArgs(
            KnapsackChangeType.Full, KnapsackType.RolePackPlain, 0L, -1, 0, 0,
            0, string.Empty, KnapsackType.RolePackPlain, -1));
    }

    /// <summary>
    /// 只替换服务器返回的虚拟物品背包快照，不影响普通背包和装备容器。
    /// </summary>
    /// <param name="itemInfos">服务器返回的最新虚拟物品列表。</param>
    public void ApplyVirtualItemSnapshot(IList<RoleItemInfo> itemInfos)
    {
        if (_itemContainers == null)
        {
            return;
        }

        RoItemContainer virtualContainer = GetContainer(KnapsackType.RoleVirtualItemPack);
        if (virtualContainer == null)
        {
            return;
        }

        List<RoItemContainer> containers = new List<RoItemContainer>(_itemContainers.Values);
        HashSet<long> loadedItemUids = new HashSet<long>();
        for (int containerIndex = 0; containerIndex < containers.Count; containerIndex++)
        {
            RoItemContainer container = containers[containerIndex];
            if (container == null || container == virtualContainer)
            {
                continue;
            }

            List<ItemBase> items = new List<ItemBase>(container.GetItemMap().Values);
            for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                ItemBase item = items[itemIndex];
                if (item != null && item.GetItemUID() > 0)
                {
                    loadedItemUids.Add(item.GetItemUID());
                }
            }
        }

        virtualContainer.Clear();
        LoadItems(itemInfos, loadedItemUids);
        NotifyKnapsackChanged(new KnapsackChangedEventArgs(
            KnapsackChangeType.Full, KnapsackType.RoleVirtualItemPack, 0L, -1, 0, 0,
            0, string.Empty, KnapsackType.RoleVirtualItemPack, -1));
    }

    /// <summary>
    /// 应用服务器推送的物品增量；该入口只更新客户端投影，不产生本地持久化脏标记。
    /// </summary>
    /// <param name="update">服务器返回的增量物品数据。</param>
    public void ApplyServerItemDelta(UpdateItem update)
    {
        if (update == null || _itemContainers == null)
        {
            return;
        }

        for (int deleteIndex = 0; deleteIndex < update.DeleteList.Count; deleteIndex++)
        {
            MapKey64Value32 deleteInfo = update.DeleteList[deleteIndex];
            if (deleteInfo == null)
            {
                continue;
            }
            RemoveServerItem(deleteInfo.Key, (ItemChangeReason)deleteInfo.Val);
        }

        for (int itemIndex = 0; itemIndex < update.ItemList.Count; itemIndex++)
        {
            RoleItemInfo itemInfo = update.ItemList[itemIndex];
            ApplyServerItem(itemInfo);
        }
    }

    /// <summary>移除服务器确认已删除的物品实例，并通知背包控制器刷新对应格子。</summary>
    private void RemoveServerItem(ulong itemUid, ItemChangeReason reason)
    {
        if (itemUid == 0UL)
        {
            return;
        }

        ItemBase item = GetItemByUID((long)itemUid);
        if (item == null)
        {
            return;
        }

        KnapsackType bagType = (KnapsackType)item.GetBagType();
        int bagIndex = item.GetBagIndex();
        int itemId = item.GetItemID();
        RoItemContainer container = GetContainer(bagType);
        if (container == null || container.SwapOutByUID((long)itemUid) == null)
        {
            return;
        }

        item.SetItemCount(0);
        NotifyKnapsackChanged(new KnapsackChangedEventArgs(
            KnapsackChangeType.Removed, bagType, (long)itemUid, bagIndex, itemId, 0,
            GetOpenedGridCount(bagType), GetItemIconPath(itemId), bagType, bagIndex));
    }

    /// <summary>新增或替换服务器推送的单个物品实例，保持 UID 和格位与服务器一致。</summary>
    private void ApplyServerItem(RoleItemInfo itemInfo)
    {
        if (itemInfo == null || itemInfo.ItemUid <= 0 || itemInfo.ItemTypeId <= 0 ||
            itemInfo.Count <= 0 || itemInfo.BagIndex < 0)
        {
            return;
        }

        ItemBase oldItem = GetItemByUID(itemInfo.ItemUid);
        KnapsackType targetType = (KnapsackType)itemInfo.BagType;
        RoItemContainer targetContainer = GetContainer(targetType);
        if (targetContainer == null)
        {
            return;
        }

        int oldBagIndex = -1;
        KnapsackType oldBagType = targetType;
        if (oldItem != null)
        {
            oldBagIndex = oldItem.GetBagIndex();
            oldBagType = (KnapsackType)oldItem.GetBagType();
            RoItemContainer oldContainer = GetContainer(oldBagType);
            if (oldContainer != null)
            {
                oldContainer.SwapOutByUID(itemInfo.ItemUid);
            }
        }

        ItemBase item = CreateLoadedItem(itemInfo);
        if (item == null || !targetContainer.SwapIn(item, itemInfo.BagIndex))
        {
            return;
        }

        KnapsackChangeType changeType = KnapsackChangeType.Added;
        if (oldItem != null)
        {
            changeType = KnapsackChangeType.CountChanged;
            if (oldBagType != targetType || oldBagIndex != itemInfo.BagIndex)
            {
                changeType = KnapsackChangeType.Moved;
            }
        }
        NotifyKnapsackChanged(new KnapsackChangedEventArgs(
            changeType, targetType, itemInfo.ItemUid, itemInfo.BagIndex, itemInfo.ItemTypeId,
            itemInfo.Count, GetOpenedGridCount(targetType), GetItemIconPath(itemInfo.ItemTypeId),
            oldBagType, oldBagIndex));
    }

    /// <summary>读取物品图标路径，供服务器增量通知转换为不可变 UI 快照。</summary>
    private static string GetItemIconPath(int itemId)
    {
        ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(itemId);
        if (config == null || string.IsNullOrEmpty(config.Icon))
        {
            return string.Empty;
        }
        return config.Icon;
    }

    /// <summary>组件装载完成后的回调，当前无额外逻辑。</summary>
    public override void AfterLoad()
    {
        
    }

    /// <summary>组件被添加到角色后的回调，当前无额外逻辑。</summary>
    public override void AfterAddedToWorld()
    {
        
    }
    /// <summary>
    /// 获取背包容器还有多少空格子
    /// </summary>
    /// <param name="type"></param>
    /// <param name="page_num">如果是仓库看看是哪一页</param>
    /// <returns></returns>
    public int GetLeftSpace(KnapsackType type, int page_num=1)
    {
        RoItemContainer container = GetContainerByType(type,page_num);
        if (container == null)
        {
            return 0;
        }
        return container.GetLeftSpace();
    }

    /// <summary>
    /// 通过类型获取容器基类
    /// </summary>
    /// <param name="type"></param>
    /// <param name="pageNum"></param>
    /// <returns></returns>
    public RoItemContainer GetContainerByType(KnapsackType type, int pageNum =1)
    {
        RoItemContainer ret = null;
        if (_itemContainers.ContainsKey(type))
        {
            //todo 后面可能有仓库
            ret =  _itemContainers[type];
            //
        }
        return ret;
    }
    /// <summary>
    /// 获取该物品最大堆叠数量
    /// </summary>
    /// <param name="item_id"></param>
    /// <returns></returns>
    public int MaxOverlap(int item_id)
    {
        ItemConfigBase itemData = LubanMgr.Instance.GetItemConfigById(item_id);
        if (itemData==null)
        {
            LogMsg.Info($"item cannot found in item table item id is [{item_id}]");
            return 1;
        }
        if (itemData.MaxStackCount > 1)
        {
            return itemData.MaxStackCount;
        }
        return 1;
    }
    /// <summary>组件每帧更新，当前物品组件无需逐帧逻辑。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        
    }

    /// <summary>释放组件持有的容器和事件订阅。</summary>
    public override void Dispose()
    {
        if (_itemContainers != null)
        {
            _itemContainers.Clear();
        }
        ItemChanged = null;
        KnapsackChanged = null;
        KnapsackChangedDetailed = null;
        base.Dispose();
    }

    /// <summary>
    /// 统计指定配置物品 ID 在背包中的总数量。
    /// 默认只统计非绑定物品，找到第一个非空容器即可提前返回；
    /// calc_all_container 为 true 时遍历全部容器累加。
    /// </summary>
    /// <param name="itemId">配置物品 ID，必须为 int 且大于 0。</param>
    /// <param name="calc_bind_item">预留参数，当前实现固定统计非绑定物品。</param>
    /// <param name="calc_all_container">是否遍历全部容器累加；false 时找到即停。</param>
    /// <returns>物品总数量，超过 int.MaxValue 时截断为 int.MaxValue。</returns>
    public int GetItemNum(int itemId, ItemBind calc_bind_item = ItemBind.kItemBindNo, bool calc_all_container = false)
    {
        if (!(itemId is int configuredItemId) || 
            configuredItemId <= 0 || _itemContainers == null)
        {
            return 0;
        }

        RoItemContainer Itemcontainer = GetDefineContiner(itemId);
        if (Itemcontainer == null)
        {
            return -1;
        }
        
        if (!calc_all_container) //> 只统计默认容器
        {
            return (int)Itemcontainer.CountItem(itemId, calc_bind_item);
        }
        
        long total = 0;
        List<RoItemContainer> containers = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containers.Count; index++)
        {
            RoItemContainer container = containers[index];
            // 当前固定统计非绑定物品（kItemBindNo）。
            total += container.CountItem(configuredItemId, calc_bind_item);
            // 非全容器模式下，只要找到数量就提前终止遍历。
            if (!calc_all_container && total > 0)
            {
                break;
            }
        }

        if (total > int.MaxValue)
        {
            return int.MaxValue;
        }
        return (int)total;
    }

    /// <summary>
    /// 向角色背包添加物品，自动堆叠到未满的同类同标记堆叠，不足时占用空格新建堆叠。
    /// 武器类配置生成 EquipBase 实例，其余生成 ItemBase 实例。
    /// 每次数量变更或新建实例都会触发 OnItemChange 通知控制器。
    /// </summary>
    /// <param name="itemId">配置物品 ID。</param>
    /// <param name="itemCount">添加数量。</param>
    /// <param name="reason">物品变更原因，用于日志与审计。</param>
    /// <param name="itemSign">物品绑定标记。</param>
    /// <param name="moneyType">单价货币类型。</param>
    /// <param name="price">物品单价。</param>
    /// <param name="updateType">更新类型，控制客户端同步方式。</param>
    /// <param name="idipParam">预留 IDIP 参数。</param>
    /// <returns>最后一次添加或堆叠的物品实例；参数非法、配置缺失或空间不足返回 null。</returns>
    public ItemBase AddItem(int itemId, int itemCount, ChangeReason reason, 
        ItemSign itemSign, int moneyType, long price, UpdateType updateType, IdipParam idipParam)
    {
        if (Owner == null || itemId <= 0 || itemCount <= 0 || price < 0)
        {
            return null;
        }
        ItemConfigBase itemData = LubanMgr.Instance.GetItemConfigById(itemId);
        RoItemContainer container = GetDefineContiner(itemId);
        if (container == null || container.GetItemMap() == null)
        {
            return null;
        }
        if (itemData == null)
        {
            return null;
        }

        // 单个堆叠的最大数量，配置为 0 或负数时按 1 处理。
        int maxStackCount = int.MaxValue;
        if (itemData != null)
        {
            maxStackCount = Math.Max(1, itemData.MaxStackCount);
        }
        int remainingCount = itemCount;
        // 收集所有同 ID、同标记且未满的堆叠，优先填充这些堆叠。
        List<ItemBase> matchingStacks = new List<ItemBase>();
        List<ItemBase> containerItems = new List<ItemBase>(container.GetItemMap().Values);
        for (int index = 0; index < containerItems.Count; index++)
        {
            ItemBase current = containerItems[index];
            if (current.GetItemID() == itemId && current.GetItemSign() == (int)itemSign &&
                current.GetItemCount() < maxStackCount)
            {
                matchingStacks.Add(current);
            }
        }

        // 使用 long 计算可用堆叠容量，避免多个堆叠容量相加时发生 int 溢出。
        long availableStackCount = 0L;
        for (int index = 0; index < matchingStacks.Count; index++)
        {
            ItemBase stack = matchingStacks[index];
            long stackCapacity = Math.Max(0, maxStackCount - stack.GetItemCount());
            // 可用容量不超过本次添加总量，多余容量无意义。
            availableStackCount = Math.Min((long)itemCount, availableStackCount + stackCapacity);
        }

        // 填充已有堆叠后剩余的数量，需要占用新格子。
        long remainingAfterFill = itemCount - availableStackCount;
        // 向上取整计算需要的新格子数。
        long requiredSlotsLong = (remainingAfterFill + maxStackCount - 1L) / maxStackCount;
        if (requiredSlotsLong > int.MaxValue)
        {
            return null;
        }

        int requiredSlots = (int)requiredSlotsLong;
        // 空格不足时整体失败，不做部分添加。
        if (container.GetEmptySize() < requiredSlots)
        {
            return null;
        }

        ItemBase lastAdded = null;
        // 第一阶段：填充已有未满堆叠。
        for (int index = 0; index < matchingStacks.Count; index++)
        {
            ItemBase stack = matchingStacks[index];
            if (remainingCount <= 0)
            {
                break;
            }

            int canAdd = Math.Min(remainingCount, maxStackCount - stack.GetItemCount());
            if (canAdd <= 0)
            {
                continue;
            }

            stack.SetItemCount(stack.GetItemCount() + canAdd);
            // 累加总价：原总价 + 本次添加数量 × 单价，带溢出保护。
            stack.SetTotalPrice(SafeAddPrice(stack.GetTotalPrice(), price, canAdd));
            remainingCount -= canAdd;
            lastAdded = stack;
            OnItemChange(stack, reason, updateType, KnapsackChangeType.CountChanged);
        }

        // 第二阶段：占用空格新建堆叠，直到剩余数量全部放入。
        while (remainingCount > 0)
        {
            int count = Math.Min(remainingCount, maxStackCount);
            ItemBase item;
            EquipItemConfig equipConfig = itemData as EquipItemConfig;
            if (equipConfig != null)
            {
                // 武器类配置生成装备实例并记录装备类型。
                item = new EquipBase
                {
                    EquipType = equipConfig.Config.EquipType
                };
                item.SetItemID(itemId);
                item.SetItemCount(count);
                item.SetItemSign((int)itemSign);
            }
            else
            {
                item = new ItemBase(itemId, count, (int)itemSign);
            }
            item.SetPrice(moneyType, SafeAddPrice(0, price, count));
            if (!container.SwapIn(item))
            {
                return null;
            }

            OnItemChange(item, reason, updateType, KnapsackChangeType.Added);
            lastAdded = item;
            remainingCount -= count;
        }

        return lastAdded;
    }

    /// <summary>删除指定实例的全部或部分数量，并统一通知客户端控制器。</summary>
    /// <param name="itemUid">物品实例 UID。</param>
    /// <param name="count">删除数量；等于当前数量时移除整个实例。</param>
    /// <param name="reason">变更原因。</param>
    /// <param name="updateType">更新类型。</param>
    /// <returns>删除成功返回 true；物品不存在或数量非法返回 false。</returns>
    public bool DeleteItem(long itemUid, int count, ChangeReason reason,
        UpdateType updateType)
    {
        RoItemContainer container = FindContainer(itemUid);
        ItemBase item = null;
        if (container != null)
        {
            item = container.GetItemByUid(itemUid);
        }
        if (item == null || count <= 0 || count > item.GetItemCount())
        {
            return false;
        }

        // 删除全部数量时从容器移除实例。
        if (count == item.GetItemCount())
        {
            ItemBase removed = container.SwapOutByUID(itemUid);
            if (removed == null)
            {
                return false;
            }
            OnItemChange(removed, reason, updateType, KnapsackChangeType.Removed,
                (KnapsackType)removed.GetBagType(), removed.GetBagIndex(), 0);
            return true;
        }

        // 部分删除只修改数量。
        if (!container.ModifyCount(itemUid, -count))
        {
            return false;
        }
        OnItemChange(item, reason, updateType, KnapsackChangeType.CountChanged);
        return true;
    }

    /// <summary>删除完整物品实例并通知客户端控制器刷新。</summary>
    public bool DeleteItem(long itemUid, ChangeReason reason)
    {
        ItemBase item = GetItemByUID(itemUid);

        if (item == null)
        {
            return false;
        }
        
        var container = GetContainer((KnapsackType)item.GetBagType());
        
        long originItemCount = item.GetItemCount();
        int tlogContainerType = item.GetBagType();

        if (container != null)
        {
            container.SwapOutByUID(item.GetItemUID());
            // 道具删除需要同步到ms，先将数量置为0
            item.SetItemCount(0);
            // 走容器道具变化接口
            container.ContainerOnItemChange(item, reason);
            container.OnSwapEnd();
        }
        
        
       
       
        
        LogMsg.Info("[道具系统]角色编号=" + Owner.GetID() + "，删除物品 UID=" +
            itemUid + "，配置编号=" + item.GetItemID() + "，数量=" + originItemCount);
        OnItemChange(item, reason, UpdateType.eUT_Update_All, KnapsackChangeType.Removed,
            (KnapsackType)item.GetBagType(), item.GetBagIndex(), 0);
        return true;
    }

  

    /// <summary>
    /// 通知任务模块
    /// </summary>
    /// <param name="item"></param>
    /// <param name="i"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void NtfTaskModleItemChange(ItemBase item, int i)
    {
        
    }

    //通过UID得到指定道具
    private ItemBase GetItemByUID(long itemUid)
    {
        List<RoItemContainer> containers = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containers.Count; index++)
        {
            RoItemContainer container = containers[index];
            if (container == null)
            {
                continue;
            }

            ItemBase item = container.GetItemByUid(itemUid);
            if (item != null)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>按增量修改物品数量，数量归零时转为删除实例。</summary>
    /// <param name="itemUid">物品实例 UID。</param>
    /// <param name="deltaCount">数量增量，正数增加、负数减少。</param>
    /// <param name="reason">变更原因。</param>
    /// <param name="updateType">更新类型。</param>
    /// <returns>修改成功返回 true；物品不存在或增量为 0 返回 false。</returns>
    public bool ModifyItem(long itemUid, int deltaCount, ChangeReason reason, UpdateType updateType)
    {
        RoItemContainer container = FindContainer(itemUid);
        ItemBase item = null;
        if (container != null)
        {
            item = container.GetItemByUid(itemUid);
        }
        if (item == null || deltaCount == 0)
        {
            return false;
        }
        ItemConfigBase itemConfig = LubanMgr.Instance.GetItemConfigById(item.GetItemID());
        int maxStackCount = 1;
        if (itemConfig != null)
        {
            maxStackCount = Math.Max(1, itemConfig.MaxStackCount);
        }
        long targetCount = (long)item.GetItemCount() + deltaCount;
        // 正增量不得突破配置的单实例最大堆叠数，负增量也不能减成负数。
        if (targetCount < 0L || targetCount > maxStackCount)
        {
            return false;
        }
        // 减少量恰好等于当前数量时走删除实例路径。
        if (deltaCount < 0 && -deltaCount == item.GetItemCount())
        {
            return DeleteItem(itemUid, item.GetItemCount(), reason, updateType);
        }
        if (!container.ModifyCount(itemUid, deltaCount))
        {
            return false;
        }

        OnItemChange(item, reason, updateType, KnapsackChangeType.CountChanged);
        return true;
    }

    /// <summary>
    /// 在两个真实容器之间移动实例；源和目标容器都会进入同一变更版本。
    /// 同容器内仅调整格子位置；跨容器移动时若目标放入失败会回滚到源容器原位置。
    /// </summary>
    /// <param name="itemUid">待移动物品实例 UID。</param>
    /// <param name="sourceType">源背包类型。</param>
    /// <param name="targetType">目标背包类型。</param>
    /// <param name="targetGridIndex">目标格子索引。</param>
    /// <param name="reason">变更原因。</param>
    /// <param name="updateType">更新类型。</param>
    /// <returns>移动成功返回 true；参数非法或目标格被其他物品占用返回 false。</returns>
    public bool SwapItem(long itemUid, KnapsackType sourceType, KnapsackType targetType, int targetGridIndex,
        ChangeReason reason, UpdateType updateType)
    {
        RoItemContainer source = GetContainer(sourceType);
        RoItemContainer target = GetContainer(targetType);
        ItemBase item = null;
        if (source != null)
        {
            item = source.GetItemByUid(itemUid);
        }
        if (item == null || target == null || targetGridIndex < 0)
        {
            return false;
        }

        // 同容器移动：仅调整格子位置，目标格不能被其他物品占用。
        if (source == target)
        {
            ItemBase occupied = target.GetItemByPos(targetGridIndex);
            if (occupied != null && occupied.GetItemUID() != itemUid)
            {
                return false;
            }
            int sameSourceGridIndex = item.GetBagIndex();
            item.SetBagIndex(targetGridIndex);
            OnItemChange(item, reason, updateType, KnapsackChangeType.Moved,
                sourceType, sameSourceGridIndex, item.GetItemCount());
            return true;
        }

        // 跨容器移动：记录源格子索引用于回滚。
        int sourceGridIndex = item.GetBagIndex();
        // 先校验目标格可放入，再从源容器移除；任一步失败则整体不移动。
        if (!target.CheckIn(item, targetGridIndex) || source.SwapOutByUID(itemUid) == null)
        {
            return false;
        }

        if (!target.SwapIn(item, targetGridIndex))
        {
            // 目标放入失败时回滚：将物品放回源容器原位置。
            source.SwapIn(item, sourceGridIndex);
            return false;
        }

        // 客户端只维护当前快照，跨容器移动完成后发出一次模型变更事件。
        OnItemChange(item, reason, updateType, KnapsackChangeType.Moved,
            sourceType, sourceGridIndex, item.GetItemCount());
        return true;
    }

    /// <summary>
    /// 所有客户端物品增删改入口最终都通过此函数发布模型变更事件。
    /// </summary>
    /// <param name="item">发生变更的物品实例。</param>
    /// <param name="reason">变更原因。</param>
    /// <param name="updateType">更新类型。</param>
    public void OnItemChange(ItemBase item, ChangeReason reason,
        UpdateType updateType, KnapsackChangeType changeType = KnapsackChangeType.Updated,
        KnapsackType previousBagType = KnapsackType.RolePackPlain,
        int previousBagIndex = -1, int countOverride = -1)
    {
        if (item == null || Owner == null)
        {
            return;
        }

        Action<ItemBase, ChangeReason, UpdateType> handler = ItemChanged;
        if (handler != null)
        {
            handler.Invoke(item, reason, updateType);
        }
        int count = item.GetItemCount();
        if (countOverride >= 0)
        {
            count = countOverride;
        }
        KnapsackType bagType = (KnapsackType)item.GetBagType();
        string iconPath = string.Empty;
        ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(item.GetItemID());
        if (config != null)
        {
            iconPath = config.Icon;
        }
        NotifyKnapsackChanged(new KnapsackChangedEventArgs(
            changeType, bagType, item.GetItemUID(), item.GetBagIndex(),
            item.GetItemID(), count, GetOpenedGridCount(bagType), iconPath,
            previousBagType, previousBagIndex));
    }

    /// <summary>统一发布背包状态变化，避免界面直接读取协议快照。</summary>
    private void NotifyKnapsackChanged(KnapsackChangedEventArgs change)
    {
        if (change != null)
        {
            bool fullRefresh = change.ChangeType == KnapsackChangeType.Full ||
                change.ChangeType == KnapsackChangeType.CapacityChanged;
            ClientChangeContext context = new ClientChangeContext("Knapsack");
            Owner.DirtyTracker.MarkKnapsack(change.BagType, fullRefresh,
                change.BagIndex, context);
            if (change.ChangeType == KnapsackChangeType.Moved)
            {
                // 移动始终标记旧格位；跨背包移动还会标记源页，供拍卖行等事务复用。
                Owner.DirtyTracker.MarkKnapsack(change.PreviousBagType, false,
                    change.PreviousBagIndex, context);
            }
        }
    }

   
    

    /// <summary>
    /// 根据物品配置 ID 确定其所属的背包容器。
    /// 配置中 PackType 决定物品应放入的背包类型。
    /// </summary>
    /// <param name="itemId">配置物品 ID。</param>
    /// <returns>对应容器实例；配置缺失或类型未注册返回 null。</returns>
    private RoItemContainer GetDefineContiner(int itemId)
    {
        RoItemContainer ret = null;
        ItemConfigBase item_data = LubanMgr.Instance.GetItemConfigById(itemId);
        
        if (item_data != null)
        {
            if (_itemContainers.ContainsKey(item_data.PackType))
            {
                ret =  _itemContainers[item_data.PackType];
            }
        }
        return ret;
    }

    /// <summary>
    /// 校验虚拟物品是否同时满足公共编号范围和 Luban 配置分类。
    /// 公共层只负责编号段判断，GameServer 在实际增减前必须确认配置存在且属于虚拟物品背包。
    /// </summary>
    /// <param name="itemId">待校验的物品配置 ID。</param>
    /// <returns>配置存在、主类型为货币且背包类型为虚拟物品背包时返回 true。</returns>
    private static bool IsConfiguredVirtualItem(int itemId)
    {
        if (!ItemDesc.IsLegalVirtualItem(itemId))
        {
            return false;
        }

        ItemConfigBase itemConfig = LubanMgr.Instance.GetItemConfigById(itemId);
        if (itemConfig == null)
        {
            return false;
        }

        return itemConfig.ItemMainType == ItemMainType.ItemType_Money &&
            itemConfig.PackType == KnapsackType.RoleVirtualItemPack;
    }

    /// <summary>
    /// 将一组协议物品信息装载到对应背包容器，按 UID 全局去重避免重复装载。
    /// 装载失败的物品会从去重集合移除并记录错误日志。
    /// </summary>
    /// <param name="itemInfos">某类背包的物品信息列表。</param>
    /// <param name="loadedItemUids">全局已装载 UID 集合，用于跨背包去重。</param>
    private void LoadItems(IList<RoleItemInfo> itemInfos, HashSet<long> loadedItemUids)
    {
        if (itemInfos == null)
        {
            return;
        }
        for (int index = 0; index < itemInfos.Count; index++)
        {
            RoleItemInfo itemInfo = itemInfos[index];
            // UID 非法或已在其他背包中装载过则跳过。
            if (itemInfo == null || itemInfo.ItemUid <= 0 || !loadedItemUids.Add(itemInfo.ItemUid))
            {
                continue;
            }

            RoItemContainer container = GetContainer((KnapsackType)itemInfo.BagType);
            ItemBase item = CreateLoadedItem(itemInfo);
            if (container == null || item == null || !container.SwapIn(item, itemInfo.BagIndex))
            {
                // 装载失败时回退去重标记，便于后续排查。
                loadedItemUids.Remove(itemInfo.ItemUid);
                LogMsg.Info("角色背包物品装载失败，ItemUID=" + itemInfo.ItemUid, LogMsgType.Error);
            }
        }
    }

    /// <summary>
    /// 从协议物品信息重建内存物品实例，支持普通物品与装备（含强化等级和词条）。
    /// 创建时间与过期时间按 UTC Ticks 恢复，非法值跳过。
    /// </summary>
    /// <param name="itemInfo">中心服返回的单条物品协议信息。</param>
    /// <returns>重建后的物品实例；字段非法返回 null。</returns>
    private static ItemBase CreateLoadedItem(RoleItemInfo itemInfo)
    {
        if (itemInfo.ItemTypeId <= 0 || itemInfo.Count <= 0 || itemInfo.BagIndex < 0)
        {
            return null;
        }

        ItemBase item;
        // 携带装备信息时重建为 EquipBase，包含强化等级和词条。
        if (itemInfo.EquipInfo != null)
        {
            EquipBase equip = EquipBase.CreateEquipForRestore();
            equip.EquipType = itemInfo.EquipInfo.EquipType;
            equip.StrengthenLevel = itemInfo.EquipInfo.StrengthenLevel;
            // 词条信息可能为空，按存在性恢复三个词条槽位。
            if (itemInfo.EquipGeneInfo != null)
            {
                equip.GeneID0 = itemInfo.EquipGeneInfo.GeneId0;
                equip.GeneID1 = itemInfo.EquipGeneInfo.GeneId1;
                equip.GeneID2 = itemInfo.EquipGeneInfo.GeneId2;
                equip.GeneValue0 = itemInfo.EquipGeneInfo.GeneValue0;
                equip.GeneValue1 = itemInfo.EquipGeneInfo.GeneValue1;
                equip.GeneValue2 = itemInfo.EquipGeneInfo.GeneValue2;
            }
            item = equip;
        }
        else
        {
            item = ItemBase.CreateForRestore();
        }

        item.SetItemUID(itemInfo.ItemUid);
        item.SetItemID(itemInfo.ItemTypeId);
        item.SetItemCount(itemInfo.Count);
        item.SetItemSign(itemInfo.ItemSign);
        item.SetPrice(itemInfo.MoneyType, itemInfo.TotalPrice);
        item.SetLocation(itemInfo.RoleId, itemInfo.BagType, itemInfo.BagIndex);
        // 创建时间按 UTC Ticks 恢复，0 或超出范围视为未设置。
        if (itemInfo.CreateTimeUtcTicks > 0 && itemInfo.CreateTimeUtcTicks <= DateTime.MaxValue.Ticks)
        {
            item.SetCreateTime(new DateTime(itemInfo.CreateTimeUtcTicks, DateTimeKind.Utc));
        }
        // 过期时间按 UTC Ticks 恢复。
        if (itemInfo.ExpireTimeUtcTicks > 0 && itemInfo.ExpireTimeUtcTicks <= DateTime.MaxValue.Ticks)
        {
            item.SetExpireTime(new DateTime(itemInfo.ExpireTimeUtcTicks, DateTimeKind.Utc));
        }
        return item;
    }

    /// <summary>
    /// 遍历全部背包容器，按 UID 查找物品所在容器。
    /// </summary>
    /// <param name="itemUid">物品实例 UID。</param>
    /// <returns>包含该物品的容器；未找到或未初始化返回 null。</returns>
    private RoItemContainer FindContainer(long itemUid)
    {
        if (_itemContainers == null)
        {
            return null;
        }
        List<RoItemContainer> containers = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containers.Count; index++)
        {
            RoItemContainer container = containers[index];
            if (container.GetItemByUid(itemUid) != null)
            {
                return container;
            }
        }
        return null;
    }

    /// <summary>
    /// 安全累加物品总价，防止 long 溢出。
    /// 当 单价 × 数量 超过剩余可表示范围时截断为 long.MaxValue。
    /// </summary>
    /// <param name="currentPrice">当前总价。</param>
    /// <param name="unitPrice">物品单价。</param>
    /// <param name="count">物品数量。</param>
    /// <returns>累加后的总价；溢出时返回 long.MaxValue。</returns>
    private static long SafeAddPrice(long currentPrice, long unitPrice, int count)
    {
        if (unitPrice <= 0 || count <= 0)
        {
            return currentPrice;
        }
        // 提前判断溢出：unitPrice * count > long.MaxValue - currentPrice
        if (unitPrice > (long.MaxValue - currentPrice) / count)
        {
            return long.MaxValue;
        }
        return currentPrice + unitPrice * count;
    }

    public CmdCode ReduceItem(int descItemId, long ItemCount, ItemChangeReason reason, ItemBind descBindOptionForTake)
    {
        if (Owner == null)
        {
            return CmdCode.RoleNotExist;
        }
        if (ItemCount <= 0)
        {
            return CmdCode.ItemNotExist;
        }
        
        RoItemContainer container = GetDefineContiner(descItemId);
        if (container == null || null == container.GetItemMap())
        {
            return CmdCode.ItemNotExist;
        }
        List<ItemBase> item_list = GetItemForReduce(descItemId, descBindOptionForTake);
        if (item_list == null || item_list.Count == 0)
        {
            return CmdCode.ItemNotExist;
        }
        long totalCount = 0L;
        for (int index = 0; index < item_list.Count; index++)
        {
            totalCount += item_list[index].GetItemCount();
            if (totalCount >= ItemCount)
            {
                break;
            }
        }
        if (totalCount < ItemCount)
        {
            return CmdCode.InsufficientItems;
        }

        long remainingCount = ItemCount;
        List<KeyValuePair<long, int>> reducePlan = new List<KeyValuePair<long, int>>();
        for (int index = 0; index < item_list.Count; index++)
        {
            ItemBase item = item_list[index];
            int reduceCount = (int)Math.Min((long)item.GetItemCount(), remainingCount);
            reducePlan.Add(new KeyValuePair<long, int>(item.GetItemUID(), reduceCount));
            remainingCount -= reduceCount;
            if (remainingCount <= 0)
            {
                break;
            }
        }

        for (int index = 0; index < reducePlan.Count; index++)
        {
            KeyValuePair<long, int> plan = reducePlan[index];
            if (!DeleteItem(plan.Key, plan.Value, new ChangeReason(reason),
                UpdateType.eUT_Update_All))
            {
                return CmdCode.ItemNotExist;
            }
        }
        //log
        //扣除货币逻辑埋点
        //LogicPointManager::Instance()->OnReduceItem(role_, item_id, count);
        return CmdCode.Succeed;
    }

    // 获取将要扣除的物品实例列表。
    public List<ItemBase> GetItemForReduce(int descItemId, ItemBind descBindOptionForTake ,int container_type = -1)
    {
        KeyValuePair<List<ItemBase>, List<ItemBase>> items = GetItemsByTID(descItemId,
            container_type);
        if (descBindOptionForTake == ItemBind.kItemBindIs)
        {
            //>> 如果是扣绑定, 把非绑定的接到后面
            items.Key.AddRange(items.Value);
            return items.Key;
        }
        return items.Value;
    }
    /// <summary>
    /// 获取容器中指定物品的绑定和非绑定实例。
    /// </summary>
    /// <param name="itemId">物品配置 ID。</param>
    /// <param name="containerType">-1 使用默认容器，-2 遍历所有容器，其它值指定容器类型。</param>
    /// <returns>Key 为绑定物品列表，Value 为非绑定物品列表。</returns>
    public KeyValuePair<List<ItemBase>, List<ItemBase>> GetItemsByTID(int itemId, int containerType = -1)
    {
        List<ItemBase> bindItems = new List<ItemBase>();
        List<ItemBase> unbindItems = new List<ItemBase>();
        List<RoItemContainer> containers = new List<RoItemContainer>();

        if (containerType == -1)
        {
            RoItemContainer container = GetDefineContiner(itemId);
            if (container != null)
            {
                containers.Add(container);
            }
        }
        else if (containerType == -2)
        {
            if (_itemContainers != null)
            {
                List<RoItemContainer> allContainers =
                    new List<RoItemContainer>(_itemContainers.Values);
                for (int index = 0; index < allContainers.Count; index++)
                {
                    RoItemContainer container = allContainers[index];
                    if (container != null)
                    {
                        containers.Add(container);
                    }
                }
            }
        }
        else
        {
            RoItemContainer container = GetContainer((KnapsackType)containerType);
            if (container != null)
            {
                containers.Add(container);
            }
        }

        for (int containerIndex = 0; containerIndex < containers.Count; containerIndex++)
        {
            RoItemContainer container = containers[containerIndex];
            if (container == null || container.GetItemMap() == null)
            {
                continue;
            }

            // 原实现从按 UID 排序的 map 尾部向前遍历，这里显式按 UID 倒序保持扣除顺序。
            List<ItemBase> containerItems = new List<ItemBase>(container.GetItemMap().Values);
            containerItems.Sort((left, right) =>
            {
                if (left == null)
                {
                    return 1;
                }
                if (right == null)
                {
                    return -1;
                }
                return right.GetItemUID().CompareTo(left.GetItemUID());
            });

            for (int itemIndex = 0; itemIndex < containerItems.Count; itemIndex++)
            {
                ItemBase item = containerItems[itemIndex];
                if (item == null || item.GetItemID() != itemId)
                {
                    continue;
                }

                if (item.IsBind() == ItemBind.kItemBindIs)
                {
                    bindItems.Add(item);
                }
                else
                {
                    unbindItems.Add(item);
                }
            }
        }

        return new KeyValuePair<List<ItemBase>, List<ItemBase>>(bindItems, unbindItems);
    }
}
