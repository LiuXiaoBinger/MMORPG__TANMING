using Google.Protobuf;

/// <summary>处理 CenterServer 返回的角色技能信息。</summary>
public sealed class RoleSkillInfoHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer && commandCode == NetDefine.CMD_RoleSkillInfoCode;
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer || context.Package == null)
        {
            LogMsg.Info("角色技能信息回包为空", LogMsgType.Error);
            return;
        }

        // 技能明细已在统一入口转发，网关负责解析和错误码提示。
    }
}
