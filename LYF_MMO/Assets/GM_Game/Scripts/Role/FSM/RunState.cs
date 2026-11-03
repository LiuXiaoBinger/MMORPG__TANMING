using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>普通跑动状态：进入时把 Movement 浮点置为2，播跑步动画。</summary>
public class RunState : RoleFSMStateBase
{

    /// <summary>保存 roleCtrl 所属控制器和 animator 动画组件，供进入、退出状态时使用。</summary>
    public RunState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 把 Movement 设为 2，切到普通跑步动画。
    /// </summary>
    public override void OnEnter()
    {
        _animator.SetFloat("Movement", 2);
    }
    /// <summary>
    /// 普通跑步退出当前无额外处理。
    /// </summary>
    public override void OnExit()
    {
      
    }
}
