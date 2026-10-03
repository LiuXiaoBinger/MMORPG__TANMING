using Google.Protobuf;

/// <summary>处理 CenterServer 返回的角色技能信息。</summary>
public sealed class RoleSkillInfoHandler : IGameMessageHandler
{
    /// <summary>确认当前处理器只消费中心服技能列表回包。</summary>
    /// <param name="source">入站命令的服务器来源。</param>
    /// <param name="commandCode">回包携带的十六位协议号。</param>
    /// <returns>来源为中心服且协议为 11080 时返回 true。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer && commandCode == NetDefine.CMD_RoleSkillInfoCode;
    }

    /// <summary>判断中心服技能列表是否属于当前复合会话已绑定的在线角色。</summary>
    /// <param name="role">按 Gate/Unity 复合会话索引解析出的当前角色。</param>
    /// <param name="response">中心服返回且携带原查询角色 ID 的技能列表。</param>
    /// <returns>角色在线、回包角色 ID 有效且两者一致时返回 true。</returns>
    public static bool IsMatchingRole(OnlineRole role, RoleSkillInfoRet response)
    {
        if (role == null || response == null || role.State != OnlineRoleState.Online ||
            response.RoleId <= 0)
        {
            return false;
        }

        return role.GetID() == (uint)response.RoleId;
    }

    /// <summary>核对中心服角色身份后，按原会话转发并装载权威技能列表。</summary>
    /// <param name="context">保留原 Gate 与 Unity 会话标识的中心服回包上下文。</param>
    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer || context.Package == null)
        {
            LogMsg.Info("角色技能信息回包为空", LogMsgType.Error);
            return;
        }

        // 11080 注册为 Internal，必须先核对角色身份，禁止旧角色回包泄漏到复用会话。
        try
        {
            // 中心服回包的持久角色 ID 和复合会话角色缺一不可。
            RoleSkillInfoRet response = RoleSkillInfoRet.Parser.ParseFrom(context.Package.Data);
            OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
                context.Package.GateSessionId, context.Package.UnitySessionId);
            if (!IsMatchingRole(role, response))
            {
                LogMsg.Info("拒绝技能列表回包：中心服角色与当前复合会话角色不一致，回包角色=" +
                    response.RoleId, LogMsgType.Error);
                return;
            }

            // 列表无效时仍向正确角色转发原结果，客户端由 Gate 处理具体错误码。
            if (context.GateSession == null)
            {
                LogMsg.Info("技能列表回包缺少原 Gate 会话，角色=" + response.RoleId, LogMsgType.Error);
                return;
            }
            context.GateSession.SendData(context.Package);

            // 技能列表不写入 GameServer 本地数据库，只供当前在线生命周期使用。
            RoleSkillComponent skillComponent;
            if (!role.TryGetComponent(out skillComponent) ||
                !skillComponent.LoadLearnedSkills(response))
            {
                LogMsg.Info("中心服技能列表装载失败，角色=" + role.GetID(), LogMsgType.Error);
            }
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("中心服技能列表解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
