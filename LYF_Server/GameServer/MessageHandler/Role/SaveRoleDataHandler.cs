using Google.Protobuf;

/// <summary>处理 CenterServer 在事务提交后返回的角色数据保存 ACK。</summary>
public sealed class SaveRoleDataHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer && commandCode == NetDefine.CMD_SaveRoleDataCode;
    }

    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer || context.Package == null)
        {
            return;
        }

        try
        {
            SaveRoleDataRet result = SaveRoleDataRet.Parser.ParseFrom(context.Package.Data);
            OnlineRole role = RoleManager.Instance.GetOnlineRole(result.RoleId);
            if (role != null)
            {
                role.OnSaveRoleDataResult(result);
            }
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("角色物品保存 ACK 解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
