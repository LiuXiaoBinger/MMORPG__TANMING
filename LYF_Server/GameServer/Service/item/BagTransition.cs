using System;
using System.Collections.Generic;


/// <summary>
/// 背包操作的公共事务基类。
/// 基类保存操作角色、背包组件和变更原因；派生类按各自已接入的入口修改内存物品。
///  一个事务绑定一个角色和其背包，负责汇总物品变化原因、通知客户端和任务系统。
/// 这里的“事务”表示一次业务操作，不自带数据库提交或整批失败回滚；持久化由角色存档流程处理。
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
    /// <summary>绑定角色并尝试取得背包组件，为本次操作创建来源记录。</summary>
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
    /// 调用组件发送已累计的物品增量，再执行 AfterNotifyClient；当前没有调用 BeforeNotifyClient。
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

    /// <summary>把传入的原因作为本次操作的主原因，清除之前的次原因数据。</summary>
    public void SetReason(ItemChangeReason itemReasonBuyItem)
    {
        record.SetItemChangeReason(new ChangeReason(itemReasonBuyItem));
    }
    /// <summary>读取用于后续增删物品的主原因。</summary>
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

/// <summary>背包操作来自哪一类业务；目前仅登记默认值与购买商品。</summary>
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

/// <summary>背包服务层的操作结果；协议处理器需按调用场景转换为网络错误码。</summary>
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
    NotImplemented = 4,

    /// <summary>卸装需要的普通装备背包格子已满。</summary>
    BagFull = 5
}

// ============================================================
// 辅助数据结构
// ============================================================

/// <summary>单次背包操作的来源与文字原因，不负责保存物品或提交数据库。</summary>
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

/// <summary>描述待处理物品的配置、实例、数量和位置；各入口只使用自己需要的字段。</summary>
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
    /// 预留的目标或来源格子下标；默认值为 0，移动入口实际使用单独传入的目标下标。
    /// </summary>
    public int GridIndex { get; set; }
}

// ============================================================
// 背包事务派生类
// ============================================================

/// <summary>扣除物品的入口集合；ItemDesc 重载已接入角色扣除，BagItemChange 重载仍未实现。</summary>
public class BagTakeItemTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
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

/// <summary>发放物品的入口集合，普通物品与虚拟物品走各自的已接入路径。</summary>
public class BagGiveItemTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
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

    /// <summary>按物品编号选择虚拟背包或普通发放路径；普通物品当前要求总价大于 0，再由角色组件按配置选择容器。</summary>
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

        // 读取发放前数量，当前变量未参与后续结算。
        int item_cout = Role.GetItemCount(itemDesc.ItemID);
        var reason = GetTotalReason();
        if (itemDesc.ChangeReason != ItemChangeReason.ITEM_REASON_NONE)
        {
            // 描述对象指定了单独原因时，覆盖本次操作记录器中的原因。
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

    /// <summary>读取本次发放携带的主原因、次原因及附加信息。</summary>
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

/// <summary>在 81 至 255 格范围内增加四类普通背包容量。</summary>
public class BagExpansionTransition : BagTransition
{
    // 扩容计算的初始格数下限。
    private const int MinOpenedGridCount = 81;
    // 容量按 byte 保存，最多 255 格。
    private const int MaxOpenedGridCount = byte.MaxValue;

    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
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
        // 是否为角色普通背包。
        bool isPlainPack = type == KnapsackType.RolePackPlain;
        // 是否为角色装备背包。
        bool isEquipPack = type == KnapsackType.RolePackEquip;
        // 是否为角色消耗品或材料背包。
        bool isConsumeOrMaterialPack = type == KnapsackType.RolePackConsume ||
            type == KnapsackType.RolePackMaterial;
        return isPlainPack || isEquipPack || isConsumeOrMaterialPack;
    }
}

