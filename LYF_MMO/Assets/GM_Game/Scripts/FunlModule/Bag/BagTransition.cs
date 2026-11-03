using System;
using System.Collections.Generic;


/// <summary>
/// 一次客户端背包操作的共同基类，保存角色、背包组件和变更原因。
/// 派生类负责增加、扣除、穿戴或扩容；部分旧接口仍只返回 NotImplemented。
/// 这里修改的是客户端数据，实际结果仍以服务器同步为准。
// 这里的“事务”表示一次背包业务操作，本类不写数据库。
/// </summary>

public class BagTransition
{
    // 本次事务操作的客户端角色。
    protected PlayerEntity m_role;
    // 角色拥有的背包组件，负责容器和容量状态。
    protected  RoItemComponent m_roItemComponent;
    // 当前事务的业务来源和审计上下文。
    protected  BagTransitionContext m_context;
    // 本次物品变更原因的聚合记录器。
    protected  BagListener record =new BagListener();
    // 本次操作关联的货币配置 ID。
    protected  int money_id_ = 0;
    // 本次操作涉及的货币数量。
    protected  int money_count_ = 0;
    // 本次操作附带的 IDIP 审计参数。
    protected  IdipParam idip_param_ = new IdipParam();
    /// <summary>保存本次操作的 role，并查找它的背包组件；同时建立空的来源记录。</summary>
    public BagTransition(PlayerEntity role)
    {
        m_role = role;
        RoItemComponent knapsackComponent;
        if (role != null && role.TryGetComponent(out knapsackComponent))
        {
            m_roItemComponent = knapsackComponent;
        }
        m_context = new BagTransitionContext();
    }

    /// <summary>
    /// 当前事务所属客户端角色。
    /// </summary>
    public PlayerEntity Role
    {
        get { return m_role; }
    }

    /// <summary>
    /// 当前事务上下文，用于记录业务来源和原因。
    /// </summary>
    public BagTransitionContext Context
    {
        get { return m_context; }
    }

    /// <summary>
    /// 设置本次背包变化的业务来源。
    /// </summary>
    public void SetAction(TransitionAction action)
    {
        m_context.Action = action;
    }

    /// <summary>
    /// 通知客户端前的扩展钩子。
    /// </summary>
    public virtual void BeforeNotifyClient()
    {
    }

    /// <summary>
    /// 通知客户端后的扩展钩子。
    /// </summary>
    public virtual void AfterNotifyClient()
    {
    }

    /// <summary>
    /// 同步本次背包变化给客户端。
    /// 当前这个保留接口只返回 NotImplemented，不发送消息。
    /// </summary>
    public virtual BagTransitionResult NotifyClient()
    {
        return BagTransitionResult.NotImplemented;
    }

    /// <summary>
    /// 通知任务系统本次背包变化。
    /// 当前尚无任务事件接口，故不执行通知。
    /// </summary>
    public virtual BagTransitionResult NotifyTask()
    {
        return BagTransitionResult.NotImplemented;
    }

    /// <summary>
    /// 校验事务依赖的角色和背包组件是否已经准备完成。
    /// </summary>
    protected BagTransitionResult ValidateContext()
    {
        if (m_role == null) { return BagTransitionResult.RoleNotFound; }

        if (m_roItemComponent == null) { return BagTransitionResult.KnapsackUnavailable; }

        // 角色基础信息、背包及次数数据准备好后，才允许继续此处的本地操作。
        if (!m_role.IsRoleDataReady()) { return BagTransitionResult.KnapsackUnavailable; }

        return BagTransitionResult.Success;
    }

    /// <summary>
    /// 校验物品变更请求的基础参数。
    /// </summary>
    protected BagTransitionResult ValidateItemChange(BagItemChange itemChange)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }

        if (itemChange == null || itemChange.ItemTypeId <= 0 || itemChange.Count <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }

        return BagTransitionResult.Success;
    }

    /// <summary>把 itemReasonBuyItem 保存为本次操作的主要变更原因。</summary>
    public void SetReason(ItemChangeReason itemReasonBuyItem)
    {
        record.SetItemChangeReason(new ChangeReason(itemReasonBuyItem));
    }
    /// <summary>取得本次操作设置的主要原因。</summary>
    public ItemChangeReason GetReason()
    {
        return record.GetItemChangeReason();
    }
}

