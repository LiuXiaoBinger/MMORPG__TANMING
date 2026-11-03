using MMoRpgCommon.SceneEntity;

/// <summary>
/// 管理一个实体当前执行的动作状态，负责注册、切换、逐帧更新和帧末播放动画。
/// 按枚举切换时查询许可表；直接传入状态实例的重载由调用方先判断许可。
/// 当前状态标记完成后，更新流程把它切回默认状态。
/// </summary>
public sealed class MStateMachine : EntityComponentBase
{

    #region 字段与属性
    /// <summary>动作状态容器，按 StateDefine 和类型双向索引已注册状态。</summary>
    private readonly MActionStateContainer _states = new MActionStateContainer();

    /// <summary>当前激活状态。</summary>
    private IMStateTransform _current;

    /// <summary>默认状态（通常是 Idle），当前动作结束后自动回退到这里。</summary>
    private IMStateTransform _default;

    /// <summary>上一个激活状态；当前 StateChanged 派发代码被注释掉，不会发送通知。</summary>
    private IMStateTransform _previous;

    /// <summary>是否有待刷新的状态动画；LateUpdate 成功播放后清除。</summary>
    private bool _isTriggered;

    /// <summary>上一次播放的动画状态名，供 <see cref="IMStateTransform.PlayAnim"/> 做去重切换。</summary>
    private string _preState;

    /// <summary>是否已经注册过默认状态列表，避免对象池复用时重复注册。</summary>
    private bool _registered;

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Sync"/>。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Sync; } }

    /// <summary>当前状态枚举，无当前状态时返回默认 kStateDefault。</summary>
    public StateDefine State {
        get
        {
            if (_current == null) 
                return StateDefine.kStateDefault;
            return _current.SelfState;
        } }

    /// <summary>当前激活状态接口。</summary>
    public IMStateTransform CurrentState { get { return _current; } }

    /// <summary>上一个激活状态接口。</summary>
    public IMStateTransform PreviousState { get { return _previous; } }

    /// <summary>默认状态接口。</summary>
    public IMStateTransform DefaultState { get { return _default; } }

    /// <summary>动作状态容器，外部可按类型或 StateDefine 查询状态。</summary>
    public MActionStateContainer States { get { return _states; } }
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>构造状态机组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public MStateMachine(ROObject owner) : base(owner) { }

    /// <summary>帧更新：把毫秒转换为秒后驱动当前状态。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public override void Update(int deltaMilliseconds)
    {
        UpdateSeconds(deltaMilliseconds * 0.001f);
    }

    /// <summary>
    /// 每帧更新当前状态；状态标记为完成后自动切换到默认状态。
    /// </summary>
    /// <param name="fDeltaT">本帧经过的秒数。</param>
    public override void Update(float fDeltaT)
    {
        _current.StateUpdate(fDeltaT);

        if (_current.IsFinished)
        {
            TransferToState(_default);
        }
    }

    /// <summary>
    /// 帧末动画刷新：仅在本帧触发过状态切换且模型 Animator 可用时，
    /// 调用当前状态的 PlayAnim 播放动画，并把已播放状态名传回状态做下次去重。
    /// </summary>
    /// <param name="deltaSeconds">距上一帧的秒数（本状态机未使用）。</param>
    public override void LateUpdate(float deltaSeconds)
    {
        base.LateUpdate(deltaSeconds);
        // 本帧发生过状态切换且当前状态对象存在时，才进入动画刷新。
        if (_isTriggered && _current != null)
        {
            // 实体模型与 Animator 均可用时，调用当前状态播放动画并回传状态名去重。
            if (Entity != null && Entity.Model != null && Entity.Model.Ator != null)
            {
                _preState = _current.PlayAnim(_preState);
                _isTriggered = false;
            }
        }
    }

    /// <summary>
    /// 状态机初始化：首次进入时按实体类型注册全部可用动作状态，
    /// 然后把默认状态设为当前状态，并标记需要播放动画。
    /// </summary>
    public override void Initialize()
    {
        if (!_registered)
        {
            RegisterDefaultStates();
            _registered = true;
        }
        SetDefaultState(_states.Get(StateDefine.kStateDefault));
        MarkTriggered();
    }

