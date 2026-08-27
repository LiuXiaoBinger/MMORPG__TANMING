using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class FastRunState : RoleFSMStateBase
{
   
    public FastRunState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void OnEnter()
    {
        _animator.SetFloat("Movement", 3);
    }
    /// <summary>
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
      
    }
}
