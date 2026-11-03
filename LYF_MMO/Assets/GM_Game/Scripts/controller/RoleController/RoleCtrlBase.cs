using System;
using System.Collections;
using System.Collections.Generic;
using cfg;
using HighlightPlus;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using MMoRpgCommon.SceneEntity;

/**
* 未绑定活动场景实体时使用的角色表现与移动基础逻辑。
*/

/// <summary>
/// 角色表现控制器基类：持有 Animator、CharacterController、状态机与重力/移动辅助方法。
/// 绑定场景实体（MEntity 且 Active）后，移动、重力与动画写入全部由实体组件链接管，
/// 旧控制器 Update/OnAnimatorMove 直接返回，避免重复推进。
/// </summary>
public class RoleCtrlBase : MonoBehaviour
{

   #region 组件与目标引用
   /// <summary>角色动画器。</summary>
   protected Animator _animator;

   /// <summary>角色 CharacterController，用于根运动和重力位移。</summary>
   protected  CharacterController _characterController;

   /// <summary>当前角色动画状态。</summary>
   public RoleState  _roleState;

   /// <summary>Animator 的 "Action" 整数参数哈希，避免运行时字符串查找。</summary>
   public int _actionId = Animator.StringToHash("Action");

   /// <summary>选中高亮管理组件，用于受击 HitFX。</summary>
   public HighlightManager _highlightManager;

   //当前角色选中对象
   /// <summary>当前被选中/锁定的目标角色。</summary>
   public RoleCtrlBase _targetRole;
   #endregion

   #region 角色基础信息
   /// <summary>角色类型（主角/怪物/NPC/其他玩家）。</summary>
   public RoleType _roleType;

   /// <summary>角色基础领域信息（生命、等级等）。</summary>
   protected RoleBaseInfo _roleBaseInfo;

   /// <summary>根运动位移倍率，滑铲或特定攻击时放大。</summary>
   protected int _rootMotionSpeed = 1;
   #endregion

   #region 相机相关
   /// <summary>相机跟随/注视点。</summary>
   [Header("相机跟随点")]public Transform LookAt;

   /// <summary>绑定的场景实体世界对象；绑定且 Active 后旧控制器停止自行推进。</summary>
   private ROObject _entityobj=null;
   #endregion

   //角色有限状态机
   /// <summary>旧版角色动画有限状态机，未绑定场景实体时使用。</summary>
   protected RoleFSM _fsm;

   #region 速度相关
   // 世界空间速度，单位为世界单位/秒；水平与竖直属性共享这个值。
   public Vector3 velocity { get; set; }

   // 读取或修改 XZ 水平速度，写入时保留原有 Y 轴速度。
   public Vector3 lastVelocity
   {
      get { return new Vector3(velocity.x, 0, velocity.z); }
      set { velocity = new Vector3(value.x, velocity.y, value.z); }
   }

   // 读取或修改 Y 轴速度，写入时保留原有 XZ 水平速度。
   public Vector3 verticalVelocity
   {
      get { return new Vector3(0,velocity.y,0); }
      set { velocity = new Vector3(velocity.x, value.y, velocity.z); }
   }

   public float accelerationMultiplier { get; set; } = 1f;       // 加速度倍率

   public float gravityMultiplier { get; set; } = 1f;            // 重力倍率

   public float topSpeedMultiplier { get; set; } = 1f;           // 最高速度倍率

   public float turningDragMultiplier { get; set; } = 1f;        // 转向阻力倍率

   public float decelerationMultiplier { get; set; } = 1f;       // 减速度倍率
   #endregion

   #region 状态 动画
   /// <summary>遗留攻击编号字段，当前类没有读取它。</summary>
   int _atkindex = 30;

   /// <summary>遗留订阅句柄，当前类没有创建或释放订阅。</summary>
   private IDisposable _obs;
   #endregion

   #region 运动逻辑 主要让角色可以丝滑运动
   // 旧地面检测逻辑保存的竖直速度；当前实际位移使用 verticalVelocity。
   protected float _verticalSpeed;

   // 旧地面检测的目标局部高度；-1000 用作停止上升的标记。
   protected float _verticalHeiht;
   #endregion

