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
   [SerializeField,Header("NPC商城")] private NpcShopWindow _shopWindow;
   [SerializeField,Header("角色信息window")] private RoleAttriibuteWindow _roleAttriibuteWindow;
   [SerializeField,Header("聊天window")] private ChatWindow _chatWindow;
   [SerializeField,Header("小地图window")] private MinMapWindow _minMapWindow;

   public RoleCurrInfoWindow RoleCurrentWindow => _roleCurrInfoWindow;
   public SkillInfoWindow SkillWindow => _skillInfoWindow;
   public NpcShopWindow ShopWindow => _shopWindow;
   public RoleAttriibuteWindow RoleAttributeWindow => _roleAttriibuteWindow;
   public KnapsackWindow KnapsackWindow => _knapsackWindow;
   public TalkWindow TalkWindow => _talkWindow;
   public MinMapWindow MinMapWindow
   {
      get
      {
         if (_minMapWindow == null)
         {
            _minMapWindow = GetComponentInChildren<MinMapWindow>(true);
         }
         return _minMapWindow;
      }
   }

   /// <summary>获取聊天窗口，兼容尚未绑定序列化字段的旧预制体。</summary>
   public ChatWindow ChatWindow
   {
      get
      {
         if (_chatWindow == null)
         {
            _chatWindow = GetComponentInChildren<ChatWindow>(true);
         }
         return _chatWindow;
      }
   }
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
