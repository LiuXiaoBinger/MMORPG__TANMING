using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using TMPro;
using UnityEngine;
/**
* Title:创建角色Window
* Descrpiton:输入昵称，左右切换已配置的职业展示对象，再把选择交给控制器。
*/

/// <summary>角色创建窗口：收集昵称和职业选择，创建协议由按钮回调的订阅者处理。</summary>
public class CreateRoleWindow : WindowBase
{
   /// <summary>创建角色时提交的昵称；窗口只检查是否为空。</summary>
   [SerializeField, Header("昵称输入框")] private TMP_InputField _iptNickname;

   /// <summary>第一个候选职业的展示对象，非空时排在选择列表前面。</summary>
   [SerializeField, Header("魔剑士")] private GameObject MJSobj;

   /// <summary>第二个候选职业的展示对象，非空时追加到选择列表。</summary>
   [SerializeField, Header("拳师")] private GameObject QSobj;

   /// <summary>Awake 收集的职业展示对象，左右切换时环绕访问。</summary>
   List<GameObject>_listObj = new List<GameObject>();

   /// <summary>提交昵称及职业编号的回调；职业编号按当前展示下标加 1 得到。</summary>
   public Action<string,int> CreateRoleBtnClickAction;

   /// <summary>当前显示对象在职业列表中的下标，从 0 开始。</summary>
   private int _jobindex = 0;

   /// <summary>按预制体引用顺序收集可选职业对象，初始选中列表第一项。</summary>
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

   /// <summary>隐藏当前职业并显示前一项；越过开头后回到最后一项。</summary>
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

   /// <summary>隐藏当前职业并显示后一项；越过末尾后回到第一项。</summary>
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

   /// <summary>昵称非空时通知控制器创建角色；其他昵称规则不在本窗口校验。</summary>
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
