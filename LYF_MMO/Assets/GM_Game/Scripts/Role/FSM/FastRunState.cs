using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>加速奔跑状态：进入时把 Movement 浮点置为3，播快跑动画。</summary>
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
