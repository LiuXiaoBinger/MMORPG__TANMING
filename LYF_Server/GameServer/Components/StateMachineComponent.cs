using System.Collections.Generic;

/// <summary>
/// 角色状态机，对应参考工程 rogamelibs/entity/action/state_machine_component.h/.cpp。
/// 只保存"默认态 / 当前态 / 上一个态"三个引用，切换顺序与 RO 完全一致：
/// 先让当前状态 Stop，再切引用，最后让新状态 OnGetPermission。
/// </summary>
public sealed class StateMachineComponent : RoleComponentBase
{

    #region 字段
    /// <summary>状态标识到状态组件的映射，同一状态只能注册一次。</summary>
    private readonly Dictionary<StateDefine, ActionStateComponent> states_ =
        new Dictionary<StateDefine, ActionStateComponent>();

    /// <summary>默认状态，动作自然结束后回到这里。</summary>
    private ActionStateComponent default_;

    /// <summary>当前状态，永远非空（注册默认态之后）。</summary>
    private ActionStateComponent current_;

    /// <summary>上一个状态，仅用于查询，不参与切换逻辑。</summary>
    private ActionStateComponent previous_;
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>创建角色状态机。</summary>
    /// <param name="owner">状态机所属的在线角色。</param>
    public StateMachineComponent(OnlineRole owner)
        : base(owner)
    {
    }

    /// <summary>注册一个状态；状态标识重复时拒绝，对应 RO 的 RegisterState 语义。</summary>
    /// <param name="state">要注册的状态组件。</param>
    /// <returns>注册成功返回 true；重复或为空返回 false。</returns>
    public bool RegisterState(ActionStateComponent state)
    {
        if (state == null)
        {
            return false;
        }
        // state_type 是本状态组件声明的状态标识，用于查重与登记，不依赖调用方传值。
        StateDefine state_type = state.SelfState();
        if (states_.ContainsKey(state_type))
        {
            LogMsg.Info("状态机重复注册状态: " + state_type, LogMsgType.Warn);
            return false;
        }
        states_.Add(state_type, state);
        return true;
    }

    /// <summary>驱动当前状态推进，并在自然结束后回到默认态。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        if (current_ == null)
        {
            return;
        }
        current_.StateUpdate(deltaMilliseconds);
        if (!current_.IsFinished())
        {
            return;
        }
        TransferToState(null, default_);
    }

    /// <summary>释放时停止当前动作并清空状态表。</summary>
    public override void Dispose()
    {
        if (current_ != null)
        {
            current_.Stop(StateDefine.kStateDefault, null);
        }
        states_.Clear();
        default_ = null;
        current_ = null;
        previous_ = null;
        base.Dispose();
    }
    #endregion

    /// <summary>
    /// 主动移动请求入口，对应 action_state_component.h 的 Move：
    /// 允许带动作移动就直接放行，否则在可打断时先停止当前动作。
    /// </summary>
    /// <returns>允许本次移动返回 true。</returns>
    public bool OnMove()
    {
        if (current_ == null)
        {
            return true;
        }
        if (current_.CanActiveMove())
        {
            return true;
        }
        if (current_.CanActiveInterrupt(StateDefine.kStateDefault))
        {
            current_.Stop(StateDefine.kStateDefault, null);
            return true;
        }
        return false;
    }

    #region 查询与判定
    /// <summary>按状态标识取状态组件。</summary>
    /// <param name="state">状态标识。</param>
    /// <returns>已注册的状态组件，未注册返回空。</returns>
    public ActionStateComponent GetState(StateDefine state)
    {
        // value 是命中时的状态组件；未注册保持为空，由调用方决定如何处理。
        ActionStateComponent value;
        states_.TryGetValue(state, out value);
        return value;
    }

    /// <summary>默认状态组件。</summary>
    public ActionStateComponent GetDefault()
    {
        return default_;
    }

    /// <summary>当前状态组件。</summary>
    public ActionStateComponent GetCurrent()
    {
        return current_;
    }

    /// <summary>上一个状态组件。</summary>
    public ActionStateComponent GetPrevious()
    {
        return previous_;
    }

    /// <summary>当前状态的状态标识；状态机未初始化时返回默认态。</summary>
    public StateDefine GetCurrentState()
    {
        // 未初始化时按默认态回答，避免调用方拿到非法值。
        if (current_ == null)
        {
            return StateDefine.kStateDefault;
        }
        return current_.SelfState();
    }

    /// <summary>
    /// 尝试切换到目标状态：先做许可判定与事件校验，通过后才真正切换，
    /// 对应 state_machine_component.cpp 的 TryTransferToState。
    /// </summary>
    /// <param name="args">触发切换的事件。</param>
    /// <param name="next">目标状态组件。</param>
    /// <returns>切换成功返回 true。</returns>
    public bool TryTransferToState(ROEvent args, ActionStateComponent next)
    {
        if (next == null)
        {
            return false;
        }
        if (!CanAction(next.SelfState()))
        {
            return false;
        }
        if (!next.CanEnter(args))
        {
            return false;
        }
        TransferToState(args, next);
        return true;
    }

    /// <summary>
    /// 判断当前状态是否允许进入目标状态，对应 state_machine_component.cpp 的 CanAction。
    /// </summary>
    /// <param name="state">目标状态标识。</param>
    public bool CanAction(StateDefine state)
    {
        if (current_ == null)
        {
            return true;
        }
        return current_.IsPermitted(state);
    }
    #endregion

    #region 核心逻辑
    /// <summary>当前状态的事件令牌，对应 RO 的 ActionToken。</summary>
    public ulong ActionToken()
    {
        // 没有当前状态时令牌为 0，表示"无动作"。
        if (current_ == null)
        {
            return 0UL;
        }
        return current_.Token();
    }

    /// <summary>
    /// 设定默认状态并立即把当前态与上一个态都指向它，
    /// 对应 state_machine_component.cpp 的 SetDefaultState。
    /// </summary>
    /// <param name="def">默认状态组件，动作结束后回到这里。</param>
    public void SetDefaultState(ActionStateComponent def)
    {
        if (def == null)
        {
            return;
        }
        default_ = def;
        current_ = def;
        previous_ = def;
        def.OnGetPermission(null);
    }

    /// <summary>
    /// 强制回到默认态，对应 state_machine_component.cpp 的 ForceToDefaultState。
    /// </summary>
    /// <param name="ignore_death">为 true 时即使当前是死亡态也强制复位。</param>
    public void ForceToDefaultState(bool ignore_death)
    {
        if (default_ == null || current_ == default_)
        {
            return;
        }
        if (!ignore_death && current_.SelfState() == StateDefine.kStateDead)
        {
            return;
        }
        TransferToState(null, default_);
    }

    /// <summary>
    /// 真正执行切换：当前态 Stop → 换引用 → 新状态 OnGetPermission，
    /// 顺序与 state_machine_component.cpp:72-83 一致。
    /// </summary>
    /// <param name="args">触发切换的事件，外部强制切换时为空。</param>
    /// <param name="next">目标状态组件。</param>
    public void TransferToState(ROEvent args, ActionStateComponent next)
    {
        if (next == null)
        {
            return;
        }
        if (current_ != null)
        {
            current_.Stop(next.SelfState(), args);
        }
        previous_ = current_;
        current_ = next;
        current_.OnGetPermission(args);
    }
    #endregion
}
