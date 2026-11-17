using System;
using System.Collections.Generic;
using Google.Protobuf;
using cfg;
using UnityEngine;

/// <summary>
/// 发送商品购买请求，检查本地余额和限购数量，并把服务器结果交给商店控制器。
/// </summary>
public class ShopMgr : Singleton<ShopMgr>
{

    #region 字段、事件与属性
    /// <summary>商城购买结果事件，参数依次为是否成功、商品编号和提示文本。</summary>
    public event Action<bool, int, string> PurchaseCompleted;

    /// <summary>当前是否存在尚未返回的购买请求。</summary>
    private bool _isPurchasePending;

    /// <summary>当前等待购买回包的商品编号。</summary>
    private int _pendingGoodsId;

    /// <summary>发出当前购买请求时使用的网络连接编号，用来排除旧连接回包。</summary>
    private long _pendingConnectionGeneration;
    #endregion

    #region 初始化与购买提示
    /// <summary>商城数据由角色组件统一维护，此入口保留给网络模块。</summary>
    public void Init()
    {
    }

    /// <summary>显示商城购买校验提示。</summary>
    private static void ShowPurchaseTip(string message)
    {
        if (TipsMgr.Instance != null)
        {
            TipsMgr.Instance.ShowSystemTips(message);
        }
    }
    #endregion

    #region 事件回调
    /// <summary>解析购买结果状态并通知商城控制器，不读取购买物品数据。</summary>
    public void HandleResponse(ByteString data)
    {
        long responseGeneration = SocketDispatcher.Instance.CurrentDispatchGeneration;
        if (!_isPurchasePending || _pendingConnectionGeneration <= 0 ||
            responseGeneration != _pendingConnectionGeneration)
        {
            // 协议没有请求号，只接受当前购买请求所在连接代次的回包。
            Debug.LogWarning("丢弃无对应购买请求或连接代次不匹配的商城回包。");
            return;
        }

        int goodsId = _pendingGoodsId;
        // 当前购买请求的结果，后续根据业务错误码提示玩家。
        BuyShopItemRes response = BuyShopItemRes.Parser.ParseFrom(data);

        bool success = false;
        string message = "商城购买结果无效";
        if (response != null && response.Error != null)
        {
            success = response.Error.Code == CmdCode.Succeed;
            message = response.Error.Message;
            if (success)
            {
            }
            if (string.IsNullOrEmpty(message))
            {
                if (success)
                {
                    message = "购买成功";
                }
                else
                {
                    message = "购买失败：" + response.Error.Code;
                }
            }
        }

        // 先清理在途状态，随后事件回调可以安全发起下一次购买。
        ClearPendingRequest();

        if (PurchaseCompleted != null)
        {
            PurchaseCompleted.Invoke(success, goodsId, message);
        }
    }

    /// <summary>如果 connectionGeneration 正是购买请求使用的连接，就清除等待状态。</summary>
    public void HandleDisconnected(long connectionGeneration)
    {
        if (connectionGeneration <= 0)
        {
            return;
        }
        if (_isPurchasePending && _pendingConnectionGeneration == connectionGeneration)
        {
            ClearPendingRequest();
        }

    }
    #endregion

    #region 购买请求发送与状态清理
    /// <summary>
    /// 检查参数、剩余次数和余额后发送购买请求，并记录商品与连接编号；是否允许连续点击由调用方判断。
    /// </summary>
    public bool SendBuyRequest(int shopTableId, int goodsId, int buyCount)
    {
        NetClient client = MNetClient.Client;
        long connectionGeneration = MNetClient.Instance.CurrentConnectionGeneration;
        PlayerEntity localRole = GetLocalRole();
        // 网络通道无效：客户端为空或连接代次尚未建立。
        bool networkInvalid = client == null || connectionGeneration <= 0;
        // 商店请求参数无效：商品表、商品编号、购买数量任一非正。
        bool shopParamInvalid = shopTableId <= 0 || goodsId <= 0 || buyCount <= 0;
        // 本地角色数据无效：角色为空或其基础信息未就绪。
        bool roleInvalid = localRole == null || localRole.BaseInfo == null;
        if (networkInvalid || shopParamInvalid || roleInvalid)
        {
            ShowPurchaseTip("购买参数无效或当前请求仍在处理中");
            return false;
        }

        ShopItemInfo shopItem = FindShopItem(goodsId);
        if (shopItem == null)
        {
            ShowPurchaseTip("商品配置不存在");
            return false;
        }

        RoleCountComponent countComponent;
        if (!localRole.TryGetComponent(out countComponent))
        {
            ShowPurchaseTip("角色购买次数数据未准备完成");
            return false;
        }
        long remainingCount = countComponent.GetLeftCount(RoleCountAction.ShopPurchase, goodsId);
        if (remainingCount >= 0L && buyCount > remainingCount)
        {
            ShowPurchaseTip("购买数量超过限购剩余次数");
            return false;
        }

        if (!CheckCurrencyBalance(localRole, shopItem.CurrencyItemId, shopItem.Price, buyCount))
        {
            return false;
        }

        BuyShopItemReq request = new BuyShopItemReq
        {
            ShopId = (uint)shopTableId,
            RoleId = (uint)localRole.BaseInfo.RoleId
        };
        request.Items.Add(new BuyShopItem
        {
            GoodsId = (uint)goodsId,
            BuyCount = (uint)buyCount
        });

        try
        {
            _isPurchasePending = true;
            _pendingGoodsId = goodsId;
            _pendingConnectionGeneration = connectionGeneration;
            client.Send(NetDefine.CMD_BuyShopItemCode, request.ToByteString());
            return true;
        }
        catch (Exception exception)
        {
            ClearPendingRequest();
            Debug.LogError("发送商城购买请求失败：" + exception.Message);
            ShowPurchaseTip("购买请求发送失败");
            return false;
        }
    }

