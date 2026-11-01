using System;
using Google.Protobuf;

/// <summary>在 GameServer 逻辑线程处理客户端无目标技能释放。</summary>
public sealed class SkillCastHandler : IGameMessageHandler
{
    /// <summary>确认仅接管 GateServer 施法请求协议。</summary>
    /// <param name="source">入站命令的服务来源。</param>
    /// <param name="commandCode">入站命令的十六位协议号。</param>
    /// <returns>来源和技能请求协议都匹配时为 true。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.GateServer &&
            commandCode == NetDefine.CMD_SkillCastCode;
    }

    /// <summary>从会话解析角色并构造客户端施法事件，交由角色派发器驱动后续释放流程。</summary>
    /// <param name="context">逻辑线程中的不可变网络命令和 Gate 会话上下文。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 上下文缺失或来源不是网关时直接丢弃该施法请求。
        if (context == null || context.Source != GameCommandSource.GateServer)
        {
            return;
        }
        // 传输链路或协议包缺失时同样无法继续处理。
        if (context.Transport == null || context.Package == null)
        {
            return;
        }

        // 格式错误也返回专用结果，使客户端能够获知请求失败。
        // 客户端施法参数，解析后交给技能组件执行权威校验。
        SkillCastReq request = SkillCastReq.Parser.ParseFrom(context.Package.Data);

        // 角色 ID 从 Gate/Unity 复合会话反查，请求本身不含可伪造的角色 ID。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        if (context.GateSession == null || role == null || role.State != OnlineRoleState.Online)
        {
            SendResult(context, new SkillCastRet
            {
                CmdCode = CmdCode.InvalidSession,
                SkillUuid = request.SkillUuid,
                SkillId = request.SkillId,
                ServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            return;
        }

        // 组件只在逻辑线程修改 MP/CD，回包后才允许向附近角色通知表现。
        RoleSkillComponent skillComponent;
        if (!role.TryGetComponent(out skillComponent))
        {
            // 基础快照可能尚未装载；无权威角色时 MP 快照使用零而不解引用空对象。
            int currentMp = 0;
            if (role.mainRoleInfo != null && 
                role.mainRoleInfo.BaseInfo != null)
            {
                currentMp = role.mainRoleInfo.BaseInfo.CurrMp;
            }
            SendResult(context, new SkillCastRet
            {
                CmdCode = CmdCode.SkillStateBlocked,
                SkillUuid = request.SkillUuid,
                SkillId = request.SkillId,
                ServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CurrentMp = currentMp
            });
            return;
        }

        // 构造客户端施法事件并交给角色派发器；释放校验、回包与广播由技能组件在事件链中完成。
        ROClientFireSkillArgs args = new ROClientFireSkillArgs();
        args.skill_id_ = request.SkillId;
        args.skill_uuid_ = request.SkillUuid;
        // 现有协议只携带技能 ID 与单次施法标识，目标与施法点保持 RO 字段的零值默认。
        args.target_ = 0UL;
        args.cast_point_ = new ROVector3(0f, 0f, 0f);
        role.GetEventDispatcher().DispatcherEvent(args);
    }

    /// <summary>沿原 Gate 链路返回含权威 MP/CD 的专用施法结果。</summary>
    /// <param name="context">保留 Gate/Unity 会话标识的入站命令上下文。</param>
    /// <param name="result">成功或拒绝的强类型技能结果。</param>
    private static void SendResult(GameMessageHandlerContext context, SkillCastRet result)
    {
        // 上下文、传输链路或协议包缺失时无法沿原路回包。
        if (context == null || context.Transport == null || context.Package == null)
        {
            return;
        }
        // 结果对象缺失时没有可回送的内容。
        if (result == null)
        {
            return;
        }
        context.Transport.Send(context.Package, NetDefine.CMD_SkillCastCode,
            result.ToByteString());
    }
}
