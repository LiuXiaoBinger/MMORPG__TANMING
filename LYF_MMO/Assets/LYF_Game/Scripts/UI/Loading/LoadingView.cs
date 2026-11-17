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

/// <summary>显示外部传入的加载进度、文字和进度标记位置，本身不执行下载或场景加载。</summary>
public class LoadingView : UIBase
{
    /// <summary>预留加载提示文本，当前 RefreshUI 未使用。</summary>
    [SerializeField] private TMP_Text _texTips;

    /// <summary>显示归一化加载进度的滑条。</summary>
    [SerializeField] private Slider _slider;

    /// <summary>直接显示调用方提供的进度说明或百分比文字。</summary>
    [SerializeField] private TMP_Text _texPgrs;

    /// <summary>沿进度条横向移动的装饰标记。</summary>
    [SerializeField] private Image _imgPoint;

    /// <summary>Start 缓存的进度条矩形宽度，单位为画布局部坐标。</summary>
    private float _slideWidth;

    /// <summary>窗口显示时先清空文字并把进度归零。</summary>
    private void OnEnable()
    {
        RefreshUI(0, "");
    }

    /// <summary>取得滑条宽度后再次重置进度，供标记位置换算。</summary>
    private void Start() {
        // 进度条对应的 UI 矩形，用它的宽度换算标记横坐标。
        RectTransform slideTrans = _slider.transform as RectTransform;
        _slideWidth = slideTrans.rect.width;
        RefreshUI(0, "");
    }

    /// <summary>
    /// 按调用方给定的进度刷新滑条、文字和标记，适用于场景等加载过程。
    /// </summary>
    /// <param name="prgs">归一化进度，调用方通常传 0 到 1；此方法不额外截断。</param>
    /// <param name="prgsTex">直接显示的加载文字。</param>
    public void RefreshUI(float prgs, string prgsTex) {
        _slider.value = prgs;
        _texPgrs.SetText(prgsTex);
        
        _imgPoint.rectTransform.anchoredPosition = new Vector3(_slideWidth * prgs, 0);
    }
}
