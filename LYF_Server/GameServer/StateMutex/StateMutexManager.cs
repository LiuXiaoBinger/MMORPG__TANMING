using System;
using System.Collections.Generic;

/// <summary>
/// 状态互斥管理器，对应参考工程 rogamelibs/state_mutex/state_mutex_manager.h/.cpp。
/// 负责注册真实存在的互斥状态、按互斥表判定能否切换、以及在需要"先退旧状态"时
/// 把请求交给角色上的 StateTransferComponent 异步推进。
/// </summary>
public sealed class StateMutexManager : Singleton<StateMutexManager>
{

    #region 字段与常量
    /// <summary>已注册的互斥状态，下标即 StateTransitionType 数值；未注册保持空。</summary>
    private readonly IStateInterface[] states_ =
        new IStateInterface[(int)StateTransitionType.kStateMaxCount];

    /// <summary>是否已经完成初始化，避免重复装载表与重复注册。</summary>
    private bool initialized_;

    #region 构造、初始化与注册
    #endregion

    /// <summary>创建状态互斥管理器；真正的注册在 Init 中完成。</summary>
    public StateMutexManager()
    {
    }

    /// <summary>
    /// 初始化：装载互斥表并注册本工程真实存在的互斥状态。
    /// 本工程没有载具/攀爬/采集/钓鱼/聊天室/观战/跳舞等玩法，因此生产注册表为空；
    /// 吟唱只作为互斥表的目标列存在，RO 也没有把它注册成源状态。
    /// 这样注册出来的都是能真正运行的判定，不留"永远 Check=false 的空壳"。
    /// </summary>
    /// <returns>互斥表装载成功返回 true。</returns>
    public bool Init()
    {
        if (initialized_)
        {
            return true;
        }
        if (!StateExclusionTable.Instance.Load())
        {
            return false;
        }
        initialized_ = true;
        return true;
    }

    /// <summary>清空注册表，对应 RO 的 UnInit；仅供测试重置与进程退出使用。</summary>
    public void UnInit()
    {
        // index 是注册表下标，范围零到 kStateMaxCount 减一，逐个置空即彻底注销。
        for (int index = 0; index < states_.Length; index++)
        {
            states_[index] = null;
        }
        initialized_ = false;
    }

    /// <summary>
    /// 注册一个互斥状态；状态标识非法或重复注册时拒绝，对应 RO 的 RegisterState。
    /// </summary>
    /// <param name="state">要注册的状态实现。</param>
    /// <returns>注册成功返回 true。</returns>
    public bool RegisterState(IStateInterface state)
    {
        if (state == null)
        {
            return false;
        }
        // type 是被注册状态自己声明的标识，同时决定它在注册表 states_ 里的下标。
        StateTransitionType type = state.GetType();
        if (!StateMutexDef.IsValidStateMutex(type))
        {
            LogMsg.Info("状态互斥注册失败：状态标识非法 " + type, LogMsgType.Warn);
            return false;
        }
        if (states_[(int)type] != null)
        {
            LogMsg.Info("状态互斥注册失败：状态重复注册 " + GetStateName(type), LogMsgType.Warn);
            return false;
        }
        states_[(int)type] = state;

    #region 业务逻辑：互斥判定与状态转移
    #endregion
        return true;
    }

    /// <summary>按状态标识取已注册的状态实现。</summary>
    /// <param name="type">状态标识。</param>
    /// <returns>已注册的状态实现，未注册或非法返回空。</returns>
    public IStateInterface GetState(StateTransitionType type)
    {
        // 非法标识直接返回空，避免越界访问注册表。
        if (!StateMutexDef.IsValidStateMutex(type))
        {
            return null;
        }
        return states_[(int)type];
    }

    /// <summary>判断该角色当前是否处于指定状态。</summary>
    /// <param name="role">待检查的在线角色。</param>
    /// <param name="type">状态标识。</param>
    /// <returns>处于该状态返回 true。</returns>
    public bool IsInState(OnlineRole role, StateTransitionType type)
    {
        if (role == null)
        {
            return false;
        }
        // state 是按标识取到的状态实现，为空表示该状态在本工程没有注册，一律按未处于处理。
        IStateInterface state = GetState(type);
        return state != null && state.Check(role);
    }

