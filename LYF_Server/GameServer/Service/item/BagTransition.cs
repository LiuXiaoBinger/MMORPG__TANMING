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
    // 本次事务操作的在线角色。
    protected  OnlineRole m_role;
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
    public BagTransition(OnlineRole role)
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
    /// 当前事务所属在线角色。
    /// </summary>
    public OnlineRole Role
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
        if (m_roItemComponent == null)
        {
            return BagTransitionResult.KnapsackUnavailable;
        }
        m_roItemComponent.NotifyItemChanges();
        AfterNotifyClient();
        return BagTransitionResult.Success;
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
    /// 在线角色不存在。
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
    /// 物品实例 UID。移动或定向操作优先使用该字段；未填写时兼容从 Item 读取。
    /// </summary>
    public long ItemUID { get; set; }

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
    public BagTakeItemTransition(OnlineRole role) : base(role)
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
    public BagGiveItemTransition(OnlineRole role) : base(role)
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
        if (Role == null)
        {
            return CmdCode.RoleNotExist;
        }

        if (itemDesc == null || itemDesc.ItemID <= 0 || itemDesc.ItemCount <= 0)
        {
            return CmdCode.ReqParamError;
        }

        // 虚拟物品统一进入不占格子的虚拟物品背包，和普通物品共用脏数据链路。
        if (ItemDesc.IsLegalVirtualItem(itemDesc.ItemID))
        {
            BagTransitionResult virtualResult = GiveItem(itemDesc, KnapsackType.RoleVirtualItemPack);
            if (virtualResult == BagTransitionResult.Success)
            {
                return CmdCode.Succeed;
            }
            return CmdCode.BagFull;
        }

        if (itemDesc.ItemCount > int.MaxValue)
        {
            LogMsg.Info($"给予物品失败，roleid-》{Role.mainRoleInfo.BaseInfo.RoleId}");
            return CmdCode.BagFull;
        }
        if (itemDesc.TotalPrice <= 0)
        {
            LogMsg.Info($"给予物品失败，roleid-》{Role.mainRoleInfo.BaseInfo.RoleId}");
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
        if (item == null)
        {
            // AddItem 失败通常表示配置无效或背包空间不足，不能向调用方返回成功。
            LogMsg.Info($"给予物品失败，添加物品返回空值，roleid-》{Role.mainRoleInfo.BaseInfo.RoleId}");
            return CmdCode.BagFull;
        }
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

    public BagExpansionTransition(OnlineRole role) : base(role)
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

    /// <summary>判断是否为持久化的四类可扩容背包。</summary>
    private static bool IsExpandableType(KnapsackType type)
    {
        return type == KnapsackType.RolePackPlain || type == KnapsackType.RolePackEquip ||
            type == KnapsackType.RolePackConsume || type == KnapsackType.RolePackMaterial;
    }
}

public class BagWearItemTransition : BagTransition
{
    public BagWearItemTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>
    /// 穿戴指定物品。当前没有装备栏和物品实例数据，不能执行。
    /// </summary>
    public virtual BagTransitionResult WearItem(int itemId)
    {
        return ValidateEquipmentItem(itemId);
    }

    /// <summary>
    /// 卸下指定物品。当前没有装备栏和物品实例数据，不能执行。
    /// </summary>
    public virtual BagTransitionResult TakeOffItem(int itemId)
    {
        return ValidateEquipmentItem(itemId);
    }

    private BagTransitionResult ValidateEquipmentItem(int itemId)
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

public class BagUseItemTransition : BagTransition
{
    public BagUseItemTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>
    /// 校验并使用物品；物品效果由上层业务处理，本事务负责原子扣除使用数量。
    /// </summary>
    public virtual BagTransitionResult UseItem(BagItemChange itemChange)
    {
        BagTransitionResult result = ValidateItemChange(itemChange);
        if (result != BagTransitionResult.Success)
        {
            return result;
        }

        // 使用物品允许从绑定或非绑定堆叠中扣除，先预检保证扣除失败时不改变状态。
        CmdCode checkCode = m_role.CheckItem(itemChange.ItemTypeId, itemChange.Count,
            ItemBind.kItemBindIs);
        if (checkCode != CmdCode.Succeed)
        {
            return BagTransitionResult.InvalidArgument;
        }

        CmdCode reduceCode = m_role.ReduceItem(itemChange.ItemTypeId, itemChange.Count,
            GetReason(), ItemBind.kItemBindIs);
        if (reduceCode == CmdCode.Succeed)
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.InvalidArgument;
    }
}

public class BagMoveItemTransition : BagTransition
{
    public BagMoveItemTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>
    /// 移动物品；校验源容器、目标容器、实例 UID 和目标格位后委托组件完成交换。
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

        RoItemContainer source = m_roItemComponent.GetContainer(sourceKnapsackType);
        RoItemContainer target = m_roItemComponent.GetContainer(targetKnapsackType);
        if (source == null || target == null)
        {
            return BagTransitionResult.InvalidArgument;
        }

        long itemUid = itemChange.ItemUID;
        if (itemUid <= 0 && itemChange.Item != null)
        {
            itemUid = itemChange.Item.GetItemUID();
        }
        if (itemUid <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }

        ItemBase sourceItem = source.GetItemByUid(itemUid);
        if (sourceItem == null || sourceItem.GetItemID() != itemChange.ItemTypeId)
        {
            return BagTransitionResult.InvalidArgument;
        }
        if (itemChange.Count > sourceItem.GetItemCount())
        {
            return BagTransitionResult.InvalidArgument;
        }

        // 容器容量为零时保留装备栏等特殊容器的自定义格位；普通容器必须落在已开启范围内。
        int targetSize = target.GetContainerSize();
        if (targetSize > 0 && targetGridIndex >= targetSize)
        {
            return BagTransitionResult.InvalidArgument;
        }

        bool moved = m_roItemComponent.SwapItem(itemUid, sourceKnapsackType,
            targetKnapsackType, targetGridIndex, new ChangeReason(GetReason()),
            UpdateType.eUT_Update_All);
        if (moved)
        {
            return BagTransitionResult.Success;
        }
        return BagTransitionResult.InvalidArgument;
    }
}

public class BagSimulateTransition : BagTransition
{
    public BagSimulateTransition(OnlineRole role) : base(role)
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
    public BagLockTransition(OnlineRole role) : base(role)
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
    public BagChangeAttrTransition(OnlineRole role) : base(role)
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
    public BagZhuDongGetItemTranstion(OnlineRole role) : base(role)
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