/// <summary>按装备实例 UID 执行穿脱、同槽换装和特技切换，再重算角色装备效果。</summary>
public class BagWearItemTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
    /// <param name="role">本次操作所属的在线角色，客户端不能指定其他角色。</param>
    public BagWearItemTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>穿戴装备背包中的指定实例；目标部位由装备配置决定，同部位旧装备返回原格。</summary>
    /// <param name="itemUid">本角色装备背包中的装备实例 UID。</param>
    public virtual BagTransitionResult WearItem(long itemUid)
    {
        // 背包恢复未完成时沿用既有事务的装载状态约束。
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }
        // 只允许从普通装备背包穿戴，不能按配置 ID 误选另一件装备。
        EquipBase equip = m_roItemComponent.GetContainer(KnapsackType.RolePackEquip)
            .GetItemByUid(itemUid) as EquipBase;
        if (equip == null)
        {
            return BagTransitionResult.InvalidArgument;
        }
        // 装备配置提供权威部位、等级与职业需求。
        cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equip.GetItemID());
        if (config == null || !Enum.IsDefined(typeof(EquipType), config.EquipType))
        {
            return BagTransitionResult.InvalidArgument;
        }
        // 角色裸属性中的等级和职业不受装备效果影响。
        RoleBaseInfo roleInfo = m_role.mainRoleInfo.BaseInfo;
        if (roleInfo.Level < config.NeedLevel)
        {
            return BagTransitionResult.InvalidArgument;
        }
        if (config.Job != 0 && config.Job != roleInfo.JobId)
        {
            return BagTransitionResult.InvalidArgument;
        }
        SetReason(ItemChangeReason.ITEM_REASON_WEAR_EQUIP);
        if (!m_roItemComponent.WearEquip(itemUid, config.EquipType, record.GetTotalReason()))
        {
            return BagTransitionResult.InvalidArgument;
        }
        CompleteEquipmentChange();
        return BagTransitionResult.Success;
    }

    /// <summary>卸下指定穿戴实例，先确认装备背包空位再移动，背包满时保持原穿戴状态。</summary>
    /// <param name="itemUid">本角色当前穿戴的装备实例 UID。</param>
    public virtual BagTransitionResult TakeOffItem(long itemUid)
    {
        // 保持和穿戴一致的角色装载要求。
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }
        // 只有当前穿戴容器中的装备可通过卸装入口移出。
        EquipBase equip = m_roItemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack)
            .GetItemByUid(itemUid) as EquipBase;
        if (equip == null)
        {
            return BagTransitionResult.InvalidArgument;
        }
        // 查找普通装备背包的第一个实际空格，容量由现有扩容数据提供。
        RoItemContainer target = m_roItemComponent.GetContainer(KnapsackType.RolePackEquip);
        // targetPosition 为找到的空格下标，-1 表示背包已满。
        int targetPosition = -1;
        // index 按已开启背包格位顺序检查，不影响任何物品状态。
        for (int index = 0; index < target.GetContainerSize(); index++)
        {
            if (target.GetItemByPos(index) == null)
            {
                targetPosition = index;
                break;
            }
        }
        if (targetPosition < 0)
        {
            return BagTransitionResult.BagFull;
        }
        SetReason(ItemChangeReason.ITEM_REASON_TAKE_OFF_EQUIP);
        if (!m_roItemComponent.SwapItem(itemUid, KnapsackType.RoleCurrtEquipPack,
            KnapsackType.RolePackEquip, targetPosition, record.GetTotalReason(), UpdateType.eUT_Normal))
        {
            return BagTransitionResult.InvalidArgument;
        }
        CompleteEquipmentChange();
        return BagTransitionResult.Success;
    }

    /// <summary>在装备已有的三个特技中启用一个非空槽位，保留其他槽位内容用于后续切换。</summary>
    /// <param name="itemUid">本角色装备背包或穿戴栏中的装备实例 UID。</param>
    /// <param name="specialSkillIndex">本次启用的特技槽位下标，范围为 0 到 2。</param>
    public virtual BagTransitionResult SwitchSpecialSkill(long itemUid, int specialSkillIndex)
    {
        // 特技状态必须在权威背包装载完成后才能持久修改。
        BagTransitionResult contextResult = ValidateContext();
        if (contextResult != BagTransitionResult.Success)
        {
            return contextResult;
        }
        // 仅查询本角色穿戴栏和装备背包，不扩大私有的全容器查询入口。
        EquipBase equip = m_roItemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack)
            .GetItemByUid(itemUid) as EquipBase;
        if (equip == null)
        {
            equip = m_roItemComponent.GetContainer(KnapsackType.RolePackEquip).GetItemByUid(itemUid) as EquipBase;
        }
        if (equip == null || specialSkillIndex < 0 || specialSkillIndex > 2)
        {
            return BagTransitionResult.InvalidArgument;
        }
        if (equip.GetSpecialSkillId(specialSkillIndex) <= 0)
        {
            return BagTransitionResult.InvalidArgument;
        }
        equip.ActiveSpecialSkillIndex = specialSkillIndex;
        m_roItemComponent.OnItemChange(equip, record.GetTotalReason(), UpdateType.eUT_Normal);
        CompleteEquipmentChange();
        return BagTransitionResult.Success;
    }

    /// <summary>操作成功后先发送完整装备增量，再由装备组件重算并通知最终属性。</summary>
    private void CompleteEquipmentChange()
    {
        NotifyClient();
        m_role.GetComponent<RoleEquipComponent>().OnEquipChange();
    }
}

