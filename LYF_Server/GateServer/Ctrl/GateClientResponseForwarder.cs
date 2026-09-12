using Google.Protobuf;

/// <summary>
/// GateServer 面向 Unity 的回包校验与转发入口。
/// 所有来自 GameServer 的客户端可见回包均在网关解析错误码，失败时统一发送 CMD_ErrCode。
/// </summary>
public static class GateClientResponseForwarder
{
    /// <summary>
    /// 校验回包协议和业务错误码后发送给目标 Unity 会话。
    /// </summary>
    public static void ForwardToUnity(Session session, BasePackage package)
    {
        if (session == null || package == null)
        {
            LogMsg.Info("网关回包转发失败：目标 Unity 会话或协议包为空", LogMsgType.Error);
            return;
        }

        CmdCode resultCode;
        if (!TryReadResultCode(package, out resultCode))
        {
            // 协议无法解析时不把不可信载荷交给客户端，使用统一错误协议通知客户端。
            session.SendError(package, CmdCode.ServerError);
            return;
        }

        if (resultCode != CmdCode.Succeed)
        {
            if (ShouldForwardBusinessResult(package.ProtoCode))
            {
                // 购买失败保留专用回包；扩容失败仍要同步权威格子数，不能替换为通用错误包。
                session.SendData(package);
                if (package.ProtoCode == NetDefine.CMD_OpenKnapsackGridCode)
                {
                    // 扩容失败在保留权威格子数后，再通过统一错误码触发客户端 Tips。
                    session.SendError(CreateErrorPackage(package), resultCode);
                }

                return;
            }

            session.SendError(package, resultCode);
            return;
        }

        session.SendData(package);
    }

    /// <summary>
    /// 按已注册的客户端可见回包读取结果码。无业务结果码的同步协议只验证 protobuf 格式。
    /// </summary>
    private static bool TryReadResultCode(BasePackage package, out CmdCode resultCode)
    {
        resultCode = CmdCode.Succeed;
        try
        {
            switch (package.ProtoCode)
            {
                case NetDefine.CMD_LoginGameServerCode:
                    resultCode = LoginGameServerRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_CreateRoleCode:
                    resultCode = CreateRoleRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_StartGameCode:
                    StartGameRet startGameResult = StartGameRet.Parser.ParseFrom(package.Data);
                    if (startGameResult.CmdCode == CmdCode.Succeed &&
                        (startGameResult.MainRoleInfo == null || startGameResult.MainRoleInfo.BaseInfo == null))
                    {
                        return false;
                    }

                    resultCode = startGameResult.CmdCode;
                    return true;
                case NetDefine.CMD_RoleSkillInfoCode:
                    resultCode = RoleSkillInfoRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_RoleKnapsackInfoCode:
                    resultCode = RoleKanpsackInfoRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_RoleShopPurchaseInfoCode:
                    resultCode = RoleShopPurchaseInfoRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_OpenKnapsackGridCode:
                    resultCode = OpenKnapsackGridRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_BuyShopItemCode:
                    BuyShopItemRes purchaseResult = BuyShopItemRes.Parser.ParseFrom(package.Data);
                    // 购买响应缺少错误对象时按服务器错误处理，避免使用未初始化的结果码。
                    if (purchaseResult.Error == null)
                    {
                        resultCode = CmdCode.ServerError;
                    }
                    else
                    {
                        resultCode = purchaseResult.Error.Code;
                    }
                    return true;
                case NetDefine.CMD_SyncRoleEnterWorldCode:
                case NetDefine.CMD_SyncotherOnlineCode:
                    RoleBaseInfo.Parser.ParseFrom(package.Data);
                    resultCode = CmdCode.Succeed;
                    return true;
                case NetDefine.CMD_SC_UpdateItemInfoCode:
                    resultCode = UpdateItemRet.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                case NetDefine.CMD_CountUpdateSyncInfoCode:
                    // 次数同步是服务端主动通知，仍需验证 protobuf 后原样转发给客户端。
                    resultCode = CountUpdateSyncInfo.Parser.ParseFrom(package.Data).CmdCode;
                    return true;
                default:
                    return false;
            }
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("网关回包协议解析失败，命令=" + package.ProtoCode + "，原因=" + ex.Message,
                LogMsgType.Error);
            return false;
        }
    }

    /// <summary>
    /// 保留已存在的专用响应契约，避免通用错误码丢失客户端继续处理所需的业务字段。
    /// </summary>
    private static bool ShouldForwardBusinessResult(int commandCode)
    {
        return commandCode == NetDefine.CMD_OpenKnapsackGridCode ||
            commandCode == NetDefine.CMD_BuyShopItemCode ||
            commandCode == NetDefine.CMD_RoleShopPurchaseInfoCode;
    }

    /// <summary>
    /// 错误包使用独立实例，避免 SendError 修改已发送的扩容业务回包。
    /// </summary>
    private static BasePackage CreateErrorPackage(BasePackage sourcePackage)
    {
        return new BasePackage
        {
            UnitySessionId = sourcePackage.UnitySessionId,
            GateSessionId = sourcePackage.GateSessionId
        };
    }
}
