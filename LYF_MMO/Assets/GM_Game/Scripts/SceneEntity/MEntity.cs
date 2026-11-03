using MMoRpgCommon.SceneEntity;
using UnityEngine;
using UVEC3 = UnityEngine.Vector3;

/// <summary>
/// 把场景模型、移动、Buff、技能和动作状态机连接成一个可更新的实体。
/// 普通组件由 Components 管理，专用组件通过快捷属性单独管理，模型内部持有动画器。
/// 本层安排更新与回收顺序，各具体实体再补充自己的数据。
/// </summary>
public abstract class MEntity : MSceneObject
{
    #region 实体属性

    /// <summary>是否战斗状态</summary>
    protected bool _inBattle = false;

    /// <summary>是否行走状态</summary>
    protected bool _isWalking = false;

    /// <summary>是否可手动移动</summary>
    protected bool _isMovable = true;

    /// <summary>是否为召唤兽</summary>
    protected bool _isMob = false;

    /// <summary>是否射线命中（选中标记）</summary>
    protected bool _isRaycastHit = false;

    /// <summary>是否自拍状态</summary>
    protected bool _isSelfy = false;

    /// <summary>是否射击状态</summary>
    protected bool _isShoot = false;

    /// <summary>是否驾驶需隐藏的变身载具（主驾隐藏表现）</summary>
    protected bool _isRideHideVehicle = false;

    /// <summary>变身后是否保留时装外观</summary>
    protected bool _isTransfigureSaveFashion = false;

    /// <summary>是否为吼妖状态</summary>
    protected bool _is_houyao = false;

    /// <summary>传送特效 ID（传送同步时播放）</summary>
    protected int _teleportFx = -1;

    /// <summary>搬运物品 ID</summary>
    protected int _carryingItemId = 0;

    /// <summary>本帧累积位移（AppendMove 叠加，Move 时消费）</summary>
    protected UnityEngine.Vector3 _movement = UnityEngine.Vector3.zero;

    /// <summary>当前选中目标实体</summary>
    protected MEntity _targetEntity = null;

    /// <summary>模型包围盒 Y 范围（min/max，用于身高与头顶点）</summary>
    protected Vector2 _boundageY = Vector2.zero;

    /// <summary>基础高度（模型包围盒高度）</summary>
    protected float _height = 2f;

    /// <summary>基础半径（碰撞半径）</summary>
    protected float _radius = 0.5f;

    /// <summary>朝向角度（Y 轴，用于同步上行）</summary>
    protected float _face = 0;

    /// <summary>上次位置同步时间（节流 0.1s）</summary>
    protected float _lastSyncTime = -1f;

    /// <summary>上次射线命中检测时间</summary>
    protected float _lastUpdateRaycatHitTime = -1f;

    /// <summary>上次上行同步的起始位置（用于计算位移增量）</summary>
    public UnityEngine.Vector3 sysnOldPos = new UnityEngine.Vector3(-1, -1, -1);

    /// <summary>是否吼妖状态</summary>
    public bool IsHouyao { get { return _is_houyao; } set { _is_houyao = value; } }

    /// <summary>是否战斗待机</summary>
    public bool BattleIdle { get; set; } = false;

    /// <summary>碰撞半径（含缩放）</summary>
    public float Radius { get { return _radius * _scale; } set { _radius = value; } }

    /// <summary>模型 box 高度（含缩放）</summary>
    public float Height { get { return _height * _scale; } set { _height = value; } }

    /// <summary>是否乘坐公共载具</summary>
    public bool IsRidePubVehicle { get { return /*PubVehicle != null*/false; } }

    #endregion


    #region 动作相关

    /// <summary>变身后是否保留时装外观</summary>
    public bool IsTransfigureSaveFashion { get { return _isTransfigureSaveFashion; } set { _isTransfigureSaveFashion = value; } }

    /// <summary>传送特效 ID</summary>
    public int TeleportFx { set { _teleportFx = value; } get { return _teleportFx; } }

