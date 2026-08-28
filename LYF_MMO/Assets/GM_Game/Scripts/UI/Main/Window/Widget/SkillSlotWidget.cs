using System;
using System.Collections;
using System.Collections.Generic;
using cfg;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class SkillSlotWidget : MonoBehaviour
{
   [SerializeField,Header("技能图标")]private Image _imgIcon;
   [SerializeField,Header("技能绑定按键")]private TMP_Text _texKey;
   [SerializeField,Header("技能cd mask")]private Image _imgMask;
   [SerializeField,Header("技能cd")]private TMP_Text _texCD;
   private SkillInfo _skillInfo;
   private string _skillKey;
   public void SetKey(string key)
   {
      _skillKey = key;
      _texKey.SetText(key);
   }
   
   public  void ReFreshUI(RoleSkillInfo roleSkillInfo)
   {
      // to
       _skillInfo =LubanMgr.Instance.GetSkillInfo(roleSkillInfo.SkillId);
       if (_skillInfo != null)
       {
          //根据当前技能槽，绑定键盘按键
          PlayerInputCtr.Instance.SkillKeyEvent += BindSkillkey;
          //设置技能图标
          ResourceMgr.Instance.LoadSpriteAsync(_skillInfo.Icon, (Sprite sprite) =>
          {
             if(sprite != null)
                _imgIcon.gameObject.Show();
                _imgIcon.sprite = sprite;
          } );
          _texCD.SetText($"{_skillInfo.CDTime}");
       }
   }
   //释放技能cd，如果cd就不能释放技能
   private bool _iscd;
   //当前技能cd
   private float _curCdTime;
   private IDisposable updateEvent;

   /// <summary>
   /// 释放技能
   /// </summary>
   /// <param name="key"></param>
   private void BindSkillkey(string key)
   {
      if (!key.Equals(_skillKey)||_skillInfo==null||_iscd==true) return;
      
      //开始释放技能
      if (Global.Instance.roleCtrlBase.UseSkill(_skillInfo))
      {
         //技能cd
         _iscd = true;
         _curCdTime = _skillInfo.CDTime;
         _imgMask.gameObject.Show();
         SkillCD();
      }
      
   }

   private void SkillCD()
   {
      updateEvent= Observable.EveryUpdate().Subscribe(_ =>
      {
         if (_curCdTime > 0)//cd中
         {
            _curCdTime -= Time.deltaTime;
            _texCD.SetText($"{_curCdTime:F1}");
            _imgMask.fillAmount = _curCdTime / _skillInfo.CDTime;
         }
         else//cd结束
         {
            _iscd = false;
            _curCdTime = 0;
            _imgMask.gameObject.Show(false);
            updateEvent.Dispose();
            updateEvent = null;
         }
      });
   }
}