// ============================================================
// 辅助枚举
// ============================================================

/// <summary>物品变化通知时机。</summary>
public enum UpdateType
{
    // 下一帧批量同步，适用于同一帧多次变换。
    eUT_Normal = 1,
    // 立即同步本帧全部物品变化。
    eUT_Update_All = 2,
    // 只同步当前修改的物品。
    eUT_Update_One = 3
}

/// <summary>标明背包操作由哪个业务发起。</summary>
public enum TransitionAction
{
    /// <summary>
    /// 未指定业务来源。
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// NPC 或商城购买商品。
    /// </summary>
    BuyGoods = 1
}

/// <summary>背包操作的本地结果；NotImplemented 表示接口仍未实现。</summary>
public enum BagTransitionResult
{
    /// <summary>
    /// 操作成功。
    /// </summary>
    Success = 0,

    /// <summary>
    /// 参数不合法。
    /// </summary>
    InvalidArgument = 1,

    /// <summary>
    /// 客户端角色不存在。
    /// </summary>
    RoleNotFound = 2,

    /// <summary>
    /// 角色背包组件尚未初始化。
    /// </summary>
    KnapsackUnavailable = 3,

    /// <summary>
    /// 当前项目尚未具备所需的权威数据或协议，不能执行该操作。
    /// </summary>
    NotImplemented = 4
}

// ============================================================
// 辅助数据结构
// ============================================================

/// <summary>保存同一次背包操作的业务来源和文字原因。</summary>
public sealed class BagTransitionContext
{
    /// <summary>
    /// 本次背包操作的业务来源。
    /// </summary>
    public TransitionAction Action { get; set; }

    /// <summary>
    /// 调用方记录的业务原因，供后续审计和日志系统接入。
    /// </summary>
    public string Reason { get; set; }
}

/// <summary>描述一次物品变化的请求，包含配置编号、数量和来源或目标格子。</summary>
public sealed class BagItemChange
{
    /// <summary>
    /// 物品配置 ID。
    /// </summary>
    public int ItemTypeId { get; set; }

    /// <summary>
    /// 可选的物品运行时数据；事务骨架不会直接修改该对象。
    /// </summary>
    public ItemBase Item { get; set; }

    /// <summary>
    /// 可选的装备运行时数据；仅在物品为装备时填写。
    /// </summary>
    public EquipBase Equip { get; set; }

    /// <summary>
    /// 本次变更数量，必须为正数。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 目标或来源背包类型。
    /// </summary>
    public KnapsackType KnapsackType { get; set; }

    /// <summary>
    /// 目标或来源格子索引；未指定时为负数。
    /// </summary>
    public int GridIndex { get; set; }
}

// ============================================================
// 背包事务派生类
// ============================================================

/// <summary>扣除物品或虚拟货币的客户端操作，包含仍未接入的旧重载。</summary>
public class BagTakeItemTransition : BagTransition
{
    public BagTakeItemTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 预检查指定物品能否扣除，不修改任何状态。
    /// </summary>
    public virtual BagTransitionResult TryTakeItem(BagItemChange itemChange)
    {
        BagTransitionResult result = ValidateItemChange(itemChange);
        if (result == BagTransitionResult.Success)
        {
            return BagTransitionResult.NotImplemented;
        }
        return result;
    }

    /// <summary>
    /// 保留的 BagItemChange 扣除入口，目前只校验参数并返回 NotImplemented。
    /// </summary>
    public virtual BagTransitionResult TakeItem(BagItemChange itemChange)
    {
        BagTransitionResult result = ValidateItemChange(itemChange);
        if (result == BagTransitionResult.Success)
        {
            return BagTransitionResult.NotImplemented;
        }
        return result;
    }

