using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 旧商城商品条目兼容组件，只负责展示和点击上报。
/// </summary>
public class GoodsItemWidget : MonoBehaviour
{
    [SerializeField, Header("物品图标")] private Image _imgIcon;
    [SerializeField, Header("物品名称")] private TMP_Text _texName;
    [SerializeField, Header("物品价格")] private TMP_Text _texPrice;
    [SerializeField, Header("商店配置编号（旧组件兼容入口）")] private int _shopId;

    private ShopGoodsViewData _viewData;
    private Action<ShopGoodsViewData> _clickHandler;
    private int _bindingVersion;

    public void RefreshUI(ShopGoodsViewData viewData, Action<ShopGoodsViewData> clickHandler)
    {
        _viewData = viewData;
        _clickHandler = clickHandler;
        int bindingVersion = ++_bindingVersion;
        if (viewData == null)
        {
            return;
        }
        _shopId = viewData.ShopId;
        if (_texName != null)
        {
            _texName.text = viewData.ProductName;
        }
        if (_texPrice != null)
        {
            string limitText = "不限购";
            if (!viewData.IsUnlimitedPurchase)
            {
                limitText = "限购 " + viewData.RemainingPurchaseCount;
            }
            _texPrice.text = viewData.Price + " " + viewData.CurrencyName + "\n" + limitText;
        }
        if (_imgIcon != null && !string.IsNullOrEmpty(viewData.ProductIconPath))
        {
            _imgIcon.sprite = null;
            ResourceMgr.Instance.LoadSpriteAsync(viewData.ProductIconPath, sprite =>
            {
                if (_imgIcon != null && sprite != null && bindingVersion == _bindingVersion)
                {
                    _imgIcon.sprite = sprite;
                }
            });
        }
    }

    public void OnBuyBtnClicked()
    {
        if (_viewData != null && _clickHandler != null)
        {
            _clickHandler.Invoke(_viewData);
        }
    }

    public void RefreshPurchaseState(uint remainingPurchaseCount)
    {
        if (_texPrice != null && _viewData != null)
        {
            _texPrice.text = _viewData.Price + " " + _viewData.CurrencyName +
                "\n限购 " + remainingPurchaseCount;
        }
    }

    private void OnDestroy()
    {
        _clickHandler = null;
    }
}