    /// <summary>手动移动许可的预留入口，目前固定返回 true</summary>
    public bool CanManualMove {
        get
        {
            return true;
            //return GetAttr(KKSG.AttrType.AttrSpecialNoMove) <= 0;
        } }

    /// <summary>实体名字（来自属性组件）</summary>
    // 实体名称由角色数据组件提供，当前实体层不直接依赖该组件。

    /// <summary>死亡查询的预留入口，目前固定返回 false，不读取血量</summary>
    public bool IsDead {
        get
        {
            /*return AttrComp == null || (AttrComp.IsDead || (AttrComp.HP <= 0 && AttrComp.MaxHP > 0));*/
            return false;
        } }

    /// <summary>是否普通跳舞中</summary>
    //public bool IsDancing { get { return DanceComp != null && CurrentState == StateDefine.kStateDance && !DanceComp.IsWildDance; } }

    /// <summary>是否狂野跳舞中</summary>
   // public bool IsWildDancing { get { return DanceComp != null && CurrentState == StateDefine.kStateDance && DanceComp.IsWildDance; } }

    /// <summary>是否移动中（来自移动组件）</summary>
    public bool IsMoving { get { return MoveComp.IsMoving; } }

    /// <summary>是否处于跳跃中，由移动组件提供</summary>
    public bool IsJump { get { return MoveComp.IsJump; } }

    /// <summary>是否与默认动作混合（动画混合开关）</summary>
    public bool IsMixToDefaultAction { get; set; } = true;

    /// <summary>是否显示待机连击动画</summary>
    bool _showIdleComboAnim = false;

    /// <summary>保存待机连击动画开关；相关广播与刷新代码目前被注释掉</summary>
    public bool ShowIdleComboAnim
    {
        get { return _showIdleComboAnim; }
        set
        {
            if (value == _showIdleComboAnim)
                return;
            if (value)
            {
                //MEventMgr.singleton.FireEvent(MEventType.MEvent_SkillComboSync, this);
            }
            _showIdleComboAnim = value;
            //MEventMgr.singleton.FireEvent(MEventType.MEvent_RefreshDefaultState, this);
        }
    }

    /// <summary>属性组件的快捷引用，由组件管理器设置，用于读取速度和表现表等数据。</summary>
    public EntityAttributesComponent AttrComp { get; private set; }

    /// <summary>移动组件（专用快捷组件，由 MComponentMgr 注入）</summary>
    public MMoveComponent MoveComp { get; private set; }

    /// <summary>从普通组件容器查询导航组件；未挂载时沿用容器的异常行为，不另存生命周期引用。</summary>
    public MNavigationComponent NavComp { get { return GetMComponentByT<MNavigationComponent>(); } }

    /// <summary>实体直属的普通位移组件，由普通容器负责初始化、每帧推进和池回收。</summary>
    protected MDisplacementComponent _displacementComp;

    /// <summary>技能计时器与输入移动读取的同一实体位移实例；实体未装配或已销毁时为空。</summary>
    public MDisplacementComponent DisplacementComp { get { return _displacementComp; } }

    /// <summary>Buff 组件（专用快捷组件，由 MComponentMgr 注入）</summary>
    public MBuffComponent Buff { get; private set; }

    /// <summary>技能组件（专用快捷组件，由 MComponentMgr 注入）</summary>
    public MSkillComponent Skill { get; private set; }

    /// <summary>状态机组件（专用快捷组件，由 MComponentMgr 注入）</summary>
    public override MStateMachine Machine { get; protected set; }

    /// <summary>实体直属模型对象</summary>
    public MModel Model { get { return _model; } }

    /// <summary>当前状态机状态，Machine 未绑定时返回默认状态</summary>
    public override StateDefine CurrentState
    {
        get
        {
            if (Machine != null)
            {
                return Machine.State;
            }

            return StateDefine.kStateDefault;
        }
    }

    /// <summary>是否为假对象（无服务器对应）</summary>
    public bool IsFake { get; set; } = false;

