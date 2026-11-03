using DG.Tweening;
using TMPro;
using UnityEngine;

/**
 * Title:
 * Description:
 */


/// <summary>输入框占位文字动画：有内容时保持上移，空输入框按焦点获得或失去上下移动。</summary>
public class InputStyle01 : MonoBehaviour {
    /// <summary>被上下移动的占位提示文字。</summary>
    [SerializeField, Header("占位符")] private TMP_Text _texPlaceholder;

    /// <summary>每次上下移动的持续秒数。</summary>
    [SerializeField, Header("动画持续时间")] private float _duretion = 0.1f;

    /// <summary>相对初始位置的上移量，单位为 UI 局部坐标。</summary>
    [SerializeField, Header("移动偏移量")] private float _moveY = 20;

    /// <summary>Start 缓存的占位文字初始 Y 坐标，失焦且输入为空时恢复。</summary>
    private float _posY;

    /// <summary>记下占位文字原位置，按初始内容调整，再注册焦点变化动画。</summary>
    private void Start() {

        _posY = _texPlaceholder.rectTransform.anchoredPosition.y;

        // 同一对象上的输入框，监听它的选择和取消选择事件。
        TMP_InputField ipt = GetComponent<TMP_InputField>();

        //默认情况下，如果输入框不为空， 则占位符位置默认在上方
        if (!string.IsNullOrEmpty(ipt.text)) {
            //DOTween
            _texPlaceholder.rectTransform.DOAnchorPosY(_posY + _moveY, _duretion);
        }

        // 输入为空时获得焦点，将占位文字上移配置的偏移量；已有内容时保持原状态。
        ipt.onSelect.AddListener((string str) => {

            if (string.IsNullOrEmpty(ipt.text)) {
                _texPlaceholder.rectTransform.DOAnchorPosY(_posY + _moveY, _duretion);
            }
        });

        // 只有失焦后仍为空才回到起点，有内容则继续停在上方。
        ipt.onDeselect.AddListener((string str) => {
            if (string.IsNullOrEmpty(ipt.text)) {
                _texPlaceholder.rectTransform.DOAnchorPosY(_posY, _duretion);
            }
        });

    }
}