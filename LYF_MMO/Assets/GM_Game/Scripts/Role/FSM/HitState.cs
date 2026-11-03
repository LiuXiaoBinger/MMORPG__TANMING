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

    /// <summary>保存 roleCtrl 所属控制器和 animator 动画组件，供进入、退出状态时使用。</summary>
    public HitState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 把动作编号设为 5，切到受击动画。
    /// </summary>
    public override void OnEnter()
    {
       _animator.SetInteger(_roleCtrl._actionId,5);
    }
    /// <summary>
    /// 受击退出当前无额外处理。
    /// </summary>
    public override void OnExit()
    {
       
    }
}