    /// <summary>
    /// 按实体类型注册默认动作状态列表。
    /// 所有实体先注册默认状态和已有技能状态；
    /// 采集物、陷阱、投射物、Dummy 和鹰到此结束，NPC 另加特殊状态；
    /// 玩家类再额外注册攀爬、采集、钓鱼、跳舞等生活动作。
    /// </summary>
    private void RegisterDefaultStates()
    {
        // 当前状态机的宿主实体。
        MEntity entity = Entity;
        if (entity == null) return;
        // 所有实体都必须有默认 Idle 状态
        RegisterState(typeof(MDefaultActionComponent));
        // Skill 是实体快捷组件也是动作状态，由状态容器持有并在状态机销毁时唯一回收。
        RegisterExistingState(entity.Skill);
        // 采集物、陷阱、投射物、Dummy、鹰保留上面已注册的默认状态和技能状态
        // 集合类与陷阱类实体不注册普通动作。
        bool isStaticTriggerEntity =
            (entity.EntityType & EntityType.Collection) != EntityType.None ||
            (entity.EntityType & EntityType.Trap) != EntityType.None;
        // 投射物、傀儡与鹰属于无普通动作的特殊实体。
        bool isNoActionEntity =
            (entity.EntityType & EntityType.Projectile) != EntityType.None ||
            (entity.EntityType & EntityType.Dummy) != EntityType.None ||
            (entity.EntityType & EntityType.Eagle) != EntityType.None;
        if (isStaticTriggerEntity || isNoActionEntity)
        {
            return;
        }
        if ((entity.EntityType & EntityType.Npc) != EntityType.None)
        {
            RegisterState(typeof(MSpecialComponent));
            return;
        }
        // 普通战斗实体注册恐惧逃跑、吟唱、特殊、受击、死亡、眩晕、出生
        RegisterState(typeof(MFearRunComponent));
        RegisterState(typeof(MSingingComponent));
        RegisterState(typeof(MSpecialComponent));
        RegisterState(typeof(MBeHitComponent));
        RegisterState(typeof(MDeadComponent));
        RegisterState(typeof(MStunComponent));
        RegisterState(typeof(MBornComponent));
        // 怪物、精英、Boss、雇佣兵、宠物不再注册生活动作
        // 怪物与精英类实体不注册生活动作。
        bool isMonsterLikeEntity =
            (entity.EntityType & EntityType.Monster) != EntityType.None ||
            (entity.EntityType & EntityType.Elite) != EntityType.None;
        // Boss、雇佣兵与宠物同样不注册生活动作。
        bool isCombatPetLikeEntity =
            (entity.EntityType & EntityType.Boss) != EntityType.None ||
            (entity.EntityType & EntityType.Mercenary) != EntityType.None ||
            (entity.EntityType & EntityType.Pet) != EntityType.None;
        if (isMonsterLikeEntity || isCombatPetLikeEntity)
        {
            return;
        }
        // 玩家类额外注册攀爬、采集、钓鱼、跳舞
        RegisterState(typeof(MClimbComponent));
        RegisterState(typeof(MCollectComponent));
        RegisterState(typeof(MFishingComponent));
        RegisterState(typeof(MDanceComponent));
    }

    /// <summary>从动作状态池获取一个状态并完成初始化，注册失败时立即归还。</summary>
    /// <param name="stateType">动作状态组件类型。</param>
    private void RegisterState(System.Type stateType)
    {
        // 从状态专用池借出的目标类型实例。
        MActionComponent state = MComponentMgr.Instance.AcquireActionState(stateType, Entity);
        if (state == null) return;
        state.Initialize();
        state.Attached();
        if (!_states.Register(state))
        {
            // 容器注册失败（重复状态等），立即归还动作池，避免泄漏
            MComponentMgr.Instance.ReleaseActionState(state);
        }
    }

    /// <summary>注册一个外部已创建的动作状态（当前用于 Skill 快捷组件）。</summary>
    /// <param name="state">外部动作状态实例。</param>
    private void RegisterExistingState(MActionComponent state)
    {
        if (state == null)
        {
            return;
        }
        if (!_states.Register(state))
        {
            // 注册失败时立即归还动作池，并清空快捷引用，避免无所有者实例泄漏。
            MComponentMgr.Instance.ReleaseActionState(state);
            if (Entity != null && Entity.Skill == state)
            {
                Entity.SetSkillComponent(null);
            }
        }
    }

    /// <summary>状态事件由各动作组件订阅，状态机本身不增加订阅。</summary>
    protected override void EventSubscribe()
    {
       
    }

    /// <summary>状态机没有独立事件订阅，无需额外解绑。</summary>
    protected override void EventUnsubscribe()
    {
        
    }

    /// <summary>
    /// 状态机销毁：停止当前状态，清空容器并释放全部动作状态，
    /// 重置内部引用和标记，等待对象池回收。
    /// </summary>
    public override void Shutdown()
    {
        if (_current != null) _current.Stop(false);
        // 容器 Clear 会把所有动作状态（含 Skill）归还动作池
        _states.Clear(Entity); 
        _current = null; 
        _default = null;
        _previous = null;
        _isTriggered = false; 
        _preState = null; 
        _registered = false;
    }
    #endregion

    /// <summary>动作状态内部统一使用秒，实体入口仍使用毫秒。</summary>
    /// <param name="deltaSeconds">距上一帧的秒数。</param>
    public void UpdateSeconds(float deltaSeconds)
    {
        if (_current == null) return;
        _current.StateUpdate(deltaSeconds);
        // 当前状态已完成且不是默认状态时，自动切回默认状态
        if (_current.IsFinished && _current != _default) TransferToState(_default);
    }

