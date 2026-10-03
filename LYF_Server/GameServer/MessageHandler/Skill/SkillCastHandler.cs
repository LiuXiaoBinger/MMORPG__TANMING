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

    /// <summary>从会话解析角色，先回权威结果，再广播一次施法开始。</summary>
    /// <param name="context">逻辑线程中的不可变网络命令和 Gate 会话上下文。</param>
    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.GateServer ||
            context.Transport == null || context.Package == null)
        {
            return;
        }

        // 格式错误也返回专用结果，使客户端能够获知请求失败。
        SkillCastReq request;
        try
        {
            request = SkillCastReq.Parser.ParseFrom(context.Package.Data);
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("客户端施法请求解析失败: " + ex.Message, LogMsgType.Error);
            SendResult(context, new SkillCastRet
            {
                CmdCode = CmdCode.ReqParamError,
                ServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            return;
        }

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
            if (role.mainRoleInfo != null && role.mainRoleInfo.BaseInfo != null)
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

        // 成功时开始通知沿用客户端技能 UUID；MP/CD 仍由服务端结算。
        SkillCastRet result;
        SkillCastStartNtf startNotice;
        bool accepted = skillComponent.TryCast(request, out result, out startNotice);
        SendResult(context, result);
        if (accepted && startNotice != null)
        {
            RoleManager.Instance.BroadcastSkillStart(role, startNotice);
        }
    }

    /// <summary>沿原 Gate 链路返回含权威 MP/CD 的专用施法结果。</summary>
    /// <param name="context">保留 Gate/Unity 会话标识的入站命令上下文。</param>
    /// <param name="result">成功或拒绝的强类型技能结果。</param>
    private static void SendResult(GameMessageHandlerContext context, SkillCastRet result)
    {
        if (context == null || context.Transport == null || context.Package == null || result == null)
        {
            return;
        }
        context.Transport.SendData(context.Package, NetDefine.CMD_SkillCastCode,
            result.ToByteString());
    }
}
