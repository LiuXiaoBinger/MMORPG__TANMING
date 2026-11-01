using System;

/// <summary>
/// 动作状态组件的统一视图，对应参考工程 rogamelibs/entity/action/state_define.h 的 IStateTransform。
/// 状态机只依赖这一层，因此吟唱、默认态、技能动作可以放进同一张状态表。
/// </summary>
public abstract class ActionStateComponent : RoleComponentBase, IROEventListener
{
    /// <summary>创建动作状态组件并绑定角色。</summary>
    /// <param name="owner">状态机所属的在线角色。</param>
    protected ActionStateComponent(OnlineRole owner)
        : base(owner)
    {
    }

    /// <summary>本组件对应的状态机状态。</summary>
    public abstract StateDefine SelfState();

    /// <summary>当前是否已经结束（未处于激活进行中）。</summary>
    public abstract bool IsFinished();

    /// <summary>本次激活对应的事件令牌，用于识别动作由哪个事件触发。</summary>
    public abstract ulong Token();

    /// <summary>判断当前状态是否允许切换到目标状态。</summary>
    /// <param name="state">状态机准备进入的目标状态。</param>
    public abstract bool IsPermitted(StateDefine state);

    /// <summary>判断状态机能否以该事件进入本状态，对应 RO 的 OnGetEvent 校验。</summary>
    /// <param name="args">触发状态切换的事件。</param>
    public abstract bool CanEnter(ROEvent args);

    /// <summary>动作事件入口：请求状态机把当前状态切换到本状态。</summary>
    /// <param name="args">驱动本动作的事件。</param>
    /// <returns>状态机切换成功时返回 true。</returns>
    public abstract bool OnActionEvent(ROEvent args);

    /// <summary>状态机批准进入本状态时回调，对应 RO 的 OnGetPermission。</summary>
    /// <param name="args">触发进入的事件，默认态为空。</param>
    public abstract void OnGetPermission(ROEvent args);

    /// <summary>停止本状态（被打断或主动终止）。</summary>
    /// <param name="next">即将进入的状态。</param>
    /// <param name="args">触发停止的事件，外部直接停止时为空。</param>
    public abstract void Stop(StateDefine next, ROEvent args);

    /// <summary>动作自然执行完成，对应 RO 的 Finish。</summary>
    public abstract void Finish();

    /// <summary>每帧状态更新入口，对应 RO 的 StateUpdate。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    public abstract void StateUpdate(int deltaMilliseconds);

    /// <summary>是否允许被主动打断。</summary>
    /// <param name="state">即将进入的目标状态。</param>
    public abstract bool CanActiveInterrupt(StateDefine state);

    /// <summary>是否允许在不打断本动作的情况下移动。</summary>
    public abstract bool CanActiveMove();
}

/// <summary>
/// 动作状态组件模板基类，对应 action_state_component.h:24-255。
/// 每个具体动作（吟唱、默认态等）既是一个角色组件，又作为状态机中的一个可切换状态。
/// </summary>
/// <typeparam name="T">驱动该动作的事件类型，必须以 ROEvent 为基类（对应 RO 的 static_assert）。</typeparam>
public abstract class ActionStateComponent<T> : ActionStateComponent where T : ROEvent
{
    /// <summary>本组件对应的状态标识，构造时固定。</summary>
    private readonly StateDefine self_state_;

    /// <summary>本次激活对应的事件令牌。</summary>
    private ulong token_;

    /// <summary>是否处于结束（未激活）状态；初始即视为已结束，等待事件激活。</summary>
    private bool is_finished_ = true;

    /// <summary>构造时指定本组件对应的状态标识。</summary>
    /// <param name="owner">状态机所属的在线角色。</param>
    /// <param name="self_state">本组件在状态机中的状态标识。</param>
    protected ActionStateComponent(OnlineRole owner, StateDefine self_state)
        : base(owner)
    {
        self_state_ = self_state;
    }

