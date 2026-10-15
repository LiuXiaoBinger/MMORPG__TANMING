using cfg;
using MMoRpgCommon.SceneEntity;
using UnityEngine;
using UVEC3 = UnityEngine.Vector3;
/// <summary>
/// 实体移动组件，独立于普通组件容器并负责默认移动动画事件。
/// 通过目标变换快照与插值时长，每帧把实体变换平滑过渡到目标点，
/// 并在移动开始/结束/变换变化时派发对应事件。
/// </summary>
public sealed class MMoveComponent : EntityComponentBase
{
    /// <summary>单次起跳采用的初始向上速度，单位为米/秒。</summary>
    private const float JumpUpwardSpeed = 10f;

    /// <summary>角色上升阶段的重力加速度，单位为米/秒平方。</summary>
    private const float RisingGravity = 28f;

    /// <summary>角色下落阶段的重力加速度，单位为米/秒平方。</summary>
    private const float FallingGravity = 38f;

    /// <summary>允许的最大下落速度绝对值，单位为米/秒。</summary>
    private const float MaximumFallingSpeed = 45f;

    /// <summary>接地时保留的向下速度，防止 CharacterController 在坡面悬空。</summary>
    private const float GroundStickSpeed = -2f;

    /// <summary>起跳后忽略旧接地结果的时间，单位为秒。</summary>
    private const float LandingProtectionSeconds = 0.1f;

    /// <summary>一轮滞空允许的最大跳跃次数。</summary>
    private const int MaximumJumpCount = 3;

    /// <summary>本次移动的目标位置与朝向快照。</summary>
    private readonly EntityTransformSnapshot _target = new EntityTransformSnapshot();

    /// <summary>本次移动插值总时长（毫秒），最小为 1。</summary>
    private int _interpolationMilliseconds = 150;

    /// <summary>本次移动已经经过的毫秒数，用于计算插值系数。</summary>
    private int _elapsedMilliseconds;

    /// <summary>组件类型标识，固定为 <see cref="EntityComponentType.Move"/>。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Move; } }

    /// <summary>当前是否处于水平移动状态</summary>
    public bool IsMoving { get { return _isMoving; }
        set
        {
            _isMoving = value;
        } }

    /// <summary>兼容旧调用方的跳跃开关；设为 true 触发一段跳，设为 false 结束当前跳跃。</summary>
    public bool IsJump
    {
        get { return _isjump; }
        set
        {
            if (value)
            {
                if (_isjump)
                {
                    return;
                }
                BeginJump(1);
                return;
            }
            EndJump();
        }
    }

    /// <summary>当前是否处于跳跃阶段，变化时通知跳跃表现组件切换动画。</summary>
    private bool _isjump = false;

    /// <summary>当前空中跳跃次数，落地后归零，合法范围为 0 到 3。</summary>
    public int JumpCount { get; private set; }

    /// <summary>当前垂直速度，正值上升、负值下落，单位为米/秒。</summary>
    private float _verticalVelocity;

    /// <summary>本轮起跳后的累计滞空时间，单位为秒，用于屏蔽起跳首帧旧接地值。</summary>
    private float _airborneElapsedSeconds;

    /// <summary>是否已经在移动更新中读取过一次 CharacterController 接地结果。</summary>
    private bool _groundedStateInitialized;

    /// <summary>当前是否按住 Shift 使用跑步模式；false 表示使用普通移动速度。</summary>
    private bool _isRunning;

    /// <summary>本地玩家当前是否仍保持非零方向输入；按下方向时设为 true，松开方向时清除。</summary>
    private bool _hasDirectionalInput;

    /// <summary>获取本地玩家当前是否仍保持方向输入，落地表现据此直接选择移动或待机。</summary>
    public bool HasDirectionalInput
    {
        get { return _hasDirectionalInput; }
    }

    /// <summary>获取当前是否处于跑步模式，该状态在跳跃期间仍会保留。</summary>
    public bool IsRunning
    {
        get { return _isRunning; }
    }

    /// <summary>是否需要水平方向的移动 Update（水平移动激活标记）</summary>
    private bool _isMoving = false;//是否需要水平方向的移动update

    /// <summary>最终面朝方向（也用作移动预测/外推的方向）</summary>
    private UVEC3 face = UVEC3.zero;//最终面朝方向

    /// <summary>目标位置（本机移动目标 / 他机服务器下发的目标点）</summary>
    private UVEC3 _targetPos = UVEC3.zero;//目标位置

    /// <summary>开始位置（本次移动或本次同步的起点）</summary>
    private UVEC3 _startPos = UVEC3.zero;//开始位置

    /// <summary>目标方向向量（水平单位化后的移动方向）</summary>
    private UVEC3 _targetDir = UVEC3.zero;//目标方向向量

    /// <summary>上次移动后离目标点的距离（用于判断是否走过头，防止在目标点附近来回抖动）</summary>
    private float _lastDis = 0.0f;//上次移动后离目标点的距离

