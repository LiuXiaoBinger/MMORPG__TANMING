using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>攻击状态：进入时按攻击类型递增动作参数形成连击序列，面向目标，并在 600 毫秒后将连击索引重置为 30。</summary>
public class AttackState : RoleFSMStateBase
{
    /// <summary>当前连击动作索引，进入时从30起递增，超过33回卷到30。</summary>
    public int _atkindex;
    /// <summary>600 毫秒后重置连击的计时订阅，退出或再次进入时取消。</summary>
    private IDisposable _obs;
    /// <summary>攻击类型，1表示普通连击序列，其他类型暂未扩展。</summary>
    public int _atkType =1;

    /// <summary>保存 roleCtrl 所属控制器和 animator 动画组件，供进入、退出状态时使用。</summary>
    public AttackState(RoleCtrlBase roleCtrl, Animator animator) : base(roleCtrl, animator)
    {
    }

    /// <summary>
    /// 普通攻击时推进连击动作编号并朝向目标；600 毫秒后把连击编号重置为 30。
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
    /// 退出攻击时取消连击重置计时。
    /// </summary>
    public override void OnExit()
    {
      if(_obs != null)_obs.Dispose();
      
    }
    
}
