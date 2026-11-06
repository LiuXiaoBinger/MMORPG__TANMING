using System;
using System.Collections.Generic;

/// <summary>
/// 客户端任务集合与追踪状态，对应 RO 的 TaskMgr。
/// 只保存服务端同步后的任务展示数据，供任务主窗口与快捷面板共用同一份来源；
/// 不自行推导进度、不本地发奖、不伪造任务完成。
/// </summary>
public class TaskMgr : Singleton<TaskMgr>
{

    #region 字段、事件与属性
    /// <summary>快捷面板最多同时展示的任务条数上限。</summary>
    private const int MaxTrackShowCount = 3;

    /// <summary>当前持有的全部任务展示数据，按服务端快照整体替换。</summary>
    private readonly List<TaskViewData> _allTasks = new List<TaskViewData>();

    /// <summary>当前副本阶段投影出的任务条目，退出时单独清除，不覆盖普通任务快照。</summary>
    private readonly List<TaskViewData> _dungeonTasks = new List<TaskViewData>();

    /// <summary>当前选中的页签，窗口与快捷面板共用。</summary>
    private TaskPageFilter _currentPage = TaskPageFilter.MainLine;

    /// <summary>当前追踪任务的 ID，为 0 表示没有追踪任务。</summary>
    private int _trackedTaskId;

    /// <summary>任务集合或追踪状态发生变化时触发，界面据此整体刷新。</summary>
    public event Action TaskDataChanged;

    /// <summary>追踪任务变化时触发，参数为新的追踪任务，可能为 null。</summary>
    public event Action<TaskViewData> TrackedTaskChanged;

    #region 查询与判定
    #endregion

    /// <summary>按页签筛选任务，窗口与快捷面板共用同一结果。</summary>
    /// <param name="page">目标页签。</param>
    /// <returns>该页签下的任务列表，按加入顺序排列，无数据时为空列表。</returns>
    public List<TaskViewData> GetPageTasks(TaskPageFilter page)
    {
        // 筛选出的该页签任务列表，按加入顺序排列。
        List<TaskViewData> result = new List<TaskViewData>();
        // 副本阶段沿服务器顺序放在任务页前方，普通任务仍按原顺序保留。
        for (int i = 0; i < _dungeonTasks.Count; i++)
        {
            if (_dungeonTasks[i].Page == page) result.Add(_dungeonTasks[i]);
        }
        // 循环索引：遍历全部任务按页签过滤。
        for (int i = 0; i < _allTasks.Count; i++)
        {
            // 当前遍历到的任务数据。
            TaskViewData data = _allTasks[i];
            if (data.Page == page)
            {
                result.Add(data);
            }
        }
        return result;
    }

    /// <summary>获取快捷面板要展示的追踪任务，最多取上限条数。</summary>
    /// <returns>需要追踪的任务列表，首期默认取主线中需要追踪的任务。</returns>
    public List<TaskViewData> GetTrackedTasks()
    {
        // 需要追踪的任务结果列表，受上限条数约束。
        List<TaskViewData> result = new List<TaskViewData>();
        // 快捷追踪只占用一行副本目标，优先下一条未完成阶段，其他行继续显示普通任务。
        for (int i = 0; i < _dungeonTasks.Count; i++)
        {
            // 当前阶段必须属于既有追踪页签，并保留服务器的完成状态。
            TaskViewData stage = _dungeonTasks[i];
            if (stage.Page != TaskPageFilter.MainLine || stage.State == TaskViewState.Finished) continue;
            result.Add(stage);
            break;
        }
        // 全部完成后保留最后阶段，显示服务端通关结果直到离开副本。
        if (result.Count == 0 && _dungeonTasks.Count > 0)
        {
            // 最后一条阶段已经包含通关出口提示。
            TaskViewData finalStage = _dungeonTasks[_dungeonTasks.Count - 1];
            if (finalStage.Page == TaskPageFilter.MainLine) result.Add(finalStage);
        }
        // 主线页签下的全部任务，追踪只从主线取。
        List<TaskViewData> mainLine = GetPageTasks(TaskPageFilter.MainLine);
        // 循环索引：按主线顺序挑选需要追踪的任务，满上限即停。
        for (int i = 0; i < mainLine.Count; i++)
        {
            if (result.Count >= MaxTrackShowCount)
            {
                break;
            }
            if (!mainLine[i].IsDungeonObjective && mainLine[i].NeedTrack)
            {
                result.Add(mainLine[i]);
            }
        }
        return result;
    }