    /// <summary>清除当前购买等待状态。</summary>
    private void ClearPendingRequest()
    {
        _isPurchasePending = false;
        _pendingGoodsId = 0;
        _pendingConnectionGeneration = 0;
    }
    #endregion

    #region 查询与判定
    /// <summary>返回当前是否存在有效的购买等待状态。</summary>
    public bool IsPurchasePending()
    {
        return _isPurchasePending;
    }

    /// <summary>判断指定商品是否不限制购买次数。</summary>
    public bool IsUnlimitedPurchase(int goodsId)
    {
        cfg.CountInfo countInfo = LubanMgr.Instance.GetCountInfo(RoleCountAction.ShopPurchase, goodsId);
        return countInfo == null || countInfo.Limit <= 0;
    }

    /// <summary>返回 goodsId 商品的剩余可买数量；不限购用 int.MaxValue 表示，角色数据未就绪返回 0。</summary>
    public int GetRemainingPurchaseCount(int goodsId)
    {
        cfg.CountInfo countInfo = LubanMgr.Instance.GetCountInfo(RoleCountAction.ShopPurchase, goodsId);
        if (countInfo == null || countInfo.Limit <= 0)
        {
            return int.MaxValue;
        }
        PlayerEntity localRole = GetLocalRole();
        if (localRole == null)
        {
            return 0;
        }

        RoleCountComponent countComponent;
        if (!localRole.TryGetComponent(out countComponent))
        {
            return 0;
        }

        long remainingCount = countComponent.GetLeftCount(RoleCountAction.ShopPurchase, goodsId);
        if (remainingCount <= 0L)
        {
            return 0;
        }
        if (remainingCount > int.MaxValue)
        {
            return int.MaxValue;
        }
        return (int)remainingCount;
    }

    /// <summary>获取当前登录角色，避免商城管理器自行缓存角色数据。</summary>
    private static PlayerEntity GetLocalRole()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return null;
        }
        return Global.Instance.SceneWorld.LocalRole;
    }

    /// <summary>按 GoodsId 查找商城商品配置。</summary>
    private static ShopItemInfo FindShopItem(int goodsId)
    {
        Dictionary<int, ShopItemInfo> shopItems = LubanMgr.Instance.GetShopItemInfos();
        if (shopItems == null)
        {
            return null;
        }
        ShopItemInfo shopItem;
        if (shopItems.TryGetValue(goodsId, out shopItem))
        {
            return shopItem;
        }
        return null;
    }

    /// <summary>校验购买所需虚拟物品余额，价格为零时不要求货币配置。</summary>
    private static bool CheckCurrencyBalance(PlayerEntity role, int currencyItemId,
        int unitPrice, int buyCount)
    {
        if (unitPrice < 0)
        {
            ShowPurchaseTip("商品价格配置无效");
            return false;
        }
        if (unitPrice == 0)
        {
            return true;
        }
        if (currencyItemId <= 0)
        {
            ShowPurchaseTip("商品货币配置无效");
            return false;
        }
        long totalPrice;
        if (unitPrice > long.MaxValue / buyCount)
        {
            ShowPurchaseTip("购买总价超出可计算范围");
            return false;
        }
        totalPrice = (long)unitPrice * buyCount;

        RoItemComponent itemComponent;
        if (!role.TryGetComponent(out itemComponent))
        {
            ShowPurchaseTip("角色虚拟物品数据未准备完成");
            return false;
        }
        long balance = itemComponent.GetVirtualItemCount(currencyItemId);
        if (balance < totalPrice)
        {
            ShowPurchaseTip("货币不足");
            return false;
        }
        return true;
    }
    #endregion

}
