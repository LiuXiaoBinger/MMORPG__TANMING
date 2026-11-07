using Google.Protobuf;

/// <summary>在逻辑线程处理装备实例操作；现有协议注册框架要求实现 IGameMessageHandler。</summary>
public sealed class EquipOperationHandler : IGameMessageHandler
{
    /// <summary>仅接收网关转交的装备操作请求。</summary>
    /// <param name="source">原始网络消息来源，装备操作只接受 GateServer。</param>
    /// <param name="commandCode">本次请求的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer && commandCode == NetDefine.CMD_EquipOperationCode;
    }

    /// <summary>直接解析协议，以原会话确定角色，再调用装备事务并回传操作结果。</summary>
    /// <param name="context">包含原网关会话和传输链路的逻辑线程命令。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 请求只携带物品实例，不接受客户端指定角色身份。
        EquipOperationReq request = EquipOperationReq.Parser.ParseFrom(context.Package.Data);
        // Gate/Unity 复合会话对应的权威在线角色。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        // 默认拒绝未绑定角色的会话，正常角色由事务结果覆盖错误码。
        EquipOperationRet result = new EquipOperationRet
        {
            CmdCode = CmdCode.InvalidSession,
            ItemUid = request.ItemUid,
            Operation = request.Operation
        };
        if (role != null)
        {
            // 一个事务只处理这一条请求，内部负责物品增量和装备属性刷新。
            BagWearItemTransition transition = new BagWearItemTransition(role);
            // 未定义的枚举和无操作请求统一按非法参数返回。
            BagTransitionResult operationResult = BagTransitionResult.InvalidArgument;
            switch (request.Operation)
            {
                case EquipOperation.Wear:
                    operationResult = transition.WearItem(request.ItemUid);
                    break;
                case EquipOperation.TakeOff:
                    operationResult = transition.TakeOffItem(request.ItemUid);
                    break;
                case EquipOperation.SwitchSpecialSkill:
                    operationResult = transition.SwitchSpecialSkill(request.ItemUid, request.SpecialSkillIndex);
                    break;
            }
            result.CmdCode = ToCommandCode(operationResult);
        }
        context.Transport.Send(context.Package, NetDefine.CMD_EquipOperationCode, result.ToByteString());
    }

    /// <summary>把事务结果转换为既有网络错误码，不创建另一套装备错误类型。</summary>
    /// <param name="result">穿脱或切换特技的服务层结果。</param>
    private static CmdCode ToCommandCode(BagTransitionResult result)
    {
        switch (result)
        {
            case BagTransitionResult.Success: return CmdCode.Succeed;
            case BagTransitionResult.BagFull: return CmdCode.BagFull;
            case BagTransitionResult.RoleNotFound: return CmdCode.RoleNotExist;
            case BagTransitionResult.KnapsackUnavailable: return CmdCode.FeatureNotReady;
            default: return CmdCode.ReqParamError;
        }
    }
}