    /// <summary>让该角色离开指定状态。</summary>
    /// <param name="role">待离开状态的在线角色。</param>
    /// <param name="type">状态标识。</param>
    /// <returns>成功离开返回 true。</returns>
    public bool LeaveState(OnlineRole role, StateTransitionType type)
    {
        if (role == null || (int)type >= (int)StateTransitionType.kStateMaxCount)
        {
            return false;
        }
        // state 是按标识取到的状态实现，为空时说明没有可离开的状态，直接返回失败。
        IStateInterface state = GetState(type);
        return state != null && state.Leave(role);
    }

    /// <summary>
    /// 检查该角色能否切换到目标状态（把"先退旧状态"也当作需要检查），
    /// 对应 state_mutex_manager.cpp 的 CheckState。
    /// </summary>
    /// <param name="role">待检查的在线角色。</param>
    /// <param name="type">目标状态。</param>
    /// <param name="is_notify">拒绝时是否走通知分支；本项目无对应客户端通知帧，保留参数语义。</param>
    /// <returns>允许切换返回 Succeed，否则返回该状态的拒绝码。</returns>
    public CmdCode CheckState(OnlineRole role, StateTransitionType type, bool is_notify)
    {
        return CheckState(role, type, is_notify, true);
    }

    /// <summary>
    /// 检查该角色能否切换到目标状态（可跳过"先退旧状态"的检查），
    /// 对应 state_mutex_manager.cpp 的 CheckState(..., is_check_code_transition)。
    /// </summary>
    /// <param name="role">待检查的在线角色。</param>
    /// <param name="type">目标状态。</param>
    /// <param name="is_notify">拒绝时是否走通知分支。</param>
    /// <param name="is_check_code_transition">是否把"先退旧状态"也当作拒绝。</param>
    /// <returns>允许切换返回 Succeed，否则返回该状态的拒绝码。</returns>
    private CmdCode CheckState(OnlineRole role, StateTransitionType type, bool is_notify,
        bool is_check_code_transition)
    {
        if (!StateMutexDef.IsValidStateMutex(type) || role == null)
        {
            return CmdCode.SkillStateBlocked;
        }
        // 当前比对的已注册状态下标，范围为零到 kStateMaxCount 减一。
        for (int index = 0; index < states_.Length; index++)
        {
            // state 是当前下标记住的已注册状态实现，空槽位表示该标识没被注册，跳过即可。
            IStateInterface state = states_[index];
            if (state == null)
            {
                continue;
            }
            // check_type 是该已注册状态的标识，作为互斥表的行（源状态）去查它到目标的取值。
            StateTransitionType check_type = state.GetType();
            // value 是互斥表里「该源状态 → 目标状态」的取值，决定跳过、直接放行还是要求先退旧状态。
            StateValueType value = StateExclusionTable.Instance.GetStateValue(check_type, type);
            if (StateMutexDef.NotCheckMutexState(value, is_check_code_transition))
            {
                continue;
            }
            if (!state.Check(role))
            {
                continue;
            }
            if (is_notify)
            {
                LogMsg.Info("角色 " + role.GetID() + " 处于 " + GetStateName(check_type) +
                    "，无法切换到 " + GetStateName(type), LogMsgType.Info);
            }
            return state.GetErrorCode();
        }
        return CmdCode.Succeed;
    }

    /// <summary>
    /// 切换预处理：先做整体许可检查，再收集所有"需要先离开"的源状态，
    /// 对应 state_mutex_manager.cpp 的 PreTransferState。
    /// 返回 Succeed 且 from_state_type_list_ 为空表示可以立即切换；
    /// 返回 Succeed 且列表非空表示需要异步推进。
    /// </summary>
    /// <param name="role">发起切换的在线角色。</param>
    /// <param name="to_type">目标状态。</param>
    /// <param name="is_notify">拒绝时是否走通知分支。</param>
    /// <param name="req_info">待填充的请求信息。</param>
    /// <returns>允许切换返回 Succeed，否则返回拒绝码。</returns>
    public CmdCode PreTransferState(OnlineRole role, StateTransitionType to_type, bool is_notify,
        TransferRequestInfo req_info)
    {
        // ret 是跳过退旧检查后的互斥判定结果，非 Succeed 就不再往下收集源状态。
        CmdCode ret = CheckState(role, to_type, is_notify, false);
        if (ret != CmdCode.Succeed)
        {
            return ret;
        }
        if (role == null || req_info == null)
        {
            return CmdCode.SkillStateBlocked;
        }
        // 当前比对的已注册状态下标，范围为零到 kStateMaxCount 减一。
        for (int index = 0; index < states_.Length; index++)
        {
            // state 是当前下标记住的已注册状态实现，空槽位表示该标识没被注册，跳过即可。
            IStateInterface state = states_[index];
            if (state == null)
            {
                continue;
            }
            // check_type 是该已注册状态的标识，作为互斥表的行去查它对目标状态的取值。
            StateTransitionType check_type = state.GetType();
            // value 是互斥表取值，只有 kNeedCodeTransition（2 先退旧状态）才需要把该状态记进待离开列表。
            StateValueType value = StateExclusionTable.Instance.GetStateValue(check_type, to_type);
            if (value != StateValueType.kNeedCodeTransition)
            {
                continue;
            }
            if (state.Check(role))
            {
                req_info.from_state_type_list_.Add(check_type);
            }
        }
        req_info.role_uid_ = role.GetID();
        req_info.to_state_type_ = to_type;
        req_info.is_notify_error_ = is_notify;
        return CmdCode.Succeed;
    }

