using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;
/**
* Title:创建角色Window
* Descrpiton:目前只有一个角色，创建角色默认为剑修
*/

public class CreateRoleWindow : WindowBase
{
   [SerializeField, Header("昵称输入框")] private TMP_InputField _iptNickname;
   [SerializeField, Header("魔剑士")] private GameObject MJSobj;
   [SerializeField, Header("拳师")] private GameObject QSobj;
   List<GameObject>_listObj = new List<GameObject>();
   public Action<string,int> CreateRoleBtnClickAction;
   private int _jobindex = 0;
   private void Awake()
   {
      if (MJSobj != null)
      {
         _listObj.Add(MJSobj);
      }

      if (QSobj != null)
      {
         _listObj.Add(QSobj);
      }

      if (_listObj.Count > 0)
      {
         _jobindex = 0;
      }
   }

   public void OnSelectRoleJobLeftClicked()
   {
      _listObj[_jobindex].Show(false);
      --_jobindex;
      if (_jobindex < 0)
      {
         _jobindex = _listObj.Count - 1;
      }
      _listObj[_jobindex].Show();
   }
   public void OnSelectRoleJobrightClicked()
   {
      _listObj[_jobindex].Show(false);
      ++_jobindex;
      if (_jobindex >= _listObj.Count)
      {
         _jobindex = 0;
      }
      _listObj[_jobindex].Show();
   }
   public void OnCreateRoleClicked()
   {
      
      //判断输入框是否为空
      if (string.IsNullOrEmpty(_iptNickname.text))
      {
         TipsMgr.Instance.ShowSystemTips("昵称为空，请输入昵称");
         return;
      }
      CreateRoleBtnClickAction?.Invoke(_iptNickname.text,_jobindex+1);      //验证昵称合法 todo
      
      
   }
}
