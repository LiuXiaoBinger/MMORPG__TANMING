using System.Collections.Generic;
using cfg;
using Google.Protobuf;

/// <summary>将普通委托快照投影到已有任务界面，将 NPC 对话中的选择发给游戏服。</summary>
public partial class TaskCtrl
{
    /// <summary>只接收本角色的权威委托列表，保留 TaskMgr 单独维护的副本目标。</summary>
    /// <param name="bytes">可靠通道中的 WorldQuestReply 数据。</param>
    private void OnWorldQuestReply(ByteString bytes)
    {
        // 快照只包含任务状态、实有数量与当前目标人物。
        WorldQuestReply reply = WorldQuestReply.Parser.ParseFrom(bytes);
        // 场景切换沿用同一角色，新角色不能继承旧界面的进度。
        PlayerEntity role = Global.Instance.SceneWorld?.LocalRole;
        if (role == null || role.BaseInfo.RoleId != reply.RoleId) return;
        // 两个现有窗口共用同一批展示条目。
        List<TaskViewData> tasks = new List<TaskViewData>();
        // 顺序由服务端任务目录决定。
        for (int i = 0; i < reply.Entries.Count; i++)
        {
            // 当前任务在服务器角色存档中的状态。
            WorldQuestEntry entry = reply.Entries[i];
            // 名称、说明、目标上限和奖励全部来自同一份发布表。
            QuestInfo quest = LubanMgr.Instance.GetQuestInfoById(entry.QuestId);
            if (quest == null) continue;
            // 可接与进行中的任务共用原任务页，支线可以放弃并重新接取。
            TaskViewData task = new TaskViewData { TaskId = quest.Id, Quest = entry, Title = quest.Name,
                Page = (TaskPageFilter)1, Description = quest.Description, TypeName = "主线", State = TaskViewState.Accepted };
            if (quest.Type == 2) task.TypeName = "支线";
            task.CanGiveUp = quest.Type == 2 && entry.State != WorldQuestState.QuestAvailable;
            if (quest.RewardItemId > 0)
                task.Description += "\n奖励：" + LubanMgr.Instance.GetItemInfoById(quest.RewardItemId).Name + " ×" + quest.RewardItemCount;
            // 任务目标名直接沿当阶段的人物或物品配置查找。
            NpcParseInfo npc = LubanMgr.Instance.GetNpcInfoById(entry.NpcId);
            if (entry.State == WorldQuestState.QuestAvailable)
            {
                task.State = TaskViewState.Available;
                task.TargetText = "可接取 · 找" + npc.Name;
            }
            else if (entry.State == WorldQuestState.QuestReady)
            {
                task.State = TaskViewState.Finishable;
                task.TargetText = "可交付 · 找" + npc.Name;
            }
            else
            {
                if (quest.TargetType == 3) task.TargetText = "与" + npc.Name + "对话";
                else if (quest.TargetType == 2) task.TargetText = "收集" + LubanMgr.Instance.GetItemInfoById(quest.TargetId).Name;
                else task.TargetText = quest.Description;
                task.TargetText += "（" + entry.Current + "/" + quest.TargetCount + "）";
            }
            tasks.Add(task);
        }
        TaskMgr.Instance.SetTasks(tasks.ToArray());
        if (!string.IsNullOrEmpty(reply.Message)) TipsMgr.Instance.ShowSystemTips(reply.Message);
    }

    /// <summary>把这个人物当前可办理的委托放在既有商店、马车等服务前。</summary>
    /// <param name="npcId">正在说话的公共地图 NPC。</param><param name="services">来自该人物对白配置的常规功能。</param>
    internal NpcDialogueCatalog.Service[] GetQuestServices(int npcId, NpcDialogueCatalog.Service[] services)
    {
        // 已接目标优先于可接的新委托，便于抵达后找到当前操作。
        List<NpcDialogueCatalog.Service> options = new List<NpcDialogueCatalog.Service>();
        // 正式发布委托均显示在任务页；副本条目没有普通任务协议记录。
        List<TaskViewData> tasks = TaskMgr.Instance.GetPageTasks((TaskPageFilter)1);
        // 每个属于本人的操作都携带明确的任务编号。
        for (int i = 0; i < tasks.Count; i++)
        {
            // 当前显示条目及其服务端阶段。
            TaskViewData task = tasks[i];
            if (task.Quest == null || task.Quest.NpcId != npcId) continue;
            // 服务选项仍然走 TalkWindow 的既有按钮模板。
            string verb;
            if (task.Quest.State == WorldQuestState.QuestAvailable) verb = "接取";
            else if (task.Quest.State == WorldQuestState.QuestReady) verb = "交付";
            else if (LubanMgr.Instance.GetQuestInfoById(task.TaskId).TargetType == 3) verb = "交谈";
            else continue;
            options.Add(new NpcDialogueCatalog.Service { Label = verb + " · " + task.Title,
                Function = NpcFunction.QuestAction, QuestId = task.TaskId });
        }
        if (services != null) options.AddRange(services);
        return options.ToArray();
    }

    /// <summary>玩家明确点击人物的委托选项后才发送接取、目标对话或交付。</summary>
    /// <param name="npc">当前对话中的真实人物。</param><param name="questId">选中按钮绑定的任务编号。</param>
    internal void ConfirmNpcQuest(NpcEntity npc, int questId)
    {
        // 快照可能已经刷新，按稳定编号读取当前状态。
        TaskViewData task = TaskMgr.Instance.GetTaskById(questId);
        if (task == null || task.Quest == null || task.Quest.NpcId != npc.NpcID) return;
        // 提交动作来自当前明确选择的对话选项。
        WorldQuestAction action = WorldQuestAction.QuestTalk;
        if (task.Quest.State == WorldQuestState.QuestAvailable) action = WorldQuestAction.QuestAccept;
        else if (task.Quest.State == WorldQuestState.QuestReady) action = WorldQuestAction.QuestDeliver;
        SendQuest(action, task.TaskId, npc.NpcID);
    }

    /// <summary>通过可靠通道提交单个委托操作。</summary>
    /// <param name="action">查询、接取、交谈、交付或放弃。</param><param name="questId">目标委托，查询时为零。</param>
    /// <param name="npcId">正在交谈的人物，查询或放弃时为零。</param>
    internal static void SendQuest(WorldQuestAction action, int questId, int npcId)
    {
        MNetClient.Instance.Send(NetDefine.CMD_WorldQuest, new WorldQuestRequest { Action = action,
            QuestId = questId, NpcId = npcId }.ToByteString());
    }
}
