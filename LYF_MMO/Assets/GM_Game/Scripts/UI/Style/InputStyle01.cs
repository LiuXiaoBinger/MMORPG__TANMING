using DG.Tweening;
using TMPro;
using UnityEngine;

/**
 * Title:
 * Description:
 */


public class InputStyle01 : MonoBehaviour {

    [SerializeField, Header("占位符")] private TMP_Text _texPlaceholder;
    [SerializeField, Header("动画持续时间")] private float _duretion = 0.1f;
    [SerializeField, Header("移动偏移量")] private float _moveY = 20;
    private float _posY;
    

    private void Start() {

        _posY = _texPlaceholder.rectTransform.anchoredPosition.y;

        TMP_InputField ipt = GetComponent<TMP_InputField>();

        //默认情况下，如果输入框不为空， 则占位符位置默认在上方
        if (!string.IsNullOrEmpty(ipt.text)) {
            //DOTween
            _texPlaceholder.rectTransform.DOAnchorPosY(_posY + _moveY, _duretion);
        }

        //当输入框被选中的时候， 占位符向上移动20
        ipt.onSelect.AddListener((string str) => {

            if (string.IsNullOrEmpty(ipt.text)) {
                _texPlaceholder.rectTransform.DOAnchorPosY(_posY + _moveY, _duretion);
            }
        });

        //当不被选中的时候回到默认位置，
        ipt.onDeselect.AddListener((string str) => {
            if (string.IsNullOrEmpty(ipt.text)) {
                _texPlaceholder.rectTransform.DOAnchorPosY(_posY, _duretion);
            }
        });

    }


}