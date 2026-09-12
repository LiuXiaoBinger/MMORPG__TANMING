using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 商城窗口，只负责按展示数据生成商品卡片。
/// </summary>
public class NpcShopWindow : WindowBase
{
    /// <summary>窗口激活期间的帧事件，由商城控制器消费次数脏标记。</summary>
    public event Action FrameUpdated;
    [SerializeField, Header("NPC商店父组件")] private Transform _content;

    /// <summary>商品点击事件，由商城控制器处理。</summary>
    public event Action<ShopGoodsViewData> GoodsClicked;

    private readonly List<GameObject> _goodsObjects = new List<GameObject>();
    private int _listVersion;

    public override void ReFreshUI(object obj)
    {
        ClearGoods();
        _listVersion++;
        List<ShopGoodsViewData> goodsList = obj as List<ShopGoodsViewData>;
        if (goodsList == null)
        {
            return;
        }

        for (int index = 0; index < goodsList.Count; index++)
        {
            AddGoodsSlot(goodsList[index], _listVersion);
        }
    }

    private void AddGoodsSlot(ShopGoodsViewData goods, int listVersion)
    {
        ResourceMgr.Instance.LoadPrefabAsync(
            "Assets/artres/Resources/UI/Prefabs/Item/BuyItemPrefab", itemObject =>
        {
            if (listVersion != _listVersion)
            {
                if (itemObject != null)
                {
                    Destroy(itemObject);
                }
                return;
            }
            if (itemObject == null || _content == null)
            {
                return;
            }

            itemObject.transform.SetParent(_content, false);
            _goodsObjects.Add(itemObject);
            BuyItemWidget widget = itemObject.GetComponent<BuyItemWidget>();
            if (widget != null)
            {
                widget.RefreshUI(goods, OnGoodsClicked);
            }
        });
    }

    private void OnGoodsClicked(ShopGoodsViewData goods)
    {
        if (GoodsClicked != null)
        {
            GoodsClicked.Invoke(goods);
        }
    }

    private void ClearGoods()
    {
        for (int index = 0; index < _goodsObjects.Count; index++)
        {
            GameObject goodsObject = _goodsObjects[index];
            if (goodsObject != null)
            {
                Destroy(goodsObject);
            }
        }
        _goodsObjects.Clear();
    }

    /// <summary>窗口激活时通知商城控制器检查角色次数变化。</summary>
    private void Update()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        Action handler = FrameUpdated;
        if (handler != null)
        {
            handler.Invoke();
        }
    }

    /// <summary>销毁窗口时清理事件引用。</summary>
    private void OnDestroy()
    {
        FrameUpdated = null;
        GoodsClicked = null;
    }
}
