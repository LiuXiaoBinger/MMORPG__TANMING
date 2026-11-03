
using DG.Tweening;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


/**
* Title:系统提示框
* Descrpiton:
*/

/// <summary>
/// 单次系统飘字：在 2 秒内按配置曲线变为红色并向上移动，3 秒后销毁实例。
/// 颜色是否呈现透明变化由起始颜色和曲线决定，这里没有单独执行淡出到透明。
/// </summary>
public class SystemTips : MonoBehaviour
{
   /// <summary>提示文本控件。</summary>
   [SerializeField, Header("提示文本")] private TMP_Text _texMsg;
   /// <summary>从当前颜色变到 Color.red 时使用的插值曲线。</summary>
   [SerializeField, Header("颜色曲线")] private AnimationCurve _colorCurve;
   /// <summary>向上漂浮的位移曲线。</summary>
   [SerializeField, Header("颜色曲线")] private AnimationCurve _moveCurve;


   /// <summary>设置提示文字，同时播放变红和向上漂浮动画，再安排延迟销毁。</summary>
   /// <param name="msg">提示内容。</param>
   public void RefreshUI(string msg)
   {
      _texMsg.text = msg;
      _texMsg.DOColor(Color.red, 2).SetEase(_colorCurve);
      
      // 提示整体的 UI 矩形，本次随机向上移动 200 到 260 个局部坐标单位。
      RectTransform rectTransform = transform as RectTransform;
      rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y
                                 +Random.Range(200,260),2).SetEase(_moveCurve);
      
      //定时销毁
      Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(V =>
      {
          Destroy(gameObject);
      });
   }
   
}
