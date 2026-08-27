
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

public class SystemTips : MonoBehaviour
{
   [SerializeField, Header("提示文本")] private TMP_Text _texMsg;
   [SerializeField, Header("颜色曲线")] private AnimationCurve _colorCurve;
   [SerializeField, Header("颜色曲线")] private AnimationCurve _moveCurve;


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
