using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商城商品卡片，只显示 ShopGoodsViewData 并抛出点击事件。
/// </summary>
public class BuyItemWidget : MonoBehaviour
{
    [SerializeField, Header("商品图标")] private Image _imgIcon;
    [SerializeField, Header("商品名称")] private Text _textName;
    [SerializeField, Header("商品详情")] private Text _textDetails;
    [SerializeField, Header("货币图标")] private Image _priceImgIcon;
    [SerializeField, Header("商品价格")] private TMP_Text _textPrice;
    [SerializeField, Header("限购数量")] private Text _textNeedNum;
    [SerializeField, Header("购买按钮")] private Button _buyButton;

    private ShopGoodsViewData _viewData;
    private Action<ShopGoodsViewData> _clickHandler;
    private int _bindingVersion;

    private void Awake()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }
        _clickHandler = null;
    }

    /// <summary>绑定控制器准备的商品展示数据。</summary>
    public void RefreshUI(ShopGoodsViewData viewData, Action<ShopGoodsViewData> clickHandler)
    {
        _viewData = viewData;
        _clickHandler = clickHandler;
        int bindingVersion = ++_bindingVersion;
        if (viewData == null)
        {
            SetPurchaseEnabled(false);
            return;
        }

        if (_textName != null)
        {
            _textName.text = viewData.ProductName;
        }
        if (_textPrice != null)
        {
            _textPrice.SetText(viewData.Price.ToString());
        }

        string limitText = "不限购";
        if (!viewData.IsUnlimitedPurchase)
        {
            limitText = "限购 " + viewData.RemainingPurchaseCount;
        }
        if (_textDetails != null)
        {
            _textDetails.text = limitText;
        }
        if (_textNeedNum != null)
        {
            _textNeedNum.text = limitText;
        }
        SetPurchaseEnabled(viewData.IsUnlimitedPurchase || viewData.RemainingPurchaseCount > 0);
        LoadIcon(_imgIcon, viewData.ProductIconPath, bindingVersion);
        LoadIcon(_priceImgIcon, viewData.CurrencyIconPath, bindingVersion);
    }

    private void LoadIcon(Image image, string path, int bindingVersion)
    {
        if (image == null)
        {
            return;
        }
        image.sprite = null;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        ResourceMgr.Instance.LoadSpriteAsync(path, sprite =>
        {
            if (image != null && sprite != null && bindingVersion == _bindingVersion)
            {
                image.sprite = sprite;
            }
        });
    }

    private void SetPurchaseEnabled(bool enabled)
    {
        if (_buyButton != null)
        {
            _buyButton.interactable = enabled;
        }
    }

    private void OnBuyButtonClicked()
    {
        if (_viewData != null && _clickHandler != null)
        {
            _clickHandler.Invoke(_viewData);
        }
    }

    /// <summary>兼容旧回调入口，正式刷新由商城控制器重新生成展示数据。</summary>
    public void RefreshPurchaseState(uint remainingPurchaseCount)
    {
        if (_viewData == null || _viewData.IsUnlimitedPurchase)
        {
            return;
        }
        if (_textDetails != null)
        {
            _textDetails.text = "限购 " + remainingPurchaseCount;
        }
        if (_textNeedNum != null)
        {
            _textNeedNum.text = remainingPurchaseCount.ToString();
        }
        SetPurchaseEnabled(remainingPurchaseCount > 0);
    }
}
