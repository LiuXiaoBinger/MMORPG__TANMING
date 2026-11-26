using System;
using System.Collections.Generic;
using System.Numerics;
using cfg;
using Google.Protobuf;

/// <summary>在 GS 逻辑线程处理角色委托，任务状态与进度随角色次数、背包一同保存。</summary>
internal static class WorldQuestSystem
{
    /// <summary>单次目标的类型值与当前发布的 QuestInfo.TargetType 对应。</summary>
    internal const int Kill = 1;
    /// <summary>交付时从背包取走配置数量的物品。</summary>
    internal const int Collect = 2;
    /// <summary>玩家必须在目标 NPC 的对话框中确认。</summary>
    internal const int Talk = 3;

    /// <summary>根据角色存档及真实背包构造可见委托，已经结束的任务只保留在存档中。</summary>
    /// <param name="role">已装载角色数据的会话所有者。</param><param name="message">本次操作的结果提示。</param>
    internal static WorldQuestReply Snapshot(OnlineRole role, string message = "")
    {
        // 客户端依据角色编号接收这份完整的普通任务列表。
        WorldQuestReply reply = new WorldQuestReply { RoleId = (int)role.GetID(), Message = message };
        // 主表按发布顺序展示，避免依赖字典枚举顺序。
        List<QuestInfo> quests = LubanMgr.Instance.ConfigTables.TbQuestInfo.DataList;
        // 配置行下标也是默认的追踪优先级。
        for (int i = 0; i < quests.Count; i++)
        {
            // 本行根据角色进度生成，尚未解锁或完成时省略。
            WorldQuestEntry entry = ReadEntry(role, quests[i]);
            if (entry != null) reply.Entries.Add(entry);
        }
        return reply;
    }

    /// <summary>将权威进度送达已装载这一角色的客户端。</summary>
    /// <param name="role">登录、物品变化或击杀的实际角色。</param>
    internal static void Publish(OnlineRole role)
    {
        role.SendToClient(NetDefine.CMD_WorldQuest, Snapshot(role).ToByteString());
    }

    /// <summary>以任务编号关联永久记录，自动接取只在满足前置的首次读取时发生。</summary>
    /// <param name="role">角色当前的服务端对象。</param><param name="quest">发布中的委托模板。</param>
    private static WorldQuestEntry ReadEntry(OnlineRole role, QuestInfo quest)
    {
        // 当前角色的次数组件已经参与角色原子快照与跨服交接。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        // 零值表示尚未接取，完成项永久用于前置判断。
        WorldQuestState state = (WorldQuestState)counts.GetCount(RoleCountAction.QuestState, quest.Id);
        if (state == WorldQuestState.QuestDone) return null;
        if (state == WorldQuestState.QuestAvailable)
        {
            if (role.mainRoleInfo.BaseInfo.Level < quest.MinLevel) return null;
            if (quest.PreQuestId > 0 && counts.GetCount(RoleCountAction.QuestState, quest.PreQuestId) != (int)WorldQuestState.QuestDone) return null;
            if (quest.IsAutoAccept)
            {
                state = WorldQuestState.QuestActive;
                counts.SetCount(RoleCountAction.QuestState, quest.Id, (int)state);
            }
        }
        // 可接阶段前往发布人；接取后根据目标类型选择本阶段的人物。
        WorldQuestEntry entry = new WorldQuestEntry { QuestId = quest.Id, State = state, NpcId = quest.AcceptNpcId };
        if (state == WorldQuestState.QuestAvailable) return entry;
        if (quest.TargetType == Collect)
            entry.Current = role.GetComponent<RoItemComponent>().GetItemNum(quest.TargetId, ItemBind.kItemBindIs);
        else
            entry.Current = (int)counts.GetCount(RoleCountAction.QuestProgress, quest.Id);
        entry.Current = Math.Min(quest.TargetCount, entry.Current);
        if (entry.Current >= quest.TargetCount)
        {
            entry.State = WorldQuestState.QuestReady;
            entry.NpcId = quest.SubmitNpcId;
        }
        else if (quest.TargetType == Talk) entry.NpcId = quest.TargetId;
        else if (quest.TargetType == Kill) entry.NpcId = 0;
        return entry;
    }

