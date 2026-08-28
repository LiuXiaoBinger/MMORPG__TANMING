using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class KnapsackSlotWidget : MonoBehaviour,
    IPointerEnterHandler,IPointerExitHandler,
    IPointerDownHandler,IPointerUpHandler,IDropHandler
{
   [SerializeField,Header("物品图标")]private Image _imgIcon;
   [SerializeField,Header("物品数量")]private TMP_Text _texCount;
   
   [SerializeField,Header("鼠标进入效果")]private Image _imgEnter;
   [SerializeField,Header("物品特效")]private Image _imgFx;
   public int _count;
   public string _spriteName;
   public void RefreshUI(RoleKanpsackSlot roleKanpsackSlot)
   {
       if (roleKanpsackSlot != null)
       {
           _count = roleKanpsackSlot.Count;
           //_spriteName = spriteName;
           if (_count > 0)
           {
               _imgIcon.gameObject.Show();
               _texCount.gameObject.Show();
               _texCount.SetText($"{_count}");
           
               //todo 配置物品表
               /*ResourceMgr.Instance.LoadSpriteAsync($"Icon/Item/{spriteName}",(Sprite sprite )=>
               {
                   _imgIcon.sprite = sprite;
               });*/
           }
           else
           {
               _imgIcon.gameObject.Show(false);
               _texCount.gameObject.Show(false);
           }
       }
       
      
       
   }
   /// <summary>
   /// 鼠标进入
   /// </summary>
   /// <param name="eventData"></param>
   public void OnPointerEnter(PointerEventData eventData)
   {
     _imgEnter.gameObject.Show();
     if (_count > 0)
     {
         TipsMgr.Instance.ShowItmeTips(eventData.position);
     }
   }
   /// <summary>
   /// 鼠标离开
   /// </summary>
   /// <param name="eventData"></param>
   public void OnPointerExit(PointerEventData eventData)
   {
       _imgEnter.gameObject.Show(false);
       TipsMgr.Instance.CloseItemTips();
   }
    
   public void OnPointerDown(PointerEventData eventData)
   {
      
   }

   public void OnPointerUp(PointerEventData eventData)
   {
      
   }

   public void OnDrop(PointerEventData eventData)
   {
       UIDragWidget uiDragWidget = eventData.pointerDrag.GetComponent<UIDragWidget>();
       if (uiDragWidget != null)
       {
           if (uiDragWidget._dragType == DragType.KanpsackSlot)
           {
              //提交给服务端
           }
       }
   }
}