   #region 生命周期与初始化
   /// <summary>取得动画、碰撞和高亮组件，创建旧状态机，再调用子类初始化。</summary>
   private void Awake()
   {
      _animator = GetComponent<Animator>();
      _characterController = GetComponent<CharacterController>();
      _highlightManager = GetComponent<HighlightManager>(); 
      _fsm = new RoleFSM(this,_animator);
      OnAwake();
   }

   /// <summary>Unity 启动阶段转交子类的 OnStart。</summary>
   private void Start()
   {
      OnStart();
   }

   /// <summary>子类取得自身组件和注册事件的位置，基类默认不处理。</summary>
   protected virtual void OnAwake()
   {
      
   }
   /// <summary>子类执行依赖其他对象 Awake 完成后的初始化，基类默认不处理。</summary>
   protected virtual void OnStart()
   {
      
   }
   /// <summary>保存角色类别和基础数据，不在此处创建模型或实体。</summary>
   /// <param name="roleType">该表现对象属于主角、其他玩家、怪物还是 NPC。</param>
   /// <param name="baseInfo">生命、等级等基础数据，直接保存传入引用。</param>
   public void InitCtrl(RoleType roleType, RoleBaseInfo baseInfo)
   {
      //todo
      _roleType = roleType;
      _roleBaseInfo =  baseInfo;
      
   }
   /// <summary>未绑定活动实体时，在基类重力和贴地处理后执行的子类逐帧逻辑。</summary>
   protected virtual void OnUpdate() { }
   /// <summary>活动场景实体已经接管时直接返回，否则推进旧重力、贴地和子类表现。</summary>
   private void Update()
   {
      // 已绑定新场景实体时，移动、重力和动画全部由实体组件链驱动；旧控制器禁止重复推进。
      MEntity sceneEntity = _entityobj as MEntity;
      if (sceneEntity != null && sceneEntity.LifecycleState == EntityLifecycleState.Active)
      {
         return;
      }

      //检测是否在地面
      //IsGorund();
      Gravity();
      _characterController.Move(transform.up * (Time.deltaTime * verticalVelocity.y));
      SnapToGround(PlayStateMgr.Instance.stats.snapForce);

      OnUpdate();
      //HandleController();
   }
   #endregion

   #region 实体绑定与状态切换
   /// <summary>仅在当前没有绑定对象时保存实体引用，不替换已有绑定。</summary>
   /// <param name="obj">控制器对应的世界对象。</param>
   public void SetROObject(ROObject obj)
   {
      if (_entityobj == null)
      {
         _entityobj = obj;
      }
   }

   /// <summary>返回此前绑定的世界对象，没有绑定时返回 null。</summary>
   public ROObject GetROObject( )
   {
      return _entityobj;
   }

   /// <summary>活动实体存在时转交新实体处理，否则交给旧 RoleFSM。</summary>
   /// <param name="state">旧表现状态；实体路径只转换当前支持的几种动作。</param>
   public void ChangeState(RoleState state)
   {
      MEntity entity = _entityobj as MEntity;
      if (entity != null && entity.LifecycleState == EntityLifecycleState.Active)
      {
         // 绑定场景实体后，新状态机拥有 Animator 唯一写入权；旧 FSM 仅供未绑定对象兼容。
         RouteStateToEntity(entity, state);
         // Jump、Slider 及未知旧状态没有对应的新状态机入口，路由后统一保持 Idle 语义。
         // 空闲与奔跑状态直接写入新状态机。
         bool isIdleOrRun = state == RoleState.Idle || state == RoleState.Run;
         // 疾跑与攻击状态也直接写入新状态机。
         bool isFastRunOrAttack = state == RoleState.FastRun || state == RoleState.Attck;
         // 受击状态直接写入新状态机。
         bool isHitState = state == RoleState.Hit;
         if (isIdleOrRun || isFastRunOrAttack || isHitState)
         {
            _roleState = state;
         }
         else
         {
            _roleState = RoleState.Idle;
         }
         return;
      }
      _fsm.ChangeState(state);
   }

