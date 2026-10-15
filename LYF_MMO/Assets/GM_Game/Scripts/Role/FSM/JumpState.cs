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

    private IDisposable _obs;

    public JumpState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
        
    }

    /// <summary>
    /// 进入状态
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
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
        if (_obs != null)
        {
            _obs.Dispose();
        }
    }
    
}