/// <summary>先检查数量再扣除消耗的物品；具体使用效果由上层业务处理。</summary>
public class BagUseItemTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
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

/// <summary>按实例UID移动整个物品堆叠；数量字段用于校验，不用于拆分堆叠。</summary>
public class BagMoveItemTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
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

/// <summary>批量增删预演的预留入口，目前不计算结果。</summary>
public class BagSimulateTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
    public BagSimulateTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>
    /// 预演入口尚未实现；校验角色上下文后直接返回 NotImplemented。
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

/// <summary>物品锁定的预留入口，目前不修改锁定状态。</summary>
public class BagLockTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
    public BagLockTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>
    /// 锁定入口尚未实现；校验角色上下文和物品配置ID后返回 NotImplemented。
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

/// <summary>物品属性修改的预留入口，目前不写入属性。</summary>
public class BagChangeAttrTransition : BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
    public BagChangeAttrTransition(OnlineRole role) : base(role)
    {
    }

    /// <summary>
    /// 属性修改入口尚未实现；校验角色上下文和物品配置ID后返回 NotImplemented。
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
// 批量领取的容量预检查；正式批量发放入口目前为空，不具备整批提交或失败回滚。
/// <summary>批量领取前估算已有堆叠与空格能否容纳物品；当前发放方法仍为空。</summary>
public class BagZhuDongGetItemTranstion :  BagTransition
{
    /// <summary>为本次操作绑定角色，复用基类保存的背包组件和原因记录。</summary>
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
            // 当前使用浮点除法后向上取整；以下复合赋值还会再次累加此前的 space_still_need。
          
            space_still_need += space_still_need += (int)Math.Ceiling((float)temp_count / maxOverlap);
            
        }

        if( temp_bag_left_space - space_still_need< 0 )
        {
            Result.ErrorCode = CmdCode.BagFull;
            // 当前返回的是累计所需格子数的绝对值，并未扣掉已有空格数。
            Result.space_still_need = Math.Abs(space_still_need);
        }
        return Result;
    }
    // 批量发放占位：当前只返回默认结果，不检查也不发放物品。
    ItemZhuDongGetResult ZhuDongGetItem(List<ItemDesc> desc, KnapsackType type = KnapsackType.RolePackPlain)
    {
        ItemZhuDongGetResult Result = default;
        return Result;
    }
};