    /// <summary>综合实体移动开关和当前动作的移动/打断许可；死亡与手动移动入口目前仍为固定值</summary>
    public virtual bool IsMovable
    {
        get
        {
            // 基础可动条件：未死亡、未被禁移动且允许手动移动。
            bool basicMovable = !IsDead && _isMovable && CanManualMove;
            // 当前状态允许移动：处于可动状态或可被打断状态。
            bool stateAllowsMove = Machine.CurrentState.IsActiveMovable ||
                Machine.CurrentState.IsActiveInterrupt;
            return basicMovable && stateAllowsMove;
        }
        set { _isMovable = value; }
    }

    /// <summary>起跳沿用基础移动限制；技能动作允许被跳跃取消，其他动作仍使用原移动许可。</summary>
    public bool CanJump
    {
        get
        {
            if (CurrentState == StateDefine.kStateSkill)
            {
                return !IsDead && _isMovable && CanManualMove;
            }
            return IsMovable;
        }
    }

    /// <summary>位置（委托给实际表现模型）</summary>
    public override UnityEngine.Vector3 Position
    {
        get { return _model.Position; }
        set { _model.Position = value; }
    }

    /// <summary>跑步速度（米/秒）；读取属性组件的载具或跑步速度，写入时同步基类速度供移动组件使用。</summary>
    public float RunSpeed {
        get { return AttrComp.VehicleOrRunSpeed; }
        set { base.RunSpeed = value; } }

    /// <summary>是否骑乘普通载具</summary>
    public bool IsRideVehicle
    {
        /*get { return VehicleCtrlComp != null; }*/
        get { return false; }
    }

    /// <summary>是否处于搬运状态的标记，本层只保存该值。</summary>
    public bool InCarrying { get; set; } = false;

    #endregion

    /// <summary>
    /// 实体构造函数，从模型池获取 MModel 实例
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="entityType">实体类型枚举</param>
    /// <param name="configId">配置表 ID</param>
    protected MEntity(ulong uid, EntityType entityType, int configId)
        : base(uid, entityType, configId)
    {
        // 从模型池获取一个空闲 MModel，复用避免频繁实例化
        _model = MModelMgr.Instance.Acquire();
    }

    /// <summary>
    /// 每帧先推进普通位移组件，再驱动原专用组件和其余普通组件，最后消费模型位移。
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数</param>
    public override void Update(int deltaMilliseconds)
    {
        if (LifecycleState != EntityLifecycleState.Active)
        {
            return;
        }

        // 程序差量先求值，专用 Move 再统一提交碰撞；保留 Move 先于技能离槽处理的原顺序。
        Components.UpdateBeforeMovement(deltaMilliseconds);
        // 移动、Buff、技能和状态机仍在其余普通表现组件之前推进。
        MComponentMgr.Instance.UpdateSpecialComponents(this, deltaMilliseconds);
        base.Update(deltaMilliseconds);
        Move();
        /*if (LifecycleState != EntityLifecycleState.Active)
        {
            return;
        }

        // 专用组件优先更新（移动、Buff、技能、状态机逻辑）
        MComponentMgr.Instance.UpdateSpecialComponents(this, deltaMilliseconds);
        base.Update(deltaMilliseconds);

        // 逻辑位置同步到模型 Transform
        _model.Position = new UnityEngine.Vector3(Transform.X, Transform.Y, Transform.Z);
        _model.Rotation = UnityEngine.Quaternion.Euler(0f, Transform.Yaw, 0f);
        // 动画速度同步
        if (_model.Ator != null)
        {
            _model.Ator.Speed = AtorSpeed;
        }
        _model.Update(deltaMilliseconds * 0.001f);*/


    }

    /// <summary>实体逻辑更新后统一驱动状态机动画刷新。</summary>
    /// <param name="deltaSeconds">本帧经过的秒数。</param>
    public override void LateUpdate(float deltaSeconds)
    {
        if (LifecycleState != EntityLifecycleState.Active)
        {
            return;
        }
        // 状态机帧末刷新动画参数
        if (Machine != null)
        {
            Machine.LateUpdate(deltaSeconds);
        }
        // 模型帧末更新（动画播放、特效等）
        if (_model != null)
        {
            _model.LateUpdate(deltaSeconds);
        }
    }

