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

    /// <summary>生成的商品卡片挂到此节点下，保持预制体的 UI 布局坐标。</summary>
    [SerializeField, Header("NPC商店父组件")] private Transform _content;

    /// <summary>商品点击事件，由商城控制器处理。</summary>
    public event Action<ShopGoodsViewData> GoodsClicked;

    /// <summary>本窗口已创建的商品实例，下一次列表刷新前统一销毁。</summary>
    private readonly List<GameObject> _goodsObjects = new List<GameObject>();

    /// <summary>商品列表刷新编号，防止旧加载回调把过期商品插入新列表。</summary>
    private int _listVersion;

    /// <summary>窗口激活时通知商城控制器检查角色次数变化。</summary>
    private void Update()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        // 取当前订阅快照，交由控制器处理购买次数等状态变化。
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

    /// <summary>先清空旧商品，再把 obj 中的商品展示列表逐项异步创建；数据类型不匹配时保持空列表。</summary>
    public override void ReFreshUI(object obj)
    {
        ClearGoods();
        _listVersion++;
        // 本轮商城商品数据，包含显示与点击时要交还控制器的信息。
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

    /// <summary>为 goods 创建商品卡片；listVersion 不是当前刷新编号时销毁迟到实例。</summary>
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
            // 商品卡片负责显示并把点击数据交还本窗口。
            BuyItemWidget widget = itemObject.GetComponent<BuyItemWidget>();
            if (widget != null)
            {
                widget.RefreshUI(goods, OnGoodsClicked);
            }
        });
    }

    /// <summary>把卡片的 goods 商品数据通知商城控制器，窗口不直接执行购买。</summary>
    private void OnGoodsClicked(ShopGoodsViewData goods)
    {
        if (GoodsClicked != null)
        {
            GoodsClicked.Invoke(goods);
        }
    }

    /// <summary>销毁已生成商品并清空实例列表，不修改商店数据。</summary>
    private void ClearGoods()
    {
        for (int index = 0; index < _goodsObjects.Count; index++)
        {
            // 上次列表创建的某个卡片实例。
            GameObject goodsObject = _goodsObjects[index];
            if (goodsObject != null)
            {
                Destroy(goodsObject);
            }
        }
        _goodsObjects.Clear();
    }
}