    /// <summary>影子跟随已经持续的时间（每帧累加 deltaTime，与 delay 比较判断是否到达目标）</summary>
    private float _server_time = 0;//影子跟随已经持续的时间

    /// <summary>RO 影子跟随的同步间隔，单位为秒；无有效耗时时使用 0.01 秒。</summary>
    private float delay = 0.15f;

    /// <summary>服务器目标到达后是否维持移动动画，取自本次同步的移动状态。</summary>
    private bool keep_moving;

    /// <summary>RO 影子跟随允许直接转向的角度阈值，单位为度。</summary>
    private const float MaxRotationAngle = 90f;

    /// <summary>RO 影子跟随忽略的微小朝向差，单位为度。</summary>
    private const float MinRotationAngle = 2f;

    /// <summary>当前同步起点指向目标点的三维单位方向，保留目标高度变化。</summary>
    private UVEC3 _syncToTargetDir;

    /// <summary>当前影子追赶目标的估算速度，单位为米/秒。</summary>
    private float _runSpeed;

    /// <summary>收到同步时当前位置到目标位置的三维距离，单位为米。</summary>
    private float _syncToTargetDist;

    /// <summary>忽略高度后的目标水平朝向，用于 RO 同名平滑转向流程。</summary>
    private Quaternion _toTargetQuat;

    /// <summary>本次同步是否由角色主动移动产生；用于远端移动动画启停。</summary>
    public bool IsMoved { get; private set; }

    /// <summary>预测的位移向量（预留的预测移动字段）</summary>
    private UVEC3 _predictedPos = UVEC3.zero;//预测的向量

    /// <summary>沿 RO 保留的预测持续时间，单位为秒；当前影子追赶不使用该值计算位移。</summary>
    private float _predictedTime = 0;//预测的持续时间

    /// <summary>自上次上报以来累计的逻辑时间，单位为秒；按 RO Time 字段发送后归零。</summary>
    public float CostTime { get; set; }//本次移动总计花费的时间

    /// <summary>插值总时长（毫秒），赋值时自动限制不小于 1。</summary>
    public int InterpolationMilliseconds
    {
        get { return _interpolationMilliseconds; }
        set
        {
            if (value < 1) _interpolationMilliseconds = 1;
            else _interpolationMilliseconds = value;
        }
    }

    /// <summary>移动事件（MEvent_Move）处理器：接收本机摇杆/寻路移动指令</summary>
    private MEventHandler _onMoveEvent;

    /// <summary>停止移动事件（MEvent_StopMove）处理器</summary>
    private MEventHandler _onStopMoveEvent;

    /// <summary>构造移动组件。</summary>
    /// <param name="owner">宿主实体。</param>
    public MMoveComponent(ROObject owner) : base(owner)
    {
        _onMoveEvent = new MEventHandler(onMoveEvent);
        _onStopMoveEvent = new MEventHandler(onStopMoveEvent);
        EventSubscribe();
    }

    /// <summary>每帧推进插值，把实体变换向目标点靠近，到达后停止并派发结束事件。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数，负数按 0 处理。</param>
    public override void Update(int deltaMilliseconds)
    {
        /*if (!IsMoving) return;
        if (deltaMilliseconds < 0) deltaMilliseconds = 0;
        _elapsedMilliseconds += deltaMilliseconds;
        // 计算 0~1 的插值系数，超过 1 时截断为 1 并结束移动
        float factor = (float)_elapsedMilliseconds / _interpolationMilliseconds;
        if (factor >= 1f) { factor = 1f; IsMoving = false; }
        // 对位置和朝向做线性插值
        Entity.Transform.X = Entity.Transform.X + (_target.X - Entity.Transform.X) * factor;
        Entity.Transform.Y = Entity.Transform.Y + (_target.Y - Entity.Transform.Y) * factor;
        Entity.Transform.Z = Entity.Transform.Z + (_target.Z - Entity.Transform.Z) * factor;
        Entity.Transform.Yaw = Entity.Transform.Yaw + (_target.Yaw - Entity.Transform.Yaw) * factor;
        DispatchEvent(MEventType.TransformChanged, null);
        if (!IsMoving) DispatchEvent(MEventType.MoveStopped, null);*/

        if (/*CheckVehicleCtrlMove() ||*/ Entity.IsFake)
        {
            return;
        }

        if (Entity.IsPlayer)
        {
            PlayerUpdate(deltaMilliseconds);
        }
        else
        {
            // 远端与本地消费相同的动画根位移，技能禁止方向输入时仍须推进其自身位移。
            UVEC3 frameMovement = UVEC3.zero;
            if (Entity.Model.RootMotion != null)
            {
                frameMovement += Entity.Model.RootMotion.ConsumeDelta();
            }
            // 实体普通容器已经求值的程序组件；移动组件不持有其生命周期或重复推进它。
            MDisplacementComponent displacementComp = Entity.DisplacementComp;
            if (displacementComp != null)
            {
                frameMovement += displacementComp.ConsumeDelta();
            }
            if (frameMovement != UVEC3.zero)
            {
                // 本帧技能位移只提交一次，不再叠加追赶同一坐标的影子移动。
                Entity.MoveCharacter(frameMovement);
                if (Entity.Model.HasSideCollision && displacementComp != null)
                {
                    displacementComp.StopDisplacement();
                }
                // 技能已改变实体位置，旧影子段只能追剩余差量，不能继续按施法前的方向和路程推进。
                _startPos = Entity.Position;
                _syncToTargetDir = _targetPos - _startPos;
                _syncToTargetDist = _syncToTargetDir.magnitude;
                if (_syncToTargetDist > 0f)
                {
                    _syncToTargetDir /= _syncToTargetDist;
                }
                else
                {
                    // 根位移已抵达停止包的位置时立即收束移动态，技能结束后恢复待机。
                    SetMoving(keep_moving);
                }
                _server_time = 0f;
            }
            else
            {
                LagSyncUpdate(deltaMilliseconds * 0.001f);
            }
        }
    }

