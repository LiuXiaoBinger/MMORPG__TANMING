using System;
using cfg;
using MMoRpgCommon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BuyItemPrefab 的商城商品绑定。
/// 该预制体来自 artres 资源，不依赖 GoodsItemWidget。
/// </summary>
public class BuyItemWidget : MonoBehaviour
{
    [SerializeField, Header("商品图标")] private Image _imgIcon;
    [SerializeField, Header("商品名称")] private Text _textName;
    [SerializeField, Header("商品详情")] private Text _textDetails;
    [SerializeField, Header("商品价格")] private TMP_Text _textPrice;
    [SerializeField, Header("限购数量")] private Text _textNeedNum;
    [SerializeField, Header("购买按钮")] private Button _buyButton;

    private ShopInfo _shopInfo;
    private ItemConfigBase _itemConfig;
    private NpcShopData _npcShopItem;

    /// <summary>
    /// 注册购买按钮点击事件。
    /// </summary>
    private void Awake()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    /// <summary>
    /// 移除购买按钮事件，防止对象销毁后保留无效引用。
    /// </summary>
    private void OnDestroy()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }
    }

    /// <summary>
    /// 绑定商店配置、商品配置和 NPC 限购数据，并刷新商品卡片。
    /// </summary>
    /// <param name="shopInfo">商品价格和货币类型配置。</param>
    /// <param name="itemConfig">物品或装备的名称、图标等基础配置。</param>
    /// <param name="npcShopItem">NPC 商店中的限购规则和数量。</param>
    public void RefreshUI(ShopInfo shopInfo, ItemConfigBase itemConfig, NpcShopData npcShopItem)
    {
        _shopInfo = shopInfo;
        _itemConfig = itemConfig;
        _npcShopItem = npcShopItem;

        if (_shopInfo == null || _itemConfig == null)
        {
            return;
        }

        RefreshDisplay(_itemConfig.ItemName, _itemConfig.Icon);
    }

    /// <summary>
    /// 刷新商品名称、限购信息、价格和图标显示。
    /// </summary>
    /// <param name="productName">显示的商品名称。</param>
    /// <param name="iconPath">商品图标资源路径。</param>
    private void RefreshDisplay(string productName, string iconPath)
    {

        if (_textName != null)
        {
            _textName.text = productName;
        }

        if (_textDetails != null)
        {
            _textDetails.text = GetLimitText(_npcShopItem);
        }

        WalletInfo walletInfo = LubanMgr.Instance.GetWalletInfoById(_shopInfo.CurrencyType);
        string currencyName = walletInfo == null
            ? $"货币 {_shopInfo.CurrencyType}"
            : walletInfo.CurrencyName;

        if (_textPrice != null)
        {
            _textPrice.SetText($"{_shopInfo.Price} {currencyName}");
        }

        if (_textNeedNum != null)
        {
            _textNeedNum.text = GetLimitCountText(_npcShopItem);
        }

        if (!string.IsNullOrEmpty(iconPath))
        {
            ResourceMgr.Instance.LoadSpriteAsync(iconPath, sprite =>
            {
                if (_imgIcon != null && sprite != null)
                {
                    _imgIcon.sprite = sprite;
                }
            });
        }
    }

    /// <summary>
    /// 打开购买确认弹窗。
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (_shopInfo == null || _itemConfig == null)
        {
            return;
        }

        // 现有购买弹窗接口使用 TMP 价格文本；名称文本在 BuyDialog 中不是必需字段。
        TipsMgr.Instance.ShowBuyGoodsDialog(_imgIcon, null, _textPrice);
    }

    /// <summary>
    /// 将限购类型和数量格式化为商品详情文本。
    /// </summary>
    /// <param name="npcShopItem">NPC 商店商品数据。</param>
    /// <returns>例如“每日限购 3”或“不限购”。</returns>
    private static string GetLimitText(NpcShopData npcShopItem)
    {
        if (npcShopItem == null || (ShopLimitType)npcShopItem.LimitType == ShopLimitType.Unlimited)
        {
            return "不限购";
        }

        ShopLimitType limitTypeValue = (ShopLimitType)npcShopItem.LimitType;
        string limitType = limitTypeValue == ShopLimitType.Daily ? "每日限购" :
            limitTypeValue == ShopLimitType.Permanent ? "永久限购" :
            limitTypeValue == ShopLimitType.Weekly ? "每周限购" :
            limitTypeValue == ShopLimitType.Monthly ? "每月限购" :
            limitTypeValue == ShopLimitType.Yearly ? "每年限购" : "限购";

        return $"{limitType} {npcShopItem.LimitCount}";
    }

    /// <summary>
    /// 获取限购数量的独立显示文本。
    /// </summary>
    /// <param name="npcShopItem">NPC 商店商品数据。</param>
    /// <returns>限购数量；不限购时为空字符串。</returns>
    private static string GetLimitCountText(NpcShopData npcShopItem)
    {
        if (npcShopItem == null || (ShopLimitType)npcShopItem.LimitType == ShopLimitType.Unlimited)
        {
            return string.Empty;
        }

        return $"{npcShopItem.LimitCount}";
    }
}
