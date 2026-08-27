using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class RoleFSMStateBase
{
   protected  Animator _animator;
   protected RoleCtrlBase _roleCtrl;
   public RoleFSMStateBase(RoleCtrlBase roleCtrl, Animator animator)
   {
      _roleCtrl =  roleCtrl;
      _animator = animator;
   }
   /// <summary>
   /// 进入状态
   /// </summary>
   public virtual void OnEnter()
   {
      
   }
   /// <summary>
   /// 退出状态
   /// </summary>
   public virtual void OnExit()
   {
      
   }
}
