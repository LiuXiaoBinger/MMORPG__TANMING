using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/**
* Title:
* Descrpiton:
*/

public class ShopWindow : WindowBase
{
   [SerializeField, Header("NPC商店父组件")] private Transform _content;

   private void Start()
   {
      AddGoodsSlot();
   }
   private void AddGoodsSlot()
   {
      //test 
      Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.PrefabPath}UIPrefabs/GoodsItemWidget").Completed +=
         (AssetOperationHandle handle) =>
         {
            for (int i = 0; i < 20; i++)
            {
               GameObject obj = handle.InstantiateSync();
               if (obj != null)
               {
                  // 将技能槽放入技能栏父节点下。
                  obj.SetParent(_content);
                 
               }
               GoodsItemWidget slot = obj.GetComponent<GoodsItemWidget>();
               if (slot != null)
               {
                  
                     slot.RefreshUI();
                        
               }
            }
         };
   }
}
