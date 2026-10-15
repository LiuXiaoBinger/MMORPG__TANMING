using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>待机状态：进入时把动作参数置为1并把 Movement 浮点归零。</summary>
public class IdleState :RoleFSMStateBase
{

    public IdleState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }
    
    /// <summary>
    /// 进入状态
    /// </summary>
    public override void OnEnter()
    {
        _animator.SetInteger(_roleCtrl._actionId,1);
        _animator.SetFloat("Movement", 0);
    }
    /// <summary>
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
       
    }
}
