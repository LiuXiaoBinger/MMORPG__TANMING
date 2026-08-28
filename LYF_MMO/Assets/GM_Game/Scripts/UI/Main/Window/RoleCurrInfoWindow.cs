using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

/**
* Title:角色信息窗口
* Descrpiton:头像信息 昵称 职业等级 HP MP 
*/

public class RoleCurrInfoWindow : WindowBase
{
   // 角色头像。
   [SerializeField,Header("头像信息")]private Image _imgHead;
   // 角色昵称文本。
   [SerializeField,Header("角色昵称")]private TMP_Text _texNickName;
   // 角色职业和等级文本。
   [SerializeField,Header("角色职业等级信息")]private TMP_Text _texJobLevel;
   // 角色当前生命值与最大生命值比例。
   [SerializeField,Header("角色血量信息")]private Slider _sliderHP;
   // 角色当前法力值与最大法力值比例。
   [SerializeField,Header("角色法力信息")]private Slider _sliderMP;

   // Hp文本。
   [SerializeField,Header("HP")]private TMP_Text _texHp;
   // Mp文本。
   [SerializeField,Header("Mp")]private TMP_Text _texMp;
   // 用于承载技能槽实例的父节点。
   [SerializeField,Header("技能槽父组件")]private Transform _skillSlotParent;
   // 技能槽显示的快捷键顺序。
   private string[] _skilKkey = { "Q","E","R","F","1","2","3","4","5","6"};
   private Dictionary<string,SkillSlotWidget> _slots = new Dictionary<string,SkillSlotWidget>();
   private Subject<Dictionary<string,SkillSlotWidget>> _sub = new Subject<Dictionary<string,SkillSlotWidget>>(); 
   /// <summary>
   /// 服务端返回角色技能信息
   /// </summary>
   /// <param name="obj"></param>
   public override void ReFreshUI(object obj)
   {
      RepeatedField<RoleSkillInfo> roleSkillInfoList = obj as RepeatedField<RoleSkillInfo>;
      if (roleSkillInfoList != null)
      {
         if (_slots.Count > 0)
         {
            UpdateSlotWidget( roleSkillInfoList);
         }
         else
         {
            //等待技能槽对象生成完成后再更新ui
            _sub.Where(_ => _slots.Count > 0)
               .Subscribe(_ =>
                  {
                     UpdateSlotWidget( roleSkillInfoList);
                  }
               );
         }
         
      }
   }

   private void UpdateSlotWidget(RepeatedField<RoleSkillInfo> roleSkillInfoList)
   {
      for (int i = 0; i < roleSkillInfoList.Count; i++)
      {
         if (roleSkillInfoList[i].Level >0&&
             !string.IsNullOrEmpty(roleSkillInfoList[i].BindKey))
         {
            if (_slots.ContainsKey(roleSkillInfoList[i].BindKey))
            {
               //刷新技能槽对象
               _slots[roleSkillInfoList[i].BindKey].ReFreshUI(roleSkillInfoList[i]);
            }
         }
      }
   }

   /// <summary>
   /// 根据传入数据刷新角色头像、昵称、职业等级及生命/法力值。
   /// </summary>
   /// <param name="obj">角色当前信息。</param>
   public  void UpdateRoleInfo()
   {
      //todo
      MainRoleInfo roleInfo = Global.Instance.mainRoleInfo;

      if (roleInfo != null)
      {
         string HeadPath = "";
         string jobstr = "";
         if (roleInfo.BaseInfo.JobId == 1)
         {
            HeadPath = "Icon/head_jianxiu";
            jobstr = "魔剑士";
         }
         
         ResourceMgr.Instance.LoadSpriteAsync(HeadPath, (Sprite sprite) =>
         {
            if (sprite != null)
            {
               _imgHead.sprite = sprite;
            }
            _texNickName.SetText(roleInfo.BaseInfo.Nickname);
            //设置职业
            _texJobLevel.SetText($"{jobstr} Lv.{roleInfo.BaseInfo.Level}");
            
            //设置角色生命值
            float val = roleInfo.BaseInfo.CurrHp / roleInfo.BaseInfo.MaxHp;
            _sliderHP.value = val;
            _texHp.SetText($"{roleInfo.BaseInfo.CurrHp}/{roleInfo.BaseInfo.MaxHp} {val*100}");
            //法力值
            val = roleInfo.BaseInfo.CurrMp / roleInfo.BaseInfo.MaxMp;
            _sliderMP.value = val;
            _texMp.SetText($"{roleInfo.BaseInfo.CurrMp}/{roleInfo.BaseInfo.MaxMp} {val*100}");
         });
      }
   }

   /// <summary>
   /// 初始化窗口并异步加载技能槽预制体。
   /// </summary>
   public override void InitWindow()
   {
      UpdateRoleInfo();
      Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.PrefabPath}UIPrefabs/SkillSlotWidget").Completed +=
         (AssetOperationHandle handle) =>
         {
            // 按快捷键列表创建对应数量的技能槽。
            for(int i=0;i<_skilKkey.Length;i++ )
            {
               GameObject obj = handle.InstantiateSync();
               if (obj != null)
               {
                  // 将技能槽放入技能栏父节点下。
                  obj.SetParent(_skillSlotParent);
               }
               SkillSlotWidget slot = obj.GetComponent<SkillSlotWidget>();
               if (slot != null)
               {
                  _slots.Add(_skilKkey[i], slot);
                  slot.SetKey(_skilKkey[i]);
               }
            }
            _sub.OnNext(_slots);
         };
   }
}