    /// <summary>应用 Shift 走跑模式；地面默认动作会重评估动画，空中保留模式供落地恢复。</summary>
    /// <param name="isRunning">true 表示使用跑步速度和跑步动画；false 表示使用普通移动。</param>
    public void SetRunning(bool isRunning)
    {
        if (_isRunning == isRunning)
        {
            return;
        }

        _isRunning = isRunning;
        // 宿主缺失、跳跃中或未处于移动态时不刷新走跑动画；保持原 || 短路顺序。
        if (Entity == null || Entity.IsJump || !Entity.IsMoving)
        {
            return;
        }
        if (Entity.Machine == null)
        {
            return;
        }

        // 当前默认动作负责走跑动画；仅处于默认状态时请求本帧重新评估动画。
        if (Entity.Machine.CurrentState == Entity.Machine.DefaultState)
        {
            Entity.Machine.MarkTriggered();
        }
    }

    /// <summary>
    /// 处理本地玩家按键跳跃：地面一段、空中二段和三段；技能状态即使 MoveCasting 为 true 也禁止主动起跳。
    /// </summary>
    /// <returns>成功开始下一段跳跃时返回 true；未接地或已达到三段时返回 false。</returns>
    public bool TryJump()
    {
        // 宿主或模型缺失时不允许起跳；保持原 || 短路顺序逐组早退。
        if (Entity == null || Entity.Model == null)
        {
            return false;
        }
        // 仅本地玩家可主动起跳；技能状态或不可移动时同样拒绝。
        if (!Entity.IsPlayer ||
            Entity.CurrentState == StateDefine.kStateSkill || !Entity.IsMovable)
        {
            return false;
        }

        // 下一段跳跃编号；地面固定从一段开始，空中在当前次数上递增。
        int nextJumpCount = 1;
        if (_isjump)
        {
            nextJumpCount = JumpCount + 1;
        }
        else if (Entity.Model.HasCharacterController && _groundedStateInitialized &&
            !Entity.Model.IsGrounded)
        {
            return false;
        }

        return BeginJump(nextJumpCount);
    }

    /// <summary>
    /// 通知移动组件开始一段、二段或三段跳；每次调用都会派发一次起跳事件，
    /// 因此二段跳和三段跳即使仍处于空中也会重新播放共用的起跳动画。
    /// </summary>
    /// <param name="jumpCount">本次空中第几次跳，取值 1、2 或 3。</param>
    /// <returns>参数在 1 到 3 范围内并成功触发时返回 true。</returns>
    public bool BeginJump(int jumpCount)
    {
        if (jumpCount < 1 || jumpCount > MaximumJumpCount)
        {
            return false;
        }
        if (_isjump && jumpCount <= JumpCount)
        {
            return false;
        }
        JumpCount = jumpCount;
        _isjump = true;
        _verticalVelocity = JumpUpwardSpeed;
        _airborneElapsedSeconds = 0f;
        DispatchEvent(MEventType.JumpStarted, null);
        return true;
    }

    /// <summary>通知移动组件落地并派发一次跳跃结束事件。</summary>
    public void EndJump()
    {
        if (!_isjump)
        {
            return;
        }
        _isjump = false;
        JumpCount = 0;
        _verticalVelocity = GroundStickSpeed;
        _airborneElapsedSeconds = 0f;
        DispatchEvent(MEventType.JumpEnded, null);
    }

