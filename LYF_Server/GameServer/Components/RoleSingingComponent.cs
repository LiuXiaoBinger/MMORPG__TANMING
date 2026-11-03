using System.Collections.Generic;

/// <summary>
/// 角色吟唱组件，对应参考工程 rogamelibs/entity/action/ExtendAction/role_singing_component.cpp。
/// 在吟唱状态之上增加"客户端施法请求入口 + 请求队列 + 每帧后置驱动"三件事：
/// 能立即吟唱就直接派发吟唱事件，不能立即吟唱就先入队，等后置帧再取队首重试。
/// 本项目还额外承担"请求终态回包"：被丢弃、超时或吟唱被打断的请求都必须回明确结果，
/// 否则客户端会一直等待一个永远不会到来的 SkillCastRet。
/// </summary>
public sealed class RoleSingingComponent : SingingComponent
{
    /// <summary>请求队列上限，对应 role_singing_component.cpp:33 的 REQUEST_LIST_SIZE。</summary>
    private const int REQUEST_LIST_SIZE = 10;

    /// <summary>队列中请求的超时时间，单位毫秒，对应 role_singing_component.cpp:34。</summary>
    private const long REQUEST_LIST_OVER_TIME = 300L;

    /// <summary>等待执行的施法请求，对应 role_singing_component.cpp 的 SkillRequest。</summary>
    private struct SkillRequest
    {
        /// <summary>技能主表 ID。</summary>
        public int skill_id_;

        /// <summary>技能目标实体 UID。</summary>
        public ulong target_;

        /// <summary>施法点世界坐标。</summary>
        public ROVector3 cast_point_;

        /// <summary>客户端本次施法标识。</summary>
        public ulong skill_uuid_;

        /// <summary>请求进入队列时的服务器 UTC 毫秒，用于超时判定。</summary>
        public long time_stamp_;
    }

    /// <summary>等待执行的施法请求队列，先进先出。</summary>
    private readonly Queue<SkillRequest> request_list_ = new Queue<SkillRequest>();

    #region 构造与初始化
    /// <summary>创建施法请求组件并绑定在线角色。</summary>
    /// <param name="owner">持有角色快照、技能组件与事件派发器的在线角色。</param>
    public RoleSingingComponent(OnlineRole owner)
        : base(owner)
    {
    }

    /// <summary>组件初始化时向角色派发器登记客户端施法事件监听。</summary>
    public override void Initialize()
    {
        base.Initialize();
        Owner.GetEventDispatcher().AddEventListener(
            ROEventDefine.kActionClienFireSkill, this, OnClientFireSkillRequest);
    }

