using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/**
* Title:按钮的特效和动效
* Descrpiton:
*/

/// <summary>
/// 按钮动效控件：按下时缩小、松开回弹，鼠标进入/离开时显示或隐藏按钮专属特效对象。
/// </summary>
public class ButtonStyle01 : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>按钮默认缩放。</summary>
    [SerializeField, Header("按钮的默认缩放")] private float _btnDefaultScale = 1 ;
    /// <summary>按钮按下时的缩放。</summary>
    [SerializeField, Header("按钮按下时的缩放")] private float _btnDownScale = 0.85f;
    /// <summary>按钮悬停时显示的特效对象。</summary>
    [SerializeField, Header("UI特效对象")] private GameObject _effectGo;
    /// <summary>按下时用 0.05 秒缩放到按压尺寸，eventData 不参与计算。</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(_btnDownScale, 0.05f);
    }

    /// <summary>抬起时用 0.05 秒恢复默认尺寸。</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(_btnDefaultScale, 0.05f);
    }

    /// <summary>鼠标进入时显示已配置的按钮特效。</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_effectGo != null)
        {
            _effectGo.Show();
        }
    }

    /// <summary>鼠标离开时隐藏按钮特效。</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if(_effectGo != null)
        {
            _effectGo.Show(false);
        }
    }
}