    /// <summary>按参考项目统一扣除物品；货币类型转入虚拟背包路径。</summary>
    public virtual CmdCode TakeItem(ItemDesc desc, KnapsackType type =KnapsackType.RolePackPlain)
    {
        if (desc == null || desc.ItemCount <= 0)
            return CmdCode.InsufficientItems;
        
        //先检查有没有这个物品
        CmdCode code = m_role.CheckItem(desc.ItemID, desc.ItemCount,desc.BindOptionForTake);
        if (code != CmdCode.Succeed)
        {
            return code;
        }

        return m_role.ReduceItem(desc.ItemID, desc.ItemCount, GetReason(),
            desc.BindOptionForTake);


    }

    
    /// <summary>原子扣除虚拟物品，余额不足时不改变角色状态。</summary>
    public virtual BagTransitionResult TakeVirtualItem(int itemId, long count)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }
        if (itemId <= 0 || count <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }
        if (m_roItemComponent != null && m_roItemComponent.TakeVirtualItem(itemId, count))
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.InvalidArgument;
    }
}

/// <summary>把物品或虚拟货币加入角色的客户端背包。</summary>
public class BagGiveItemTransition : BagTransition
{
    public BagGiveItemTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 保留的 BagItemChange 发放入口，目前只校验参数并返回 NotImplemented。
    /// </summary>
    public virtual BagTransitionResult GiveItem(BagItemChange itemChange)
    {
        BagTransitionResult result = ValidateItemChange(itemChange);
        if (result == BagTransitionResult.Success)
        {
            return BagTransitionResult.NotImplemented;
        }
        return result;
    }

    /// <summary>按参考项目统一发放物品；货币类型进入不占格子的虚拟背包。</summary>
    public virtual BagTransitionResult GiveItem(ItemDesc desc, KnapsackType type)
    {
        if (desc == null || desc.ItemCount <= 0) return BagTransitionResult.InvalidArgument;
        if (type == KnapsackType.RoleVirtualItemPack)
        {
            if (m_roItemComponent != null && m_roItemComponent.GiveVirtualItem(desc.ItemID, desc.ItemCount))
            {
                return BagTransitionResult.Success;
            }
            return BagTransitionResult.InvalidArgument;
        }
        return BagTransitionResult.NotImplemented;
    }

    /// <summary>根据 itemDesc 发放物品；虚拟物品走货币入口，其余交给角色 AddItem。当前没有检查 AddItem 的返回结果。</summary>
    public CmdCode GiveItem(ItemDesc itemDesc)
    {
        //校验
        if (Role == null)
        {
            return CmdCode.RoleNotExist;
        }

        // 虚拟物品统一进入不占格子的虚拟物品背包。
        if (itemDesc != null && ItemDesc.IsLegalVirtualItem(itemDesc.ItemID))
        {
            BagTransitionResult virtualResult = GiveItem(itemDesc, KnapsackType.RoleVirtualItemPack);
            if (virtualResult == BagTransitionResult.Success)
            {
                return CmdCode.Succeed;
            }
            return CmdCode.BagFull;
        }

        if (itemDesc.ItemCount <= 0 || itemDesc.ItemCount > int.MaxValue)
        {
            LogMsg.Info("给予物品失败，角色编号=" + Role.GetID(), LogMsgType.Error);
            return CmdCode.BagFull;
        }
        if (itemDesc.TotalPrice <= 0)
        {
            LogMsg.Info("给予物品失败，角色编号=" + Role.GetID(), LogMsgType.Error);
            return CmdCode.BagFull;
        }
        //todo判断是否是经验

        // 读取发放前数量；当前局部值没有参与后续发放。
        int item_cout = Role.GetItemCount(itemDesc.ItemID);
        var reason = GetTotalReason();
        if (itemDesc.ChangeReason != ItemChangeReason.ITEM_REASON_NONE)
        {
            // 单个物品显式指定原因时，优先使用该原因。
            reason = new ChangeReason(itemDesc.ChangeReason);
        }

        Int32 itemSign = itemDesc.ItemSign|(Int32)itemDesc.BindOptionForGive;
        // ItemDesc 为跨模块 long 数量，进入当前 int 堆叠模型前必须显式完成上界校验。
        ItemBase item = Role.AddItem(itemDesc.ItemID, (int)itemDesc.ItemCount, reason,
            (ItemSign)itemSign, money_id_,itemDesc.Price,false, out idip_param_);
        return CmdCode.Succeed;
    }