    /// <summary>
    /// 执行初始化：先创建并初始化专用组件，再走基类普通组件初始化流程
    /// </summary>
    public override void Initialize()
    {
        if (LifecycleState != EntityLifecycleState.Created)
        {
            return;
        }

        // 专用组件（Move/Buff/Skill/Machine）优先创建和初始化
        MComponentMgr.Instance.CreateSpecialComponents(this);
        MComponentMgr.Instance.InitializeSpecialComponents(this);
        base.Initialize();
    }

    /// <summary>显式挂接实体位移组件，再交基类进入普通组件初始化流程。</summary>
    protected override void OnBeforeInitialize()
    {
        _displacementComp = MComponentMgr.singleton.CreateComponent<MDisplacementComponent>(this);
        base.OnBeforeInitialize();
    }

    /// <summary>消费本帧位移：MoveModel 推进实际表现模型</summary>
    protected virtual void Move()
    {
        if (_movement == UnityEngine.Vector3.zero)
        {
            return;
        }
        MoveModel(_model, _movement);
        _movement = UnityEngine.Vector3.zero;
    }

    /// <summary>
    /// 调用模型的 Move；原高度、飞行和墙体处理分支目前全部被注释掉。
    /// movement 只用于计算未继续使用的 target，当前不按 type 选择移动方式。
    /// </summary>
    /// <param name="model">本次要推进的模型。</param>
    /// <param name="movement">调用方累计的位移，当前只参与未继续使用的目标点计算。</param>
    /// <param name="type">保留的移动方式参数，当前不参与分支。</param>
    /// <returns>当前实现固定返回 false。</returns>
    public static bool MoveModel(MModel model, UnityEngine.Vector3 movement, MoveType type = MoveType.Normal)
    {
        // 原移动分支使用的目标点；当前分支保留计算，但没有应用它。
        UnityEngine.Vector3 target = model.Position + movement;
        /*if (type == MoveType.Fly)
        {
            if (MSceneGridMgr.singleton.TryGetHeight(target, out float y))
            {
                if (MCommonFunctions.IsLess(y, 0))
                {
                    return false;
                }
                if (MCommonFunctions.IsEqual(y, 0))
                {
                    if (MNavigationMgr.singleton.TryGetHeight(target.x, target.z, out float yy))
                    {
                        if (MCommonFunctions.IsEqualGreater(target.y, yy))
                        {
                            model.Position = target;
                            return false;
                        }

                        if (MCommonFunctions.IsGreater(movement.y, 0))
                        {
                            model.Position += new UnityEngine.Vector3(0, movement.y, 0);
                        }
                        return true;
                    }

                }
                if (MCommonFunctions.IsEqualGreater(target.y, y))
                {
                    model.Position = target;
                    return false;
                }
                else
                {
                    if (MCommonFunctions.IsGreater(movement.y, 0))
                    {
                        model.Position += new UnityEngine.Vector3(0, movement.y, 0);
                    }
                    return true;
                }
            }
            return false;
        }
        else if (type == MoveType.Fall)
        {
            model.Position = target;
            return false;
        }
        else
        {
            if (!MNavigationMgr.singleton.TrySetHeight(ref target))
            {
                return false;
            }

            //圆形空气墙平滑操作
            UnityEngine.Vector3 smooth_pos = MAirWallMgr.singleton.CheckSphereCollision(model.Position, target);
            if (smooth_pos != target)
            {
                target = smooth_pos;
            }

            //因为上面对target进行了校正
            if (false == MAirWallMgr.singleton.CheckWall(model.Position, target))
            {
                return false;
            }

            model.Position = target;
        }*/
        //model.Position = target;
        model.Move();
        return false;
    }

