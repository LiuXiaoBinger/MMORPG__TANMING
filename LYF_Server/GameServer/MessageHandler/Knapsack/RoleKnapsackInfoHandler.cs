using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>处理 CenterServer 返回的角色背包信息。</summary>
public sealed class RoleKnapsackInfoHandler : IGameMessageHandler
{
    /// <summary>判断当前处理器是否接管中心服角色背包回包。</summary>
    /// <param name="source">消息来源。</param>
    /// <param name="commandCode">协议号。</param>
    /// <returns>来源和协议匹配时返回 true。</returns>
    public bool CanHandle(GameCommandSource source, ushort commandCode)
    {
        return source == GameCommandSource.CenterServer && commandCode == NetDefine.CMD_RoleKnapsackInfoCode;
    }

    /// <summary>装载角色背包和次数数据，并在完整装载后解除保存门闩。</summary>
    /// <param name="context">中心服消息上下文。</param>
    public void Handle(GameMessageHandlerContext context)
    {
        if (context == null || context.Source != GameCommandSource.CenterServer ||
            context.Package == null || context.Package.Data == null)
        {
            LogMsg.Info("角色背包信息回包为空", LogMsgType.Error);
            return;
        }
        try
        {
            RoleKanpsackInfoRet result = RoleKanpsackInfoRet.Parser.ParseFrom(context.Package.Data);
            if (result == null || result.CmdCode != CmdCode.Succeed || result.RoleKanpsackInfo == null)
            {
                LogMsg.Info("角色背包信息回包不完整，暂不解除角色数据加载门闩", LogMsgType.Error);
                return;
            }

            OnlineRole role = RoleManager.Instance.GetOnlineRoleBySession(
                context.Package.GateSessionId, context.Package.UnitySessionId);
            if (role == null)
            {
                LogMsg.Info("角色背包信息找不到对应在线角色", LogMsgType.Error);
                return;
            }

            // 登录回包携带中心服权威角色版本，先建立本地保存版本基线。
            if (!role.InitializeSaveVersion(result.RoleKanpsackInfo.SaveVersion))
            {
                return;
            }

            RoItemComponent itemComponent = role.GetComponent<RoItemComponent>();
            if (itemComponent != null)
            {
                // 回包已经由统一入口转发客户端，GameServer 同时装载权威运行时背包。
                itemComponent.LoadKnapsack(result);
            }

            // 次数数据与背包数据同包返回，交给次数组件恢复运行时状态。
            RoleCountComponent countComponent;
            if (role.TryGetComponent(out countComponent))
            {
                List<RoleCountItemInfo> values = new List<RoleCountItemInfo>();
                List<RoleCountInfo> countInfos =
                    new List<RoleCountInfo>(result.RoleKanpsackInfo.RoleCountInfoList);
                for (int i = 0; i < countInfos.Count; i++)
                {
                    RoleCountInfo info = countInfos[i];
                    DateTime refreshTime = DateTime.MinValue;
                    if (info.LastRefreshTime > 0L)
                    {
                        refreshTime = new DateTime(info.LastRefreshTime, DateTimeKind.Utc).ToLocalTime();
                    }
                    values.Add(new RoleCountItemInfo(info.Action, info.CountKey, info.Count)
                    {
                        LastRefreshDate = refreshTime
                    });
                }
                countComponent.Load(values);
            }

            // 两个组件都从同一份中心服回包完成装载后，才允许业务修改和延迟保存。
            if (itemComponent == null || countComponent == null || !role.CompleteRoleDataLoad())
            {
                LogMsg.Info("角色背包或次数组件未完成初始化，继续保持角色数据加载门闩", LogMsgType.Error);
            }
        }
        catch (InvalidProtocolBufferException ex)
        {
            LogMsg.Info("角色背包信息解析失败: " + ex.Message, LogMsgType.Error);
        }
    }
}
