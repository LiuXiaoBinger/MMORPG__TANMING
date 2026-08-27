using System;
using UniRx;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class SliderState : RoleFSMStateBase
{
    private const float AutoExitDelaySeconds = 0.3f;
    private IDisposable _autoExitTimer;
    private float _enteredAt;
    
    public SliderState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void OnEnter()
    {
        _enteredAt = Time.time;
        _animator.SetInteger(_roleCtrl._actionId,41);

        _autoExitTimer?.Dispose();
        _autoExitTimer = Observable.Timer(TimeSpan.FromSeconds(AutoExitDelaySeconds)).Subscribe(_ =>
        {
            if (_roleCtrl._roleState == RoleState.Slider && Time.time - _enteredAt >= AutoExitDelaySeconds)
            {
                _roleCtrl.ChangeState(RoleState.Idle);
            }
        });
    }
    /// <summary>
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
        _autoExitTimer?.Dispose();
        _autoExitTimer = null;
    }
   
}
