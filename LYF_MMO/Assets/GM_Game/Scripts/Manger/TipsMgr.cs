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
   #region 系统提示

   /// <summary>
   /// 显示短时间自动消失的系统消息。
   /// </summary>
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

   #endregion

   #region 购买确认提示

   // 购买弹窗异步加载期间也要上锁，避免同一帧的连续点击创建多个实例。
   private bool _isBuyGoodsDialogLoading;
   private BuyDialog _buyGoodsDialog;

   /// <summary>
   /// 显示商品购买确认弹窗；同一时间只允许存在一个。
   /// </summary>
   public void ShowBuyGoodsDialog(Image imgIcon, TMP_Text texName, TMP_Text texPrice)
   {
      if (_isBuyGoodsDialogLoading || _buyGoodsDialog != null)
      {
         return;
      }

      _isBuyGoodsDialogLoading = true;
      ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/TipsDiialog/BuyDialog", (GameObject go) =>
      {
         _isBuyGoodsDialogLoading = false;
         if(go == null){return;}

         // 异步回调前弹窗可能已由其他流程创建，丢弃这次多余实例。
         if (_buyGoodsDialog != null)
         {
            GameObject.Destroy(go);
            return;
         }

         GameObject canvas = GameObject.Find("Canvas");
         if (canvas == null)
         {
            Debug.LogError("未找到 Canvas，无法显示购买弹窗。");
            GameObject.Destroy(go);
            return;
         }

         BuyDialog dialog = go.GetComponent<BuyDialog>();
         if (dialog == null)
         {
            Debug.LogError("BuyDialog 预制体缺少 BuyDialog 组件。");
            GameObject.Destroy(go);
            return;
         }

         go.SetParent(canvas.transform);
         _buyGoodsDialog = dialog;
         dialog.Initialize(OnBuyGoodsDialogClosed);
      });
   }

   /// <summary>
   /// 购买弹窗关闭或被销毁后解除锁定，允许再次选择商品。
   /// </summary>
   private void OnBuyGoodsDialogClosed(BuyDialog dialog)
   {
      if (_buyGoodsDialog == dialog)
      {
         _buyGoodsDialog = null;
      }
   }

   #endregion

   #region 物品信息提示

   // 当前显示中的物品详情提示，可同时存在多个。
   private List<GameObject> _itemsTips = new List<GameObject>();

   /// <summary>
   /// 在鼠标位置附近显示物品详情提示。
   /// </summary>
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
         foreach (GameObject itemTip in _itemsTips)
         {
            GameObject.Destroy(itemTip);
         }

         _itemsTips.Clear();
      }
   }

   #endregion
  
}