    /// <summary>
    /// 异步切换状态：需要先离开旧状态时把请求交给角色的 StateTransferComponent，
    /// 最终结果只从回调拿；不需要异步时同步回调一次。
    /// 对应 state_mutex_manager.cpp 的 TransferStateByFunc（不带 token 的重载）。
    /// </summary>
    /// <param name="role">发起切换的在线角色。</param>
    /// <param name="type">目标状态。</param>
    /// <param name="is_notify">拒绝时是否走通知分支。</param>
    /// <param name="call_back_func">最终结果回调，保证恰好被调用一次。</param>
    /// <returns>请求已受理（结果在回调里）返回 Succeed，否则返回拒绝码。</returns>
    public CmdCode TransferStateByFunc(OnlineRole role, StateTransitionType type, bool is_notify,
        Action<CmdCode> call_back_func)
    {
        // req_token 承接内部重载写出的请求令牌，本重载不对外暴露，因此只做占位接收。
        ulong req_token;
        return TransferStateByFunc(role, type, is_notify, null, call_back_func, out req_token);
    }

    /// <summary>
    /// 异步切换状态并返回请求令牌（不跨帧持有事件），
    /// 对应 state_mutex_manager.cpp 的 out_req_token 重载。
    /// </summary>
    /// <param name="role">发起切换的在线角色。</param>
    /// <param name="type">目标状态。</param>
    /// <param name="is_notify">拒绝时是否走通知分支。</param>
    /// <param name="call_back_func">最终结果回调，保证恰好被调用一次。</param>
    /// <param name="out_req_token">异步请求令牌；同步完成时为零。</param>
    /// <returns>请求已受理（结果在回调里）返回 Succeed，否则返回拒绝码。</returns>
    public CmdCode TransferStateByFunc(OnlineRole role, StateTransitionType type, bool is_notify,
        Action<CmdCode> call_back_func, out ulong out_req_token)
    {
        return TransferStateByFunc(role, type, is_notify, null, call_back_func, out out_req_token);
    }

    /// <summary>
    /// 异步切换状态并返回请求令牌，对应 state_mutex_manager.cpp 的 out_req_token 重载。
    /// 令牌可用于 CancelTransferState 取消尚未完成的请求。
    /// </summary>
    /// <param name="role">发起切换的在线角色。</param>
    /// <param name="type">目标状态。</param>
    /// <param name="is_notify">拒绝时是否走通知分支。</param>
    /// <param name="held_event">需要跨帧持有的事件；请求会接管它的一个引用，回调或移除时释放。</param>
    /// <param name="call_back_func">最终结果回调，保证恰好被调用一次。</param>
    /// <param name="out_req_token">异步请求令牌；同步完成时为零。</param>
    /// <returns>请求已受理（结果在回调里）返回 Succeed，否则返回拒绝码。</returns>
    public CmdCode TransferStateByFunc(OnlineRole role, StateTransitionType type, bool is_notify,
        ROEvent held_event, Action<CmdCode> call_back_func, out ulong out_req_token)
    {
        out_req_token = 0UL;
        // callback 是本次切换的最终结果回调；为空时换成空委托，避免后续每次调用都要判空。
        Action<CmdCode> callback = call_back_func;
        if (callback == null)
        {
            callback = delegate { };
        }
        if (role == null)
        {
            callback(CmdCode.SkillStateBlocked);
            return CmdCode.SkillStateBlocked;
        }

        // req_info 是本次切换的请求信息载体，先在互斥预检里被填满，再决定同步回调还是交给组件挂起。
        TransferRequestInfo req_info = new TransferRequestInfo();
        req_info.held_event_ = held_event;
        // ret 是互斥预检结果，非 Succeed 说明被禁止，直接同步回拒绝码、不建立挂起请求。
        CmdCode ret = PreTransferState(role, type, is_notify, req_info);
        if (ret != CmdCode.Succeed)
        {
            // 被互斥拒绝：不需要异步转移，同步回结果。
            callback(ret);
            return ret;
        }
        if (req_info.from_state_type_list_.Count == 0)
        {
            // 没有需要先离开的旧状态，直接放行。
            callback(CmdCode.Succeed);
            return CmdCode.Succeed;
        }

        // transfer_component 接收角色的状态切换组件，异步挂起必须由它托管，缺失时只能同步拒绝。
        StateTransferComponent transfer_component;
        if (!role.TryGetComponent(out transfer_component))
        {
            callback(CmdCode.SkillStateBlocked);
            return CmdCode.SkillStateBlocked;
        }
        req_info.call_back_func_ = callback;
        out_req_token = transfer_component.AddNewRequest(req_info);
        return CmdCode.Succeed;
    }

