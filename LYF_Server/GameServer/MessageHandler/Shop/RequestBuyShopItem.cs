using System;
using System.Collections.Generic;
using cfg;
using Google.Protobuf;

/// <summary>
/// 处理客户端商城购买请求。
/// 网络线程只负责入队，购买校验在 GameServer 逻辑线程执行。
/// </summary>
public sealed class RequestBuyShopItem : IGameMessageHandler
{
    // 单次购买请求允许携带的最大商品种类数，防止异常请求占用过多逻辑帧时间。
    private const int MaxItemCount = 50;
    // 单项商品一次请求允许购买的最大份数。
    private const uint MaxBuyCountPerItem = 999;

    /// <summary>判断当前消息是否为来自网关的商城购买请求。</summary>
    /// <param name="source">消息来源服务器。</param>
    /// <param name="commandCode">协议命令号。</param>
    /// <returns>本处理器能够处理时返回 true。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_BuyShopItemCode;
    }

    /// <summary>校验商城购买请求，并在逻辑线程完成虚拟物品扣除与商品发放。</summary>
    /// <param name="context">当前消息及网络会话上下文。</param>
    public void Handle(GameMessageHandlerContext context)
    {
        #region 校验

        if (context == null || context.Source != GameCommandSource.GateServer ||
            context.Transport == null || context.Package == null || context.Command == null ||
            context.Command.Session == null || context.GateSession == null)
        {
            SendResult(context, CmdCode.InvalidSession, "购买请求的网关会话无效");
            return;
        }

        BuyShopItemReq request;
        try
        {
            request = BuyShopItemReq.Parser.ParseFrom(context.Package.Data);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("商城购买请求协议解析失败: " + ex.Message, LogMsgType.Error);
            SendResult(context, CmdCode.InvalidQuantity, "购买请求格式无效");
            return;
        }

        if (request.Items == null || request.Items.Count == 0 || request.Items.Count > MaxItemCount)
        {
            SendResult(context, CmdCode.InvalidQuantity, "购买商品列表数量无效");
            return;
        }

        // 先限制单项购买数量，避免异常请求参与价格和奖励数量计算。
        for (int index = 0; index < request.Items.Count; index++)
        {
            BuyShopItem item = request.Items[index];
            if (item == null || item.GoodsId == 0 || item.BuyCount == 0 ||
                item.BuyCount > MaxBuyCountPerItem)
            {
                SendResult(context, CmdCode.InvalidQuantity, "商品购买数量无效");
                return;
            }
        }

        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Command.Session.GateSessionId, context.Command.Session.UnitySessionId);
        if (role == null || role.State != OnlineRoleState.Online || role.mainRoleInfo == null ||
            role.mainRoleInfo.BaseInfo == null)
        {
            SendResult(context, CmdCode.InvalidSession, "角色会话无效或角色不在线");
            return;
        }

        // 角色 ID 只用于和会话角色做一致性校验，不能作为身份来源。
        if (request.RoleId == 0 || request.RoleId != (uint)role.mainRoleInfo.BaseInfo.RoleId)
        {
            SendResult(context, CmdCode.InvalidSession, "请求角色与会话角色不一致");
            return;
        }

        if (request.ShopId == 0 || request.ShopId > int.MaxValue)
        {
            SendResult(context, CmdCode.InvalidShop, "商店配置无效");
            return;
        }
        #endregion
        
        
        // 顶层 shop_id 先定位商店配置，再由配置读取具体商店类型。
        ShopTable shopTable = LubanMgr.Instance.GetShopTableById((int)request.ShopId);
        if (shopTable == null || !Enum.IsDefined(typeof(ShopType), (ShopType)shopTable.ShopType))
        {
            SendResult(context, CmdCode.InvalidShop, "商店配置不存在或类型无效");
            return;
        }

        ShopType shopType = (ShopType)shopTable.ShopType;
        if (!ShopManager.Instance.IsOpen(role, shopType))
        {
            SendResult(context, CmdCode.InvalidShop, "商店未打开");
            return;
        }

      

       
        

        // 在 GameServer 逻辑线程内先扣除虚拟物品，再发放商品；两者共用物品脏数据链路。
        RoItemComponent itemComponent = role.GetComponent<RoItemComponent>();
        if (itemComponent == null)
        {
            SendResult(context, CmdCode.InvalidSession, "角色背包未初始化");
            return;
        }

        if (ShopManager.Instance.GetShopbyID(request.ShopId) != null
            && ShopManager.Instance.GetShopbyID(request.ShopId).BuyItems(role, request) != CmdCode.Succeed)
        {
            SendResult(context, CmdCode.InvalidSession, "购买失败");
            return;
        }
        // 限购次数已在 ShopStatic 中扣减并由角色次数组件单独同步，无需重复写入购买回包。
        SendResult(context, CmdCode.Succeed, "购买成功");
    }

    /// <summary>向当前客户端返回商城购买失败结果。</summary>
    /// <param name="context">当前消息及网络会话上下文。</param>
    /// <param name="code">购买错误码。</param>
    /// <param name="message">用于诊断的中文错误信息。</param>
    private static void SendResult(GameMessageHandlerContext context, CmdCode code, string message)
    {
        if (context == null || context.Transport == null || context.Package == null)
        {
            return;
        }

        BuyShopItemRes response = new BuyShopItemRes
        {
            Error = new ErrorInfo
            {
                Code = code,
                Message = message ?? string.Empty
            }
        };
        context.Transport.SendData(context.Package, NetDefine.CMD_BuyShopItemCode,
            response.ToByteString());
    }
    /// <summary>购买失败时返还已经扣除的虚拟物品，保持本次购买在内存中的原子性。</summary>
    /// <param name="role">需要返还虚拟物品的在线角色。</param>
    /// <param name="spent">物品配置 ID 到已扣数量的映射。</param>
    private static void RollbackVirtualItems(OnlineRole role, Dictionary<int, long> spent)
    {
        List<KeyValuePair<int, long>> rollbackSnapshot = new List<KeyValuePair<int, long>>(spent);
        for (int index = 0; index < rollbackSnapshot.Count; index++)
        {
            KeyValuePair<int, long> rollback = rollbackSnapshot[index];
            BagGiveItemTransition giver = new BagGiveItemTransition(role);
            giver.GiveItem(new ItemDesc(rollback.Key, rollback.Value), KnapsackType.RoleVirtualItemPack);
        }
    }
}
