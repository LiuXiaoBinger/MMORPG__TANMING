using System;
using System.Collections.Generic;

/// <summary>
/// 单个状态切换请求，对应参考工程 rogamelibs/entity/component/state_transfer_component.h 的 TransferRequestInfo。
/// 请求额外保留触发事件的池引用；等待旧状态退出期间，引用计数防止该事件被提前重置并供其他请求复用。
/// </summary>
public sealed class TransferRequestInfo
{
    /// <summary>发起请求的角色 ID，对应 RO 的 role_uid。</summary>
    public uint role_uid_;

    /// <summary>请求的唯一序号，对应 RO 的 serial_id。</summary>
    public ulong serial_id_;

    /// <summary>请求开始时间，单位毫秒。</summary>
    public long begin_time_;

    /// <summary>目标状态。</summary>
    public StateTransitionType to_state_type_ = StateTransitionType.kStateMaxCount;

    /// <summary>需要先离开的源状态集合，对应 RO 的 from_state_type_list。</summary>
    public readonly HashSet<StateTransitionType> from_state_type_list_ = new HashSet<StateTransitionType>();

    /// <summary>失败时是否通知客户端，对应 RO 的 is_notify_error。</summary>
    public bool is_notify_error_ = true;

    /// <summary>最终结果回调，对应 RO 的 call_back_func。</summary>
    public Action<CmdCode> call_back_func_;

    /// <summary>本次切换持有的触发事件；请求创建时 AddRef，回调或移除时 Release。</summary>
    public ROEvent held_event_;

