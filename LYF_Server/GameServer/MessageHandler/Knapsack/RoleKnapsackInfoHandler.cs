using System;
using System.Collections.Generic;
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

    /// <summary>装载角色背包和次数数据，全部完成后才允许在线业务改动物品并产生存档。</summary>
    /// <param name="context">中心服消息上下文。</param>
    public void OnCall(GameMessageHandlerContext context)
    {
        // 上下文缺失或来源不是中心服时丢弃该回包。
        if (context == null || context.Source != GameCommandSource.CenterServer)
        {
            LogMsg.Info("角色背包信息回包为空", LogMsgType.Error);
            return;
        }
        // 协议包或包数据缺失同样视为空回包。
        if (context.Package == null || context.Package.Data == null)
        {
            LogMsg.Info("角色背包信息回包为空", LogMsgType.Error);
            return;
        }
        // 中心服返回的背包与次数数据，是本次在线角色初始化的权威来源。
        RoleKanpsackInfoRet result = RoleKanpsackInfoRet.Parser.ParseFrom(context.Package.Data);
        if (result == null || result.CmdCode != CmdCode.Succeed || result.RoleKanpsackInfo == null)
        {
            LogMsg.Info("角色背包信息回包不完整，暂不解除角色数据加载门闩", LogMsgType.Error);
            return;
        }

        // 回包复合会话对应的在线角色，后续只更新该角色的运行时组件。
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

        // 当前角色的物品组件，负责装载中心服背包快照。
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
            // 转换后的次数记录，全部构造完成后一次交给次数组件。
            List<RoleCountItemInfo> values = new List<RoleCountItemInfo>();
            // 本次回包中的次数记录快照，按索引读取并保留原记录引用。
            List<RoleCountInfo> countInfos =
                new List<RoleCountInfo>(result.RoleKanpsackInfo.RoleCountInfoList);
            // i 为本次次数快照的索引，范围为 0 到记录数量减一。
            for (int i = 0; i < countInfos.Count; i++)
            {
                // 当前次数记录，包含次数键、数量和上次刷新时间。
                RoleCountInfo info = countInfos[i];
                // 组件使用的本地刷新时间；服务端未提供时保持 DateTime.MinValue。
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
            return;
        }
        // 已穿戴装备从存档恢复后再计算效果，裸属性不写入装备加成。
        role.GetComponent<RoleEquipComponent>().OnEquipChange();
    }
}
