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

    #region 构造与初始化
    /// <summary>创建商城控制器并注册视图和模型事件。</summary>
    /// <param name="mainView">提供 NPC 商店窗口的主界面。</param>
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
    #endregion

    #region 商品展示数据构建与商店界面
    /// <summary>把配置和角色次数状态转换为商城展示模型。</summary>
    /// <param name="npcEntity">商店所属 NPC，从 ShopId 查询商店和商品配置。</param>
    private List<ShopGoodsViewData> BuildGoodsViewData(NpcEntity npcEntity)
    {
        // 该 NPC 商店的商品展示列表，无效商店返回空列表。
        List<ShopGoodsViewData> result = new List<ShopGoodsViewData>();
        if (npcEntity == null)
        {
            return result;
        }

        // NPC 引用的商店配置，必须是 NPC 类型商店。
        ShopTable shopTable = LubanMgr.Instance.GetShopTableById(npcEntity.ShopId);
        if (shopTable == null || shopTable.ShopType != (int)ShopType.Npc)
        {
            Debug.LogWarning("NPC 商店配置无效：" + npcEntity.ShopId);
            return result;
        }

        // 全部商品配置，下面只保留当前商店的条目。
        Dictionary<int, ShopItemInfo> shopItems = LubanMgr.Instance.GetShopItemInfos();
        if (shopItems == null)
        {
            return result;
        }
        // 把配置值复制成可按下标遍历的列表，条目仍是原配置引用。
        List<ShopItemInfo> itemSnapshot = new List<ShopItemInfo>(shopItems.Values);
        for (int index = 0; index < itemSnapshot.Count; index++)
        {
            // 当前检查的商品配置，所属商店不匹配时跳过。
            ShopItemInfo shopItem = itemSnapshot[index];
            if (shopItem == null || shopItem.ShopId != shopTable.Id)
            {
                continue;
            }

            // 商品实际获得的物品配置，提供名称和图标。
            ItemConfigBase itemConfig = LubanMgr.Instance.GetItemConfigById(shopItem.ItemId);
            if (itemConfig == null)
            {
                continue;
            }
            // 支付货币也用物品配置表示，缺失时只显示编号文字。
            ItemConfigBase currencyConfig = LubanMgr.Instance.GetItemConfigById(shopItem.CurrencyItemId);
            string currencyName = "货币物品 " + shopItem.CurrencyItemId;
            string currencyIconPath = string.Empty;
            if (currencyConfig != null)
            {
                currencyName = currencyConfig.ItemName;
                currencyIconPath = currencyConfig.Icon;
            }

            // 是否不限购买次数，与玩家当前货币数量不是同一限制。
            bool unlimited = ShopMgr.Instance.IsUnlimitedPurchase(shopItem.GoodsId);
            // 角色计数与商品限购规则算出的剩余可买份数。
            int remainingCount = ShopMgr.Instance.GetRemainingPurchaseCount(shopItem.GoodsId);
            // 弹窗数量上限；不限次数商品使用 int.MaxValue，实际购买仍会校验。
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

    /// <summary>打开指定 NPC 的商店并生成展示数据。</summary>
    /// <param name="npcEntity">本次打开商店的 NPC，保存到控制器供后续刷新。</param>
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

    /// <summary>处理商品点击并打开购买确认弹窗。</summary>
    /// <param name="goods">玩家点击的商品展示数据。</param>
    private void OnGoodsClicked(ShopGoodsViewData goods)
    {
        if (goods == null)
        {
            return;
        }
        // 当前逻辑只提示已有请求，下面仍会继续打开购买弹窗。
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
    #endregion

    #region 事件回调
    /// <summary>商城可见时检查角色次数是否变化；有变化就重建商品展示并清除标记。</summary>
    private void OnShopWindowFrameUpdated()
    {
        if (_shopWindow == null || !_shopWindow.isActiveAndEnabled)
        {
            return;
        }
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return;
        }
        PlayerEntity role = Global.Instance.SceneWorld.LocalRole;
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

    /// <summary>把弹窗确认数量提交给商城模型。</summary>
    /// <param name="goods">打开弹窗时的商品数据。</param>
    /// <param name="buyCount">用户确认的购买份数。</param>
    private void OnBuyConfirmed(ShopGoodsViewData goods, int buyCount)
    {
        // 发送前的参数、限购和货币校验由商城模型统一完成，失败时模型已经提示原因。
        ShopMgr.Instance.SendBuyRequest(goods.ShopId, goods.GoodsId, buyCount);
    }

    /// <summary>显示购买结果提示；本方法不刷新商品，限购刷新由窗口帧事件读取次数变化标记完成。</summary>
    /// <param name="success">购买是否成功，当前显示逻辑不单独使用。</param>
    /// <param name="goodsId">本次商品编号，当前显示逻辑不单独使用。</param>
    /// <param name="message">商城模型生成的结果提示。</param>
    private void OnPurchaseCompleted(bool success, int goodsId, string message)
    {
        // 这里只使用 message 显示提示，success 和 goodsId 当前未参与分支。
        TipsMgr.Instance.ShowSystemTips(message);
    }
    #endregion
}