    #region 事件回调
    /// <summary>从宿主实体卸载时清空实体对状态机的快捷引用。</summary>
    public override void OnDetachFromHost()
    {
        // 当前状态机的宿主实体。
        MEntity entity = Entity;
        if (entity != null) entity.SetMachineComponent(null);
        base.OnDetachFromHost();
    }

    /// <summary>挂载到新宿主实体时设置实体对状态机的快捷引用。</summary>
    /// <param name="hostObj">新挂载的宿主对象。</param>
    public override void OnAttachToHost(ROObject hostObj)
    {
        // 新宿主，用于设置实体上的 Machine 快捷引用。
        MEntity entity = hostObj as MEntity;
        if (entity != null) entity.SetMachineComponent(this);
        base.OnAttachToHost(hostObj);
    }
    #endregion

    #region 查询与判定
    /// <summary>查询当前状态是否允许释放技能：可打断状态或允许转到吟唱状态时返回 true。</summary>
    /// <returns>允许释放技能返回 true。</returns>
    public bool CanSkill()
    {
        if (_current == null) return false;
        return _current.IsActiveInterrupt || CanTransfer(StateDefine.kStateSinging);
    }

    /// <summary>查询当前状态是否允许切换到目标状态。</summary>
    /// <param name="nextState">目标状态枚举。</param>
    /// <returns>允许切换返回 true。</returns>
    public bool CanTransfer(StateDefine nextState)
    {
        if (_current == null) return false;
        return CommonStateTypeTool.StatePermitted(_current.SelfState, nextState);
    }
    #endregion

    #region 核心逻辑
    /// <summary>把指定状态设为默认状态，并同时设为当前/上一个状态，立即获取权限。</summary>
    /// <param name="state">默认状态实例，为空时清空全部状态引用。</param>
    public void SetDefaultState(IMStateTransform state)
    {
        if (state == null)
        {
            _default = null; _current = null; _previous = null; return;
        }
        _default = state; 
        _current = state; 
        _previous = state; 
        _current.OnGetPermission();
    }

    /// <summary>按状态枚举切换到对应状态，先查状态实例再做权限校验。</summary>
    /// <param name="state">目标状态枚举。</param>
    /// <returns>切换成功返回 true，状态不存在或不允许切换返回 false。</returns>
    public bool TransferToState(StateDefine state)
    {
        // 状态容器中已登记的目标状态实例。
        MActionComponent next = _states.Get(state);
        if (next == null || !CanTransfer(state))
        {
            // 未注册或不允许的状态请求属于无效输入，统一恢复默认 Idle 语义。
            ForceToDefault();
            return false;
        }
        TransferToState(next); return true;
    }

    /// <summary>
    /// 直接切换到指定实例：停止旧状态，保存前后引用，再让新状态开始。
    /// 此重载不查许可表；StateChanged 通知目前被注释掉，不会派发。
    /// </summary>
    /// <param name="next">目标状态接口，为空直接忽略。</param>
    public void TransferToState(IMStateTransform next)
    {
        if (next == null)
        {
            // 空状态不能成为当前状态；存在默认状态时立即归一到默认动作。
            ForceToDefault();
            return;
        }
        // 为下方暂未启用的状态变更通知保留切换前枚举。
        StateDefine previousState = State;
        // 停止旧状态但不先切默认状态；第二个参数告诉旧状态是否正在切回自身
        if (_current != null) 
            _current.Stop(false, next == _current);
        _previous = _current; 
        _current = next; 
        _current.OnGetPermission(); 
        //MarkTriggered();
        // 状态确实发生变化才派发事件，避免切回自身也触发通知
        /*if (previousState != _current.SelfState && Entity != null)
        {
            // 从状态事件参数池获取切换快照，分发完成后由事件分发器统一归还。
            Entity.DispatchEvent(MEventType.StateChanged,
                EntityStateChangedEventArgs.Create(previousState, _current.SelfState));
        }*/
    }

    /// <summary>强制切回默认状态（通常用于动作被打断、复活等场景）。</summary>
    public void ForceToDefault()
    {
        if (_default != null && _current != _default) TransferToState(_default);
    }

    /// <summary>调用当前状态的 Stop，按该状态的默认规则中断并处理回默认状态。</summary>
    public void Stop() { if (_current != null) _current.Stop(); }

    /// <summary>完成当前状态，通常由状态内部在动画播完后调用。</summary>
    public void Finish() { if (_current != null) _current.Finish(); }

    /// <summary>标记本帧需要在 LateUpdate 播放动画。</summary>
    public void MarkTriggered() { _isTriggered = true; }

    /// <summary>设置实体 Animator 播放速度。</summary>
    /// <param name="speed">动画速度倍率，1 为正常速度。</param>
    public void SetAnimationSpeed(float speed)
    {
        if (Entity != null) Entity.AtorSpeed = speed;
    }
    #endregion
}
