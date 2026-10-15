
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
/// 单次弹出的系统飘字提示：用 DOTween 做红色渐隐与向上漂浮，3 秒后自动销毁实例。
/// </summary>
public class SystemTips : MonoBehaviour
{
   /// <summary>提示文本控件。</summary>
   [SerializeField, Header("提示文本")] private TMP_Text _texMsg;
   /// <summary>文本颜色渐隐曲线。</summary>
   [SerializeField, Header("颜色曲线")] private AnimationCurve _colorCurve;
   /// <summary>向上漂浮的位移曲线。</summary>
   [SerializeField, Header("颜色曲线")] private AnimationCurve _moveCurve;


   /// <summary>用指定文本刷新飘字并播放渐隐与漂浮动画。</summary>
   /// <param name="msg">提示内容。</param>
   public void RefreshUI(string msg)
   {
      _texMsg.text = msg;
      _texMsg.DOColor(Color.red, 2).SetEase(_colorCurve);
      
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
