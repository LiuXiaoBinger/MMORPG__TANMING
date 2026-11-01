/// <summary>处理 CenterServer 在事务提交后返回的角色数据保存 ACK。</summary>
public sealed class SaveRoleDataHandler : IGameMessageHandler
{
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer && commandCode == NetDefine.CMD_SaveRoleDataCode;
    }

    public void OnCall(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer || context.Package == null)
        {
            return;
        }

        // 中心服返回的角色保存结果，交给对应在线角色推进保存状态。
        SaveRoleDataRet result = SaveRoleDataRet.Parser.ParseFrom(context.Package.Data);
        // 保存结果所属的在线角色，离线后不存在时不再更新运行时状态。
        OnlineRole role = RoleManager.Instance.GetOnlineRole(result.RoleId);
        if (role != null)
        {
            role.OnSaveRoleDataResult(result);
        }
    }
}
