using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>展示原魔神塔20002黑幕，协议筛选与播放时序由TeamCtrl负责。</summary>
public sealed class MoshentaBlackCurtainWindow : WindowBase
{
    /// <summary>正式预制体中的全屏黑底，沿现有MainView画布显示。</summary>
    [SerializeField] private Image _background;
    /// <summary>原五行剧情文字，使用项目已有TMP中文字体。</summary>
    [SerializeField] private TMP_Text[] _lines;

    /// <summary>展示控制器已计算的本帧透明度，不自行调度业务。</summary>
    /// <param name="backgroundAlpha">黑底透明度，范围0至1。</param>
    /// <param name="texts">按原Content顺序排列的五行原文。</param>
    /// <param name="alphas">与五行对应的当前显示透明度。</param>
    public void Display(float backgroundAlpha, string[] texts, float[] alphas)
    {
        _background.color = new Color(0f, 0f, 0f, backgroundAlpha);
        for (int index = 0; index < _lines.Length; index++)
        {
            _lines[index].text = texts[index];
            _lines[index].alpha = alphas[index];
        }
    }
}