    /// <summary>本组件对应的状态机状态。</summary>
    public override StateDefine SelfState()
    {
        return self_state_;
    }

    /// <summary>当前是否已经结束（未处于激活进行中）。</summary>
    public override bool IsFinished()
    {
        return is_finished_;
    }

    /// <summary>本次激活对应的事件令牌。</summary>
    public override ulong Token()
    {
        return token_;
    }

    /// <summary>
    /// 判断当前状态是否允许切换到目标状态。
    /// RO 还会查 CommonStateTypeTool 的 16×16 静态许可表；本项目只注册默认态、吟唱态、技能动作态三个状态，
    /// 该表未移植，许可退化为"已结束即放行，未结束看能否主动打断"，
    /// 因此三态之间具体放行还是拒绝，由各状态的 CanActiveInterrupt 与互斥表共同决定，
    /// 只是覆盖这三个状态的最小规则，不等价于 RO 的全量许可表。
    /// </summary>
    /// <param name="state">状态机准备进入的目标状态。</param>
    public override bool IsPermitted(StateDefine state)
    {
        if (IsFinished())
        {
            return true;
        }
        return CanActiveInterrupt(state);
    }

    /// <summary>判断状态机能否以该事件进入本状态，对应 RO 的 OnGetEvent 校验。</summary>
    /// <param name="args">触发状态切换的事件。</param>
    public override bool CanEnter(ROEvent args)
    {
        // typed_args 是按本状态事件类型转换后的事件；转换失败说明这不是驱动本状态的事件。
        T typed_args = args as T;
        if (typed_args == null)
        {
            return false;
        }
        // last_state 记录切换前状态机所处的状态，供 OnGetEvent 判断动作来源；取不到状态机时按默认态。
        StateDefine last_state = StateDefine.kStateDefault;
        // machine 是本角色身上的状态机组件，未注册时为空，此时只能按默认态判断。
        StateMachineComponent machine = GetStateMachine();
        if (machine != null && machine.GetCurrent() != null)
        {
            last_state = machine.GetCurrent().SelfState();
        }
        return OnGetEvent(typed_args, last_state);
    }

    /// <summary>
    /// 动作事件入口，对应 action_state_component.h 的 OnActionEvent。
    /// 先请状态机切换，成功后才记录本次事件令牌。
    /// </summary>
    /// <param name="args">驱动本动作的事件。</param>
    /// <returns>状态机切换成功时返回 true。</returns>
    public override bool OnActionEvent(ROEvent args)
    {
        // typed_args 是按本状态事件类型转换后的事件；转换失败说明本状态不接受该事件。
        T typed_args = args as T;
        if (typed_args == null)
        {
            return false;
        }
        // machine 是本角色身上的状态机组件；没有状态机就无法完成切换，直接拒绝。
        StateMachineComponent machine = GetStateMachine();
        if (machine == null)
        {
            return false;
        }
        if (!machine.TryTransferToState(args, this))
        {
            return false;
        }
        token_ = args.GetToken();
        return true;
    }

    /// <summary>停止本状态；已结束时直接忽略，对应 action_state_component.h 的 Stop。</summary>
    /// <param name="next">即将进入的状态。</param>
    /// <param name="args">触发停止的事件，外部直接停止时为空。</param>
    public override void Stop(StateDefine next, ROEvent args)
    {
        if (is_finished_)
        {
            return;
        }
        is_finished_ = true;
        OnStop(next, args);
        AfterFinish();
    }

    /// <summary>动作自然执行完成；已结束时直接忽略，对应 action_state_component.h 的 Finish。</summary>
    public override void Finish()
    {
        if (is_finished_)
        {
            return;
        }
        is_finished_ = true;
        OnFinished();
        AfterFinish();
    }

    /// <summary>每帧状态更新：先执行 PreUpdate，已结束则不再执行 OnUpdate。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    public override void StateUpdate(int deltaMilliseconds)
    {
        PreUpdate(deltaMilliseconds);
        if (IsFinished())
        {
            return;
        }
        OnUpdate(deltaMilliseconds);
    }