    /// <summary>获取当前追踪的任务，没有追踪时返回 null。</summary>
    /// <returns>追踪中的任务展示数据或 null。</returns>
    public TaskViewData GetTrackedTask()
    {
        return GetTaskById(_trackedTaskId);
    }

    /// <summary>获取当前页签。</summary>
    /// <returns>当前选中的页签。</returns>
    public TaskPageFilter GetCurrentPage()
    {
        return _currentPage;
    }

    /// <summary>按任务 ID 查找展示数据。</summary>
    /// <param name="taskId">目标任务 ID。</param>
    /// <returns>命中的任务数据，未命中或 ID 为 0 时返回 null。</returns>
    public TaskViewData GetTaskById(int taskId)
    {
        if (taskId == 0)
        {
            return null;
        }
        // 副本显示ID使用负的目标编号，与普通任务配置编号分开。
        for (int i = 0; i < _dungeonTasks.Count; i++)
        {
            if (_dungeonTasks[i].TaskId == taskId) return _dungeonTasks[i];
        }
        // 循环索引：顺序查找 ID 匹配的任务，命中即返回。
        for (int i = 0; i < _allTasks.Count; i++)
        {
            if (_allTasks[i].TaskId == taskId)
            {
                return _allTasks[i];
            }
        }

    #region 核心逻辑
    #endregion
        return null;
    }

    /// <summary>用服务端下发的任务快照整体替换本地集合，并按需重选默认追踪。</summary>
    /// <param name="tasks">服务端任务展示数据数组，为空表示清空到空态。</param>
    public void SetTasks(TaskViewData[] tasks)
    {
        _allTasks.Clear();
        if (tasks != null)
        {
            // 循环索引：按快照顺序写入全部任务，跳过空元素。
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null)
                {
                    _allTasks.Add(tasks[i]);
                }
            }
        }
        ResetTrackedIfNeeded();
        NotifyTaskDataChanged();
    }

    /// <summary>仅替换当前副本来源的任务展示，普通任务快照和玩家选择仍由原流程维护。</summary>
    /// <param name="tasks">服务器副本目标转换出的条目，空数组表示退出副本。</param>
    public void SetDungeonTasks(TaskViewData[] tasks)
    {
        _dungeonTasks.Clear();
        // 按权威目标顺序存入当前副本条目，不修改任务进度。
        for (int i = 0; i < tasks.Length; i++) _dungeonTasks.Add(tasks[i]);
        ResetTrackedIfNeeded();
        NotifyTaskDataChanged();
    }

    /// <summary>设置当前追踪任务，仅更新界面追踪标识，不改变业务状态。</summary>
    /// <param name="taskId">目标任务 ID，传 0 取消追踪。</param>
    public void SetTrackedTask(int taskId)
    {
        if (_trackedTaskId == taskId)
        {
            return;
        }
        _trackedTaskId = taskId;
        // 新追踪任务的数据，ID 无对应任务时为空。
        TaskViewData tracked = GetTaskById(taskId);
        if (TrackedTaskChanged != null)
        {
            TrackedTaskChanged.Invoke(tracked);
        }
        NotifyTaskDataChanged();
    }

    /// <summary>切换当前页签，窗口与快捷面板读取同一值。</summary>
    /// <param name="page">目标页签。</param>
    public void SetCurrentPage(TaskPageFilter page)
    {
        if (_currentPage == page)
        {
            return;
        }
        _currentPage = page;
        NotifyTaskDataChanged();
    }

    /// <summary>当没有追踪任务时，把主线中首个需要追踪的任务设为默认追踪。</summary>
    private void ResetTrackedIfNeeded()
    {
        // 当前追踪 ID 对应的任务，为空说明原追踪已失效需重选。
        TaskViewData current = GetTaskById(_trackedTaskId);
        if (current != null)
        {
            return;
        }
        // 重选候选的追踪任务列表。
        List<TaskViewData> tracked = GetTrackedTasks();
        _trackedTaskId = 0;
        if (tracked.Count > 0)
    #endregion
        {
            _trackedTaskId = tracked[0].TaskId;
        }
    }

    /// <summary>通知界面任务数据变化。</summary>
    private void NotifyTaskDataChanged()
    {
        if (TaskDataChanged != null)
        {
            TaskDataChanged.Invoke();
        }
    }
}
