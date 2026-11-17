/// <summary>
/// RO 客户端大状态互斥的状态枚举，数值即 <c>StateExclusionTable</c> 的行下标与列下标。
/// 与 RO <c>MoonClient/StateExclusion/StateExclusionEnum.cs</c> 的 <c>MExlusionStates</c> 保持同名同序，
/// 非负状态值必须与配置表的行列顺序一致；新增状态需同时更新表，并保持 State_Last 等于状态数。
/// </summary>
public enum MExlusionStates
{
    /// <summary>不存在的状态，矩阵中表示“该切换路径没有配置”，客户端按不可切换处理。</summary>
    State_Not_Exist = -3,

    /// <summary>未持有任何大状态，等价于默认态；连续状态列表中不会出现。</summary>
    State_None_State = -2,

    /// <summary>默认状态标识，不是有效矩阵下标；是否允许切换由调用入口和当前状态列表决定。</summary>
    State_Default_State = -1,

    /// <summary>主动变身，下标 0。</summary>
    State_A_InitiativeTransfigured,

    /// <summary>单人地面载具，下标 1。</summary>
    State_B_GroundVehicle,

    /// <summary>单人飞行载具，下标 2。</summary>
    State_C_FlyVehicle,

    /// <summary>场景交互（触发物件），下标 3。</summary>
    State_D_SceneInteraction,

    /// <summary>自拍动作，下标 4。</summary>
    State_E_Photo,

    /// <summary>单人动作（AnimationTable 中的动作表演），下标 5。</summary>
    State_F_SingleAction,

    /// <summary>攀爬，下标 6。</summary>
    State_G_Climb,

    /// <summary>采集（含自动采集外的普通采集），下标 7。</summary>
    State_H_Collection,

    /// <summary>被动变身，RO 已标注废弃，下标 8。</summary>
    State_I_PassiveTransfigured,

    /// <summary>任务道具交互动作，RO 已标注废弃，下标 9。</summary>
    State_J_TaskInteractive,

    /// <summary>NPC 双人动作，纯前端状态，下标 10。</summary>
    State_K_NpcInteractive,

    /// <summary>玩家交互，RO 已标注无效状态，下标 11。</summary>
    State_L_PlayerInteractive,

    /// <summary>战斗状态，下标 12。</summary>
    State_M_GoFight,

    /// <summary>被动进入战斗，RO 已标注废弃，下标 13。</summary>
    State_N_PassiveGoFight,

    /// <summary>假死（死亡拟态）状态，下标 14。</summary>
    State_O_UnrealDeath,

    /// <summary>切换分线，下标 15。</summary>
    State_P_ChangeLine,

    /// <summary>战斗载具状态，下标 16。</summary>
    State_Q_RidingFight,

    /// <summary>天气动作，纯前端状态，下标 17。</summary>
    State_R_WeatherAction,

    /// <summary>钓鱼状态，下标 18。</summary>
    State_S_FishingAction,

    /// <summary>公共载具状态，下标 19。</summary>
    State_T_PublicVehicle,

    /// <summary>回到公会，下标 20。</summary>
    State_U_BackToGuild,

    /// <summary>双人地面载具的乘客位，下标 21。</summary>
    State_V_CoupleGroundVehicle,

    /// <summary>双人飞行载具的乘客位，下标 22。</summary>
    State_W_CoupleFlyVehicle,

    /// <summary>片段型双人动作（拥抱等），下标 23。</summary>
    State_X_CoupleActionFragment,

    /// <summary>持续型双人动作（拉手等），下标 24；RO 原枚举沿用 X 前缀拼写，为与 RO 表逐位对齐这里保留原样。</summary>
    State_X_CoupleActionContinue,

    /// <summary>原地聊天室，下标 25。</summary>
    State_Z_ChatRoom,

    /// <summary>转职，服务端功能，下标 26。</summary>
    State_AA_TransferProfession,

    /// <summary>特殊战斗 Buff，服务端功能，下标 27。</summary>
    State_AB_SpecialBuff,

    /// <summary>跑商，下标 28。</summary>
    State_AC_RunBusiness,

    /// <summary>寻路，下标 29。</summary>
    State_AD_PathFinding,

    /// <summary>搬运物品，下标 30。</summary>
    State_AE_CarryItem,

    /// <summary>Buff 触发进入的互斥状态（公会宴会抱抱熊等），下标 31。</summary>
    State_AF_BuffStateMutex,

    /// <summary>观战，下标 32。</summary>
    State_AG_Spectator,

    /// <summary>跳舞，下标 33。</summary>
    State_AH_Dance,

    /// <summary>脱离卡死，下标 34。</summary>
    State_AI_Detached,

    /// <summary>吟唱（状态机请求释放技能），下标 35；客户端施法发包前判定互斥的目标状态。</summary>
    State_AJ_Singing,

    /// <summary>野外跳舞，下标 36。</summary>
    State_AK_WildDance,

    /// <summary>自动采摘/挖矿，下标 37。</summary>
    State_AQ_AutoCollect,

    /// <summary>状态总数，同时也是矩阵每行的列数；新增状态必须加在它上方。</summary>
    State_Last,
}

/// <summary>
/// 互斥矩阵单元格的取值含义，与 RO <c>MExlusionStatesCode</c> 完全一致。
/// 单元格内容按“当前状态行 → 目标状态列”解读。
/// </summary>
public enum MExlusionStatesCode
{
    /// <summary>理论上不会出现的状态切换，只表示矩阵没有为该路径配置值。</summary>
    NoExist = -1,

    /// <summary>无法从当前状态切到目标状态，界面需要提示“A 无法 B”。</summary>
    CanNotTrans = 0,

    /// <summary>可以从当前状态直接切到目标状态。</summary>
    NormalPss = 1,

    /// <summary>需要先离开当前状态再进入目标状态；服务端先退旧状态。</summary>
    OneLeaveOneEnter = 2,

    /// <summary>纯前端处理，后端认为可以直接切换。</summary>
    ClientState = 3,
}
