using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:主城场景相关view
* Descrpiton: 角色信息窗口 聊天信息窗口 小地图 背包信息窗口 技能信息窗口 ，Npc
*/

public class MainView : UIBase
{
   [SerializeField,Header("角色当前信息window")] private RoleCurrInfoWindow _roleCurrInfoWindow;
   [SerializeField,Header("技能信息window")] private SkillInfoWindow _skillInfoWindow;
   [SerializeField,Header("背包")] private KnapsackWindow _knapsackWindow;
   [SerializeField,Header("交谈相关Window")] private TalkWindow _talkWindow;
   [SerializeField,Header("NPC商城")] private ShopWindow _shopWindow;
   [SerializeField,Header("角色信息window")] private RoleAttriibuteWindow _roleAttriibuteWindow;
   public override void InitView()
   {
      base.InitView();
      windowDic[WindowType.RoleCurrtInfoWindow] = _roleCurrInfoWindow;
      windowDic[WindowType.SkillInfoWindow] = _skillInfoWindow;
      windowDic[WindowType.KnapsackWindow] = _knapsackWindow;
      windowDic[WindowType.TalkWindow] = _talkWindow;
      windowDic[WindowType.ShopWindow] = _shopWindow;
      windowDic[WindowType.RoleAttriibuteWindow] = _roleAttriibuteWindow;
      _roleCurrInfoWindow.InitWindow();
   }
}