    /// <summary>
    /// 注册本组件关心的全部事件：移动 / 停止移动 / 传送带 / 携带物 / 骑乘 / 换装 / 武器切换等。
    /// </summary>
    protected override void EventSubscribe()
    {
        RegisterEvent(MEventType.MEvent_Move, _onMoveEvent);
        RegisterEvent(MEventType.MEvent_StopMove, _onStopMoveEvent);
        //RegisterEvent(MEventType.MEvent_EnterWall, _onEnterWall);

        //RegisterEvent(MEventType.MEvent_CarryItem, _onCarryItemEvent);
        //RegisterEvent(MEventType.MEvent_RideVehicle, _onPrepairAnimEvent);
        //RegisterEvent(MEventType.MEvent_UnRideVehicle, _onPrepairAnimEvent);
        //RegisterEvent(MEventType.MEvent_OnFashionChange, _onPrepairAnimEvent);
        //RegisterEvent(MEventType.MEvent_ReloadBattleRideAnimation, _onReloadBattleRideAnimation);
        //RegisterEvent(MEventType.MEvent_EntityWeaponTypeChanged, _onPrepairAnimEvent);
    }

    /// <summary>反注册 EventSubscribe 中注册的全部事件。</summary>
    protected override void EventUnsubscribe()
    {
        RemoveEventListener(MEventType.MEvent_Move, _onMoveEvent);
        RemoveEventListener(MEventType.MEvent_StopMove, _onStopMoveEvent);
        //RemoveEventListener(MEventType.MEvent_EnterWall, _onEnterWall);

        //RemoveEventListener(MEventType.MEvent_CarryItem, _onCarryItemEvent);
        //RemoveEventListener(MEventType.MEvent_RideVehicle, _onPrepairAnimEvent);
        //RemoveEventListener(MEventType.MEvent_UnRideVehicle, _onPrepairAnimEvent);
       // RemoveEventListener(MEventType.MEvent_OnFashionChange, _onPrepairAnimEvent);
        //RemoveEventListener(MEventType.MEvent_ReloadBattleRideAnimation, _onReloadBattleRideAnimation);
        //RemoveEventListener(MEventType.MEvent_EntityWeaponTypeChanged, _onPrepairAnimEvent);
    }

    /// <summary>设置新的移动目标并启动插值，首次进入移动状态时派发 MoveStarted 事件。</summary>
    /// <param name="target">目标变换快照，为空直接忽略。</param>
    /// <param name="durationMilliseconds">本次插值时长（毫秒），大于 0 时覆盖默认时长。</param>
    public void ApplyTarget(EntityTransformSnapshot target, int durationMilliseconds)
    {
        if (target == null) return;
        // 复制目标快照，避免外部对象后续修改影响插值过程
        _target.X = target.X; _target.Y = target.Y; _target.Z = target.Z; _target.Yaw = target.Yaw;
        if (durationMilliseconds > 0) InterpolationMilliseconds = durationMilliseconds;
        _elapsedMilliseconds = 0;
        bool wasMoving = IsMoving;
        IsMoving = true;
        // 仅在从静止进入移动时派发开始事件，避免同帧重复触发
        if (!wasMoving) DispatchEvent(MEventType.MoveStarted, null);
    }

    /// <summary>
    /// 模型加载完成（同步或异步）后回调：启用组件，以当前位置初始化起点/终点与朝向，并准备移动动画。
    /// </summary>
    public override void Attached()
    {
        base.Attached();
        Enabled = true;
        _startPos = _targetPos = Entity.Position;
        face = Entity.Model.Forward;
        prepairAnim();
    }

    /// <summary>
    /// 统一准备移动动画入口：按公共载具/坐骑/角色/怪物等分类设置移动、自拍移动动画与脚步音效。
    /// </summary>
    private void prepairAnim()
    {
        MDefaultActionComponent defaultActionComponent = Entity.GetMComponentByT<MDefaultActionComponent>();
        if (Entity.IsRole)
        {
            /*if (((MRole)Entity).AttrRole.IsMale)
            {
                string anim = "Common_M_PaiZhao_Run_01";
                Entity.OverrideAnim(MAnimator.STATE_SELFY_MOVE, anim);
            }
            else
            {
                string anim = "Common_F_PaiZhao_Run_01";
                Entity.OverrideAnim(MAnimator.STATE_SELFY_MOVE, anim);
            }*/
            //todo 这里应该区分男女 暂时不区分
            RoleEntity role = Entity as RoleEntity;
            if (role != null)
            {
                // 读取当前角色的职业编号，用于查询该职业对应的移动动画表现表
                int Jobid = role.BaseInfo.JobId;
                ProfessionInfo professionInfo= LubanMgr.Instance.GetProfessionInfoById(Jobid);
                PresentTable presentTable = LubanMgr.Instance.GetPresentTableById(professionInfo.PresentTableID);
                
                /// <summary>战斗行走动画状态名。</summary>
                string STATE_ZD_MOVE_WALK = presentTable.WalkFAnim;

                /// <summary>战斗奔跑动画状态名。</summary>
                string STATE_ZD_MOVE_RUN = presentTable.RunFAnim;

                /// <summary>非战斗行走动画状态名。</summary>
                string STATE_NONZD_MOVE_WALK = presentTable.WalkAnim;

                /// <summary>非战斗奔跑动画状态名。</summary>
                string STATE_NONZD_MOVE_RUN = presentTable.RunAnim;

                Entity.OverrideAnim(MAnimator.STATE_ZD_MOVE_WALK, STATE_ZD_MOVE_WALK);
                Entity.OverrideAnim(MAnimator.STATE_ZD_MOVE_RUN, STATE_ZD_MOVE_RUN);
                Entity.OverrideAnim(MAnimator.STATE_NONZD_MOVE_WALK, STATE_NONZD_MOVE_WALK);
                Entity.OverrideAnim(MAnimator.STATE_NONZD_MOVE_RUN, STATE_NONZD_MOVE_RUN);
            }
        }
        /* 声音组件尚未接入，脚步音效后续按角色表现配置绑定。 */
    }

