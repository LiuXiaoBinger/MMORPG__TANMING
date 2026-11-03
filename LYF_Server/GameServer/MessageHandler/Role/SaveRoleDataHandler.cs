/// <summary>处理 CenterServer 在事务提交后返回的角色数据保存 ACK。</summary>
public sealed class SaveRoleDataHandler : IGameMessageHandler
{
    /// <summary>只匹配中心服角色存档结果。</summary>
    /// <param name="source">消息来自网关或中心服。</param>
    /// <param name="commandCode">消息的协议编号。</param>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer && commandCode == NetDefine.CMD_SaveRoleDataCode;
    }

    /// <summary>按角色编号把保存结果交给仍保留在内存中的角色，包括等待保存完成的离线角色。</summary>
    /// <param name="context">逻辑线程中的消息、会话和原网络连接。</param>
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