    /// <summary>是否允许被主动打断，默认不允许，子类按需重写。</summary>
    /// <param name="state">即将进入的目标状态。</param>
    public override bool CanActiveInterrupt(StateDefine state)
    {
        return false;
    }

    /// <summary>是否允许在不打断本动作的情况下移动，默认允许。</summary>
    public override bool CanActiveMove()
    {
        return true;
    }

    /// <summary>
    /// 本组件监听的驱动事件类型；返回空表示本状态不由事件驱动（例如默认态）。
    /// 对应 RO 在组件挂载时把 kActionXxx 事件绑定到本状态 OnActionEvent 的做法。
    /// </summary>
    protected virtual ROEventDefine? ActionEventType
    {
        get { return null; }
    }

    /// <summary>组件初始化时把驱动事件登记到角色派发器。</summary>
    public override void Initialize()
    {
        base.Initialize();
        ROEventDefine? event_type = ActionEventType;
        if (event_type.HasValue && Owner != null)
        {
            Owner.GetEventDispatcher().AddEventListener(event_type.Value, this, OnActionEventFromDispatcher);
        }
    }

    /// <summary>派发器回调：转交给本状态的动作事件入口。</summary>
    /// <param name="args">派发器传入的事件。</param>
    private void OnActionEventFromDispatcher(ROEvent args)
    {
        OnActionEvent(args);
    }

    /// <summary>组件释放时先解除驱动事件监听并停止本状态，再交由基类解除角色绑定。</summary>
    public override void Dispose()
    {
        ROEventDefine? event_type = ActionEventType;
        if (event_type.HasValue && Owner != null)
        {
            Owner.GetEventDispatcher().RemoveEventListener(event_type.Value, this);
        }
        Stop(StateDefine.kStateDefault, null);
        base.Dispose();
    }

    /// <summary>取本角色身上的状态机组件；未注册时为空。</summary>
    /// <returns>角色状态机组件，未注册返回空。</returns>
    protected StateMachineComponent GetStateMachine()
    {
        if (Owner == null)
        {
            return null;
        }
        // machine 接收角色身上的状态机组件，用于在移动、受伤等事件里查询与切换状态。
        StateMachineComponent machine;
        if (Owner.TryGetComponent(out machine))
        {
            return machine;
        }
        return null;
    }

    /// <summary>状态机批准进入本状态时回调：标记为进行中并触发 OnStart 钩子。</summary>
    /// <param name="args">触发进入的事件。</param>
    public override void OnGetPermission(ROEvent args)
    {
        is_finished_ = false;
        OnStart(args as T);
    }

    /// <summary>状态机切换前对事件的校验，默认通过，子类按需重写。</summary>
    /// <param name="args">触发状态切换的事件。</param>
    /// <param name="last_state">切换前状态机所处的状态。</param>
    protected virtual bool OnGetEvent(T args, StateDefine last_state)
    {
        return true;
    }

    /// <summary>进入本状态时调用（状态机已批准）。</summary>
    /// <param name="args">触发进入的事件。</param>
    protected virtual void OnStart(T args)
    {
    }

    /// <summary>每帧更新逻辑，仅在进行中时被调用。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    protected virtual void OnUpdate(int deltaMilliseconds)
    {
    }

    /// <summary>每帧前置处理，即使已结束也会调用。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    protected virtual void PreUpdate(int deltaMilliseconds)
    {
    }

    /// <summary>被停止（打断）时调用。</summary>
    /// <param name="next">即将进入的状态。</param>
    /// <param name="args">触发停止的事件。</param>
    protected virtual void OnStop(StateDefine next, ROEvent args)
    {
    }

    /// <summary>自然完成时调用。</summary>
    protected virtual void OnFinished()
    {
    }

    /// <summary>结束（Stop 或 Finish）之后的统一收尾钩子。</summary>
    protected virtual void AfterFinish()
    {
    }
}
