using System;
using Google.Protobuf;

/// <summary>
/// 施法结果的统一回包出口：SkillCastRet 只有这一个协议号，
/// 入口拒绝、吟唱中断终态、队列丢弃和成功提交都走这里，避免各处重复拼包。
/// </summary>
public static class SkillCastReply
{
    /// <summary>沿角色现有网关会话回送施法结果；角色或结果为空时直接忽略。</summary>
    /// <param name="role">权威结果对应的在线角色。</param>
    /// <param name="result">成功或拒绝的强类型施法结果。</param>
    public static void Send(OnlineRole role, SkillCastRet result)
    {
        if (role == null || result == null)
        {
            return;
        }
        role.SendToClient(NetDefine.CMD_SkillCastCode, result.ToByteString());
    }

    /// <summary>按技能标识与明确结果码构造终态结果，回带原技能 ID、施法标识与权威 MP。</summary>
    /// <param name="role">用于读取权威 MP 的在线角色。</param>
    /// <param name="skill_id">客户端原始请求的技能主表 ID。</param>
    /// <param name="skill_uuid">客户端原始请求的施法标识。</param>
    /// <param name="code">具体结果码。</param>
    /// <returns>可直接回送的施法结果。</returns>
    public static SkillCastRet Build(OnlineRole role, int skill_id, ulong skill_uuid, CmdCode code)
    {
        // result 是本次构造的强类型回包，字段全部取自入参与角色权威数据，不做二次加工。
        SkillCastRet result = new SkillCastRet
        {
            CmdCode = code,
            ServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CurrentMp = ReadMp(role),
            SkillId = skill_id,
            SkillUuid = skill_uuid
        };
        return result;
    }

    /// <summary>读取在线角色的权威内存 MP，不把客户端上报值作为来源。</summary>
    /// <param name="role">待读取的在线角色。</param>
    /// <returns>当前非负法力值；角色快照尚未就绪时为零。</returns>
    public static int ReadMp(OnlineRole role)
    {
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return 0;
        }
        // 负数法力属于异常数据，按零回报，避免客户端拿到负值。
        int current_mp = role.mainRoleInfo.BaseInfo.CurrMp;
        if (current_mp < 0)
        {
            return 0;
        }
        return current_mp;
    }
}

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

    /// <summary>
    /// 从会话解析角色，按 RO 的真实入口顺序推进：
    /// 非提交校验（未学/配置缺失/冷却/MP 不足/状态不允许在此直接拒绝）
    /// → 池取 ROClientFireSkillArgs → 状态互斥申请 → 最终回调重查角色 → 派发事件。
    /// 对应参考工程 rpcc2g_singingrequest.cpp:25-115。
    /// </summary>
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

        // 客户端施法参数，解析后交给技能组件执行权威校验。
        SkillCastReq request = SkillCastReq.Parser.ParseFrom(context.Package.Data);

        // 角色 ID 从 Gate/Unity 复合会话反查，请求本身不含可伪造的角色 ID。
        OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
            context.Package.GateSessionId, context.Package.UnitySessionId);
        if (context.GateSession == null || role == null || role.State != OnlineRoleState.Online)
        {
            SendResult(context, SkillCastReply.Build(role, ReadSkillId(request), ReadSkillUuid(request),
                CmdCode.InvalidSession));
            return;
        }

        // 组件只在逻辑线程修改 MP/CD，回包后才允许向附近角色通知表现。
        RoleSkillComponent skillComponent;
        if (!role.TryGetComponent(out skillComponent))
        {
            SendResult(context, SkillCastReply.Build(role, ReadSkillId(request),
                ReadSkillUuid(request), CmdCode.SkillStateBlocked));
            return;
        }

        // 非提交校验：未学、配置缺失、冷却、MP 不足、状态不允许等在这里尽早拒绝，
        // 不派发事件、不进入读条、不扣 MP 与 CD，避免先无意义读完条再拒绝。
        SkillCastRet precheck;
        if (!skillComponent.CheckCastRequest(ReadSkillId(request), ReadSkillUuid(request),
            out precheck))
        {
            SendResult(context, precheck);
            return;
        }

        // 角色 ID 在异步期间保持不变；回调里必须按 ID 重查，不能捕获 OnlineRole 强引用。
        uint role_uid = role.GetID();

        // 池取客户端施法事件：本方法只负责填充与移交，归还点在下面的 Release。
        // 这里不额外包 try/catch：异常沿 ClientMessageManager 的统一处理路径上抛，
        // 只保留 try/finally 保证池化事件在任何路径下都被归还。
        ROClientFireSkillArgs args = ROEventPool<ROClientFireSkillArgs>.Instance.Get();
        try
        {
            args.skill_id_ = request.SkillId;
            args.skill_uuid_ = request.SkillUuid;
            // RO 的施法点只决定普攻朝向，命中目标由游戏服查询。
            args.target_ = 0UL;
            args.cast_point_ = new ROVector3(0f, 0f, 0f);
            if (request.CastPoint != null)
                args.cast_point_ = new ROVector3(request.CastPoint.X, 0f, request.CastPoint.Z);

            // req_token 是本次状态切换请求的序号；最终结果由下面的回调给出，不靠序号同步等待。
            ulong req_token;
            StateMutexManager.Instance.TransferStateByFunc(
                role,
                StateTransitionType.kStateTransitionRequestSinging,
                true,
                args,
                // code 是互斥框架回填的本次切换结果码：非 Succeed 即拒绝，只有放行时才派发事件。
                delegate(CmdCode code)
                {
                    // 最终回调：状态切换可能跨帧，期间角色可能已下线，必须重查。
                    OnlineRole current_role = RoleManager.Instance.GetOnlineRole((int)role_uid);
                    if (current_role == null || current_role.State != OnlineRoleState.Online)
                    {
                        return;
                    }
                    if (code != CmdCode.Succeed)
                    {
                        // 互斥拒绝：按既有拒绝码回包，不进入吟唱也不扣 MP。
                        SendResult(context, SkillCastReply.Build(current_role, request.SkillId,
                            request.SkillUuid, code));
                        return;
                    }
                    // 互斥放行：把事件交给角色派发器，后续由吟唱状态与技能组件完成。
                    current_role.GetEventDispatcher().DispatcherEvent(args);
                },
                out req_token);
        }
        finally
        {
            // 调用方引用在这里归还；异步期间由 StateTransferComponent 持有的那份引用继续保活。
            args.Release();
        }
    }

    /// <summary>读取请求里的技能主表 ID；请求缺失时为零。</summary>
    /// <param name="request">客户端原始请求。</param>
    private static int ReadSkillId(SkillCastReq request)
    {
        if (request == null)
        {
            return 0;
        }
        return request.SkillId;
    }

    /// <summary>读取请求里的客户端施法标识；请求缺失时为零。</summary>
    /// <param name="request">客户端原始请求。</param>
    private static ulong ReadSkillUuid(SkillCastReq request)
    {
        if (request == null)
        {
            return 0UL;
        }
        return request.SkillUuid;
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
