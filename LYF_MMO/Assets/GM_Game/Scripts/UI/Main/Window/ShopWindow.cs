using System.Collections.Generic;
using cfg;
using MMoRpgCommon;
using UnityEngine;

/// <summary>
/// NPC 商城窗口，根据当前 NPC 的商品配置动态生成商品列表。
/// </summary>
public class ShopWindow : WindowBase
{
    [SerializeField, Header("NPC商店父组件")] private Transform _content;

    private readonly List<GameObject> _goodsObjects = new List<GameObject>();

    public override void ReFreshUI(object obj)
    {
        NpcEntity npcEntity = obj as NpcEntity;
        ClearGoods();
        if (npcEntity == null)
        {
            return;
        }

        if (npcEntity.ShopItemList == null)
        {
            return;
        }

        foreach (NpcShopData npcShopItem in npcEntity.ShopItemList)
        {
            ShopInfo shopInfo = LubanMgr.Instance.GetShopInfoById(npcShopItem.ShopID);
            if (shopInfo == null)
            {
                Debug.LogWarning($"NPC {npcEntity.NpcID} 的商城配置不存在：{npcShopItem.ShopID}", this);
                continue;
            }

            ItemConfigBase itemConfig = LubanMgr.Instance.GetItemConfigById(shopInfo.ItemId);
            if (itemConfig == null)
            {
                Debug.LogWarning($"商城配置 {shopInfo.Id} 的统一物品配置不存在：{shopInfo.ItemId}", this);
                continue;
            }

            AddGoodsSlot(shopInfo, itemConfig, npcShopItem);
        }
    }

    private void AddGoodsSlot(ShopInfo shopInfo, ItemConfigBase itemConfig, NpcShopData npcShopItem)
    {
        ResourceMgr.Instance.LoadPrefabAsync("Assets/artres/Resources/UI/Prefabs/Item/BuyItemPrefab", obj =>
        {
            if (obj == null || _content == null)
            {
                return;
            }

            obj.transform.SetParent(_content, false);
            _goodsObjects.Add(obj);
            BuyItemWidget widget = obj.GetComponent<BuyItemWidget>();
            if (widget != null)
            {
                widget.RefreshUI(shopInfo, itemConfig, npcShopItem);
            }
        });
    }

    private void ClearGoods()
    {
        foreach (GameObject goodsObject in _goodsObjects)
        {
            if (goodsObject != null)
            {
                Destroy(goodsObject);
            }
        }

        _goodsObjects.Clear();
    }
}
