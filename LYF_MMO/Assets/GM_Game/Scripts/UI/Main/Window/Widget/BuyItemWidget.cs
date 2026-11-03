using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商城商品卡片，只显示 ShopGoodsViewData 并抛出点击事件。
/// </summary>
public class BuyItemWidget : MonoBehaviour
{
    /// <summary>本商品的异步图标显示控件。</summary>
    [SerializeField, Header("商品图标")] private Image _imgIcon;

    /// <summary>商品名称文本。</summary>
    [SerializeField, Header("商品名称")] private Text _textName;

    /// <summary>当前显示不限购或剩余限购次数的说明文本。</summary>
    [SerializeField, Header("商品详情")] private Text _textDetails;

    /// <summary>支付货币的图标控件。</summary>
    [SerializeField, Header("货币图标")] private Image _priceImgIcon;

    /// <summary>商品单价文本，直接使用展示数据中的价格。</summary>
    [SerializeField, Header("商品价格")] private TMP_Text _textPrice;

    /// <summary>限购状态文本，与详情一起更新剩余次数。</summary>
    [SerializeField, Header("限购数量")] private Text _textNeedNum;

    /// <summary>购买按钮，有限购且剩余次数为零时不可点击。</summary>
    [SerializeField, Header("购买按钮")] private Button _buyButton;

    /// <summary>当前绑定商品，点击时把同一份展示数据交回控制器。</summary>
    private ShopGoodsViewData _viewData;

    /// <summary>当前卡片的点击接收方，重绑时替换，销毁时清空。</summary>
    private Action<ShopGoodsViewData> _clickHandler;

    /// <summary>绑定编号，阻止旧商品或货币图标回填到新商品上。</summary>
    private int _bindingVersion;

    /// <summary>为预制体购买按钮添加本组件的点击转发。</summary>
    private void Awake()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    /// <summary>注销按钮监听并释放对外部点击接收方的引用。</summary>
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
        // 本轮商品与货币两次图标请求共用的绑定编号。
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

        // 根据是否不限购生成统一的数量说明。
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

    /// <summary>先清空 image 图标，再加载 path；只有 bindingVersion 仍属于当前商品时回填。</summary>
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

    /// <summary>把 enabled 应用到购买按钮可交互状态，不执行库存或支付判断。</summary>
    private void SetPurchaseEnabled(bool enabled)
    {
        if (_buyButton != null)
        {
            _buyButton.interactable = enabled;
        }
    }

    /// <summary>把当前商品交给外部点击回调，本卡片不直接发送购买协议。</summary>
    private void OnBuyButtonClicked()
    {
        if (_viewData != null && _clickHandler != null)
        {
            _clickHandler.Invoke(_viewData);
        }
    }

    /// <summary>按 remainingPurchaseCount 更新有限购商品的文字和按钮，不改写缓存商品数据；正式数据刷新仍由控制器提供。</summary>
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
