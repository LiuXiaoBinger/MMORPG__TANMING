using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 商城商品条目。
/// </summary>
public class GoodsItemWidget : MonoBehaviour
{
    [SerializeField, Header("物品图标")] private Image _imgIcon;
    [SerializeField, Header("物品名称")] private TMP_Text _texName;
    [SerializeField, Header("物品价格")] private TMP_Text _texPrice;

    public void RefreshUI(ShopInfo shopInfo, ItemInfo itemInfo, NpcShopItem npcShopItem)
    {
        if (shopInfo == null || itemInfo == null)
        {
            return;
        }

        if (_texName != null)
        {
            _texName.SetText(string.IsNullOrEmpty(itemInfo.Desc) ? $"物品 {itemInfo.ItemTypeId}" : itemInfo.Desc);
        }

        WalletInfo walletInfo = LubanMgr.Instance.GetWalletInfoById(shopInfo.CurrencyType);
        string currencyName = walletInfo == null ? $"货币 {shopInfo.CurrencyType}" : walletInfo.CurrencyName;
        if (_texPrice != null)
        {
            _texPrice.SetText($"{shopInfo.Price} {currencyName}\n{GetLimitText(npcShopItem)}");
        }

        ResourceMgr.Instance.LoadSpriteAsync(itemInfo.Icon, sprite =>
        {
            if (_imgIcon != null && sprite != null)
            {
                _imgIcon.sprite = sprite;
            }
        });
    }

    private static string GetLimitText(NpcShopItem npcShopItem)
    {
        if (npcShopItem == null || npcShopItem.LimitType == ShopLimitType.Unlimited)
        {
            return "不限购";
        }

        string limitType = npcShopItem.LimitType == ShopLimitType.Daily ? "每日限购" :
            npcShopItem.LimitType == ShopLimitType.Permanent ? "永久限购" :
            npcShopItem.LimitType == ShopLimitType.Weekly ? "每周限购" :
            npcShopItem.LimitType == ShopLimitType.Monthly ? "每月限购" :
            npcShopItem.LimitType == ShopLimitType.Yearly ? "每年限购" : "限购";
        return $"{limitType} {npcShopItem.LimitCount}";
    }

    public void OnBuyBtnClicked()
    {
        TipsMgr.Instance.ShowBuyGoodsDialog(_imgIcon, _texName, _texPrice);
    }
}
