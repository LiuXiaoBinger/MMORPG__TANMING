using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>跳跃状态：进入时把动作参数置为21，并用 UniRx 逐帧检测 Jump_Loop 标签后切到23过渡态，退出时释放订阅。</summary>
public class JumpState : RoleFSMStateBase
{

    /// <summary>等待跳跃循环动画出现的逐帧订阅，命中或退出状态时释放。</summary>
    private IDisposable _obs;

    /// <summary>保存 roleCtrl 所属控制器和 animator 动画组件，供进入、退出状态时使用。</summary>
    public JumpState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
        
    }

    /// <summary>
    /// 先播放动作 21；逐帧等到 Jump_Loop 标签出现，再切到动作 23 并结束监听。
    /// </summary>
    public override void OnEnter()
    {
      _animator.SetInteger(_roleCtrl._actionId,21);
      _obs = Observable.EveryUpdate().Subscribe(_ =>
      {
          if (_animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump_Loop"))
          {
              _animator.SetInteger("Action",23);
              _obs.Dispose();
          }
      });
    }
    /// <summary>
    /// 退出跳跃时释放逐帧监听，避免继续改变动画。
    /// </summary>
    public override void OnExit()
    {
        if (_obs != null)
        {
            _obs.Dispose();
        }
    }
    
}
