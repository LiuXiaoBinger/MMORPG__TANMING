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

    /// <summary>保存 roleCtrl 所属控制器和 animator 动画组件，供进入、退出状态时使用。</summary>
    public IdleState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }
    
    /// <summary>
    /// 把动作编号设为 1、Movement 设为 0，切回待机。
    /// </summary>
    public override void OnEnter()
    {
        _animator.SetInteger(_roleCtrl._actionId,1);
        _animator.SetFloat("Movement", 0);
    }
    /// <summary>
    /// 待机退出当前无额外处理。
    /// </summary>
    public override void OnExit()
    {
       
    }
}