    /// <summary>读取本次操作的完整原因，供新物品记录使用。</summary>
    private ChangeReason GetTotalReason()
    {
        return record.GetTotalReason();
    }


    /// <summary>增加虚拟物品，虚拟物品不占用普通物品格子。</summary>
    public virtual BagTransitionResult GiveVirtualItem(int itemId, long count)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }

        if (itemId <= 0 || count <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }
        if (m_roItemComponent != null && m_roItemComponent.GiveVirtualItem(itemId, count))
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.InvalidArgument;
    }
}

/// <summary>增加四类普通背包的已开启格数，限制最终容量为 81 到 255。</summary>
public class BagExpansionTransition : BagTransition
{
    /// <summary>可扩容背包至少按 81 格起算。</summary>
    private const int MinOpenedGridCount = 81;
    /// <summary>容量沿用 byte 可表示的上限，即 255 格。</summary>
    private const int MaxOpenedGridCount = byte.MaxValue;

    public BagExpansionTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 解锁指定背包的格子。
    /// 校验背包类型和扩容数量后更新角色组件及对应容器容量。
    /// </summary>
    public virtual BagTransitionResult UnlockGrid(KnapsackType knapsackType, int gridCount)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }

        if (!IsExpandableType(knapsackType) || gridCount <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }

        int currentCount = m_roItemComponent.GetOpenedGridCount(knapsackType);
        if (currentCount < MinOpenedGridCount)
        {
            currentCount = MinOpenedGridCount;
        }

        long targetCount = (long)currentCount + gridCount;
        if (targetCount > MaxOpenedGridCount)
        {
            return BagTransitionResult.InvalidArgument;
        }

        if (m_roItemComponent.SetOpenedGridCount(knapsackType, (int)targetCount))
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.KnapsackUnavailable;
    }

    /// <summary>判断是否为四类可扩容背包。</summary>
    private static bool IsExpandableType(KnapsackType type)
    {
        // 普通格与装备格属于可扩容背包类型。
        bool isBaseType = type == KnapsackType.RolePackPlain || type == KnapsackType.RolePackEquip;
        // 消耗品格与材料格同样属于可扩容背包类型。
        bool isResourceType = type == KnapsackType.RolePackConsume || type == KnapsackType.RolePackMaterial;
        return isBaseType || isResourceType;
    }
}

