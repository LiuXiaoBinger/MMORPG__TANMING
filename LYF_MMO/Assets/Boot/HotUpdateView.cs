using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>启动时显示资源更新进度；接收加载流程传来的进度和提示文字，不负责下载。</summary>

public class HotUpdateView : MonoBehaviour
{
    // 预制体中绑定的提示控件；当前脚本没有读写它，实际提示显示在 _texPgrs。
    [SerializeField] private TMP_Text _texTips;
    // 表示更新进度的滑条，传入进度按 0 到 1 使用。
    [SerializeField] private Slider _slider;
    // 显示当前步骤或下载进度的文字。
    [SerializeField] private TMP_Text _texPgrs;
    // 跟随进度移动的装饰光点，位置相对于进度条左端计算。
    [SerializeField] private Image _imgPoint;

    // 启动时记录的进度条宽度，单位为 UI 坐标，用于计算光点位置。
    private float _slideWidth;

    // 界面首次启用时记录进度条宽度，后续刷新直接复用。
    private void Start() {
        // 进度条的 UI 矩形，用它的宽度将进度比例换算成移动距离。
        RectTransform slideTrans = _slider.transform as RectTransform;
        _slideWidth = slideTrans.rect.width;
    }


    /// <summary>
    /// 刷新滑条、阶段文字和光点位置；调用方决定当前进度。
    /// </summary>
    /// <param name="prgs">当前进度比例，0 表示开始，1 表示本步骤完成。</param>
    /// <param name="prgsTex">要显示的步骤说明或下载进度文字。</param>
    public void RefreshUI(float prgs, string prgsTex) {
        _slider.value = prgs;
        _texPgrs.SetText(prgsTex);
        
        _imgPoint.rectTransform.anchoredPosition = new Vector3(_slideWidth * prgs, 0);
    }
}