    /// <summary>
    /// 加载实体模型，实体状态机无需等待模型加载完成即可运行。
    /// 异步加载完成后通过回调通知。
    /// </summary>
    /// <param name="location">模型资源路径</param>
    /// <param name="controllerLocation">Animator 控制器路径</param>
    /// <param name="useObjectAnimator">是否使用对象动画器</param>
    /// <param name="callback">加载完成回调，参数为加载好的 MModel</param>
    public void LoadModel(string location, string controllerLocation, bool useObjectAnimator,
        System.Action<MModel> callback)
    {
        _model.Load(location, controllerLocation, useObjectAnimator, callback);
    }

    /// <summary>
    /// 销毁时释放专用组件和模型对象，回收进各自对象池
    /// </summary>
    protected override void OnDestroying()
    {
        // 普通容器已回收位移组件，清快捷引用后再释放技能，避免碰触已归池实例。
        _displacementComp = null;
        // 先释放专用组件
        MComponentMgr.Instance.ReleaseSpecialComponents(this);
        // 再回收模型到模型池
        MModelMgr.Instance.Release(_model);
        _model = null;
        base.OnDestroying();
    }

    /// <summary>
    /// 由 MComponentMgr 注入移动组件
    /// </summary>
    /// <param name="component">移动组件实例</param>
    internal void SetMoveComponent(MMoveComponent component)
    {
        MoveComp = component;
    }

    /// <summary>
    /// 由 MComponentMgr 设置属性组件快捷引用
    /// </summary>
    /// <param name="component">属性组件实例</param>
    internal void SetAttComponent(EntityAttributesComponent component)
    {
        AttrComp = component;
    }

    /// <summary>
    /// 由 MComponentMgr 注入 Buff 组件
    /// </summary>
    /// <param name="component">Buff 组件实例</param>
    internal void SetBuffComponent(MBuffComponent component)
    {
        Buff = component;
    }

    /// <summary>
    /// 由 MComponentMgr 注入技能组件
    /// </summary>
    /// <param name="component">技能组件实例</param>
    internal void SetSkillComponent(MSkillComponent component)
    {
        Skill = component;
    }

    /// <summary>
    /// 由 MComponentMgr 注入状态机组件
    /// </summary>
    /// <param name="component">状态机实例</param>
    internal void SetMachineComponent(MStateMachine component)
    {
        Machine = component;
    }

    /// <summary>
    /// 对象池复用时重置实体全部运行时状态，恢复默认值。
    /// 重新从模型池获取 MModel，清空所有战斗、移动、目标等运行时字段。
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        base.PrepareForReuse(uid, entityType, configId);

        // 重新从模型池获取模型
        _model = MModelMgr.Instance.Acquire();

        // 重置所有运行时状态标记
        _inBattle = false;
        _isWalking = false;
        _isMovable = true;
        _isMob = false;
        _isRaycastHit = false;
        _isSelfy = false;
        _isShoot = false;
        _isRideHideVehicle = false;
        _isTransfigureSaveFashion = false;
        _is_houyao = false;
        _teleportFx = -1;
        _carryingItemId = 0;

        // 重置位移、目标、尺寸等运行时数据
        _movement = UnityEngine.Vector3.zero;
        _targetEntity = null;
        _boundageY = UnityEngine.Vector2.zero;
        _height = 2f;
        _radius = 0.5f;
        _face = 0f;
        _lastSyncTime = -1f;
        _lastUpdateRaycatHitTime = -1f;
        sysnOldPos = new UnityEngine.Vector3(-1f, -1f, -1f);