    /// <summary>
    /// 每帧后置更新：清理超时请求并尝试执行队首，对应 role_singing_component.cpp:205-258。
    /// 必须在角色 Update 之后调用，保证同帧内技能组件已完成上一动作的推进。
    /// </summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    public override void LateUpdate(int deltaMilliseconds)
    {
        if (Owner == null)
        {
            return;
        }
        ClearOverTimeRequest(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if (request_list_.Count == 0)
        {
            return;
        }
        if (!CanStartSingingNow())
        {
            return;
        }
        // head 是本帧出队并立即执行的请求，出队后不再回到队列。
        SkillRequest head = request_list_.Dequeue();
        DispatchSinging(head.skill_id_, head.target_, head.cast_point_, head.skill_uuid_);
    }

    /// <summary>组件释放时解除监听、停止吟唱并清空请求队列。</summary>
    public override void Dispose()
    {
        if (Owner != null)
        {
            Owner.GetEventDispatcher().RemoveEventListener(
                ROEventDefine.kActionClienFireSkill, this);
        }
        // 销毁路径不再逐个回包：会话可能已关闭，这里只保证队列与状态资源被释放。
        request_list_.Clear();
        base.Dispose();
    }
    #endregion

    #region 事件回调
    /// <summary>
    /// 处理客户端施法请求，对应 role_singing_component.cpp:65-161。
    /// 队列为空且当前可立即施法时直接派发吟唱事件，否则按 RO 规则入队等待。
    /// 本项目未移植技能替换（GetReplaceSkillCoreOnClientRequest）与连招表校验，
    /// 这部分校验由技能组件在入口与技能态进入前用同一份非提交校验完成。
    /// </summary>
    /// <param name="args">派发器传入的客户端施法事件。</param>
    private void OnClientFireSkillRequest(ROEvent args)
    {
        // 事件类型由监听表保证，这里只做载荷类型确认。
        ROClientFireSkillArgs action = args as ROClientFireSkillArgs;
        if (action == null || Owner == null)
        {
            return;
        }
        // skill_component 用于后续非提交校验；取不到说明角色未装配技能组件，本次请求直接丢弃。
        RoleSkillComponent skill_component;
        if (!Owner.TryGetComponent(out skill_component))
        {
            return;
        }

        // 滑步没有吟唱阶段，直接交给技能动作事件校验并接管，不能排队等待原技能结束。
        cfg.SkillInfo definition = LubanMgr.Instance.GetSkillInfoById(action.skill_id_);
        if (definition != null && definition.Type == 3)
        {
            // 池化攻击事件仍走 ROSkillComponent.OnGetEvent 的完整费用、冷却和取消校验。
            ROAttackActionArgs attack = ROEventPool<ROAttackActionArgs>.Instance.Get();
            try
            {
                attack.skill_id_ = action.skill_id_;
                attack.skill_uuid_ = action.skill_uuid_;
                attack.target_ = action.target_;
                attack.cast_point_ = action.cast_point_;
                Owner.GetEventDispatcher().DispatcherEvent(attack);
            }
            finally { attack.Release(); }
            return;
        }

        // 队列为空且当前动作允许立刻吟唱时，直接派发吟唱事件，不经过队列。
        if (request_list_.Count == 0 && CanStartSingingNow())
        {
            DispatchSinging(action.skill_id_, action.target_, action.cast_point_, action.skill_uuid_);
            return;
        }

        // 当前不能立即执行：组装缓存请求并压入队列，等待后置帧驱动。
        SkillRequest request;
        request.skill_id_ = action.skill_id_;
        request.target_ = action.target_;
        request.cast_point_ = action.cast_point_;
        request.skill_uuid_ = action.skill_uuid_;
        request.time_stamp_ = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        PushToRequestList(request);
    }

    /// <summary>
    /// 吟唱事件未被状态机接受时的终态回包：拒绝可能来自状态许可或事件校验，
    /// 两种情况下请求都不会进入读条，必须回明确结果而不是静默丢弃。
    /// </summary>
    /// <param name="args">派发器传入的吟唱事件。</param>
    /// <returns>状态机切换成功返回 true。</returns>
    public override bool OnActionEvent(ROEvent args)
    {
        // accepted 是动作态组件对本事件的受理结果，为真时本组件不再重复处理。
        bool accepted = base.OnActionEvent(args);
        if (accepted)
        {
            return true;
        }
        // singing_args 是把通用动作事件转成吟唱事件的结果，为空说明事件类型不匹配、直接跳过。
        ROSingingArgs singing_args = args as ROSingingArgs;
        if (singing_args == null)
        {
            return false;
        }
        ReplyTerminal(singing_args.skill_id_, singing_args.skill_uuid_, CmdCode.SkillStateBlocked);
        return false;
    }

    /// <summary>
    /// 吟唱被打断（移动、死亡、离线、销毁）时给出明确终态：
    /// 按 RO 的 SkillEnd 语义用现有 SkillCastRet 收尾，不发 Start、不扣 MP/CD，
    /// 并原样回带被打断请求的技能 ID 与施法标识。
    /// </summary>
    /// <param name="next">即将进入的状态。</param>
    /// <param name="args">触发停止的事件。</param>
    protected override void OnStop(StateDefine next, ROEvent args)
    {
        // 只有在真正读过条时才算"被打断"；零时长技能在 OnStart 里直接 Finish，不会走到这里。
        bool interrupted = start_singing_;
        // skill_id 是本次被打断请求的技能主表 ID，先取成本地副本供终态回包使用。
        int skill_id = skill_id_;
        // skill_uuid 是同一请求的客户端施法标识，用于让终态与请求一一配对。
        ulong skill_uuid = skill_uuid_;
        base.OnStop(next, args);
        if (!interrupted)
        {
            return;
        }
        ReplyTerminal(skill_id, skill_uuid, CmdCode.SkillStateBlocked);
    }
    #endregion

    /// <summary>
    /// 把请求压入队列，对应 role_singing_component.cpp:164-184。
    /// 入队前先清理超时项；队列已满时丢弃本次请求，并回明确终态。
    /// </summary>
    /// <param name="request">待入队的施法请求。</param>
    private void PushToRequestList(SkillRequest request)
    {
        ClearOverTimeRequest(request.time_stamp_);
        if (request_list_.Count < REQUEST_LIST_SIZE)
        {
            request_list_.Enqueue(request);
            return;
        }
        LogMsg.Info("施法请求队列已满，丢弃请求，角色=" + Owner.GetID() +
            "，技能=" + request.skill_id_, LogMsgType.Warn);
        // 被丢弃的请求不会再进入吟唱，必须回终态，否则客户端一直等待结果。
        ReplyTerminal(request.skill_id_, request.skill_uuid_, CmdCode.SkillStateBlocked);
    }

    /// <summary>当前是否可以直接进入吟唱：吟唱状态已结束，或允许被本次施法打断。</summary>
    /// <returns>可以立即吟唱返回 true。</returns>
    private bool CanStartSingingNow()
    {
        // machine 是本角色的状态机；空表示状态未装配，按"当前不能立即吟唱"处理。
        StateMachineComponent machine = GetStateMachine();
        if (machine == null)
        {
            return false;
        }
        return machine.CanAction(StateDefine.kStateSinging);
    }

    #region 核心逻辑
    /// <summary>
    /// 从队首开始清理超时请求，对应 role_singing_component.cpp:187-202。
    /// 队列按时间先后排列，遇到首个未超时请求即停止；超时请求逐个回明确终态。
    /// </summary>
    /// <param name="now_time">当前服务器 UTC 毫秒。</param>
    private void ClearOverTimeRequest(long now_time)
    {
        while (request_list_.Count > 0)
        {
            // head 是队首请求的只读副本，用于比较入队时间戳并决定是否清理。
            SkillRequest head = request_list_.Peek();
            if (now_time - head.time_stamp_ < REQUEST_LIST_OVER_TIME)
            {
                break;
            }
            request_list_.Dequeue();
            LogMsg.Info("施法请求超时，丢弃请求，角色=" + Owner.GetID() +
                "，技能=" + head.skill_id_, LogMsgType.Warn);
            // 超时请求同样不会再进入吟唱，回终态避免客户端悬挂。
            ReplyTerminal(head.skill_id_, head.skill_uuid_, CmdCode.SkillStateBlocked);
        }
    }

    /// <summary>
    /// 从对象池取吟唱事件并派发，派发结束后立即释放调用方引用。
    /// 事件字段在派发期间被吟唱状态同步读取，因此不需要跨帧持有。
    /// </summary>
    /// <param name="skill_id">技能主表 ID。</param>
    /// <param name="target">技能目标实体 UID。</param>
    /// <param name="cast_point">施法点世界坐标。</param>
    /// <param name="skill_uuid">客户端本次施法标识。</param>
    private void DispatchSinging(int skill_id, ulong target, ROVector3 cast_point, ulong skill_uuid)
    {
        // singing_args 是池取的吟唱事件：字段填满即派发，finally 里立刻归还调用方这一份引用。
        ROSingingArgs singing_args = ROEventPool<ROSingingArgs>.Instance.Get();
        try
        {
            singing_args.skill_id_ = skill_id;
            singing_args.target_ = target;
            singing_args.cast_point_ = cast_point;
            singing_args.skill_uuid_ = skill_uuid;
            Owner.GetEventDispatcher().DispatcherEvent(singing_args);
        }
        finally
        {
            singing_args.Release();
        }
    }

    /// <summary>按技能标识与明确结果码回一次终态；角色已不在线时只记录，不再发包。</summary>
    /// <param name="skill_id">被打断或丢弃请求的技能主表 ID。</param>
    /// <param name="skill_uuid">被打断或丢弃请求的客户端施法标识。</param>
    /// <param name="code">具体结果码。</param>
    private void ReplyTerminal(int skill_id, ulong skill_uuid, CmdCode code)
    {
        if (Owner == null)
        {
            return;
        }
        // 已下线的角色没有可用会话，这里只保证请求被终结，不再尝试发包。
        if (Owner.State != OnlineRoleState.Online)
        {
            return;
        }
        SkillCastReply.Send(Owner, SkillCastReply.Build(Owner, skill_id, skill_uuid, code));
    }
    #endregion
}
