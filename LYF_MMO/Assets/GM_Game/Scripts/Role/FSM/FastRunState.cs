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

    /// <summary>保存 roleCtrl 所属控制器和 animator 动画组件，供进入、退出状态时使用。</summary>
    public FastRunState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 把 Movement 设为 3，切到快跑动画。
    /// </summary>
    public override void OnEnter()
    {
        _animator.SetFloat("Movement", 3);
    }
    /// <summary>
    /// 快跑退出当前无额外处理。
    /// </summary>
    public override void OnExit()
    {
      
    }
}
