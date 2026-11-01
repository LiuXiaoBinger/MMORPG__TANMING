/// <summary>
/// 状态互斥状态枚举，忠实照抄参考工程 rogamelibs/state_mutex/state_mutex_def.h:9-55。
/// 名称与数值全部保持 RO 原样，因为状态互斥表的列序依赖这些下标，重编号会导致整张表错位。
/// </summary>
public enum StateTransitionType
{
    /// <summary>变身(主动)。</summary>
    kInitiativeTransfigured = 0,
    /// <summary>地面载具。</summary>
    kGroundVehicle,
    /// <summary>飞行载具。</summary>
    kFlyVehicle,
    /// <summary>场景交互。</summary>
    kSceneInteraction,
    /// <summary>自拍。</summary>
    kPhoto,
    /// <summary>单人动作。</summary>
    kSingleAction,
    /// <summary>攀爬。</summary>
    kClimbAction,
    /// <summary>采集。</summary>
    kCollectionAction,
    /// <summary>变身(被动)。</summary>
    kPassiveTransfigured,
    /// <summary>任务道具交互动作。</summary>
    kTaskInteractive,
    /// <summary>Npc 交互。</summary>
    kNpcInteractive,
    /// <summary>玩家交互(无效状态)。</summary>
    kPlayerInteractive,
    /// <summary>进入战斗。</summary>
    kGoFight,
    /// <summary>被动进战斗。</summary>
    kPassiveGoFight,
    /// <summary>假死（装死，不是血量归零）。</summary>
    kUnrealDeath,
    /// <summary>切换分线。</summary>
    kChangeLine,
    /// <summary>骑乘战斗。</summary>
    kRidingFight,
    /// <summary>天气动作。</summary>
    kWeatherAction,
    /// <summary>钓鱼动作。</summary>
    kFishingAction,
    /// <summary>公共载具。</summary>
    kPublicVehicle,
    /// <summary>回到公会。</summary>
    kBackToGuild,
    /// <summary>双人地面载具。</summary>
    kCoupleGroundVehicle,
    /// <summary>双人飞行载具。</summary>
    kCoupleFlyVehicle,
    /// <summary>片段型双人动作。</summary>
    kCoupleActionFragment,
    /// <summary>持续型双人动作。</summary>
    kCoupleActionContinue,
    /// <summary>原地聊天室。</summary>
    kChatRoom,
    /// <summary>转职。</summary>
    kTransferProfession,
    /// <summary>Buff 特殊状态。</summary>
    kSpecialBuff,
    /// <summary>跑商状态。</summary>
    kRunBusiness,
    /// <summary>寻路状态。</summary>
    kPathFinding,
    /// <summary>搬运状态。</summary>
    kCarryItem,
    /// <summary>Buff 触发进入互斥状态。</summary>
    kBuffStateMutex,
    /// <summary>观战。</summary>
    kStateTransitionWatch,
    /// <summary>跳舞。</summary>
    kStateTransitionDance,
    /// <summary>脱离卡死。</summary>
    kStateRoleDetached,
    /// <summary>请求释放技能（以前叫 singing），只作为互斥表的目标列。</summary>
    kStateTransitionRequestSinging,
    /// <summary>野外跳舞。</summary>
    kStateTransitionWildDance,
    /// <summary>自动采摘/挖矿。</summary>
    kStateTransitionAutoCollect,
    /// <summary>Buff 触发技能释放。</summary>
    kStateBuffTriggerSkill,
    /// <summary>状态数量上界，用于数组与循环边界，本身不是状态。</summary>
    kStateMaxCount,

    /// <summary>
    /// 物品使用限制。RO 明确注释它不属于互斥状态，只是借用同一套切换逻辑，因此取值 500 与之隔离。
    /// </summary>
    kUseItemLimit = 500
}

/// <summary>
/// 互斥表单元格取值，忠实照抄 state_mutex_def.h 的 StateValueType。
/// </summary>
public enum StateValueType
{
    /// <summary>状态码 -1，表里未配置，理论上不会发生这种状态改变。</summary>
    kMutexStateNone = -1,
    /// <summary>状态码 0，无法从 A 状态切换到 B 状态，并提示 A 无法 B。</summary>
    kForbidTransition = 0,
    /// <summary>状态码 1，可以直接切换。</summary>
    kCanTransition = 1,
    /// <summary>状态码 2，可以切换，但需要程序特殊处理（先 leave A，再 enter B）。</summary>
    kNeedCodeTransition = 2,
    /// <summary>状态码 3，纯前端处理，后端认为可以直接切换且不改变原有状态。</summary>
    kClientProcess = 3
}

/// <summary>
/// 互斥判定的静态工具，对应 state_mutex_def.h 的 StateMutexDef。
/// </summary>
public static class StateMutexDef
{
    /// <summary>判断状态下标是否落在互斥表的有效范围内。</summary>
    /// <param name="type">待判定的状态标识。</param>
    public static bool IsValidStateMutex(StateTransitionType type)
    {
        return type >= StateTransitionType.kInitiativeTransfigured &&
            type < StateTransitionType.kStateMaxCount;
    }

    /// <summary>
    /// 判断该取值是否需要做互斥检查，对应 StateMutexDef::NotCheckMutexState。
    /// 允许切换与纯前端处理一律不检查；"先退旧状态"只在允许跳过代码处理时放行。
    /// </summary>
    /// <param name="type">互斥表单元格取值。</param>
    /// <param name="is_check_code_transition">是否把"先退旧状态"也当作需要检查。</param>
    public static bool NotCheckMutexState(StateValueType type, bool is_check_code_transition)
    {
        return type == StateValueType.kCanTransition || type == StateValueType.kClientProcess ||
            (!is_check_code_transition && type == StateValueType.kNeedCodeTransition);
    }
}
