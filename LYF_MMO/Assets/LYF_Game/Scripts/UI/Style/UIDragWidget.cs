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

/// <summary>
/// 可拖拽 UI 控件：拖拽期间把自身挂到 Canvas 顶层避免被遮挡，结束后若未落到目标槽位则恢复原始布局。
/// </summary>
public class UIDragWidget : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
   /// <summary>拖拽柄图。</summary>
   private Image _imgDrag;

   //默认父组件
   /// <summary>Start 时记录的父节点；未被其他槽位接收时恢复到该节点。</summary>
   public Transform _defluParent;

   //拖拽类型
   /// <summary>拖拽类型枚举。</summary>
   public DragType _dragType;

   //拖拽Widget 在拖拽的过程中，有可能被其他ui挡住，所以在拖拽中的时候，把它的父组件设置为canvas
   /// <summary>顶层 Canvas，拖拽中把父级切到它上面。</summary>
   private Canvas _canvas;

   /// <summary>拖拽期间关闭 blocksRaycasts，避免自身挡住目标槽位。</summary>
   private CanvasGroup _canvasGroup;

   /// <summary>被拖动控件的矩形和布局数据。</summary>
   private RectTransform _rectTransform;

   // Start 时的 RectTransform 布局数据；取消拖拽时恢复初始布局，并非每次拖拽前重新采样。
   /// <summary>初始化时矩形的最小锚点。</summary>
   private Vector2 _initialAnchorMin;

   /// <summary>初始化时矩形的最大锚点。</summary>
   private Vector2 _initialAnchorMax;

   /// <summary>初始化时的轴心位置，取值按矩形宽高归一化。</summary>
   private Vector2 _initialPivot;

   /// <summary>初始化时相对锚点范围的尺寸差。</summary>
   private Vector2 _initialSizeDelta;

   /// <summary>初始化时相对锚点的三维局部位置。</summary>
   private Vector3 _initialAnchoredPosition3D;

   /// <summary>初始化时相对父节点的缩放。</summary>
   private Vector3 _initialLocalScale;

   /// <summary>初始化时相对父节点的旋转。</summary>
   private Quaternion _initialLocalRotation;

   /// <summary>取得拖拽控件、父节点及根画布，并保存初始布局用于归位。</summary>
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
   /// 开始拖拽时关闭自身射线遮挡并移到根画布下，让底部槽位能接收放下事件。
   /// </summary>
   /// <param name="eventData">Unity 提供的指针数据；移动阶段使用其中的屏幕位置。</param>
   public void OnBeginDrag(PointerEventData eventData)
   {
      _canvasGroup.blocksRaycasts = false;
      gameObject.SetParent(_canvas.transform);
   }

   /// <summary>
   /// 拖拽中把屏幕指针位置换算为画布局部位置，更新拖动对象。
   /// </summary>
   /// <param name="eventData">Unity 提供的指针数据；移动阶段使用其中的屏幕位置。</param>
   public void OnDrag(PointerEventData eventData)
   {
      _rectTransform.anchoredPosition = UIRoot.Instance.ScreenPointToviewPoint(eventData.position);
   }

   /// <summary>
   /// 结束拖拽时恢复射线遮挡；父节点仍是根画布表示未被目标槽位接管，恢复初始布局。
   /// </summary>
   /// <param name="eventData">Unity 提供的指针数据；移动阶段使用其中的屏幕位置。</param>
   public void OnEndDrag(PointerEventData eventData)
   {
      _canvasGroup.blocksRaycasts = true;
      if (transform.parent == _canvas.transform)
      {
         RestoreInitialTransform();
      }
      
   }

   /// <summary>挂回初始父节点，逐项恢复启动时记录的锚点、尺寸、位置、旋转和缩放。</summary>
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