   /// <summary>把旧状态请求转换为实体移动标记、默认状态或受击事件；旧攻击请求不启动技能。</summary>
   /// <param name="entity">已绑定且活动的场景实体。</param>
   /// <param name="state">来自旧控制器的状态。</param>
   private static void RouteStateToEntity(MEntity entity, RoleState state)
   {
      if (state == RoleState.Idle)
      {
         entity.MoveComp.SetMovingForPresentation(false);
         entity.Machine.ForceToDefault();
      }
      else if (state == RoleState.Run || state == RoleState.FastRun)
      {
         entity.MoveComp.SetMovingForPresentation(true);
         entity.Machine.ForceToDefault();
      }
      else if (state == RoleState.Attck)
      {
         // 旧控制器没有技能 ID、等级和效果载荷，不能再派发空的 SkillStarted；
         // 正式技能表现统一由 SkillCtrl 通过强类型事件启动。
         entity.MoveComp.SetMovingForPresentation(false);
         entity.Machine.ForceToDefault();
      }
      else if (state == RoleState.Hit)
      {
         entity.DispatchEvent(MEventType.DamageReceived, null);
      }
      else
      {
         // 旧状态机传入的新实体未知状态（包括旧 Jump/Slider）统一回默认状态；
         // Special 只允许由明确的业务事件触发，不能作为状态路由兜底。
         entity.MoveComp.SetMovingForPresentation(false);
         entity.Machine.ForceToDefault();
      }
   }
   #endregion

   #region 动画驱动与技能表现
   /// <summary>
   /// 未被场景实体接管时，把动画本帧的根位移乘动作倍率后交给 CharacterController。
   /// </summary>
   private void OnAnimatorMove()
   {
      // 新场景实体由 MMoveComponent 独占 CharacterController 位移，禁止旧根运动重复推进。
      MEntity sceneEntity = _entityobj as MEntity;
      if (sceneEntity != null && sceneEntity.LifecycleState == EntityLifecycleState.Active)
      {
         return;
      }

      if (_roleState == RoleState.Slider)
      {
         _rootMotionSpeed = 5;
      }
      else if (_roleState == RoleState.Attck)
      {
         AttackState attackState =_fsm.GetState(RoleState.Attck)as AttackState;
         if (attackState._atkindex==35)
         {
            _rootMotionSpeed = 3;
         }if (attackState._atkindex==36)
         {
            _rootMotionSpeed = 2;
         }
      }
      else
      {
         _rootMotionSpeed = 1;
      }
      if (_animator.deltaPosition != Vector3.zero)
      {
         _characterController.Move(_animator.deltaPosition*_rootMotionSpeed);
      }
   }

   /// <summary>旧技能接口的默认实现，不执行技能并返回 false。</summary>
   /// <param name="skillInfo">由具体兼容控制器解释的技能数据。</param>
   public virtual bool UseSkill(SkillInfo skillInfo){return false;}
   /// <summary>目标不为空时，让高亮组件播放该目标的受击效果。</summary>
   /// <param name="target">需要受击高亮的目标变换。</param>
   public void HitFx(Transform target)
   {
      if (target != null)
      {
         _highlightManager.HitFX(target);
      }
   }
   #endregion

   #region 地面检测与重力
   /// <summary>
   /// 未启用的旧升降流程：比较目标高度、调整竖直速度，再移动并检测接地。
   /// </summary>
   private void IsGorund()
   {
      // 旧地面检测的目标局部高度；-1000 用作停止上升的标记。， 大于 角色当前的高度， 角色需要上升
      if (_verticalHeiht > transform.localPosition.y && CheckShereGround())
      {
         verticalVelocity = new Vector3(0,20,0);
      }
      else if (_verticalHeiht < transform.localPosition.y && _verticalHeiht != -1000)//角色当前的高度 大于了 需要到达的高度后，角色就开始下降
      {
         _verticalHeiht = -1000;
         _verticalSpeed = -20;
         verticalVelocity = new Vector3(0,-20,0);
      }
      else
      {
        
      }

      //过渡的值
      //_verticalSpeed -= Mathf.Abs(_verticalSpeed) * Time.deltaTime * 1.5f;
      verticalVelocity -= new Vector3(0,Mathf.Abs(verticalVelocity.y) * Time.deltaTime * 1.5f,0);
      //if (_verticalSpeed > -50)
      //{
      //    Debug.Log("_verticalSpeed::" + _verticalSpeed);
      //}

      _characterController.Move(transform.up * (Time.deltaTime * verticalVelocity.y));

      //检测是否在地面
      if (CheckShereGround())
      {
         
         verticalVelocity = new Vector3(0,-100,0);
         _verticalHeiht = transform.localPosition.y;
      }

   }

