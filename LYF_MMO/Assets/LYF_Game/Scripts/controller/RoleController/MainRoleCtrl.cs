using System.Collections;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

/**
* 旧主角表现控制器，保留未绑定实体时的兼容逻辑。
*/

/// <summary>
/// 本地主控角色表现控制器：把自身注册为 Global.roleCtrlBase，挂载输入与残影组件，
/// 未绑定活动场景实体时可在滑铲状态生成残影。正式移动与施法由场景实体组件和 SkillCtrl 处理。
/// </summary>
public class MainRoleCtrl : RoleCtrlBase
{

   #region 字段
   // 旧版移动锁定时间字段；当前移动判断没有读取它
   protected float m_movementDirectionUnlockTime;

   /// <summary>角色输入组件，用于读取移动与技能按键。</summary>
   private PlayerInputCtr _inputCtrl;

   /// <summary>主相机变换缓存，用于把屏幕输入换算为世界朝向。</summary>
   private Transform _mainCameraTransform;

   /// <summary>移动速度，普通为10、加速为18。</summary>
   private float _moveSpeed = 10;

   /// <summary>旋转速度（历史字段，旧移动路径使用）。</summary>
   private float _rotationSpeed = 10000;

   /// <summary>挂在主角模型上的残影组件，滑铲时生成鬼影。</summary>
   private GhostEffect _ghostEffect;
   #endregion

   /// <summary>确保主界面控制器存在，并补齐主界面和技能的输入事件注册。</summary>
   protected override void OnStart()
   {
      // 直接从主城场景启动时不会经过选角回调，主角入场必须补建正式主界面控制器。
      if (UIRoot.Instance != null && UIRoot.Instance.MainCtrl == null)
      {
         UIRoot.Instance.InitMainCtrl();
      }
      // 主角输入组件可能晚于主界面控制器创建，统一走幂等注册补齐技能快捷键。
      if (UIRoot.Instance != null)
      {
         UIRoot.Instance.RegisterMainUIKeyHandler();
      }
   }

   #region 事件回调
   /// <summary>登记主角表现入口，取得输入和残影组件并缓存相机。</summary>
   protected override void OnAwake()
   {
      Global.Instance.roleCtrlBase = this;
      
      _inputCtrl = GetComponent<PlayerInputCtr>();
      _ghostEffect = GetComponent<GhostEffect>();
      
      CacheMainCameraTransform();
      RegistKeyEventHandle();
      
   }

   /// <summary>旧控制器仍接管角色时，在滑铲状态生成残影；旧手动移动调用已停用。</summary>
   protected override void OnUpdate()
   {
      if (_roleState == RoleState.Slider)
      {
         _ghostEffect.CreateGhostEffectObject(Color.white,0.2f,
            0.2f,0.2f,0.2f);
      }
     // PlayerMovement();
   }
   #endregion

   // ReSharper disable Unity.PerformanceAnalysis
   /// <summary>把角色局部 XZ 方向转换到相机的水平朝向；相机缺失时原样返回。</summary>
   /// <param name="direction">x 为右、z 为前的输入方向。</param>
   private Vector3 GetCameraRelativeDirection(Vector3 direction)
   {
      if (_mainCameraTransform == null)
      {
         CacheMainCameraTransform();
      }

      if (_mainCameraTransform == null)
      {
         return direction;
      }

      // 相机前方向投影到地面，避免镜头俯仰使移动产生上下分量。
      Vector3 forward = Vector3.ProjectOnPlane(_mainCameraTransform.forward, Vector3.up).normalized;
      // 相机右方向投影到地面，与前方向共同换算输入。
      Vector3 right = Vector3.ProjectOnPlane(_mainCameraTransform.right, Vector3.up).normalized;
      return right * direction.x + forward * direction.z;
   }

   #region 核心逻辑
   /// <summary>旧输入订阅位置；方法内订阅语句目前均已注释，不会实际注册。</summary>
   private void RegistKeyEventHandle()
   {
     // _inputCtrl.ShiftPressedEvent += ShifKeyIsPress;
      //_inputCtrl.SkillKeyEvent += SkillKeyEvent;
   }

   /// <summary>
   /// 旧技能兼容入口：角色存活且参数非空时请求攻击状态，不发送网络协议，也不按技能 ID 选动作。
   /// </summary>
   /// <param name="skillInfo">仅检查非空的技能数据；当前没有读取其中的技能配置。</param>
   /// <returns>通过基础检查并提交旧攻击状态时为 true。</returns>
   public override bool UseSkill(SkillInfo skillInfo)
   {
      if(skillInfo==null||_roleBaseInfo == null||_roleBaseInfo.CurrHp<=0){return false;}

      // 当前只设置旧状态机的攻击类型，正式发包由 SkillCtrl 负责。
      AttackState attackState =_fsm.GetState(RoleState.Attck) as AttackState;
      attackState._atkType = 1;
      ChangeState(RoleState.Attck);
      return true;
   }

   /// <summary>旧 Shift 回调：切换移动速度；目前没有订阅输入事件。</summary>
   /// <param name="ispressed">true 用 18 世界单位/秒，false 用 10 世界单位/秒。</param>
   private void ShifKeyIsPress(bool ispressed)
   {
      if (ispressed)
      {
         _moveSpeed = 18;
      }
      else
      {
         _moveSpeed = 10;
      }
   }

