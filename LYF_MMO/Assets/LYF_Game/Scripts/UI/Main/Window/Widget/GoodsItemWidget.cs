using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 旧商城商品条目兼容组件，只负责展示和点击上报。
/// </summary>
public class GoodsItemWidget : MonoBehaviour
{
    /// <summary>旧商品条目使用的图标控件。</summary>
    [SerializeField, Header("物品图标")] private Image _imgIcon;

    /// <summary>当前商品名称文本。</summary>
    [SerializeField, Header("物品名称")] private TMP_Text _texName;

    /// <summary>合并显示价格、货币名称和限购次数的文本。</summary>
    [SerializeField, Header("物品价格")] private TMP_Text _texPrice;

    /// <summary>绑定数据中的商店配置编号；点击传出完整展示数据而非单独使用此字段。</summary>
    [SerializeField, Header("商店配置编号（旧组件兼容入口）")] private int _shopId;

    /// <summary>当前商品摘要，供按钮点击及限购刷新使用。</summary>
    private ShopGoodsViewData _viewData;

    /// <summary>接收商品点击的控制器回调。</summary>
    private Action<ShopGoodsViewData> _clickHandler;

    /// <summary>图标绑定编号，重绑商品后忽略旧请求。</summary>
    private int _bindingVersion;

    /// <summary>清空外部点击回调引用。</summary>
    private void OnDestroy()
    {
        _clickHandler = null;
    }

    /// <summary>显示 viewData 名称、单价及限购，并把点击交给 clickHandler。</summary>
    public void RefreshUI(ShopGoodsViewData viewData, Action<ShopGoodsViewData> clickHandler)
    {
        _viewData = viewData;
        _clickHandler = clickHandler;
        // 本次商品图标请求所属的绑定编号。
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
            // 当前限购状态的文字，和价格一起排版。
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

    /// <summary>将当前商品交给点击接收方，不在条目内购买。</summary>
    public void OnBuyBtnClicked()
    {
        if (_viewData != null && _clickHandler != null)
        {
            _clickHandler.Invoke(_viewData);
        }
    }

    /// <summary>只用 remainingPurchaseCount 更新限购文本，不修改商品数据或按钮状态。</summary>
    public void RefreshPurchaseState(uint remainingPurchaseCount)
    {
        if (_texPrice != null && _viewData != null)
        {
            _texPrice.text = _viewData.Price + " " + _viewData.CurrencyName +
                "\n限购 " + remainingPurchaseCount;
        }
    }
}
