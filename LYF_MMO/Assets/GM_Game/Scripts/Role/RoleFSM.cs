using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class RoleFSM 
{
   RoleCtrlBase _roleCtrl;
   private Dictionary<RoleState, RoleFSMStateBase> _stateDic;

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
