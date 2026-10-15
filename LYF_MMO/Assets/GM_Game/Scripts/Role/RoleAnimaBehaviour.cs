using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 角色动画事件挂载组件：挂在角色模型上，接收 AnimationEvent 回调，负责播放攻击特效并驱动受击状态。
/// </summary>
public class RoleAnimaBehaviour : MonoBehaviour
{
   /// <summary>所属角色表现控制器，用于查找挂点、驱动目标受击状态。</summary>
   private RoleCtrlBase _roleCtrl;

   /// <summary>攻击特效挂载点，由 Inspector 指定的骨骼节点位置。</summary>
   [SerializeField, Header("特效位置点")] private Transform _effectpos;

   private void Awake()
   {
      _roleCtrl = GetComponent<RoleCtrlBase>();
   }

   /// <summary>动画剪辑结束回调（当前体内仅保留历史切回待机的注释，暂未启用）。</summary>
   private void AnimaEnd()
   {
      //_roleCtrl.ChangeState(RoleState.Idle);
   }

   /// <summary>按攻击配置实例化技能特效并挂到配置指定的骨骼节点。</summary>
   /// <param name="atkConfigEntity">本次攻击的特效/音频/震屏配置。</param>
   private void DoAttackConfig(atkConfigEntity atkConfigEntity)
   {
      if (atkConfigEntity != null&&atkConfigEntity._effectInfo._fx!=null)
      {
         //实例化特效资源
         ParticleSystem fx = Instantiate(atkConfigEntity._effectInfo._fx);
         if (fx != null)
         {
            if (!string.IsNullOrEmpty(atkConfigEntity._effectInfo._parentName))
            {
               Transform parent = _roleCtrl.transform.Find(atkConfigEntity._effectInfo._parentName);
               if (parent!=null)
               {
                  fx.transform.SetParent(parent);
               }
              
            }
            fx.transform.localPosition = atkConfigEntity._effectInfo._position;
            fx.transform.localEulerAngles = atkConfigEntity._effectInfo._eulerAngles;
            fx.transform.localScale = Vector3.one;
         }
      }
   }

   /// <summary>响应角色剪辑的战斗特效事件；组件失活时忽略仍可能送达的动画事件。</summary>
   /// <param name="animationEvent">剪辑事件携带的攻击配置、配置索引与技能阶段标记。</param>
   private void PlayEffect(AnimationEvent animationEvent)
   {
      // Unity 的 AnimationEvent 可能调用已禁用的 MonoBehaviour，失活组件不得执行战斗表现。
      if (!isActiveAndEnabled)
      {
         return;
      }

      // 事件引用的攻击表现配置，仅用于本次动画事件。
      RoleAtkConfig config = animationEvent.objectReferenceParameter as RoleAtkConfig;

      // 配置列表中的事件索引，由动画资源的整数参数指定。
      int index = animationEvent.intParameter;
      if (config != null)
      {
         // 当前事件要实例化的攻击特效配置，后续阶段标记可能调整其空间参数。
         atkConfigEntity atkConfigEntity= config.atkConfigEntities[index];

         if (!string.IsNullOrEmpty(animationEvent.stringParameter))
         {
            switch(animationEvent.stringParameter)
            {
               case "skill03_2":
                  atkConfigEntity._effectInfo._position = _roleCtrl.transform.localPosition+_roleCtrl.transform.forward*6;
                  atkConfigEntity._effectInfo._eulerAngles = new Vector3(-90,_roleCtrl.transform.localEulerAngles.y,_roleCtrl.transform.localEulerAngles.z);
                  break;
               case "skill04_2":
                  atkConfigEntity._effectInfo._position = _roleCtrl.transform.localPosition+_roleCtrl.transform.forward*10;
                  //atkConfigEntity._effectInfo._eulerAngles = new Vector3(-90,_roleCtrl.transform.localEulerAngles.y,_roleCtrl.transform.localEulerAngles.z);
                  break;
               default:
                  break;
            }
         }
         DoAttackConfig(atkConfigEntity);
      }



      if (_roleCtrl._targetRole != null)
      {
         _roleCtrl._targetRole.ChangeState(RoleState.Hit);
         _roleCtrl.HitFx(_roleCtrl._targetRole.transform);
      }
   }
}
