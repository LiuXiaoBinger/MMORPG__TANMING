using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:管理所有tips
* Descrpiton:
*/

public class TipsMgr :Singleton<TipsMgr>
{
   public void ShowSystemTips(string msg)
   {
      ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/TipsDiialog/SystemTips", (GameObject go) =>
      {
         if(go == null){return;}
         
         go.transform.SetParent(GameObject.Find("Canvas").transform);
         go.transform.localPosition = new Vector2(0, 160);
         go.transform.localScale = Vector3.one;
         SystemTips tips =go.GetComponent<SystemTips>();
         if (tips != null)
         {
            tips.RefreshUI(msg);
         }
      });
   }
   
   
   public void ShowBuyGoodsDialog(Image imgIcon, TMP_Text texName, TMP_Text texPrice)
   {
      ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/TipsDiialog/BuyDialog", (GameObject go) =>
      {
         if(go == null){return;}
         
         go.SetParent(GameObject.Find("Canvas").transform);
        
      });
   }

   #region 显示物品的tips
   private List<GameObject> _itemsTips = new List<GameObject>();
   public void ShowItmeTips(Vector3 mousePos, Image imgIcon =null, TMP_Text texName= null, TMP_Text texPrice= null)
   {
      ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/TipsDiialog/EquipItemTips", (GameObject go) =>
      {
         if(go == null){return;}
         _itemsTips.Add(go);
         go.SetParent(GameObject.Find("Canvas").transform);
         var rectTransform = go.transform as RectTransform;
         Vector3 mosuepos_word=UIRoot.Instance.ScreenPointToviewPoint(mousePos);
         mosuepos_word.y -= rectTransform.rect.height / 2;
         mosuepos_word.x += rectTransform.rect.width / 2;
         rectTransform.anchoredPosition = mosuepos_word;
      });
   }

   public void CloseItemTips()
   {
      if (_itemsTips.Count > 0)
      {
         for (int i = 0; i < _itemsTips.Count; i++)
         {
            GameObject.Destroy(_itemsTips[i]);
            _itemsTips.RemoveAt(i);
         }
      }
   }
   

   #endregion
  
}
