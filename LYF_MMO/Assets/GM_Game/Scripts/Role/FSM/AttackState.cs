using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class AttackState : RoleFSMStateBase
{
    public int _atkindex;
    private IDisposable _obs;
    public int _atkType =1;

    public AttackState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void OnEnter()
    {
        if (_atkType == 1)
        {
            if(_obs != null)_obs.Dispose();
            _obs= Observable.Timer(TimeSpan.FromMilliseconds(600)).Subscribe(_ =>
            {
                _atkindex = 30;
            });
            _animator.SetInteger(_roleCtrl._actionId,++_atkindex);
            if (_atkindex > 33)
            {
                _atkindex = 30;
            }
        }

        if (_roleCtrl._targetRole != null)
        {
            _roleCtrl.transform.LookAt(_roleCtrl._targetRole.transform);
        }
        _animator.SetInteger(_roleCtrl._actionId,_atkindex);
    }
    /// <summary>
    /// 退出状态
    /// </summary>
    public override void OnExit()
    {
      if(_obs != null)_obs.Dispose();
      
    }
    
}
