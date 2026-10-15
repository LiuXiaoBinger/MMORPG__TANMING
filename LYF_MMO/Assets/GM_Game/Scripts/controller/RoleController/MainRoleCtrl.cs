using System.Collections;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

/**
* Title:主角控制器类
* Descrpiton:
*/

/// <summary>
/// 本地主控角色表现控制器：把自身注册为 Global.roleCtrlBase，挂载输入与残影组件，
/// 并在滑铲时生成残影、在攻击状态切回时驱动对应动画。正式移动已由场景实体链接管，本类仅保留兼容路径。
/// </summary>
public class MainRoleCtrl : RoleCtrlBase
{
   // 用于锁定移动方向的时间戳（当前时间小于此值时，禁止移动输入）
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

   protected override void OnAwake()
   {
      Global.Instance.roleCtrlBase = this;
      
      _inputCtrl = GetComponent<PlayerInputCtr>();
      _ghostEffect = GetComponent<GhostEffect>();
      
      
      CacheMainCameraTransform();
      RegistKeyEventHandle();
      
      
   }

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
   
   private void RegistKeyEventHandle()
   {
     // _inputCtrl.ShiftPressedEvent += ShifKeyIsPress;
      //_inputCtrl.SkillKeyEvent += SkillKeyEvent;
   }
   /// <summary>
   /// 角色释放技能
   /// </summary>
   /// <param name="skillInfo"></param>
   /// <returns></returns>
   public override bool UseSkill(SkillInfo skillInfo)
   {
      if(skillInfo==null||_roleBaseInfo == null||_roleBaseInfo.CurrHp<=0){return false;}

      //向服务端发送请求，角色释放技能 todo
      AttackState attackState =_fsm.GetState(RoleState.Attck) as AttackState;
      attackState._atkType = 1;
      ChangeState(RoleState.Attck);
      return true;
   }

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

   protected override void OnUpdate()
   {
      if (_roleState == RoleState.Slider)
      {
         _ghostEffect.CreateGhostEffectObject(Color.white,0.2f,
            0.2f,0.2f,0.2f);
      }
     // PlayerMovement();
   }

   

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
   
   // ReSharper disable Unity.PerformanceAnalysis
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

      Vector3 forward = Vector3.ProjectOnPlane(_mainCameraTransform.forward, Vector3.up).normalized;
      Vector3 right = Vector3.ProjectOnPlane(_mainCameraTransform.right, Vector3.up).normalized;
      return right * direction.x + forward * direction.z;
   }

   private void CacheMainCameraTransform()
   {
      Camera mainCamera = Camera.main;
      if (mainCamera != null)
      {
         _mainCameraTransform = mainCamera.transform;
      }
   }
   
   /// <summary>
   /// 在指定方向上平滑移动玩家（加速度控制）
   /// </summary>
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
        
      var turningDrag =  PlayStateMgr.Instance.stats.turningDrag;
      var acceleration = PlayStateMgr.Instance.stats.acceleration;
      var finalAcceleration = PlayStateMgr.Instance.stats.airAcceleration; // 空中与地面不同
      //var topSpeed =  PlayStateMgr.Instance.stats.topSpeed;
        
      Accelerate(direction, turningDrag, finalAcceleration, topSpeed);
   }
   /// <summary>
   /// 平滑地改变玩家水平角度 参数是水平速度
   /// </summary>
   /// <param name="direction"></param>
   public void FaceDirectionSmooth(Vector3 direction)
   {
      FaceDirection(direction, PlayStateMgr.Instance.stats.rotationSpeed);
   }
   /// <summary>
   /// 技能相关按键事件
   /// </summary>
   /// <param name="obj"></param>
   /// <exception cref="NotImplementedException"></exception>
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

   
}
