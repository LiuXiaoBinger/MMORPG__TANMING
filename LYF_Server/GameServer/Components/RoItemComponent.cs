using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 角色物品组件，管理在线角色的全部背包容器与延迟持久化状态。
/// 职责包括：背包容量扩容、物品增删改查、跨容器移动、登录数据装载，
/// 以及将物品变更统一转换为 ItemDataListDB 脏标记，由定时保存链路汇总落库。
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
    // 每个真实容器对应一个延迟持久化状态对象。
    private Dictionary<KnapsackType, ItemDataListDB> _itemDataLists;
    // 角色物品内存状态的单调递增版本，每次物品变更自增，用于 CenterServer 幂等与 ACK 对齐。
    //需要同步给客户端的道具id
    private Dictionary<long,ChangeReason> _need_update_item;
    
    // 需要延时删除的道具id vector
    private Dictionary<long,ChangeReason> _delay_del_item_map;
    /// <summary>
    /// 构造角色物品组件。
    /// </summary>
    /// <param name="owner">所属在线角色，不能为空。</param>
    public RoItemComponent(OnlineRole owner)
        : base(owner)
    {
    }

    // 背包容器按 KnapsackType 分类保存；容器本身负责物品位置和容量校验。
    private Dictionary<KnapsackType, RoItemContainer> _itemContainers;

    /// <summary>
    /// 初始化组件，创建五类背包容器与对应的持久化状态对象，并为四类可扩容背包设置默认 81 格容量。
    /// </summary>
    public override void Initialize()
    {
        _itemContainers = new Dictionary<KnapsackType, RoItemContainer>();
        _itemDataLists = new Dictionary<KnapsackType, ItemDataListDB>();
        _need_update_item = new Dictionary<long, ChangeReason>();
        _delay_del_item_map = new Dictionary<long, ChangeReason>();

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
            _itemDataLists[type] = new ItemDataListDB(_itemContainers[type]);
        }

        // 四类可扩容背包统一从最小 81 格初始化，穿戴栏不参与扩容。
        KnapsackType[] expandableTypes = GetExpandableTypes();
        for (int index = 0; index < expandableTypes.Length; index++)
        {
            KnapsackType type = expandableTypes[index];
            ApplyOpenedGridCount(type, MinOpenedGridCount);
        }
    }

    public override RoleModuleType GetModuleId()
    {
        return RoleModuleType.kRoleModuleTypeItemComponent;
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

    /// <summary>增加虚拟物品；虚拟物品没有容量限制但仍通过普通物品脏数据链路保存。</summary>
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

        List<ItemBase> itemSnapshot = new List<ItemBase>(container.GetItemMap().Values);
        List<ItemBase> stacks = new List<ItemBase>();
        for (int index = 0; index < itemSnapshot.Count; index++)
        {
            ItemBase item = itemSnapshot[index];
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
        return ApplyOpenedGridCount(type, count);
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
        return ApplyOpenedGridCount(type, Math.Max(MinOpenedGridCount, count));
    }

    /// <summary>统一写入容量字典并同步对应容器。</summary>
    /// <param name="type">背包类型。</param>
    /// <param name="count">目标格子数。</param>
    /// <returns>成功返回 true；类型不可扩容、容器为空或越界返回 false。</returns>
    private bool ApplyOpenedGridCount(KnapsackType type, int count)
    {
        RoItemContainer container = GetContainer(type);
        if (!IsExpandableType(type) || container == null || count < MinOpenedGridCount || count > MaxOpenedGridCount)
        {
            return false;
        }
        container.SetContainerSize(count);
        _openedGridCounts[type] = count;
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

    /// <summary>返回容器只读视图，供服务层查询而不替换组件内部字典。</summary>
    /// <returns>背包类型到容器的只读字典。</returns>
    public IReadOnlyDictionary<KnapsackType, RoItemContainer> GetContainers()
    {
        return _itemContainers;
    }

    /// <summary>
    /// 从中心服登录回包装载角色全部背包数据：先恢复各背包容量，再清空容器，
    /// 按 UID 去重后逐类装入物品，最后恢复保存版本并重置所有持久化状态为干净。
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
        List<RoItemContainer> containerSnapshot = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containerSnapshot.Count; index++)
        {
            RoItemContainer container = containerSnapshot[index];
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

        // 恢复中心服已持久化的物品版本号，作为本地变更版本的起点。
        // 装载完成后所有容器视为干净状态，等待后续变更触发脏标记。
        List<ItemDataListDB> dataListSnapshot = new List<ItemDataListDB>(_itemDataLists.Values);
        for (int index = 0; index < dataListSnapshot.Count; index++)
        {
            ItemDataListDB itemDataList = dataListSnapshot[index];
            itemDataList.ResetLoadedState();
        }
    }

    /// <summary>组件装载完成后的回调，当前无额外逻辑。</summary>
    public override void AfterLoad()
    {
        
    }

    /// <summary>组件被添加到角色后的回调，当前无额外逻辑。</summary>
    public override void AfterAddedToRole()
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
        var container = GetContainerByType(type,page_num);
        return container!=null ? container.GetLeftSpace() : 0;
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
        return itemData.MaxStackCount>1 ? itemData.MaxStackCount : 1;
    }
    /// <summary>组件每帧更新，当前物品组件无需逐帧逻辑。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        
    }

    /// <summary>释放组件持有的容器与持久化状态字典引用。</summary>
    public override void Dispose()
    {
        _itemContainers?.Clear();
        _itemDataLists?.Clear();
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
        
        ItemBind enum_bind = calc_bind_item ==ItemBind.kItemBindIs ? ItemBind.kItemBindIs : ItemBind.kItemBindNo;

        if (!calc_all_container) //> 只统计默认容器
        {
            return (int)Itemcontainer.CountItem(itemId, calc_bind_item);
        }
        
        long total = 0;
        List<RoItemContainer> containerSnapshot = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containerSnapshot.Count; index++)
        {
            RoItemContainer container = containerSnapshot[index];
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
    /// 每次数量变更或新建实例都会触发 OnItemChange 置脏。
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
        List<ItemBase> itemSnapshot = new List<ItemBase>(container.GetItemMap().Values);
        for (int index = 0; index < itemSnapshot.Count; index++)
        {
            ItemBase current = itemSnapshot[index];
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
            OnItemChange(stack, reason, updateType);
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

            // sizeDelta=1 表示新增实例，触发 OnAddItem 脏标记。
            OnItemChange(item, reason, updateType, 1);
            lastAdded = item;
            remainingCount -= count;
        }

        return lastAdded;
    }

    /// <summary>删除指定实例的全部或部分数量，并统一触发延迟持久化脏标记。</summary>
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
            // sizeDelta=-1 表示实例减少，触发 OnDelItem 脏标记。
            OnItemChange(removed, reason, updateType, -1);
            return true;
        }

        // 部分删除只修改数量。
        if (!container.ModifyCount(itemUid, -count))
        {
            return false;
        }
        OnItemChange(item, reason, updateType);
        return true;
    }

    public bool DeleteItem(long itemUid, ChangeReason reason,bool need_delelay_del = false, bool need_tlog = true)
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
        
        var itemDataListDb = GetItemDataListDB(item.GetBagType());
        if (itemDataListDb != null)
        {
            itemDataListDb.OnDelItem(Owner.GetSaveVersion());
        }
        //通知任务模块
        NtfTaskModleItemChange( item, 0);
        
        if (!_need_update_item.ContainsKey(item.GetItemUID()))
        {
            Ntf2ClientItemChange();
        }

        if (need_delelay_del)
        {
            _delay_del_item_map[itemUid] = reason; // 或 TryAdd(uid, reason)
        }
        else
        {
            LogMsg.Info($"[道具系统]角色id: {Owner.GetID()} 删除道具uid:{itemUid}, id:{item.GetItemID()}, 数量:{originItemCount}");
            UpdateItemRet ret =  new UpdateItemRet();
            ret.CmdCode = CmdCode.Succeed;
            MapKey64Value32 map = new MapKey64Value32();
            map.Key = (ulong)itemUid;
            map.Val = (int)reason;
            ret.ItemDate.DeleteList.Add(map);
            Owner.SendToClient(NetDefine.CMD_SC_UpdateItemInfoCode,ret.ToByteString());

        }
        return true;
    }

    private void Ntf2ClientItemChange()
    {
        if (Owner == null || _need_update_item == null || _need_update_item.Count == 0)
        {
            return;
        }
        UpdateItemRet ret = new UpdateItemRet
        {
            CmdCode = CmdCode.Succeed,
            ItemDate = new UpdateItem()
        };
        List<long> itemUids = new List<long>(_need_update_item.Keys);
        for (int index = 0; index < itemUids.Count; index++)
        {
            ItemBase item = GetItemByUID(itemUids[index]);
            if (item != null)
            {
                ret.ItemDate.ItemList.Add(ToRoleItemInfo(item));
            }
        }
        if (ret.ItemDate.ItemList.Count > 0)
        {
            ret.ItemDate.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Owner.SendToClient(NetDefine.CMD_SC_UpdateItemInfoCode, ret.ToByteString());
        }
        _need_update_item.Clear();
    }

    /// <summary>发送本次背包事务累积的物品增量。</summary>
    public void NotifyItemChanges()
    {
        Ntf2ClientItemChange();
    }

    /// <summary>获取虚拟物品背包快照，供商城购买结果同步余额。</summary>
    public List<RoleItemInfo> GetVirtualItemSnapshot()
    {
        List<RoleItemInfo> result = new List<RoleItemInfo>();
        RoItemContainer container = GetContainer(KnapsackType.RoleVirtualItemPack);
        if (container == null)
        {
            return result;
        }
        List<ItemBase> items = new List<ItemBase>(container.GetItemMap().Values);
        for (int index = 0; index < items.Count; index++)
        {
            result.Add(ToRoleItemInfo(items[index]));
        }
        return result;
    }

    /// <summary>将运行时物品转换为客户端增量协议结构。</summary>
    private static RoleItemInfo ToRoleItemInfo(ItemBase item)
    {
        RoleItemInfo info = new RoleItemInfo
        {
            ItemUid = item.GetItemUID(), Count = item.GetItemCount(), RoleId = item.GetRoleID(),
            ItemTypeId = item.GetItemID(), BagType = item.GetBagType(), BagIndex = item.GetBagIndex(),
            ItemSign = item.GetItemSign(), MoneyType = item.GetMoneyType(), TotalPrice = item.GetTotalPrice(),
            CreateTimeUtcTicks = ToUtcTicks(item.GetCreateTime()), ExpireTimeUtcTicks = ToUtcTicks(item.GetExpireTime())
        };
        return info;
    }

    private static long ToUtcTicks(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return 0L;
        }
        return value.ToUniversalTime().Ticks;
    }

    public ItemDataListDB GetItemDataListDB(int getBagType)
    {
        if (_itemDataLists.ContainsKey((KnapsackType)getBagType))
        {
            return _itemDataLists[(KnapsackType)getBagType];
        }
        return null;
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
        List<RoItemContainer> containerSnapshot = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containerSnapshot.Count; index++)
        {
            RoItemContainer container = containerSnapshot[index];
            if (container == null)
            {
                continue;
            }

            var item = container.GetItemByUid(itemUid);
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

        OnItemChange(item, reason, updateType);
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
            item.SetBagIndex(targetGridIndex);
            OnItemChange(item, reason, updateType);
            return true;
        }

        // 跨容器移动：记录源格子索引用于回滚。
        int sourceGridIndex = item.GetBagIndex();
        // 先校验目标格可放入，再从源容器移除；任一步失败则整体不移动。
        if (!target.CheckIn(item, targetGridIndex) || source.SwapOutByUID(itemUid) == null)
        {
            return false;
        }

        // 记录物品原始背包类型，用于标记源容器的删除脏状态。
        int sourceBagType = item.GetBagType();
        if (!target.SwapIn(item, targetGridIndex))
        {
            // 目标放入失败时回滚：将物品放回源容器原位置。
            source.SwapIn(item, sourceGridIndex);
            return false;
        }

        // 跨容器移动使用同一个变更版本，源容器标记删除、目标容器标记新增，并额外标记交换脏状态。
        long version = Owner.GetSaveVersion();
        MarkDataListChanged((KnapsackType)sourceBagType, version, -1, true);
        MarkDataListChanged(targetType, version, 1, true);
        return true;
    }

    /// <summary>
    /// 所有物品增删改入口最终都通过此函数设置对应 ItemDataListDB 的脏状态。
    /// 内部自增变更版本并按 sizeDelta 区分新增/删除/修改。
    /// </summary>
    /// <param name="item">发生变更的物品实例。</param>
    /// <param name="reason">变更原因。</param>
    /// <param name="updateType">更新类型。</param>
    /// <param name="sizeDelta">实例数量变化：1 新增、-1 删除、0 修改。</param>
    public void OnItemChange(ItemBase item, ChangeReason reason, 
        UpdateType updateType, 
        int sizeDelta = 0)
    {
        if (item == null || Owner == null)
        {
            return;
        }

        long version = Owner.GetSaveVersion();
        MarkDataListChanged((KnapsackType)item.GetBagType(), 
            version, sizeDelta, false);
        _need_update_item[item.GetItemUID()] = reason;
    }

    /// <summary>判断是否存在任意背包容器需要持久化保存。</summary>
    /// <returns>任一容器有脏数据返回 true；全部干净或未初始化返回 false。</returns>
    public override bool NeedSave()
    {
        if (_itemDataLists == null)
        {
            return false;
        }
        List<ItemDataListDB> dataListSnapshot = new List<ItemDataListDB>(_itemDataLists.Values);
        for (int index = 0; index < dataListSnapshot.Count; index++)
        {
            ItemDataListDB itemDataList = dataListSnapshot[index];
            if (itemDataList.NeedSave())
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 将所有脏容器的完整快照写入字段管理器，由调用方组装为保存请求发送 CenterServer。
    /// 脏版本高于传入 saveVersion 的容器会跳过，避免覆盖更新的本地变更。
    /// </summary>
    /// <param name="fieldManager">角色数据字段管理器，用于写入序列化后的物品列表。</param>
    /// <param name="saveVersion">本次保存对应的角色数据版本。</param>
    /// <returns>至少一个容器成功写入返回 true。</returns>
    public override bool Save(RoleDataFieldManager fieldManager, long saveVersion)
    {
        bool saved = false;
        List<ItemDataListDB> dataListSnapshot = new List<ItemDataListDB>(_itemDataLists.Values);
        for (int index = 0; index < dataListSnapshot.Count; index++)
        {
            ItemDataListDB itemDataList = dataListSnapshot[index];
            saved |= itemDataList.Save2Db(fieldManager, saveVersion);
        }
        return saved;
    }

    /// <summary>
    /// CenterServer 保存成功 ACK 回调，按确认版本清除对应脏状态。
    /// 在途期间又发生变化时 dirtyVersion 会更大，旧 ACK 不会清除新脏数据。
    /// </summary>
    /// <param name="version">CenterServer 确认已持久化的版本号。</param>
    public override void OnSaveAcknowledged(long version)
    {
        List<ItemDataListDB> dataListSnapshot = new List<ItemDataListDB>(_itemDataLists.Values);
        for (int index = 0; index < dataListSnapshot.Count; index++)
        {
            ItemDataListDB itemDataList = dataListSnapshot[index];
            itemDataList.ResetNeedSave(version);
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
    private void LoadItems(IEnumerable<RoleItemInfo> itemInfos, HashSet<long> loadedItemUids)
    {
        List<RoleItemInfo> itemSnapshot = new List<RoleItemInfo>(itemInfos);
        for (int index = 0; index < itemSnapshot.Count; index++)
        {
            RoleItemInfo itemInfo = itemSnapshot[index];
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
        List<RoItemContainer> containerSnapshot = new List<RoItemContainer>(_itemContainers.Values);
        for (int index = 0; index < containerSnapshot.Count; index++)
        {
            RoItemContainer container = containerSnapshot[index];
            if (container.GetItemByUid(itemUid) != null)
            {
                return container;
            }
        }
        return null;
    }

    /// <summary>
    /// 将物品变更标记到对应背包的 ItemDataListDB 脏状态。
    /// 普通变更按 sizeDelta 区分新增/删除/修改；跨容器交换时额外调用 OnSwapItem，
    /// 确保源和目标容器都标记为脏且共享同一版本。
    /// </summary>
    /// <param name="type">发生变更的背包类型。</param>
    /// <param name="version">本次变更对应的版本号。</param>
    /// <param name="sizeDelta">实例数量变化：1 新增、-1 删除、0 修改。</param>
    /// <param name="isSwap">是否为跨容器交换操作。</param>
    private void MarkDataListChanged(KnapsackType type, long version, 
        int sizeDelta, bool isSwap)
    {
        if (_itemDataLists == null || !_itemDataLists.TryGetValue(type, out ItemDataListDB itemDataList))
        {
            return;
        }
        // 跨容器交换：源和目标都需要标记，sizeDelta 区分删除/新增，再统一标记交换脏状态。
        if (isSwap)
        {
            if (sizeDelta > 0)
            {
                itemDataList.OnAddItem(version);
            }
            else if (sizeDelta < 0)
            {
                itemDataList.OnDelItem(version);
            }
            itemDataList.OnSwapItem(version);
            return;
        }
        // 普通变更：按实例数量变化路由到对应脏标记入口。
        if (sizeDelta > 0)
        {
            itemDataList.OnAddItem(version);
        }
        else if (sizeDelta < 0)
        {
            itemDataList.OnDelItem(version);
        }
        else
        {
            itemDataList.OnModifyItem(version);
        }
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

    /// <summary>
    /// 按配置 ID 扣除角色背包物品，支持跨堆叠扣除并在每次变更后标记持久化脏状态。
    /// </summary>
    /// <param name="descItemId">待扣除的物品配置 ID。</param>
    /// <param name="ItemCount">待扣除数量，必须为正数。</param>
    /// <param name="reason">扣除原因，用于变更记录和客户端同步。</param>
    /// <param name="descBindOptionForTake">绑定筛选；绑定选项允许按规则合并绑定与非绑定物品。</param>
    /// <returns>扣除成功返回 Succeed，物品不存在或参数非法返回对应错误码。</returns>
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
        long need_reduce = ItemCount;
        List<long> need_del_vec =  new List<long>();
        for(int i=0;i<item_list.Count;i++)
        {
            ItemBase item = item_list[i]; // ✅ 生效
            if (item.GetItemCount() <= need_reduce)
            {
                need_reduce -= item.GetItemCount();
                need_del_vec.Add(item.GetItemUID());
            }
            else
            {
                // 部分扣除必须实际减少堆叠数量，再通过 OnItemChange 进入统一脏数据链路。
                int reduceCount = (int)need_reduce;
                if (!container.ModifyCount(item.GetItemUID(), -reduceCount))
                {
                    return CmdCode.ItemNotExist;
                }
                OnItemChange(item, new ChangeReason(reason),
                    UpdateType.eUT_Update_All);
                need_reduce = 0;
            }
            if (need_reduce <= 0)
            {
                break;
            }
        }

        for (int index = 0; index < need_del_vec.Count; index++)
        {
            long uid = need_del_vec[index];
            DeleteItem(uid,new ChangeReason(reason));
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
                List<RoItemContainer> containerSnapshot = new List<RoItemContainer>(_itemContainers.Values);
                for (int index = 0; index < containerSnapshot.Count; index++)
                {
                    RoItemContainer container = containerSnapshot[index];
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

        for (int index = 0; index < containers.Count; index++)
        {
            RoItemContainer container = containers[index];
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
