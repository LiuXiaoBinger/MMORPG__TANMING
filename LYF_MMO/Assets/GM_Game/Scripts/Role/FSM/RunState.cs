using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class RunState : RoleFSMStateBase
{
    
    public RunState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void OnEnter()
    {
        _animator.SetFloat("Movement", 2);
    }
    /// <summary>
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
      
    }
}
