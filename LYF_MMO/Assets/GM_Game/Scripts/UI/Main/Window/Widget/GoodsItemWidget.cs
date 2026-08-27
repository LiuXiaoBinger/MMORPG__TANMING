using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class GoodsItemWidget : MonoBehaviour
{
    [SerializeField, Header("物品图标")] private Image _imgIcon;
    [SerializeField, Header("物品名称")] private TMP_Text _texName;
    [SerializeField, Header("物品价格")] private TMP_Text _texPrice;

    public void RefreshUI()
    {

    }

    public void OnBuyBtnClicked()
    {
        TipsMgr.Instance.ShowBuyGoodsDialog(_imgIcon,_texName,_texPrice);
    }
}