        // 重置动作和组件引用
        BattleIdle = false;
        IsMixToDefaultAction = true;
        MoveComp = null;
        _displacementComp = null;
        Buff = null;
        Skill = null;
        Machine = null;
    }

    /// <summary>累加一帧世界空间位移增量，暂存到待提交位移中，由移动组件统一提交碰撞。</summary>
    /// <param name="moveDelta">本帧世界空间位移增量，单位为米。</param>
    public void AppendMove(Vector3 moveDelta)
    {
        _movement += moveDelta;
    }

    /// <summary>由移动组件统一将世界空间位移交给模型碰撞控制器。</summary>
    /// <param name="movement">本帧世界空间位移，单位为米。</param>
    public void MoveCharacter(Vector3 movement)
    {
        if (_model == null)
        {
            return;
        }

        _model.MoveCharacter(movement);
    }

    /// <summary>追加位移（分量）</summary>
    /// <param name="x">世界 X 轴位移增量，单位为场景单位。</param>
    /// <param name="y">世界 Y 轴位移增量，单位为场景单位。</param>
    /// <param name="z">世界 Z 轴位移增量，单位为场景单位。</param>
    public void AppendMove(float x, float y, float z)
    {
        _movement.x += x;
        _movement.y += y;
        _movement.z += z;
    }

    #region 静态函数

    /// <summary>使用全局转向阻力和空中加速度，转调完整的速度计算方法。</summary>
    /// <param name="entity">待加速的实体。</param>
    /// <param name="direction">期望移动方向向量。</param>
    /// <param name="topSpeed">本次加速的目标顶速（米/秒）。</param>
    public static void Accelerate(MEntity entity,Vector3 direction ,float topSpeed)
    {

        // 每秒消除侧向速度的强度，取自全局移动配置。
        var turningDrag =  PlayStateMgr.Instance.stats.turningDrag;
        // 读取的地面加速度；当前简化入口未把此值传给下一级。
        var acceleration = PlayStateMgr.Instance.stats.acceleration;
        // 当前实际传入的空中加速度，未按是否着地分支。
        var finalAcceleration = PlayStateMgr.Instance.stats.airAcceleration; // 空中与地面不同
        //var topSpeed =  PlayStateMgr.Instance.stats.topSpeed;

        Accelerate(entity,direction, turningDrag, finalAcceleration, topSpeed);
    }

    /// <summary>把当前速度分成目标方向和侧向两部分，沿目标方向加速并逐步消除侧向速度。</summary>
    /// <param name="entity">保存当前模型速度的实体。</param>
    /// <param name="direction">期望前进方向，调用方提供世界方向向量。</param>
    /// <param name="turningDrag">每秒消除侧向速度的强度。</param>
    /// <param name="acceleration">目标方向加速度，单位为场景单位/秒²。</param>
    /// <param name="topSpeed">最高移动速度，单位为场景单位/秒。</param>
    public static void Accelerate(MEntity entity,Vector3 direction, float turningDrag,
        float acceleration, float topSpeed)
    {
     float accelerationMultiplier  = 1f;       // 加速度倍率


     float topSpeedMultiplier  = 1f;           // 最高速度倍率

     float turningDragMultiplier  = 1f;        // 转向阻力倍率

     float decelerationMultiplier  = 1f;       // 减速度倍率
        // 判断方向是否有效（不为零向量）
        if (direction.sqrMagnitude > 0)
        {
            // 计算当前速度在目标方向上的投影速度（标量）
            var speed = Vector3.Dot(direction, entity.Model.lastVelocity);
            // 计算当前速度在目标方向上的向量部分
            var velocity = direction * speed;
            // 计算当前速度中垂直于目标方向的部分（转向速度）
            var turningVelocity = entity.Model.lastVelocity - velocity;
            // 计算转向阻力对应的速度变化量（根据转向阻力系数和时间增量）
            var turningDelta = turningDrag * turningDragMultiplier * Time.deltaTime;
            // 计算最大允许速度（考虑速度倍率）
            var targetTopSpeed = topSpeed * topSpeedMultiplier;

            // 如果当前速度未达最大速度，或当前速度与目标方向相反，则加速
            if (entity.Model.lastVelocity.magnitude < targetTopSpeed || speed < 0)
            {
                // 增加速度，受加速度倍率和时间影响
                speed += acceleration * accelerationMultiplier * Time.deltaTime;
                // 限制速度在[-最大速度, 最大速度]范围内
                speed = Mathf.Clamp(speed, -targetTopSpeed, targetTopSpeed);
            }

            // 重新计算目标方向速度向量
            velocity = direction * speed;
            // 将转向速度平滑减小到0，实现自然转向过渡
            turningVelocity = Vector3.MoveTowards(turningVelocity, Vector3.zero, turningDelta);
            // 更新横向速度为目标方向速度与转向速度之和
            entity.Model.lastVelocity = velocity + turningVelocity;
        }
    }

    /// <summary>
    /// 按全局旋转速度平滑转向目标世界方向
    /// </summary>
    /// <param name="entity">需要转向的实体。</param>
    /// <param name="direction">要面向的世界方向，零向量不改变朝向。</param>
    public static void FaceDirectionSmooth(MEntity entity,Vector3 direction)
    {
        FaceDirection(entity,direction, PlayStateMgr.Instance.stats.rotationSpeed);
    }

    /// <summary>按本帧允许的最大角度旋转模型，不改变位置。</summary>
    /// <param name="entity">需要转向的实体。</param>
    /// <param name="direction">目标世界方向。</param>
    /// <param name="currentRotationSpeed">旋转速度，单位为度/秒。</param>
    protected static void FaceDirection(MEntity entity,Vector3 direction, float currentRotationSpeed)
    {
        if (direction != Vector3.zero)
        {
            // 模型当前的世界旋转。
            var rotation=entity.Model.Rotation;
            // 本帧最多旋转的角度，单位为度。
            var rotationDelta =currentRotationSpeed*Time.deltaTime;
            // 以世界上方向为基准，构造面向目标方向的旋转。
            var target=Quaternion.LookRotation(direction, Vector3.up);
            entity.Model.Rotation = Quaternion.RotateTowards(rotation,target,rotationDelta);
        }
    }

    #endregion

    /// <summary>
    /// 将配置动画名解析为 YooAsset Location，并交给模型动画器异步覆盖目标状态。
    /// </summary>
    /// <param name="key">AnimatorOverrideController 中需要替换的原始剪辑键。</param>
    /// <param name="clipName">动画索引中的短名，或资源包可验证的完整 Location。</param>
    /// <param name="blendTreeKey">可选混合树状态键；为空时使用覆盖键作为逻辑状态键。</param>
    /// <returns>实际参与异步准备的动画信息；动画器不可用或路径无效时返回 null。</returns>
    public MAnimator.MAnimInfo OverrideAnim(string key, string clipName,
        string blendTreeKey = null)
    {
        // 经动画管理器索引解析后的完整 YooAsset Location。
        string clipPath = string.Empty;
        if (Model.Ator != null)
        {
            clipPath = MAnimationMgr.singleton.GetClipPath(clipName);
        }

        if (!string.IsNullOrEmpty(clipPath))
        {
            // 返回真实动画绑定信息，调用方可查询准备中路径、时长和就绪状态。
            MAnimator.MAnimInfo animInfo = Model.Ator.OverrideAnim(key,
                clipPath, blendTreeKey);
            if (animInfo != null)
            {
                //必须延迟1帧，因为这次重载带了返回值，外部逻辑可能会对返回值进行一些操作，进行同步通知的话
                //可能会再次触发重载动画的操作，导致在这一次返回前形成新的修改
                /*var evt = 
                    MEventPool<MAfterOverrideAnimEventArgs>.GetEvent();
                evt.key = key;
                evt.clipName = clipName;
                DelayFireEvent(MEventType.MEvent_AfterOverrideAnim, 0.01f, evt);*/
            }
            return animInfo;
        }
        return null;
    }

    /// <summary>移动方式：普通/飞行/下落</summary>
    public enum MoveType
    {
        /// <summary>普通移动类别，当前 MoveModel 未按此值分支。</summary>
        Normal,
        /// <summary>飞行类别，原高度处理分支目前未启用。</summary>
        Fly,
        /// <summary>下落类别，原直接设置位置的分支目前未启用。</summary>
        Fall,
    }
}