    /// <summary>兼容旧控制器只提供移动状态、尚未提供目标点的表现桥接。</summary>
    /// <param name="moving">是否进入移动状态。</param>
    public void SetMovingForPresentation(bool moving)
    {
        // 展示状态入口可能重复收到 Idle；即使运行态已经为 false，也必须清除残留方向标记。
        bool stateChanged = IsMoving != moving;
        IsMoving = moving;
        _elapsedMilliseconds = 0;
        if (moving)
        {
            // 旧角色控制器以展示状态启动移动时，同步补齐方向输入标记，避免默认动作判定为 Idle。
            _hasDirectionalInput = true;
        }
        else
        {
            // 旧角色控制器切回 Idle 时必须清除方向输入，避免下一帧重新选择 Walk/Run。
            _hasDirectionalInput = false;
            _targetDir = UVEC3.zero;
            _predictedPos = UVEC3.zero;
            if (Entity != null && Entity.Machine != null &&
                Entity.CurrentState == StateDefine.kStateDefault)
            {
                // 即使 IsMoving 本来就是 false，也强制默认状态机重新选择 Idle，覆盖 Animator 残留 Walk 的情况。
                Entity.Machine.MarkTriggered();
            }
        }
        if (!stateChanged)
        {
            return;
        }
        if (moving) DispatchEvent(MEventType.MoveStarted, null);
        else DispatchEvent(MEventType.MoveStopped, null);
    }

    /// <summary>按当前输入推进玩家水平移动、跳跃垂直速度与落地检测。</summary>
    /// <param name="deltaTime">本次移动逻辑帧间隔，单位为毫秒；位移积分会将负值按零毫秒处理。</param>
    private void PlayerUpdate(int deltaTime)
    {
#if true
        // 当前逻辑帧秒数；负时间按零处理，避免反向积分移动。
        float deltaSeconds = deltaTime * 0.001f;
        if (deltaSeconds < 0f)
        {
            deltaSeconds = 0f;
        }

        // RO 在方向判定前累计秒数，静止、跳跃和技能位移的末次同步也使用该时间。
        CostTime += deltaSeconds;

        // 当前模型的水平速度；跳跃和水平移动在此合成为一次 CharacterController.Move。
        UVEC3 horizontalVelocity = Entity.Model.lastVelocity;
        // 技能动作按 MoveCasting 决定水平输入；禁止移动的技能即使在空中也不保留水平速度。
        if (!Entity.IsMovable &&
            (Entity.CurrentState == StateDefine.kStateSkill || !Entity.IsJump))
        {
            stopMove();
            horizontalVelocity = UVEC3.zero;
        }
        else if (_isMoving && _targetDir != UVEC3.zero)
        {
            // 当前输入模式对应的水平移动速度，单位为米/秒。
            float speed = GetCurrentMoveSpeed();
            MEntity.Accelerate(Entity, _targetDir, speed);
            // 当前水平速度平方，用于 Shift 松开时立即限制到普通移动上限。
            float horizontalSpeedSquared = Entity.Model.lastVelocity.sqrMagnitude;
            // 当前移动速度上限平方，避免只为比较大小执行平方根计算。
            float maximumSpeedSquared = speed * speed;
            if (horizontalSpeedSquared > maximumSpeedSquared && speed > 0f)
            {
                Entity.Model.lastVelocity = Entity.Model.lastVelocity.normalized * speed;
            }
            MEntity.FaceDirectionSmooth(Entity, Entity.Model.lastVelocity);
            horizontalVelocity = Entity.Model.lastVelocity;
        }
        else
        {
            // 没有方向输入时立即清理旧水平速度，避免松键后继续滑行。
            horizontalVelocity = UVEC3.zero;
            Entity.Model.lastVelocity = UVEC3.zero;
        }

        UpdateVerticalMovement(deltaSeconds);

        // 本帧完整运动位移，水平与垂直只由移动组件合并并提交一次。
        UVEC3 frameMovement = horizontalVelocity * deltaSeconds;
        frameMovement.y = _verticalVelocity * deltaSeconds;
        if (Entity.Model.RootMotion != null)
        {
            // 实际技能动画的水平delta与输入、重力只提交一次碰撞移动；不会放开MoveCasting。
            frameMovement += Entity.Model.RootMotion.ConsumeDelta();
        }
        // RO 程序段与动画根窗口已禁止重叠，输入、根、程序和重力仍仅一次碰撞提交。
        // 从实体读取当前普通组件，允许未装配位移的实体仍执行原输入及动画根移动。
        MDisplacementComponent displacementComp = Entity.DisplacementComp;
        if (displacementComp != null)
        {
            frameMovement += displacementComp.ConsumeDelta();
        }
        if (frameMovement != UVEC3.zero)
        {
            Entity.MoveCharacter(frameMovement);
            if (Entity.Model.HasSideCollision && displacementComp != null)
            {
                displacementComp.StopDisplacement();
            }
        }

        // 首次更新后才把 CharacterController 的接地结果作为后续首跳校验依据。
        if (Entity.Model.HasCharacterController)
        {
            _groundedStateInitialized = true;
        }

        // CharacterController.Move 后才能取得本帧可靠接地值；下降并接地时结束整轮跳跃。
        // 跳跃中且已度过落地保护时间、垂直速度转为下降。
        bool landingTimingReady = _isjump && _airborneElapsedSeconds >= LandingProtectionSeconds &&
            _verticalVelocity <= 0f;
        if (landingTimingReady && Entity.Model.IsGrounded)
        {
            EndJump();
        }
#endif
#if false
        if (!Entity.IsMovable)
        {
            stopMove();
            return;
        }
        CostTime += deltaTime;
        //导航
        if (_targetDir == UVEC3.zero)
        {
            stopMove();
            return;
        }
        
        var speed = Entity.RunSpeed;
        UVEC3 moveDelta = speed * _targetDir * deltaTime;
        UVEC3 disVec = MCommonFunctions.Horizontal(_targetPos - Entity.Position);
        float dis = disVec.magnitude;
        if (dis < speed * deltaTime || dis > _lastDis)
        {
            moveDelta = _targetPos - Entity.Position;
            /*if (Entity.IsFly)
            {
                moveDelta.y = 0;
            }*/
            _targetDir = UVEC3.zero;
        }
        _lastDis = dis;
        
        Entity.AppendMove(moveDelta);
#endif
        
    }

