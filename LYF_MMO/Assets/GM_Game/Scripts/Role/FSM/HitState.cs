using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>受击硬直状态：进入时把动作参数置为5，播放受击动画。</summary>
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