    /// <summary>核对玩家当前的 NPC 操作，按状态执行一次接取、对话、交付或放弃。</summary>
    /// <param name="role">发包会话绑定的在线角色。</param><param name="request">只包含意图、委托和交谈对象。</param>
    internal static string Apply(OnlineRole role, WorldQuestRequest request)
    {
        if (request.Action == WorldQuestAction.QuestRead) return string.Empty;
        // 所有奖励、数量和目标只能从 GS 的发布表读取。
        QuestInfo quest = LubanMgr.Instance.GetQuestInfoById(request.QuestId);
        if (quest == null) return "未找到该委托";
        // 每次操作重新读取权威状态，完成过的任务不会再进入发奖分支。
        WorldQuestEntry entry = ReadEntry(role, quest);
        if (entry == null) return "该委托已结束或尚未开放";
        // 服务端永久进度及击杀计数都按配置编号隔离。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        if (request.Action == WorldQuestAction.QuestAbandon)
        {
            if (quest.Type == 1 || entry.State == WorldQuestState.QuestAvailable) return "当前不能放弃该委托";
            counts.SetCount(RoleCountAction.QuestProgress, quest.Id, 0);
            counts.SetCount(RoleCountAction.QuestState, quest.Id, (int)WorldQuestState.QuestAvailable);
            return "已放弃 · " + quest.Name;
        }
        // 任务人物采用实际出生表的地图及米制坐标，与正常 NPC 对话距离一致。
        NpcParseInfo npc = LubanMgr.Instance.GetNpcInfoById(request.NpcId);
        // 位置信息来自角色移动同步，客户端提交的动作不包含位置。
        RoleBaseInfo location = role.mainRoleInfo.BaseInfo;
        if (npc == null || npc.ID != entry.NpcId || location.InstanceId != 0) return "请与当前任务人物对话";
        if (location.MapId != npc.MapID || Vector3.Distance(location.Pos.ToVector3(), npc.Position) > 11f)
            return "请走近任务人物";
        switch (request.Action)
        {
            case WorldQuestAction.QuestAccept:
                if (entry.State != WorldQuestState.QuestAvailable) break;
                counts.SetCount(RoleCountAction.QuestState, quest.Id, (int)WorldQuestState.QuestActive);
                return "已接取 · " + quest.Name;
            case WorldQuestAction.QuestTalk:
                if (entry.State != WorldQuestState.QuestActive || quest.TargetType != Talk) break;
                counts.SetCount(RoleCountAction.QuestProgress, quest.Id, quest.TargetCount);
                if (quest.IsAutoSubmit && quest.SubmitNpcId == npc.ID) return Deliver(role, quest);
                return "已完成对话，请前往交付";
            case WorldQuestAction.QuestDeliver:
                if (entry.State == WorldQuestState.QuestReady) return Deliver(role, quest);
                break;
        }
        return "委托进度已更新，请查看当前目标";
    }

    /// <summary>在同一游戏线程完成奖励入包、任务物品扣除和永久完成标记。</summary>
    /// <param name="role">领取奖励的角色。</param><param name="quest">已核对目标、人物和前置状态的委托。</param>
    private static string Deliver(OnlineRole role, QuestInfo quest)
    {
        // 使用与进度查询相同的角色背包和绑定范围。
        RoItemComponent items = role.GetComponent<RoItemComponent>();
        // 奖励整体进入背包前，AddItem 会先核对全部所需容量。
        ChangeReason reason = new ChangeReason(ItemChangeReason.ITEM_REASON_TASK_REWARD);
        if (quest.RewardItemId > 0 && items.AddItem(quest.RewardItemId, quest.RewardItemCount, reason,
            ItemSign.IsNoSign, 0, 0, UpdateType.eUT_Update_All, default(IdipParam)) == null) return "奖励空间不足，请整理背包后再交付";
        if (quest.TargetType == Collect)
        {
            // 扣除的物品与本帧 ReadEntry 检查的是同一批堆叠。
            CmdCode result = items.ReduceItem(quest.TargetId, quest.TargetCount, ItemChangeReason.ITEM_REASON_TASK_REMOVE, ItemBind.kItemBindIs);
            if (result != CmdCode.Succeed)
            {
                if (quest.RewardItemId > 0) items.ReduceItem(quest.RewardItemId, quest.RewardItemCount, ItemChangeReason.ITEM_REASON_TASK_REMOVE, ItemBind.kItemBindNo);
                return "任务物品不足";
            }
        }
        role.GetComponent<RoleCountComponent>().SetCount(RoleCountAction.QuestState, quest.Id, (int)WorldQuestState.QuestDone);
        items.NotifyItemChanges();
        return "已完成 · " + quest.Name;
    }

    /// <summary>只在真实击杀或角色实有物品变动与已接目标匹配时更新追踪栏。</summary>
    /// <param name="role">实际击杀者或背包所有者。</param><param name="type">击杀或收集目标类型。</param><param name="subject">怪物配置编号或道具类型编号。</param>
    internal static void OnTargetChanged(OnlineRole role, int type, int subject)
    {
        if (role == null || !role.IsRoleDataReady()) return;
        // 未接取及已经交付的委托不会消费这次事件。
        RoleCountComponent counts = role.GetComponent<RoleCountComponent>();
        // 现行委托主表行，只有匹配的目标才读取角色记录。
        List<QuestInfo> quests = LubanMgr.Instance.ConfigTables.TbQuestInfo.DataList;
        // 多条委托可共享同一事件，所有匹配条目更新后只发布一份列表。
        bool changed = false;
        // 本次事件检查的配置行下标。
        for (int i = 0; i < quests.Count; i++)
        {
            // 本行定义了所需的怪物或背包物品。
            QuestInfo quest = quests[i];
            if (quest.TargetType != type || quest.TargetId != subject) continue;
            if (counts.GetCount(RoleCountAction.QuestState, quest.Id) != (int)WorldQuestState.QuestActive) continue;
            if (type == Kill)
            {
                // 每次真实死亡贡献一次，达到配置数量后停止累计。
                long current = counts.GetCount(RoleCountAction.QuestProgress, quest.Id);
                if (current >= quest.TargetCount) continue;
                counts.SetCount(RoleCountAction.QuestProgress, quest.Id, current + 1);
            }
            changed = true;
        }
        if (changed) Publish(role);
    }
}