   /// <summary>未启用的旧移动流程：读取方向、切换跑步动画、加速转身并移动 CharacterController。</summary>
   private void PlayerMovement()
   {
      if(_roleState == RoleState.Attck)return;
      if (_inputCtrl.Movement != Vector2.zero)
      {
         /*Vector3 targetPos = GetMovementDirection(_inputCtrl.Movement,InputSystem.settings.defaultDeadzoneMin);
         targetPos = GetCameraRelativeDirection(targetPos);*/
         Vector3 targetPos = GetMovementCameraDirection(_mainCameraTransform, _inputCtrl.Movement,
            InputSystem.settings.defaultDeadzoneMin);
         //targetPos *= Time.deltaTime * _moveSpeed;
         //角色跳跃不允许切入
         if (_roleState != RoleState.Jump && _roleState != RoleState.Slider)
         {
            if (_moveSpeed == 10)
            {
            
               ChangeState(RoleState.Run);
            }else if (_moveSpeed == 18)
            {
           
               ChangeState(RoleState.FastRun);
            }
         }
         
         Accelerate(targetPos,_moveSpeed);
         
         FaceDirectionSmooth(lastVelocity);
         //对象的旋转
         //transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(targetPos), _rotationSpeed*Time.deltaTime);
         
         _characterController.Move(lastVelocity*Time.deltaTime);
        
      }
      else
      {
         /*Decelerate(PlayStateMgr.Instance.stats.friction,Vector3.zero);
         _characterController.Move(lastVelocity*Time.deltaTime);
         if(lastVelocity==Vector3.zero)
            _animator.SetFloat("Movement", 0);*/
         if (_roleState == RoleState.Run || _roleState == RoleState.FastRun)
         {
            ChangeState(RoleState.Idle);
         }
      }
   }

   /// <summary>查找带 MainCamera 标签的相机，有结果时更新缓存。</summary>
   private void CacheMainCameraTransform()
   {
      // 当前场景的主相机，找不到时保留已有缓存值。
      Camera mainCamera = Camera.main;
      if (mainCamera != null)
      {
         _mainCameraTransform = mainCamera.transform;
      }
   }

   /// <summary>
   /// 在指定方向上平滑移动玩家（加速度控制）
   /// </summary>
   /// <param name="direction">世界空间水平移动方向。</param>
   /// <param name="topSpeed">基础最高速度，单位为世界单位/秒。</param>
   public virtual void Accelerate(Vector3 direction ,float topSpeed)
   {
      /*// 根据是否按下 Run 键、是否在地面，决定不同的转向阻尼与加速度
      var turningDrag = isGrounded && inputs.GetRun() ? stats.current.runningTurningDrag : stats.current.turningDrag;
      var acceleration = isGrounded && inputs.GetRun() ? stats.current.runningAcceleration : stats.current.acceleration;
      var finalAcceleration = isGrounded ? acceleration : stats.current.airAcceleration; // 空中与地面不同
      var topSpeed = inputs.GetRun() ? stats.current.runningTopSpeed : stats.current.topSpeed;

      // 调用底层 Accelerate(方向, 转向阻尼, 加速度, 最大速度)
      Accelerate(direction, turningDrag, finalAcceleration, topSpeed);

      // 如果刚松开跑步键，限制最大速度，避免瞬间超速
      if (inputs.GetRunUp())
      {
          lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, topSpeed);
      }*/
        
      // 减小旧方向速度的阻力，交给基类加速函数。
      var turningDrag =  PlayStateMgr.Instance.stats.turningDrag;
      // 读取的地面加速度；当前兼容实现没有使用这个局部值。
      var acceleration = PlayStateMgr.Instance.stats.acceleration;
      // 实际传给基类的加速度，当前固定取配置的 airAcceleration。
      var finalAcceleration = PlayStateMgr.Instance.stats.airAcceleration; // 当前兼容路径始终使用空中加速度。
      //var topSpeed =  PlayStateMgr.Instance.stats.topSpeed;
        
      Accelerate(direction, turningDrag, finalAcceleration, topSpeed);
   }

   /// <summary>
   /// 按角色配置的旋转速度，让角色朝向给定方向。
   /// </summary>
   /// <param name="direction">目标朝向向量，通常取当前水平速度；零向量不旋转。</param>
   public void FaceDirectionSmooth(Vector3 direction)
   {
      FaceDirection(direction, PlayStateMgr.Instance.stats.rotationSpeed);
   }

   /// <summary>
   /// 未注册的旧快捷键处理：F 滑铲，Q 普攻，E/R/1/2 选择旧攻击动作。
   /// </summary>
   /// <param name="key">旧输入回调传入的快捷键字符。</param>
   private void SkillKeyEvent(string key)
   {
      AttackState attackState =_fsm.GetState(RoleState.Attck) as AttackState;
      switch (key)
      {
         case"F":
            ChangeState(RoleState.Slider);
            break;
         case "Q":
            attackState._atkType = 1;

            ChangeState(RoleState.Attck);

            break;
         case "E":
            attackState._atkType = 2;
            attackState._atkindex = 34;
            ChangeState(RoleState.Attck);
            break;
         case "R":
            attackState._atkType = 2;
            attackState._atkindex = 35;
            ChangeState(RoleState.Attck);
            break;
         case "1":
            attackState._atkType = 2;
            attackState._atkindex = 36;
            ChangeState(RoleState.Attck);
            break;
         case "2":
            attackState._atkType = 2;
            attackState._atkindex = 37;
            ChangeState(RoleState.Attck);
            break;
         default:
            break;
      }
   }
   #endregion

}
