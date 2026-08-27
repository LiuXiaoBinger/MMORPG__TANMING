using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class RoleAnimaBehaviour : MonoBehaviour
{
   private RoleCtrlBase _roleCtrl;
   [SerializeField, Header("特效位置点")] private Transform _effectpos;
   private void Awake()
   {
      _roleCtrl = GetComponent<RoleCtrlBase>();
   }

   private void AnimaEnd()
   {
      _roleCtrl.ChangeState(RoleState.Idle);
   }
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
   private void PlayEffect(AnimationEvent animationEvent)
   {
      RoleAtkConfig config = animationEvent.objectReferenceParameter as RoleAtkConfig;

      int index = animationEvent.intParameter;
      if (config != null)
      {
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
