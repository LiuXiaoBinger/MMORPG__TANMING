using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class HitState : RoleFSMStateBase
{
    public HitState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }
    
    /// <summary>
    /// 进入状态
    /// </summary>
    public override void OnEnter()
    {
       _animator.SetInteger(_roleCtrl._actionId,5);
    }
    /// <summary>
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
       
    }
}