    /// <summary>根据当前跳跃阶段推进垂直速度与重力。</summary>
    /// <param name="deltaSeconds">当前逻辑帧间隔，单位为秒且不小于零。</param>
    private void UpdateVerticalMovement(float deltaSeconds)
    {
        if (_isjump)
        {
            _airborneElapsedSeconds += deltaSeconds;
            // 上升使用较缓重力，下落使用较强重力，保证起跳清晰且落地利落。
            float gravity = FallingGravity;
            if (_verticalVelocity > 0f)
            {
                gravity = RisingGravity;
            }
            _verticalVelocity -= gravity * deltaSeconds;
            if (_verticalVelocity < -MaximumFallingSpeed)
            {
                _verticalVelocity = -MaximumFallingSpeed;
            }
            return;
        }

        // 非跳跃状态仍施加轻微向下速度，使 CharacterController 持续贴住地面。
        _verticalVelocity = GroundStickSpeed;
    }

    /// <summary>根据 Shift 跑步状态读取当前水平移动速度。</summary>
    /// <returns>当前移动速度，单位为米/秒；未按 Shift 时为普通移动速度。</returns>
    private float GetCurrentMoveSpeed()
    {
        if (_isRunning)
        {
            return PlayStateMgr.Instance.stats.runningTopSpeed;
        }

        return PlayStateMgr.Instance.stats.topSpeed;
    }

    /// <summary>供技能状态开始时立即停止水平移动，保留跳跃垂直速度和落地流程。</summary>
    public void StopHorizontalMovement()
    {
        stopMove();
    }

    /// <summary>停止当前水平移动，并清除方向输入、预测位移和移动运行态。</summary>
    private void stopMove()
    {
        // 停止前是否仍记录方向输入；用于在移动运行态已提前结束时补发动画刷新。
        bool hadDirectionalInput = _hasDirectionalInput;
        // 停止前是否仍处于水平移动运行态；true 时 SetMoving 会负责触发默认动画刷新。
        bool wasMoving = _isMoving;
        // 无论当前标记是否已被特殊状态改写，都清理方向和预测目标，避免停止后残留移动。
        _hasDirectionalInput = false;
        _targetDir = UVEC3.zero;
        _predictedPos = UVEC3.zero;
        if (Entity != null && Entity.Model != null)
        {
            // lastVelocity 只写水平分量，不改跳跃中的垂直速度。
            Entity.Model.lastVelocity = UVEC3.zero;
        }
        SetMoving(false);
        // 是否需要补发动画刷新：清理前仍有方向输入且原移动运行态已停止。
        bool needsIdleRefresh = hadDirectionalInput && !wasMoving;
        // 宿主是否可接收刷新：实体、状态机就绪且当前处于默认动作状态。
        bool hostReady = Entity != null &&
            Entity.Machine != null && Entity.CurrentState == StateDefine.kStateDefault;
        if (needsIdleRefresh && hostReady)
        {
            // 方向输入本身已改变时，即使运行态此前已为 false，也必须让默认动画重新选择 Idle。
            // 这条路径覆盖快速点按、状态切换期间提前清理 _isMoving 等竞态。
            Entity.Machine.MarkTriggered();
        }
    }

