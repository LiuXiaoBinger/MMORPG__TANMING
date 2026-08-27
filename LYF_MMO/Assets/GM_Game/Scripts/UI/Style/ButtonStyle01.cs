using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/**
* Title:按钮的特效和动效
* Descrpiton:
*/

public class ButtonStyle01 : MonoBehaviour, 
    IPointerDownHandler, IPointerUpHandler, 
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Header("按钮的默认缩放")] private float _btnDefaultScale = 1 ;
    [SerializeField, Header("按钮按下时的缩放")] private float _btnDownScale = 0.85f;
    [SerializeField, Header("UI特效对象")] private GameObject _effectGo;
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(_btnDownScale, 0.05f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(_btnDefaultScale, 0.05f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_effectGo != null)
        {
            _effectGo.Show();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(_effectGo != null)
        {
            _effectGo.Show(false);
        }
    }
}