   /// <summary>
   /// 检测是否在地面
   /// </summary>
   /// <returns>角色脚底上方 0.1 单位处、半径 0.2 的球是否碰到 Geometry 层。</returns>
   public bool CheckShereGround()
   {
      //用于检测当前位置周围半径范围内所有的碰撞体，如果有碰撞则返回true
      Vector3 pos = transform.position + new Vector3(0, 0.1f, 0);
      return Physics.CheckSphere(pos, 0.2f, 1 << LayerMask.NameToLayer("Geometry"));
   }

   /// <summary>
   /// 施加重力，使玩家下落
   /// </summary>
   public virtual void Gravity()
   {
      if(PlayStateMgr.Instance==null) return;
      //isGrounded = false;
      if (!CheckShereGround() && verticalVelocity.y > - PlayStateMgr.Instance.stats.gravityTopSpeed)
      {
         var speed = verticalVelocity.y;
         // 上升时用普通重力，下落时用更强的下落重力
         float force;
         if (verticalVelocity.y > 0)
         {
            force = PlayStateMgr.Instance.stats.gravity;
         }
         else
         {
            force = PlayStateMgr.Instance.stats.fallGravity;
         }
         speed -= force * gravityMultiplier * Time.deltaTime;

         // 限制最大下落速度
         speed = Mathf.Max(speed, -PlayStateMgr.Instance.stats.gravityTopSpeed);
         verticalVelocity = new Vector3(0, speed, 0);
      }
   }

   // 将角色吸附到地面（防止悬空）
   /// <param name="force">接地且未上升时使用的向下速度，单位为世界单位/秒。</param>
   public virtual void SnapToGround(float force)
   {
      // 只有接触地面，且垂直速度是向下的（y <= 0）才生效
      if (CheckShereGround() && (verticalVelocity.y <= 0))
      {
         // 设置固定向下速度，下一次移动时持续贴住地面。
         verticalVelocity = Vector3.down * force;
         /*if (_animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump_Loop"))
         {
            _animator.SetInteger("Action",23);
         }*/
      }
   }
   #endregion

   #region 输入与移动计算
   /// <summary>
   /// 按两个轴分别处理输入死区，得到 XZ 方向；当前没有执行时间锁定判断。
   /// </summary>
   /// <param name="value">左右和前后的二维方向输入。</param>
   /// <param name="deadzone">绝对值不超过该阈值的轴置零。</param>
   public virtual Vector3 GetMovementDirection( Vector2 value,float deadzone)
   {
      //if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;
      return GetAxisWithCrossDeadZone(value,deadzone);
   }