    /// <summary>对象池回收时重置移动状态和目标快照，保证下次复用时干净。</summary>
    public override void Shutdown()
    {
        IsMoving = false; _elapsedMilliseconds = 0; _interpolationMilliseconds = 150;
        _isjump = false;
        JumpCount = 0;
        _verticalVelocity = GroundStickSpeed;
        _airborneElapsedSeconds = 0f;
        _groundedStateInitialized = false;
        _isRunning = false;
        _hasDirectionalInput = false;
        CostTime = 0f;
        delay = 0.15f;
        keep_moving = false;
        IsMoved = false;
        _server_time = 0f;
        _predictedTime = 0f;
        _predictedPos = UVEC3.zero;
        _syncToTargetDir = UVEC3.zero;
        _syncToTargetDist = 0f;
        _runSpeed = 0f;
        _toTargetQuat = Quaternion.identity;
        _target.X = 0f; _target.Y = 0f; _target.Z = 0f; _target.Yaw = 0f;
    }

    /// <summary>
    /// 移动事件处理（MEvent_Move）：区分本机与他机。
    /// 本机：校验可移动性后取目标方向，立即驱动本地移动（预测），并做卡死检测；
    /// 他机：走 onSyncMoveEvent 影子跟随逻辑。
    /// </summary>
    /// <param name="e">移动事件参数，本地输入包含世界空间方向，他机同步包含目标快照。</param>
    private void onMoveEvent(IEventArg e)
    {
        if (/*CheckVehicleCtrlMove() || */Entity.IsFake)//坐骑系统的不可移动判定
        {
            return;
        }
        var args = e as MMoveEventArgs;
        if (!Entity.IsPlayer)//不是玩家的话走sync逻辑
        {
            onSyncMoveEvent(args);
            return;
        }
        // MoveCasting 只影响技能水平输入，其他动作保留原有空中方向控制。
        if (!Entity.IsMovable &&
            (Entity.CurrentState == StateDefine.kStateSkill || !Entity.IsJump))
        {
            return;
        }
       
#if false
 _targetPos = args.targetPos;
        float dis = (_targetPos - Entity.Position).magnitude;
        if (dis < 0.001f)//移动目标点太近
        {
            return;
        }
        _targetDir = MCommonFunctions.
            Horizontal(_targetPos - Entity.Position);
        face = _targetDir;
        if (_targetDir.sqrMagnitude > 0.01f)//去掉Y轴的偏移来确定目标方向和具体唯一向量
        {
            _lastDis = float.MaxValue;
            _targetDir = _targetDir.normalized;
        }
        else
        {
            _targetDir = UVEC3.zero;
            return;
        }
        Entity.Machine.Current.Move();//执行具体状态的move逻辑，可能会打断该状态或者发送打断状态的协议
        
#endif
#if true
        _targetDir = args.face;
        face = _targetDir;
        if (_targetDir.sqrMagnitude > 0.01f)//去掉Y轴的偏移来确定目标方向和具体唯一向量
        {
            _lastDis = float.MaxValue;
            _targetDir = _targetDir.normalized;
        }
        else
        {
            stopMove();
            return;
        }
#endif
        // 非零方向已经通过校验，记录按键保持状态；该状态不会被跳跃落地流程覆盖。
        _hasDirectionalInput = true;
        Entity.Machine.CurrentState.Move();//执行具体状态的move逻辑，可能会打断该状态或者发送打断状态的协议
        SetMoving(true);//激活update

        // 检查是否需要脱离卡死
        /*var player = Entity as MPlayer;
        if (player != null && !player.IsFly && !player.IsRidePubVehicle)
        {
            if (!MNavigationMgr.singleton.IsPtValid(player.Position))
            {
                player.StuckCount++;
            }
            else
            {
                player.StuckCount = 0;
            }
        }*/
    }

    /// <summary>停止移动事件处理：调用 stopMove 停止移动。</summary>
    /// <param name="e">停止移动事件参数；当前事件不携带额外数据。</param>
    private void onStopMoveEvent(IEventArg e)
    {
        stopMove();
    }