/// <summary>在装备背包和已穿戴栏之间移动装备实例。</summary>
public class BagWearItemTransition : BagTransition
{
    public BagWearItemTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>把装备背包中的指定装备移动到对应的穿戴栏。</summary>
    public virtual BagTransitionResult WearItem(int itemId)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }
        EquipBase equip = FindEquipment(KnapsackType.RolePackEquip, itemId);
        if (equip == null || equip.EquipType < 0 || equip.EquipType >= 256)
        {
            return BagTransitionResult.InvalidArgument;
        }
        RoItemContainer target = m_roItemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack);
        if (target == null || target.GetItemByPos(equip.EquipType) != null)
        {
            return BagTransitionResult.KnapsackUnavailable;
        }
        bool moved = m_roItemComponent.SwapItem(equip.GetItemUID(),
            KnapsackType.RolePackEquip, KnapsackType.RoleCurrtEquipPack,
            equip.EquipType,
            new ChangeReason(ItemChangeReason.ITEM_REASON_WEAR_EQUIP),
            UpdateType.eUT_Update_All);
        if (moved)
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.KnapsackUnavailable;
    }

    /// <summary>把穿戴栏中的指定装备移动回普通装备背包。</summary>
    public virtual BagTransitionResult TakeOffItem(int itemId)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }
        EquipBase equip = FindEquipment(KnapsackType.RoleCurrtEquipPack, itemId);
        if (equip == null)
        {
            return BagTransitionResult.InvalidArgument;
        }
        RoItemContainer target = m_roItemComponent.GetContainer(KnapsackType.RolePackEquip);
        if (target == null)
        {
            return BagTransitionResult.KnapsackUnavailable;
        }
        int targetGrid = FindEmptyGrid(target);
        if (targetGrid < 0)
        {
            return BagTransitionResult.KnapsackUnavailable;
        }
        bool moved = m_roItemComponent.SwapItem(equip.GetItemUID(),
            KnapsackType.RoleCurrtEquipPack, KnapsackType.RolePackEquip,
            targetGrid,
            new ChangeReason(ItemChangeReason.ITEM_REASON_WEAR_EQUIP),
            UpdateType.eUT_Update_All);
        if (moved)
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.KnapsackUnavailable;
    }

    /// <summary>从指定容器查找配置 ID 对应的装备实例。</summary>
    private EquipBase FindEquipment(KnapsackType type, int itemId)
    {
        RoItemContainer container = m_roItemComponent.GetContainer(type);
        if (container == null || container.GetItemMap() == null)
        {
            return null;
        }
        List<ItemBase> items = new List<ItemBase>(container.GetItemMap().Values);
        for (int index = 0; index < items.Count; index++)
        {
            EquipBase equip = items[index] as EquipBase;
            if (equip != null && equip.GetItemID() == itemId)
            {
                return equip;
            }
        }
        return null;
    }

    /// <summary>查找目标容器中的首个空格，避免直接暴露容器内部索引逻辑。</summary>
    private static int FindEmptyGrid(RoItemContainer container)
    {
        int size = container.GetContainerSize();
        for (int index = 0; index < size; index++)
        {
            if (container.GetItemByPos(index) == null)
            {
                return index;
            }
        }
        return -1;
    }
}

/// <summary>预留的使用物品入口，尚未执行扣除或道具效果。</summary>
public class BagUseItemTransition : BagTransition
{
    public BagUseItemTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 校验并使用物品。物品效果系统尚未接入，当前不会扣除道具。
    /// </summary>
    public virtual BagTransitionResult UseItem(BagItemChange itemChange)
    {
        BagTransitionResult result = ValidateItemChange(itemChange);
        if (result == BagTransitionResult.Success)
        {
            return BagTransitionResult.NotImplemented;
        }
        return result;
    }
}

/// <summary>预留的背包移动接口；实际容器移动另由 RoItemComponent.SwapItem 处理。</summary>
public class BagMoveItemTransition : BagTransition
{
    public BagMoveItemTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 保留的移动入口，校验角色、物品和目标格后返回 NotImplemented；不会移动或拆分物品。
    /// </summary>
    public virtual BagTransitionResult MoveItem(
        BagItemChange itemChange,
        KnapsackType sourceKnapsackType,
        KnapsackType targetKnapsackType,
        int targetGridIndex)
    {
        BagTransitionResult result = ValidateItemChange(itemChange);
        if (result != BagTransitionResult.Success)
        {
            return result;
        }

        if (targetGridIndex < 0)
        {
            return BagTransitionResult.InvalidArgument;
        }
        return BagTransitionResult.NotImplemented;
    }
}

/// <summary>预留的批量增删预演接口，目前不计算结果。</summary>
public class BagSimulateTransition : BagTransition
{
    public BagSimulateTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 保留的批量增删预演入口，当前仅检查角色组件，并未处理 addItems、removeItems 或 knapsackType。
    /// </summary>
    public virtual BagTransitionResult Simulate(
        IList<BagItemChange> addItems,
        IList<BagItemChange> removeItems,
        KnapsackType knapsackType)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }

        return BagTransitionResult.NotImplemented;
    }
}