   /// <summary>
   /// 根据十字形死区修正输入值（Input System 默认是圆形死区）
   /// </summary>
   /// <param name="axis">左右和前后的二维输入。</param>
   /// <param name="deadzone">各轴单独忽略的输入阈值，需小于 1。</param>
   public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis ,float deadzone)
   {
      //Debug.Log("axis " + axis.ToString());
      
      if (Mathf.Abs(axis.x) > deadzone) axis.x = RemapToDeadzone(axis.x, deadzone);
      else axis.x = 0f;
      if (Mathf.Abs(axis.y) > deadzone) axis.y = RemapToDeadzone(axis.y, deadzone);
      else axis.y = 0f;
      return new Vector3(axis.x, 0, axis.y);
   }

   /// <summary>
   /// 按现有偏移公式放大有符号输入；没有把结果限制到 -1 到 1。
   /// </summary>
   //protected float RemapToDeadzone(float value,float deadzone)=>(value - deadzone) / (1-deadzone);
   /// <param name="value">已经超过死区的单轴输入。</param>
   /// <param name="deadzone">用于偏移和缩放的死区值，不能为 1。</param>
   protected float RemapToDeadzone(float value,float deadzone)
   {
      // 正输入使用负偏移，负输入使用正偏移；下式减去它会放大绝对值。
      float offset = deadzone;
      if (value > 0) offset = -deadzone;
      return (value - offset) / (1f - deadzone);
   }

   /// <summary>把处理死区后的输入按相机水平朝向旋转，并归一化为世界方向。</summary>
   /// <param name="mainCameraTransform">提供 Y 轴朝向的相机。</param>
   /// <param name="value">左右和前后的二维输入。</param>
   /// <param name="deadzone">单轴输入的忽略阈值。</param>
   public virtual Vector3 GetMovementCameraDirection(Transform mainCameraTransform ,Vector2 value,float deadzone)
   {
      // 1. 获取移动方向（通常是玩家输入的水平/垂直方向，比如 WSAD 或摇杆）
      var direction = GetMovementDirection(value ,deadzone);

      // 2. 如果有输入（不是零向量）
      if (direction.sqrMagnitude > 0)
      {
         // 3. 构建一个旋转：根据摄像机的 Y 轴角度（水平朝向）
         // Quaternion.AngleAxis(angle, axis) 表示绕某个轴旋转一个角度
         var rotation = Quaternion.AngleAxis(
            mainCameraTransform.eulerAngles.y, Vector3.up);

         // 4. 把原始输入方向旋转到摄像机的朝向下
         direction = rotation * direction;

         // 5. 归一化，保持方向向量的长度为 1（只保留方向）
         direction = direction.normalized;
      }

      // 6. 返回最终的世界空间移动方向
      return direction;
   }

   // 根据输入的方向平滑加速移动
   /// <param name="direction">目标水平单位方向。</param>
   /// <param name="turningDrag">每秒减少侧向速度的量。</param>
   /// <param name="acceleration">沿目标方向增加速度的加速度。</param>
   /// <param name="topSpeed">乘倍率前的最高速度，单位为世界单位/秒。</param>
   public virtual void Accelerate(Vector3 direction, float turningDrag, float acceleration, float topSpeed)
   {
      // 判断方向是否有效（不为零向量）
      if (direction.sqrMagnitude > 0)
      {
         // 计算当前速度在目标方向上的投影速度（标量）
         var speed = Vector3.Dot(direction, lastVelocity);
         // 计算当前速度在目标方向上的向量部分
         var velocity = direction * speed;
         // 计算当前速度中垂直于目标方向的部分（转向速度）
         var turningVelocity = lastVelocity - velocity;
         // 计算转向阻力对应的速度变化量（根据转向阻力系数和时间增量）
         var turningDelta = turningDrag * turningDragMultiplier * Time.deltaTime;
         // 计算最大允许速度（考虑速度倍率）
         var targetTopSpeed = topSpeed * topSpeedMultiplier;

         // 如果当前速度未达最大速度，或当前速度与目标方向相反，则加速
         if (lastVelocity.magnitude < targetTopSpeed || speed < 0)
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
         lastVelocity = velocity + turningVelocity;
      }
   }

   // 平滑减速，速度逐渐趋近于 0（水平速度减速）
   /// <param name="deceleration">每秒允许改变的水平速度量。</param>
   /// <param name="tagetSpeed">希望逐渐靠近的水平目标速度，零向量表示停止。</param>
   public virtual void Decelerate(float deceleration, Vector3 tagetSpeed )
   {
      // 计算本帧的减速度（decelerationMultiplier 可用于调节全局减速效果）
      var delta = deceleration * decelerationMultiplier * Time.deltaTime;
      // 把当前水平速度逐渐靠近传入的 tagetSpeed；目标不一定是零。
      // 第三个参数是本帧允许的最大速度变化量
      lastVelocity = Vector3.MoveTowards(lastVelocity, tagetSpeed, delta);
   }

   /// <summary>按当前速度位移：CharacterController 启用时走碰撞移动，否则直接改位置；当前 Update 未调用它。</summary>
   protected virtual void HandleController()
   {
      if (_characterController.enabled)
      {
         _characterController.Move(velocity * Time.deltaTime);
         return;
      }
      transform.position += velocity * Time.deltaTime;
   }

   /// <summary>以每帧最大角度逐渐转向目标，零方向不处理。</summary>
   /// <param name="direction">希望面朝的世界方向。</param>
   /// <param name="currentRotationSpeed">每秒最多旋转的度数。</param>
   protected virtual void FaceDirection(Vector3 direction, float currentRotationSpeed)
   {
      if (direction != Vector3.zero)
      {
         //当前旋转 
         var rotation=transform.rotation;
         // 本帧允许旋转的最大角度，单位为度。
         var rotationDelta =currentRotationSpeed*Time.deltaTime;
         // 以世界上方向为参考，计算目标朝向。
         var target=Quaternion.LookRotation(direction, Vector3.up);
         transform.rotation = Quaternion.RotateTowards(rotation,target,rotationDelta);
      }
   }
   #endregion

}