    /// <summary>设置水平移动运行态，并在状态变化时刷新动画和派发移动事件。</summary>
    /// <param name="move">true 表示实体本帧需要持续水平移动；false 表示停止水平位移。</param>
    private void SetMoving(bool move)
    {
        if (_isMoving == move)
        {
            return;
        }

        _isMoving = move;
        if (Entity != null && Entity.Machine != null &&
            Entity.CurrentState == StateDefine.kStateDefault)
        {
            Entity.Machine.MarkTriggered();
        }
        if (move)
        {
            DispatchEvent(MEventType.MoveStarted, null);
        }
        else
        {
            DispatchEvent(MEventType.MoveStopped, null);
        }
    }

    /// <summary>按 RO 同名入口记录远端目标、耗时和朝向，准备当前影子追赶段。</summary>
    /// <param name="e">服务器同步构造的池化移动参数，位置为世界米、耗时为秒。</param>
    private void onSyncMoveEvent(MMoveEventArgs e)
    {
        keep_moving = e.keepMoving;
        _predictedPos = UVEC3.zero;
        _server_time = _predictedTime = 0f;
        IsMoved = e.moved;
        _startPos = Entity.Position;
        // 上次服务器目标，用于恢复实际水平移动方向。
        UVEC3 last_tarpos = _targetPos;
        _targetPos = e.targetPos;
        _targetDir = MCommonFunctions.Horizontal(_targetPos - last_tarpos);
        delay = e.delay;
        if (delay <= 0f)
        {
            delay = 0.01f;
        }
        _syncToTargetDir = e.targetPos - Entity.Position;
        _syncToTargetDist = _syncToTargetDir.magnitude;
        // 停止同步沿用前段速度，避免停止包短耗时导致最后一步突然加速。
        if (keep_moving || e.isLocalMove || _runSpeed < 1f)
        {
            // 沿 RO 默认追赶系数；本项目尚无 RO 的 RTT 延迟管理器。
            float delay_ratio = 1.1f;
            _runSpeed = _syncToTargetDist / (delay * delay_ratio + 0.03f);
        }
        if (_syncToTargetDist > 0f)
        {
            _syncToTargetDir /= _syncToTargetDist;
        }
        // 水平朝向单独去除高度，避免斜坡上的模型俯仰抖动。
        UVEC3 tmpDir = _syncToTargetDir;
        tmpDir.y = 0f;
        if (tmpDir != UVEC3.zero)
        {
            _toTargetQuat = Quaternion.LookRotation(tmpDir);
        }
        face = e.face;
        if (_syncToTargetDist > 0.1f || keep_moving)
        {
            _lastDis = _targetDir.magnitude;
            _targetDir = _targetDir.normalized;
            if (!_isMoving && IsMoved)
            {
                SetMoving(true);
            }
            Enabled = true;
        }
        else
        {
            _targetDir = face;
            Entity.Position = _targetPos;
            if (face != UVEC3.zero && Entity.CurrentState == StateDefine.kStateDefault)
            {
                Entity.Model.Forward = face;
            }
            // 本项目技能程序位移也由此组件消费，停止影子移动不能禁用整个组件。
            SetMoving(false);
        }
    }

    /// <summary>按 RO 同名流程追赶服务器目标，抵达后保留或结束移动动画。</summary>
    /// <param name="deltaTime">本次逻辑帧间隔，单位为秒。</param>
    private void LagSyncUpdate(float deltaTime)
    {
        if (!Entity.Machine.CurrentState.IsActiveMovable)
        {
            return;
        }
        if (!_isMoving)
        {
            if (IsMoved && Entity.CurrentState == StateDefine.kStateDefault &&
                MCommonFunctions.Horizontal(Entity.Position - _targetPos).magnitude > 0.1f)
            {
                SetMoving(true);
            }
            else if (!keep_moving)
            {
                return;
            }
        }
        if (_syncToTargetDir == UVEC3.zero)
        {
            return;
        }
        // 加本帧时间前是否已经抵达，用于只在首次抵达时吸附一次目标。
        bool reach = _server_time * _runSpeed >= _syncToTargetDist;
        _server_time += deltaTime;
        if (_server_time * _runSpeed >= _syncToTargetDist)
        {
            if (!reach)
            {
                Entity.Position = _targetPos;
            }
            if (!keep_moving)
            {
                Entity.Model.Forward = face;
            }
            SetMoving(keep_moving);
            return;
        }
        // 本项目朝向由模型持有；仅水平转向，保持 RO 90/2 度阈值与 0.2 插值。
        float angle = Quaternion.Angle(Entity.Model.Rotation, _toTargetQuat);
        if (angle > MaxRotationAngle)
        {
            Entity.Model.Rotation = _toTargetQuat;
        }
        else if (angle > MinRotationAngle)
        {
            Entity.Model.Rotation = Quaternion.Slerp(Entity.Model.Rotation, _toTargetQuat, 0.2f);
        }
        // 项目没有 RO 导航高度接口，沿服务器提供的目标高度追赶。
        UVEC3 end = Entity.Position + _syncToTargetDir * _runSpeed * deltaTime;
        Entity.Position = end;
    }
}
