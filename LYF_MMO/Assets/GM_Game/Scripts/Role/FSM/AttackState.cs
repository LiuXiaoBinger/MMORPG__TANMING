using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>攻击状态：进入时按攻击类型递增动作参数形成连击序列，面向目标，并在600毫秒后推进连击索引。</summary>
public class AttackState : RoleFSMStateBase
{
    /// <summary>当前连击动作索引，进入时从30起递增，超过33回卷到30。</summary>
    public int _atkindex;
    /// <summary>600毫秒连击推进定时器，退出或重复进入时释放。</summary>
    private IDisposable _obs;
    /// <summary>攻击类型，1表示普通连击序列，其他类型暂未扩展。</summary>
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