    /// <summary>取消一个尚未完成的切换请求，对应 RO 的 CancelTransferState；取消后回调不再触发。</summary>
    /// <param name="role">发起切换的在线角色。</param>
    /// <param name="token">TransferStateByFunc 返回的请求令牌。</param>
    public void CancelTransferState(OnlineRole role, ulong token)
    {
        if (token == 0UL || role == null)
        {
            return;
        }
        // transfer_component 接收角色的状态切换组件，取消操作要按令牌从它的挂起表里摘除。
        StateTransferComponent transfer_component;
        if (!role.TryGetComponent(out transfer_component))
        {
            return;
        }
        transfer_component.RemoveRequest(token);
    }

    /// <summary>
    /// 强制离开所有与目标状态互斥的旧状态，对应 RO 的 TransferStateReady。
    /// 用于死亡、离线等必须无条件回默认态的场景。
    /// </summary>
    /// <param name="role">待处理在线角色。</param>
    /// <param name="to_type">目标状态。</param>
    /// <returns>全部离开成功返回 true。</returns>
    public bool TransferStateReady(OnlineRole role, StateTransitionType to_type)
    {
        if (role == null)
        {
            return false;
        }
        // 当前比对的已注册状态下标，范围为零到 kStateMaxCount 减一。
        for (int index = 0; index < states_.Length; index++)
        {
            // state 是当前下标记住的已注册状态实现，空槽位表示该标识没被注册，跳过即可。
            IStateInterface state = states_[index];
            if (state == null)
            {
                continue;
            }
            // check_type 是该已注册状态的标识，作为互斥表的行去查它对目标状态的取值。
            StateTransitionType check_type = state.GetType();
            // value 是互斥表取值，本方法只强制处理 kNeedCodeTransition（先退旧状态）这一类。
            StateValueType value = StateExclusionTable.Instance.GetStateValue(check_type, to_type);
            if (value != StateValueType.kNeedCodeTransition)
            {
                continue;
            }
            if (state.Check(role) && !state.Leave(role))
            {
                return false;

    #region 查询
    #endregion
            }
        }
        return true;
    }

    /// <summary>收集该角色当前所处的全部已注册状态。</summary>
    /// <param name="role">待检查的在线角色。</param>
    /// <param name="states">输出集合，调用前会被清空。</param>
    /// <returns>至少处于一个已注册状态时返回 true。</returns>
    public bool GetNowStates(OnlineRole role, HashSet<int> states)
    {
        if (role == null || states == null)
        {
            return false;
        }
        states.Clear();
        // 当前比对的已注册状态下标，范围为零到 kStateMaxCount 减一。
        for (int index = 0; index < states_.Length; index++)
        {
            // state 是当前下标记住的已注册状态实现，空槽位表示该标识没被注册，跳过即可。
            IStateInterface state = states_[index];
            if (state != null && state.Check(role))
            {
                states.Add((int)state.GetType());
            }
        }
        return states.Count > 0;
    }
    #endregion
    /// <summary>取状态下标对应的 RO 原始状态名，供日志定位。</summary>
    /// <param name="type">状态标识。</param>
    public string GetStateName(StateTransitionType type)
    {
        return StateExclusionTable.Instance.GetStateName(type);
    }
}
