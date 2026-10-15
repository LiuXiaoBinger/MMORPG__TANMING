using System;
using UniRx;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>滑铲/倒地滑行状态：进入时动作参数置41，0.3秒后若仍处于本状态自动切回待机。</summary>
public class SliderState : RoleFSMStateBase
{
    /// <summary>自动退出滑铲的延迟秒数。</summary>
    private const float AutoExitDelaySeconds = 0.3f;
    /// <summary>自动退出定时器，退出状态时释放。</summary>
    private IDisposable _autoExitTimer;
    /// <summary>进入滑铲状态的 Time.time，用于校验延迟后是否仍处于滑铲。</summary>
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
