using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using TMPro;
using UnityEngine;
using YooAsset;

/**
* Title:
* Descrpiton:
*/

public class SkillInfoWindow : WindowBase
{
   [SerializeField, Header("技能列表父组件")] private Transform _content;

   [SerializeField, Header("职业")] private TMP_Text _texJob;
   [SerializeField,Header("技能升级点")] private TMP_Text _texPoint;


   private void Start()
   {
      
   }
   public  override void ReFreshUI(object obj)
   {
      MainRoleInfo roleInfo = Global.Instance.mainRoleInfo;
      if (roleInfo != null)
      {
         _texJob.text = ConstDefine.RoleJobName[roleInfo.BaseInfo.JobId];
         _texPoint.text = roleInfo.BaseInfo.Level.ToString();
      }
      //根据服务端返回所学技能信息 更新ui
      //todo
      RepeatedField<RoleSkillInfo> roleSkillInfoList = obj as RepeatedField<RoleSkillInfo>;
      if (roleSkillInfoList != null)
      {
         Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.PrefabPath}UIPrefabs/SkillItemWidget").Completed +=
            (AssetOperationHandle handle) =>
            {
               for (int i = 0; i < roleSkillInfoList.Count; i++)
               {
                  GameObject obj = handle.InstantiateSync();
                  if (obj != null)
                  {
                     // 将技能槽放入技能栏父节点下。
                     obj.SetParent(_content);
                  }
                  SkillItemWidget slot = obj.GetComponent<SkillItemWidget>();
                  if (slot != null)
                  {
                     slot.RefreshUI(roleSkillInfoList[i]);
                  }
               }
            };
      }
   }
}
