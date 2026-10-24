#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务界面联调用的示例数据注入器，仅在编辑器播放模式下编译，打包后整体剔除。
/// 不属于正式任务流程：正式任务必须由服务端任务快照经 TaskMgr.SetTasks 下发。
/// 只在任务集合为空时注入一次，已存在真实数据时不覆盖。
/// </summary>
public static class TaskDevSampleData
{
    /// <summary>示例任务是否已注入，避免切换场景或重复初始化时反复写入。</summary>
    private static bool _injected;

    /// <summary>任务集合为空时注入一批示例任务，供页签切换、列表滚动与详情联动联调。</summary>
    public static void InjectIfEmpty()
    {
        if (_injected)
        {
            return;
        }
        if (TaskMgr.Instance == null)
        {
            return;
        }
        // 任意页签已有数据，说明服务端已下发，示例数据不得覆盖。
        if (TaskMgr.Instance.GetPageTasks(TaskPageFilter.MainLine).Count > 0 ||
            TaskMgr.Instance.GetPageTasks(TaskPageFilter.Daily).Count > 0 ||
            TaskMgr.Instance.GetPageTasks(TaskPageFilter.Activity).Count > 0)
        {
            _injected = true;
            return;
        }
        List<TaskViewData> samples = BuildSamples();
        TaskMgr.Instance.SetTasks(samples.ToArray());
        _injected = true;
        Debug.LogWarning("[TaskDevSampleData] 任务服务端未接入，已注入 " + samples.Count + " 条界面联调示例数据，仅编辑器生效。");
    }

    /// <summary>构造三个页签下的示例任务，覆盖有奖励、有时限、可放弃等展示分支。</summary>
    /// <returns>示例任务列表。</returns>
    private static List<TaskViewData> BuildSamples()
    {
        List<TaskViewData> list = new List<TaskViewData>();

        list.Add(Create(1001, "初入主城", "主线", TaskPageFilter.MainLine, TaskViewState.Accepted,
            "前往主城中央广场，找到指引官并了解这个世界。",
            "与指引官对话（0/1）", true, "剩余 23:59:00", true, false, "经验 +1200"));
        list.Add(Create(1002, "剿灭城外野狼", "主线", TaskPageFilter.MainLine, TaskViewState.Accepted,
            "城外的野狼近期频繁袭击商队，前往南门外清理狼群。",
            "击杀野狼（3/8）", false, string.Empty, false, true, "经验 +2400"));
        list.Add(Create(1003, "收集药草", "主线", TaskPageFilter.MainLine, TaskViewState.Finishable,
            "药师需要新鲜的回魂草来配制伤药。",
            "采集回魂草（5/5）", false, string.Empty, false, false, "金币 +300"));
        list.Add(Create(1004, "前往北境", "主线", TaskPageFilter.MainLine, TaskViewState.Accepted,
            "听说北境出现了异动，前往查看情况。",
            "抵达北境哨站（0/1）", true, "剩余 05:00:00", false, true, "经验 +3600"));

        list.Add(Create(2001, "每日签到", "日常", TaskPageFilter.Daily, TaskViewState.Accepted,
            "每日登录都可以领取一次签到奖励。",
            "完成签到（0/1）", false, string.Empty, false, false, "金币 +100"));
        list.Add(Create(2002, "日常悬赏", "日常", TaskPageFilter.Daily, TaskViewState.Accepted,
            "完成悬赏榜上的委托，换取额外报酬。",
            "完成悬赏（1/3）", false, string.Empty, true, true, "声望 +50"));
        list.Add(Create(2003, "公会捐献", "日常", TaskPageFilter.Daily, TaskViewState.Accepted,
            "为公会仓库捐献物资，提升公会等级。",
            "捐献物资（0/5）", false, string.Empty, false, false, "贡献 +80"));

        list.Add(Create(3001, "限时活动：世界BOSS", "活动", TaskPageFilter.Activity, TaskViewState.Accepted,
            "世界BOSS出现在主城东侧荒地，集结队伍前往讨伐。",
            "参与讨伐（0/1）", true, "剩余 01:30:00", false, false, "宝箱 x1"));
        list.Add(Create(3002, "限时活动：钓鱼大赛", "活动", TaskPageFilter.Activity, TaskViewState.Accepted,
            "钓鱼大赛正在进行中，比拼谁钓到的鱼更多。",
            "钓到稀有鱼（0/3）", true, "剩余 00:45:00", false, true, "金币 +800"));

        return list;
    }

    /// <summary>按字段构造一条示例任务展示数据。</summary>
    private static TaskViewData Create(int id, string title, string typeName, TaskPageFilter page,
        TaskViewState state, string description, string targetText, bool hasDeadline, string deadlineText,
        bool hasLimit, bool canGiveUp, string rewardHint)
    {
        TaskViewData data = new TaskViewData();
        data.TaskId = id;
        data.Title = title;
        data.TypeName = typeName;
        data.Page = page;
        data.State = state;
        data.Description = description;
        data.TargetText = targetText;
        data.HasDeadline = hasDeadline;
        data.DeadlineText = deadlineText;
        data.HasLimit = hasLimit;
        data.LimitText = hasLimit ? "每日限完成 1 次" : string.Empty;
        // 示例数据用描述末尾的奖励提示占位，正式数据由服务端下发奖励列表。
        data.HasReward = string.IsNullOrEmpty(rewardHint) == false;
        data.CanGiveUp = canGiveUp;
        data.NeedTrack = page == TaskPageFilter.MainLine;
        return data;
    }
}
#endif
