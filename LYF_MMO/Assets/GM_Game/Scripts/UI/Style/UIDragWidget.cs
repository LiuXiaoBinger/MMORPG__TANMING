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
   // 拖拽前的 RectTransform 数据，用于拖拽取消时恢复原始布局。
   private Vector2 _initialAnchorMin;
   private Vector2 _initialAnchorMax;
   private Vector2 _initialPivot;
   private Vector2 _initialSizeDelta;
   private Vector3 _initialAnchoredPosition3D;
   private Vector3 _initialLocalScale;
   private Quaternion _initialLocalRotation;

   private void Start()
   {
      _rectTransform  =transform as RectTransform;
      _imgDrag = GetComponent<Image>();
      _defluParent = transform.parent;
      _canvas = UIRoot.Instance._canvas;
      _canvasGroup = GetComponent<CanvasGroup>();

      // 记录 Inspector 中的锚点、边距、位置、缩放与旋转数据。
      _initialAnchorMin = _rectTransform.anchorMin;
      _initialAnchorMax = _rectTransform.anchorMax;
      _initialPivot = _rectTransform.pivot;
      _initialSizeDelta = _rectTransform.sizeDelta;
      _initialAnchoredPosition3D = _rectTransform.anchoredPosition3D;
      _initialLocalScale = _rectTransform.localScale;
      _initialLocalRotation = _rectTransform.localRotation;
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
         RestoreInitialTransform();
      }
      
   }

   private void RestoreInitialTransform()
   {
      transform.SetParent(_defluParent, false);
      _rectTransform.anchorMin = _initialAnchorMin;
      _rectTransform.anchorMax = _initialAnchorMax;
      _rectTransform.pivot = _initialPivot;
      _rectTransform.sizeDelta = _initialSizeDelta;
      _rectTransform.anchoredPosition3D = _initialAnchoredPosition3D;
      _rectTransform.localScale = _initialLocalScale;
      _rectTransform.localRotation = _initialLocalRotation;
   }
}
