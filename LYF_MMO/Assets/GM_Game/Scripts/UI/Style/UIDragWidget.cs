using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class UIDragWidget : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
   private Image _imgDrag;
   //默认父组件
   public Transform _defluParent;
   //拖拽类型
   public DragType _dragType;
   //拖拽Widget 在拖拽的过程中，有可能被其他ui挡住，所以在拖拽中的时候，把它的父组件设置为canvas
   private Canvas _canvas;

   private CanvasGroup _canvasGroup;
   private RectTransform _rectTransform;
   private void Start()
   {
      _rectTransform  =transform as RectTransform;
      _imgDrag = GetComponent<Image>();
      _defluParent = transform.parent;
      _canvas = UIRoot.Instance._canvas;
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   /// <summary>
   /// 开始拖拽
   /// </summary>
   /// <param name="eventData"></param>
   public void OnBeginDrag(PointerEventData eventData)
   {
      _canvasGroup.blocksRaycasts = false;
      gameObject.SetParent(_canvas.transform);
   }
   /// <summary>
   /// 拖拽中
   /// </summary>
   /// <param name="eventData"></param>
   public void OnDrag(PointerEventData eventData)
   {
      _rectTransform.anchoredPosition = UIRoot.Instance.ScreenPointToviewPoint(eventData.position);
   }
   /// <summary>
   /// 拖拽结束
   /// </summary>
   /// <param name="eventData"></param>
   public void OnEndDrag(PointerEventData eventData)
   {
      _canvasGroup.blocksRaycasts = true;
      if (transform.parent == _canvas.transform)
      {
        
         gameObject.SetParent(_defluParent);
      }
      
   }
}