/// <summary>预留的物品锁定接口，目前只做基础检查。</summary>
public class BagLockTransition : BagTransition
{
    public BagLockTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 保留的锁定入口，检查角色和 itemId 后返回 NotImplemented，不改变物品锁定状态。
    /// </summary>
    public virtual BagTransitionResult LockItem(int itemId)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }

        if (itemId <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }
        return BagTransitionResult.NotImplemented;
    }
}

/// <summary>预留的物品属性修改接口，目前只做基础检查。</summary>
public class BagChangeAttrTransition : BagTransition
{
    public BagChangeAttrTransition(PlayerEntity role) : base(role)
    {
    }

    /// <summary>
    /// 保留的属性修改入口，检查角色和 itemId 后返回 NotImplemented，不修改任何属性。
    /// </summary>
    public virtual BagTransitionResult ChangeAttribute(int itemId)
    {
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }

        if (itemId <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }
        return BagTransitionResult.NotImplemented;
    }
}
// 批量领奖的空间预检工具；真正发奖的 ZhuDongGetItem 目前仍为空实现。
public class BagZhuDongGetItemTranstion :  BagTransition
{
    public BagZhuDongGetItemTranstion(PlayerEntity role) : base(role)
    {
    }
    

    // 预检整批物品是否均可放入目标容器；不修改任何物品数据。
    public ItemZhuDongGetResult TryZhuDongGetItem(List<ItemDesc> descs, 
        KnapsackType type = KnapsackType.RolePackPlain)
    {
        ItemZhuDongGetResult Result = default;
        
        int temp_bag_left_space = m_role.GetComponent<RoItemComponent>().
            GetLeftSpace(type);
        int space_still_need = 0;
        RoItemContainer container =m_role.GetComponent<RoItemComponent>().GetContainerByType(type);
        if (container == null)
        {
            LogMsg.Info($" unkown bag type, bag type is {type}");
            Result.ErrorCode = CmdCode.IncorrectBackpackType;
        }

        List<ItemDesc> descList = new List<ItemDesc>(descs);
        for (int descIndex = 0; descIndex < descList.Count; descIndex++)
        {
            ItemDesc desc = descList[descIndex];
            if(desc.ItemID == 0 ||
               ItemDesc.IsVirtualItem(desc.ItemID)|| desc.ItemCount ==0 )
            {
                continue;
            }
            // Bag::MaxOverlap：取得该物品单个格子的最大堆叠数量。
            int maxOverlap = m_role.GetComponent<RoItemComponent>().
                MaxOverlap(desc.ItemID);
            
            var same_item_list = container.FindItemForGive(desc);
            int temp_count = (int)desc.ItemCount;
            if (same_item_list != null)
            {
                List<ItemDesc> sameItemList = new List<ItemDesc>(same_item_list);
                for (int itemIndex = 0; itemIndex < sameItemList.Count; itemIndex++)
                {
                    ItemDesc item = sameItemList[itemIndex];
                    if (item.ItemCount == maxOverlap)
                    {
                        continue;
                    }
                    if (item.ItemCount + temp_count <= maxOverlap)
                    {
                        temp_count = 0;
                        break;
                    }
                    else
                    {
                        temp_count -= maxOverlap - (int)item.ItemCount;
                    }
                }
            }

            // ceil：按堆叠上限向上取整，计算剩余数量需要新开的格子数。
            // 现有表达式重复累加 space_still_need，保留原实现；此结果不能当作已验证的正确缺格数。
          
            space_still_need += space_still_need += (int)Math.Ceiling((float)temp_count / maxOverlap);
            
        }

        if( temp_bag_left_space - space_still_need< 0 )
        {
            Result.ErrorCode = CmdCode.BagFull;
            // 这里保存的是计算出的需要格子数的绝对值，当前没有减去已有空格。
            Result.space_still_need = Math.Abs(space_still_need);
        }
        return Result;
    }
    // 尚未实现发奖，仅返回默认结果，不会向背包添加物品。
    ItemZhuDongGetResult ZhuDongGetItem(List<ItemDesc> desc, KnapsackType type = KnapsackType.RolePackPlain)
    {
        ItemZhuDongGetResult Result = default;
        return Result;
    }
};
