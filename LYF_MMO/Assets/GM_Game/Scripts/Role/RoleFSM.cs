using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 角色有限状态机：按 RoleState 管理各动画状态实例，负责状态切换时退出旧状态、进入新状态并同步 Animator 参数。
/// </summary>
public class RoleFSM
{
   /// <summary>状态机所属的角色表现控制器，状态切换时读写其当前状态与类型。</summary>
   RoleCtrlBase _roleCtrl;
   /// <summary>RoleState 到具体状态实例的映射，构造时一次性注册全部状态。</summary>
   private Dictionary<RoleState, RoleFSMStateBase> _stateDic;

   /// <summary>构造状态机并注册待机、跑、快跑、跳、滑铲、攻击、受击七个状态。</summary>
   /// <param name="roleCtrl">所属角色表现控制器。</param>
   /// <param name="animator">驱动角色动画的 Animator。</param>
   public RoleFSM( RoleCtrlBase roleCtrl,Animator animator)
   {
      _roleCtrl  = roleCtrl;
      _stateDic = new Dictionary<RoleState, RoleFSMStateBase>();
      _stateDic.Add(RoleState.Idle,new IdleState(roleCtrl, animator));
      _stateDic.Add(RoleState.Run,new RunState(roleCtrl, animator));
      _stateDic.Add(RoleState.FastRun,new FastRunState(roleCtrl, animator));
      _stateDic.Add(RoleState.Jump,new JumpState(roleCtrl, animator));
      _stateDic.Add(RoleState.Slider,new SliderState(roleCtrl, animator));
      _stateDic.Add(RoleState.Attck,new AttackState(roleCtrl, animator));
      _stateDic.Add(RoleState.Hit,new HitState(roleCtrl, animator));
   }
   /// <summary>
   /// 获取状态
   /// </summary>
   /// <param name="state"></param>
   /// <returns></returns>
   public RoleFSMStateBase  GetState(RoleState state)
   {
      if (_stateDic.ContainsKey(state))
      {
         return _stateDic[state];
      }
      return null;
   }
   /// <summary>
   /// 改变角色状态
   /// </summary>
   /// <param name="state"></param>
   public void ChangeState(RoleState newstate)
   {
      if (!_stateDic.ContainsKey(newstate))
      {
         return;
      }

      if (newstate == RoleState.Slider)
      {
         Debug.Log($"Slider currstar:{_roleCtrl._roleState.ToString()}");
      }
      //角色当前状态，等于要改变的状态 那么就return
      if (_roleCtrl._roleState == newstate && newstate != RoleState.Attck)
      {
         return;
      }
      //如果是主角，且已经打开属性面板，改变状态需要恢复相机视角
      if (_roleCtrl._roleType == RoleType.MainRole&& CameraMgr.Instance._isOpenRoleAtttWindow)
      {
         CameraMgr.Instance.RecoverAngle();
      }
      //退出当前状态
      _stateDic[_roleCtrl._roleState].OnExit();
      //角色状态赋值
      _roleCtrl._roleState = newstate;
      _stateDic[newstate].OnEnter();
   }
}
