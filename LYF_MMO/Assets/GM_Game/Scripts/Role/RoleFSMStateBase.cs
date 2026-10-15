using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 角色动画状态基类，持有 Animator 与所属控制器，子类重写 OnEnter/OnExit 完成具体状态的动画切换。
/// </summary>
public class RoleFSMStateBase
{
   /// <summary>状态驱动的角色 Animator。</summary>
   protected  Animator _animator;
   /// <summary>状态所属的角色表现控制器，用于读取当前动作参数ID等共享数据。</summary>
   protected RoleCtrlBase _roleCtrl;
   /// <summary>构造时注入 Animator 与控制器引用。</summary>
   /// <param name="roleCtrl">所属角色表现控制器。</param>
   /// <param name="animator">驱动角色动画的 Animator。</param>
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
