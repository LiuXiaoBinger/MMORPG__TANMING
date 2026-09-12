using System.Collections.Generic;
using cfg;
using MMoRpgCommon;
using UnityEngine;

/// <summary>
/// 商城控制器，负责配置查询、购买流程和界面刷新。
/// </summary>
public sealed class ShopCtrl : CtrlBase
{
    /// <summary>商城窗口视图。</summary>
    private readonly NpcShopWindow _shopWindow;
    /// <summary>当前正在浏览商店的 NPC。</summary>
    private NpcEntity _currentNpc;

    /// <summary>创建商城控制器并注册视图和模型事件。</summary>
    public ShopCtrl(MainView mainView) : base(mainView)
    {
        _shopWindow = mainView.ShopWindow;
        if (_shopWindow != null)
        {
            _shopWindow.GoodsClicked += OnGoodsClicked;
            _shopWindow.FrameUpdated += OnShopWindowFrameUpdated;
        }
        ShopMgr.Instance.PurchaseCompleted += OnPurchaseCompleted;
    }

    /// <summary>商城窗口帧循环消费次数模块脏标记。</summary>
    private void OnShopWindowFrameUpdated()
    {
        if (_shopWindow == null || !_shopWindow.isActiveAndEnabled ||
            Global.Instance == null || Global.Instance.RoleWorld == null)
        {
            return;
        }
        ClientRole role = Global.Instance.RoleWorld.LocalRole;
        if (role == null || role.DirtyTracker == null)
        {
            return;
        }
        ClientDirtySnapshot snapshot;
        if (!role.DirtyTracker.TryGet(ClientDirtyModule.RoleCount, out snapshot))
        {
            return;
        }
        RefreshShopWindow();
        role.DirtyTracker.Clear(ClientDirtyModule.RoleCount);
    }

    /// <summary>打开指定 NPC 的商店并生成展示数据。</summary>
    public void OpenNpcShop(NpcEntity npcEntity)
    {
        _currentNpc = npcEntity;
        RefreshShopWindow();
        if (_shopWindow != null)
        {
            _shopWindow.Show(true);
        }
    }

    /// <summary>重新生成当前 NPC 商店的展示数据。</summary>
    private void RefreshShopWindow()
    {
        if (_shopWindow == null)
        {
            return;
        }
        _shopWindow.ReFreshUI(BuildGoodsViewData(_currentNpc));
    }

    /// <summary>把配置和角色次数状态转换为商城展示模型。</summary>
    private List<ShopGoodsViewData> BuildGoodsViewData(NpcEntity npcEntity)
    {
        List<ShopGoodsViewData> result = new List<ShopGoodsViewData>();
        if (npcEntity == null)
        {
            return result;
        }

        ShopTable shopTable = LubanMgr.Instance.GetShopTableById(npcEntity.ShopId);
        if (shopTable == null || shopTable.ShopType != (int)ShopType.Npc)
        {
            Debug.LogWarning("NPC 商店配置无效：" + npcEntity.ShopId);
            return result;
        }

        Dictionary<int, ShopItemInfo> shopItems = LubanMgr.Instance.GetShopItemInfos();
        if (shopItems == null)
        {
            return result;
        }
        List<ShopItemInfo> itemSnapshot = new List<ShopItemInfo>(shopItems.Values);
        for (int index = 0; index < itemSnapshot.Count; index++)
        {
            ShopItemInfo shopItem = itemSnapshot[index];
            if (shopItem == null || shopItem.ShopId != shopTable.Id)
            {
                continue;
            }

            ItemConfigBase itemConfig = LubanMgr.Instance.GetItemConfigById(shopItem.ItemId);
            if (itemConfig == null)
            {
                continue;
            }
            ItemConfigBase currencyConfig = LubanMgr.Instance.GetItemConfigById(shopItem.CurrencyItemId);
            string currencyName = "货币物品 " + shopItem.CurrencyItemId;
            string currencyIconPath = string.Empty;
            if (currencyConfig != null)
            {
                currencyName = currencyConfig.ItemName;
                currencyIconPath = currencyConfig.Icon;
            }

            bool unlimited = ShopMgr.Instance.IsUnlimitedPurchase(shopItem.GoodsId);
            int remainingCount = ShopMgr.Instance.GetRemainingPurchaseCount(shopItem.GoodsId);
            int maxPurchaseCount = remainingCount;
            if (unlimited)
            {
                maxPurchaseCount = int.MaxValue;
            }
            result.Add(new ShopGoodsViewData(shopTable.Id, shopItem.GoodsId,
                itemConfig.ItemName, itemConfig.Icon, shopItem.Price, shopItem.CurrencyItemId,
                currencyName, currencyIconPath, remainingCount, maxPurchaseCount, unlimited));
        }
        return result;
    }

    /// <summary>处理商品点击并打开购买确认弹窗。</summary>
    private void OnGoodsClicked(ShopGoodsViewData goods)
    {
        if (goods == null)
        {
            return;
        }
        if (ShopMgr.Instance.IsPurchasePending())
        {
            TipsMgr.Instance.ShowSystemTips("购买请求处理中");
            //return;
        }
        if (!goods.IsUnlimitedPurchase && goods.RemainingPurchaseCount <= 0)
        {
            TipsMgr.Instance.ShowSystemTips("该商品已达到限购数量");
            return;
        }

        TipsMgr.Instance.ShowBuyGoodsDialog(null, goods.ProductName, goods.ProductIconPath,
            goods.Price, goods.CurrencyItemId, goods.CurrencyName, goods.CurrencyIconPath,
            goods.MaxPurchaseCount, buyCount => OnBuyConfirmed(goods, buyCount));
    }

    /// <summary>把弹窗确认数量提交给商城模型。</summary>
    private void OnBuyConfirmed(ShopGoodsViewData goods, int buyCount)
    {
        // 发送前的参数、限购和货币校验由商城模型统一完成，失败时模型已经提示原因。
        ShopMgr.Instance.SendBuyRequest(goods.ShopId, goods.GoodsId, buyCount);
    }

    /// <summary>处理购买回包提示并刷新商品限购显示。</summary>
    private void OnPurchaseCompleted(bool success, int goodsId, string message)
    {
        // 商城模型只传递购买状态和提示文本，控制器负责界面提示和刷新。
        TipsMgr.Instance.ShowSystemTips(message);
    }

    /// <summary>注销商城窗口和模型事件。</summary>
    public override void Dispose()
    {
        ShopMgr.Instance.PurchaseCompleted -= OnPurchaseCompleted;
        if (_shopWindow != null)
        {
            _shopWindow.GoodsClicked -= OnGoodsClicked;
            _shopWindow.FrameUpdated -= OnShopWindowFrameUpdated;
        }
    }
}
