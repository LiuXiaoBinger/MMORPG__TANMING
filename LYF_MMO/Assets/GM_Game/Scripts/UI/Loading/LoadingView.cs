using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class LoadingView : UIBase
{
    [SerializeField] private TMP_Text _texTips;
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _texPgrs;
    [SerializeField] private Image _imgPoint;

    private float _slideWidth;

    private void Start() {
        RectTransform slideTrans = _slider.transform as RectTransform;
        _slideWidth = slideTrans.rect.width;
        RefreshUI(0, "");
    }

    private void OnEnable()
    {
        RefreshUI(0, "");
    }


    /// <summary>
    /// 更新热更新UI
    /// </summary>
    /// <param name="prgs"></param>
    /// <param name="prgsTex"></param>
    public void RefreshUI(float prgs, string prgsTex) {
        _slider.value = prgs;
        _texPgrs.SetText(prgsTex);
        
        _imgPoint.rectTransform.anchoredPosition = new Vector3(_slideWidth * prgs, 0);
    }
}