    /// <summary>以当前时间初始化请求。</summary>
    public TransferRequestInfo()
    {
        begin_time_ = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

/// <summary>
/// 状态切换组件，对应参考工程 rogamelibs/entity/component/state_transfer_component.cpp。
/// 负责在"需要先离开旧状态"时记录请求、驱动旧状态离开，并在全部离开后进入新状态。
/// </summary>
public sealed class StateTransferComponent : RoleComponentBase
{
    /// <summary>结束切换请求的两种结果，对应 RO 的 EnterStateType。</summary>
    private enum EnterStateType
    {
        /// <summary>正常进入。</summary>
        kNormal = 0,
        /// <summary>前面操作失败了，再进入。</summary>
        kFail = 1
    }

    /// <summary>请求序号种子，从零开始自增，零保留为"无请求"。</summary>
    private ulong serial_id_;

    /// <summary>请求序号到请求信息的映射。</summary>
    private readonly Dictionary<ulong, TransferRequestInfo> state_request_transfer_map_ =
        new Dictionary<ulong, TransferRequestInfo>();

    /// <summary>源状态到等待它离开的请求序号的映射，对应 RO 的 state_type_serial_id_map_。</summary>
    private readonly Dictionary<StateTransitionType, HashSet<ulong>> state_type_serial_id_map_ =
        new Dictionary<StateTransitionType, HashSet<ulong>>();

    #region 构造与初始化
    /// <summary>创建状态切换组件。</summary>
    /// <param name="owner">组件所属的在线角色。</param>
    public StateTransferComponent(OnlineRole owner)
        : base(owner)
    {
    }

    /// <summary>
    /// 组件释放时取消全部挂起请求并释放事件引用，避免角色销毁后仍持有池化事件。
    /// </summary>
    public override void Dispose()
    {
        // serial_ids 是全部挂起请求序号的键快照：移除会改动映射，不能在遍历原键集时删。
        List<ulong> serial_ids = new List<ulong>(state_request_transfer_map_.Keys);
        // index 是请求序号快照下标，逐个取消并释放事件引用，避免边遍历边删原映射。
        for (int index = 0; index < serial_ids.Count; index++)
        {
            RemoveRequest(serial_ids[index]);
        }
        state_request_transfer_map_.Clear();
        state_type_serial_id_map_.Clear();
        base.Dispose();
    }
    #endregion

    #region 离开状态回调与请求登记
    /// <summary>
    /// 某个源状态已经离开时回调：该请求的所有源状态都离开后进入新状态，
    /// 对应 state_transfer_component.cpp 的 OnLeaveState。
    /// </summary>
    /// <param name="state_type">刚刚离开的源状态。</param>
    public void OnLeaveState(StateTransitionType state_type)
    {
        if (state_request_transfer_map_.Count == 0)
        {
            return;
        }
        // serial_ids 是等待该源状态离开的请求序号集合，没有登记说明本次离开与切换无关。
        HashSet<ulong> serial_ids;
        if (!state_type_serial_id_map_.TryGetValue(state_type, out serial_ids))
        {
            return;
        }
        // 复制一份序号快照：进入新状态会修改映射，不能在遍历原集合时改动它。
        List<ulong> serial_id_snapshot = new List<ulong>(serial_ids);
        // index 是请求序号快照下标，从零到快照长度减一，遍历期间不改动原集合。
        for (int index = 0; index < serial_id_snapshot.Count; index++)
        {
            // req 是本轮待判定的切换请求；快照下标 index 只用于按下标取序号。
            TransferRequestInfo req;
            if (!state_request_transfer_map_.TryGetValue(serial_id_snapshot[index], out req))
            {
                LogMsg.Info("状态切换请求丢失，序号=" + serial_id_snapshot[index], LogMsgType.Error);
                continue;
            }
            // all_left 记录该请求的全部源状态是否都已离开；只要还有一个没离开就继续等。
            bool all_left = true;
            // wait_types 是源状态集合的成员快照，集合没有下标，先取快照再按下标遍历。
            List<StateTransitionType> wait_types = new List<StateTransitionType>(req.from_state_type_list_);
            // wait_index 是源状态快照下标，一旦发现还有源状态未离开就提前跳出。
            for (int wait_index = 0; wait_index < wait_types.Count; wait_index++)
            {
                if (StateMutexManager.Instance.IsInState(Owner, wait_types[wait_index]))
                {
                    all_left = false;
                    break;
                }
            }
            if (all_left)
            {
                EnterState(req, EnterStateType.kNormal);
            }
        }
    }

    /// <summary>
    /// 添加状态切换请求并立即尝试推进，对应 state_transfer_component.cpp 的 AddNewRequest。
    /// 请求会接管调用方传入事件的一个引用，回调、移除或销毁时释放。
    /// </summary>
    /// <param name="req">已填充好的切换请求。</param>
    /// <returns>请求序号；添加失败返回零。</returns>
    public ulong AddNewRequest(TransferRequestInfo req)
    {
        if (req == null)
        {
            return 0UL;
        }
        serial_id_++;
        if (serial_id_ == 0UL)
        {
            serial_id_++;
        }
        // new_serial_id 是本次请求的序号（自增且跳过零），同时作为两张索引表的键，生命周期直到请求完成或取消。
        ulong new_serial_id = serial_id_;
        req.serial_id_ = new_serial_id;
        // 请求要在多个逻辑帧内活着，必须先给事件加一个引用，否则池化实例会被提前归还。
        if (req.held_event_ != null && !req.held_event_.AddRef())
        {
            return 0UL;
        }
        state_request_transfer_map_[new_serial_id] = req;
        // 每个需要先离开的源状态都登记到索引里，便于 OnLeaveState 反查。
        // from_types 是源状态集合的成员快照：集合本身没有稳定下标，先取快照再按下标遍历。
        List<StateTransitionType> from_types = new List<StateTransitionType>(req.from_state_type_list_);
        // index 是源状态快照下标，从零到成员数减一，只用于登记反向索引。
        for (int index = 0; index < from_types.Count; index++)
        {
            // from_type 是本轮要登记的源状态；index 是快照下标，只用于按下标取成员。
            StateTransitionType from_type = from_types[index];
            // serial_ids 是该源状态对应的等待请求序号集合，缺失时新建并挂到反向索引上。
            HashSet<ulong> serial_ids;
            if (!state_type_serial_id_map_.TryGetValue(from_type, out serial_ids))
            {
                serial_ids = new HashSet<ulong>();
                state_type_serial_id_map_.Add(from_type, serial_ids);
            }
            serial_ids.Add(new_serial_id);
        }
        ProcessRequest(req);
        return new_serial_id;
    }
    #endregion

    #region 移除与回收
    /// <summary>
    /// 移除请求并释放它持有的事件引用；移除后回调不再触发，
    /// 对应 state_transfer_component.cpp 的 RemoveRequest。
    /// </summary>
    /// <param name="serial_id">要移除的请求序号。</param>
    /// <returns>找到并移除返回 true。</returns>
    public bool RemoveRequest(ulong serial_id)
    {
        // req 是摘除下来的请求对象，为空说明序号不存在（已被回调或销毁流程处理过）。
        TransferRequestInfo req = DetachRequest(serial_id);
        if (req == null)
        {
            return false;
        }
        ReleaseHeldEvent(req);
        return true;
    }

    /// <summary>释放请求持有的事件引用；空引用直接忽略。</summary>
    /// <param name="req">待释放的请求。</param>
    private static void ReleaseHeldEvent(TransferRequestInfo req)
    {
        if (req == null || req.held_event_ == null)
        {
            return;
        }
        req.held_event_.Release();
        req.held_event_ = null;
    }
    #endregion

    #region 转移流程处理
    /// <summary>
    /// 把请求从两张索引表里摘掉并清空回调，但不释放事件引用。
    /// 供 EnterState 在回调执行完之后再释放，避免回调里用到已被回收的池化事件。
    /// </summary>
    /// <param name="serial_id">要摘除的请求序号。</param>
    /// <returns>被摘除的请求；不存在时返回空。</returns>
    private TransferRequestInfo DetachRequest(ulong serial_id)
    {
        // req 是待摘除的请求；序号不存在说明已被回调或销毁流程处理过，直接返回空。
        TransferRequestInfo req;
        if (!state_request_transfer_map_.TryGetValue(serial_id, out req))
        {
            return null;
        }
        // from_types 是源状态集合的成员快照：集合没有稳定下标，先取快照再按下标逐个从反向索引摘除。
        List<StateTransitionType> from_types = new List<StateTransitionType>(req.from_state_type_list_);
        // index 是源状态快照下标，从零到成员数减一，只用于逐个摘除反向索引。
        for (int index = 0; index < from_types.Count; index++)
        {
            // from_type 是本轮要摘除的源状态；index 只是快照下标。
            StateTransitionType from_type = from_types[index];
            // serial_ids 是该源状态当前登记的等待请求序号集合，缺失说明索引已被清空。
            HashSet<ulong> serial_ids;
            if (!state_type_serial_id_map_.TryGetValue(from_type, out serial_ids))
            {
                continue;
            }
            serial_ids.Remove(serial_id);
            if (serial_ids.Count == 0)
            {
                state_type_serial_id_map_.Remove(from_type);
            }
        }
        state_request_transfer_map_.Remove(serial_id);
        req.call_back_func_ = null;
        return req;
    }

    /// <summary>
    /// 处理请求：逐个离开源状态，对应 state_transfer_component.cpp 的 ProcessRequest。
    /// 任一源状态离开失败时按失败进入新状态，让回调拿到失败码而不是永久挂起。
    /// </summary>
    /// <param name="req">待处理的请求。</param>
    private void ProcessRequest(TransferRequestInfo req)
    {
        // serial_id 是本次处理的请求序号，离开状态可能触发回调并改动请求表，后续都要按序号重查。
        ulong serial_id = req.serial_id_;
        // from_types 是源状态集合的成员快照：离开状态会触发回调并改动请求表。
        List<StateTransitionType> from_types = new List<StateTransitionType>(req.from_state_type_list_);
        // index 是源状态快照下标，按顺序离开；任一离开失败即转成失败收尾并结束。
        for (int index = 0; index < from_types.Count; index++)
        {
            if (StateMutexManager.Instance.LeaveState(Owner, from_types[index]))
            {
                continue;
            }
            // current_req 是重查后的同一请求；回调可能已经把它摘掉，摘掉就不再重复收尾。
            TransferRequestInfo current_req;
            if (state_request_transfer_map_.TryGetValue(serial_id, out current_req))
            {
                EnterState(current_req, EnterStateType.kFail);
            }
            return;
        }

        // Leave 是同步实现：所有源状态都真的离开后，本次请求在这里完成进入。
        // 若某个状态的 Leave 是异步的（离开后仍 Check 为真），则留给 OnLeaveState 继续推进。
        // req_after_leave 是离开动作之后重查到的同一请求。
        TransferRequestInfo req_after_leave;
        if (!state_request_transfer_map_.TryGetValue(serial_id, out req_after_leave))
        {
            return;
        }
        // left_types 是源状态集合的成员快照；只要还残留一个源状态就交给 OnLeaveState 继续推进。
        List<StateTransitionType> left_types = new List<StateTransitionType>(req_after_leave.from_state_type_list_);
        // index 是源状态快照下标，只要还残留一个未离开的源状态就交给 OnLeaveState 继续推进。
        for (int index = 0; index < left_types.Count; index++)
        {
            if (StateMutexManager.Instance.IsInState(Owner, left_types[index]))
            {
                return;
            }
        }
        EnterState(req_after_leave, EnterStateType.kNormal);
    }

    /// <summary>
    /// 进入新状态并回调结果，对应 state_transfer_component.cpp 的 EnterState。
    /// 正常进入前会再检查一次互斥表，请求可能等到后续回调才完成，期间角色的其他状态可能已改变。
    /// </summary>
    /// <param name="req">待完成的请求。</param>
    /// <param name="enter_type">进入来源。</param>
    private void EnterState(TransferRequestInfo req, EnterStateType enter_type)
    {
        if (enter_type == EnterStateType.kNormal)
        {
            // ret 是进入新状态前的二次互斥检查结果，非 Succeed 就改按失败收尾，避免异步期间被别的状态插入。
            CmdCode ret = StateMutexManager.Instance.CheckState(Owner, req.to_state_type_, false);
            if (ret != CmdCode.Succeed)
            {
                enter_type = EnterStateType.kFail;
            }
        }
        // 正常进入失败时改按失败收尾，让回调拿到明确的拒绝码而不是永久挂起。
        CmdCode result = CmdCode.SkillStateBlocked;
        if (enter_type == EnterStateType.kNormal)
        {
            result = CmdCode.Succeed;
        }
        // callback 是本次切换的最终结果回调；先取出来再摘请求，因为摘除会把请求上的回调清空。
        Action<CmdCode> callback = req.call_back_func_;
        // 先摘掉请求再回调：回调里可能立刻发起新的切换请求，避免重入时看到旧记录。
        DetachRequest(req.serial_id_);
        try
        {
            if (callback != null)
            {
                callback(result);
            }
        }
        finally
        {
            // 事件引用必须在回调之后释放，因为回调里可能仍在派发这个池化事件；
            // 但回调抛出异常时同样要归还引用，否则池化事件会被这条异常路径永久占住。
            // 这里只做释放，不捕获也不包装异常，业务异常继续向上抛。
            ReleaseHeldEvent(req);
        }
    }
    #endregion
}
