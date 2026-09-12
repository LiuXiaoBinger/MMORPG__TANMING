using System;
using System.Collections.Generic;


/// <summary>
/// 背包操作的公共事务基类。
/// 当前项目尚未提供物品明细、货币和装备栏的权威内存状态，
/// 因此该类只保存事务上下文和角色组件引用，不直接修改任何背包数据。
///  一个事务绑定一个角色和其背包，负责汇总物品变化原因、通知客户端和任务系统。
// 它不是数据库事务；数据库落地由角色的定时/退出存档流程统一处理。
/// </summary>

public class BagTransition
{
    // 本次事务操作的客户端角色。
    protected ClientRole m_role;
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
    public BagTransition(ClientRole role)
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
    public ClientRole Role
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
    /// 当前尚无增量背包协议，故不执行发送操作。
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

        // 中心服背包和次数数据未完整装载前，禁止任何会改变角色状态的事务。
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

    public void SetReason(ItemChangeReason itemReasonBuyItem)
    {
        record.SetItemChangeReason(new ChangeReason(itemReasonBuyItem));
    }
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

public class BagTakeItemTransition : BagTransition
{
    public BagTakeItemTransition(ClientRole role) : base(role)
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
    /// 扣除指定物品。待背包物品明细接入后实现。
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

public class BagGiveItemTransition : BagTransition
{
    public BagGiveItemTransition(ClientRole role) : base(role)
    {
    }

    /// <summary>
    /// 发放物品。待背包明细和容量占格规则接入后实现。
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

        //给句物品
        int item_cout = Role.GetItemCount(itemDesc.ItemID);
        var reason = GetTotalReason();
        if (itemDesc.ChangeReason != ItemChangeReason.ITEM_REASON_NONE)
        {
            //2020-06-10 组队队长额外奖励增加的 itemchange
            reason = new ChangeReason(itemDesc.ChangeReason);
        }

        Int32 itemSign = itemDesc.ItemSign|(Int32)itemDesc.BindOptionForGive;
        // ItemDesc 为跨模块 long 数量，进入当前 int 堆叠模型前必须显式完成上界校验。
        ItemBase item = Role.AddItem(itemDesc.ItemID, (int)itemDesc.ItemCount, reason,
            (ItemSign)itemSign, money_id_,itemDesc.Price,false, out idip_param_);
        return CmdCode.Succeed;
    }

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

public class BagExpansionTransition : BagTransition
{
    private const int MinOpenedGridCount = 81;
    private const int MaxOpenedGridCount = byte.MaxValue;

    public BagExpansionTransition(ClientRole role) : base(role)
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
        return type == KnapsackType.RolePackPlain || type == KnapsackType.RolePackEquip ||
            type == KnapsackType.RolePackConsume || type == KnapsackType.RolePackMaterial;
    }
}

public class BagWearItemTransition : BagTransition
{
    public BagWearItemTransition(ClientRole role) : base(role)
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

public class BagUseItemTransition : BagTransition
{
    public BagUseItemTransition(ClientRole role) : base(role)
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

public class BagMoveItemTransition : BagTransition
{
    public BagMoveItemTransition(ClientRole role) : base(role)
    {
    }

    /// <summary>
    /// 移动物品。当前无物品明细和格子占用信息，不能直接交换或拆分物品。
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

public class BagSimulateTransition : BagTransition
{
    public BagSimulateTransition(ClientRole role) : base(role)
    {
    }

    /// <summary>
    /// 预演增删物品操作。当前无物品明细，无法计算真实可用格子数。
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

public class BagLockTransition : BagTransition
{
    public BagLockTransition(ClientRole role) : base(role)
    {
    }

    /// <summary>
    /// 锁定指定物品。当前无物品实例状态，不能执行。
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

public class BagChangeAttrTransition : BagTransition
{
    public BagChangeAttrTransition(ClientRole role) : base(role)
    {
    }

    /// <summary>
    /// 修改指定物品属性。当前没有物品实例和属性持久化接口，不能执行。
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
// 批量发奖事务：先检查全部物品是否都能放入，全部满足后才一次性提交；否则不发任何物品。
public class BagZhuDongGetItemTranstion :  BagTransition
{
    public BagZhuDongGetItemTranstion(ClientRole role) : base(role)
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
            // temp_count / overlap 向上取整，纯整数版本
          
            space_still_need += space_still_need += (int)Math.Ceiling((float)temp_count / maxOverlap);
            
        }

        if( temp_bag_left_space - space_still_need< 0 )
        {
            Result.ErrorCode = CmdCode.BagFull;
            // abs：记录本次领取还需要的额外格子数量。
            Result.space_still_need = Math.Abs(space_still_need);
        }
        return Result;
    }
    // 仅在整批物品均可放入时一次性发放；任一项无法放入则不提交。
    ItemZhuDongGetResult ZhuDongGetItem(List<ItemDesc> desc, KnapsackType type = KnapsackType.RolePackPlain)
    {
        ItemZhuDongGetResult Result = default;
        return Result;
    }
};
