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
* Title:role基类
* Descrpiton:
*/

public class RoleCtrlBase : MonoBehaviour
{
   protected Animator _animator;
   protected  CharacterController _characterController;
   public RoleState  _roleState;
   public int _actionId = Animator.StringToHash("Action");
   
   public HighlightManager _highlightManager;
   //当前角色选中对象
   public RoleCtrlBase _targetRole;
  
   #region 角色基础信息
   public RoleType _roleType;
   protected RoleBaseInfo _roleBaseInfo;
   protected int _rootMotionSpeed = 1;
   #endregion
   #region 相机相关

   [Header("相机跟随点")]public Transform LookAt;

   private ROObject _entityobj=null;

   #endregion
   
   //角色有限状态机
   protected RoleFSM _fsm;
   #region 速度相关
   //当前速度
   public Vector3 velocity { get; set; }

   //2d速度
   public Vector3 lastVelocity
   {
      get { return new Vector3(velocity.x, 0, velocity.z); }
      set { velocity = new Vector3(value.x, velocity.y, value.z); }
   }
   //垂直速度
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

   public void SetROObject(ROObject obj)
   {
      if (_entityobj == null)
      {
         _entityobj = obj;
      }
   }
   public ROObject GetROObject( )
   {
      return _entityobj;
   }
   #endregion
   private void Awake()
   {
      _animator = GetComponent<Animator>();
      _characterController = GetComponent<CharacterController>();
      _highlightManager = GetComponent<HighlightManager>(); 
      _fsm = new RoleFSM(this,_animator);
      OnAwake();
   }
   
   private void Start()
   {
      OnStart();
   }

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
   
   protected virtual void OnAwake()
   {
      
   }

   protected virtual void OnStart()
   {
      
   }
   protected virtual void OnUpdate() { }

   #region 状态 动画 

   int _atkindex = 30;
   private IDisposable _obs;
   public void ChangeState(RoleState state)
   {
      MEntity entity = _entityobj as MEntity;
      if (entity != null && entity.LifecycleState == EntityLifecycleState.Active)
      {
         // 绑定场景实体后，新状态机拥有 Animator 唯一写入权；旧 FSM 仅供未绑定对象兼容。
         RouteStateToEntity(entity, state);
         // Jump、Slider 及未知旧状态没有对应的新状态机入口，路由后统一保持 Idle 语义。
         if (state == RoleState.Idle || state == RoleState.Run ||
             state == RoleState.FastRun || state == RoleState.Attck ||
             state == RoleState.Hit)
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

   /// <summary>
   /// 跟运动动画回调函数
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

   public void HitFx(Transform target)
   {
      if (target != null)
      {
         _highlightManager.HitFX(target);
      }
   }
   #endregion
   
   
   



   #region 运动逻辑 主要让角色可以丝滑运动 

   //上升或则下降的速度
   protected float _verticalSpeed;
   //需要到达的高度
   protected float _verticalHeiht;
   


   /// <summary>
   /// 检测是否在地面
   /// </summary>
   /// <exception cref="NotImplementedException"></exception>
   private void IsGorund()
   {
      //需要到达的高度， 大于 角色当前的高度， 角色需要上升
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
   /// <returns></returns>
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
   public virtual void SnapToGround(float force)
   {
      // 只有接触地面，且垂直速度是向下的（y <= 0）才生效
      if (CheckShereGround() && (verticalVelocity.y <= 0))
      {
         // 将垂直速度设置为一个恒定向下的力（防止离地浮空）
         verticalVelocity = Vector3.down * force;
         /*if (_animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump_Loop"))
         {
            _animator.SetInteger("Action",23);
         }*/
      }
   }
   /// <summary>
   /// 获取移动方向输入（带十字型死区判断）
   /// 如果在锁定时间内，则返回 Vector3.zero
   /// </summary>
   public virtual Vector3 GetMovementDirection( Vector2 value,float deadzone)
   {
      //if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;
      return GetAxisWithCrossDeadZone(value,deadzone);
   }

   /// <summary>
   /// 根据十字形死区修正输入值（Input System 默认是圆形死区）
   /// </summary>
   /// <param name="axis">输入轴</param>
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
   /// 将输入值按给定死区重新映射到 0-1
   /// </summary>
   //protected float RemapToDeadzone(float value,float deadzone)=>(value - deadzone) / (1-deadzone);
   protected float RemapToDeadzone(float value,float deadzone)
   {
      float offset = deadzone;
      if (value > 0) offset = -deadzone;
      return (value - offset) / (1f - deadzone);
   }

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
   public virtual void Decelerate(float deceleration, Vector3 tagetSpeed )
   {
      // 计算本帧的减速度（decelerationMultiplier 可用于调节全局减速效果）
      var delta = deceleration * decelerationMultiplier * Time.deltaTime;
      // 将 lateralVelocity（水平速度向量）逐渐插值到 Vector3.zero（完全停止）
      // 第三个参数是本帧允许的最大速度变化量
      lastVelocity = Vector3.MoveTowards(lastVelocity, tagetSpeed, delta);
   }
   
   protected virtual void HandleController()
   {
      if (_characterController.enabled)
      {
         _characterController.Move(velocity * Time.deltaTime);
         return;
      }
      transform.position += velocity * Time.deltaTime;
   }
   
   protected virtual void FaceDirection(Vector3 direction, float currentRotationSpeed)
   {
      if (direction != Vector3.zero)
      {
         //当前旋转 
         var rotation=transform.rotation;
         //
         var rotationDelta =currentRotationSpeed*Time.deltaTime;
         //
         var target=Quaternion.LookRotation(direction, Vector3.up);
         transform.rotation = Quaternion.RotateTowards(rotation,target,rotationDelta);
      }
   }
   
   
   #endregion

  

   public void InitCtrl(RoleType roleType, RoleBaseInfo baseInfo)
   {
      //todo
      _roleType = roleType;
      _roleBaseInfo =  baseInfo;
      
   }
   
   public virtual bool UseSkill(SkillInfo skillInfo){return false;}
}
