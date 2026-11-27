using System;

/// <summary>
/// 连接任务主窗口、快捷任务面板和 TaskMgr：同步选中任务与页签，处理窗口开关。
/// 普通委托由游戏服驱动；副本目标取自当前实例，点击条目后沿现有导航网前进。
/// </summary>
public partial class TaskCtrl : CtrlBase
{

    #region 字段
    /// <summary>主界面视图，持有任务窗口与快捷面板。</summary>
    private readonly MainView _mainView;

    /// <summary>任务主窗口，绑定 Task.prefab。</summary>
    private readonly TaskWindow _taskWindow;

    /// <summary>快捷任务面板，绑定 QuickTaskPanel.prefab。</summary>
    private readonly QuickTaskPanelWidget _quickPanel;

    /// <summary>界面事件是否已注册，避免重复订阅。</summary>
    private bool _actionsRegistered;

    #region 构造、初始化与事件注册
    #endregion

    /// <summary>创建任务控制器并注册任务界面事件。</summary>
    /// <param name="view">主界面视图实例。</param>
    public TaskCtrl(UIBase view) : base(view)
    {
        _mainView = view as MainView;
        if (_mainView == null)
        {
            return;
        }
        _taskWindow = _mainView.TaskWindow;
        _quickPanel = _mainView.QuickTaskPanel;
        RegisterActions();
        TeamData.Instance.DungeonFlowUpdated += RefreshDungeonTasks;
        TeamData.Instance.BossProgressUpdated += RefreshDungeonTasks;
        RefreshDungeonTasks();
    }

    /// <summary>注册任务窗口与快捷面板抛出的界面事件，已注册时跳过。</summary>
    private void RegisterActions()
    {
        if (_actionsRegistered)
        {
            return;
        }
        if (_taskWindow != null)
        {
            _taskWindow.CloseRequested += OnTaskWindowCloseRequested;
            _taskWindow.TaskSelected += OnTaskSelected;
            _taskWindow.AcceptRequested += RequestAccept;
            _taskWindow.DeliverRequested += RequestDeliver;
            _taskWindow.GiveUpRequested += RequestGiveUp;
            _taskWindow.NavigateRequested += RequestNavigate;
            _taskWindow.PageChanged += OnPageChanged;
        }
        if (_quickPanel != null)
        {
            _quickPanel.TaskSelected += OnTaskSelected;
            _quickPanel.PageChanged += OnPageChanged;
            _quickPanel.TaskWindowRequested += OnQuickPanelOpenTaskWindow;
            _quickPanel.NavigateRequested += RequestNavigate;
            _quickPanel.GiveUpRequested += RequestGiveUp;
        }
        if (TaskMgr.Instance != null)
        {
            TaskMgr.Instance.TaskDataChanged += OnTaskDataChanged;
        }
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_WorldQuest, OnWorldQuestReply);
        _actionsRegistered = true;
    }

    /// <summary>注销网络、输入和视图事件，控制器销毁时调用。</summary>
    public override void Dispose()
    {
        TeamData.Instance.DungeonFlowUpdated -= RefreshDungeonTasks;
        TeamData.Instance.BossProgressUpdated -= RefreshDungeonTasks;
        if (!_actionsRegistered)
        {
            return;
        }
        if (_taskWindow != null)
        {
            _taskWindow.CloseRequested -= OnTaskWindowCloseRequested;
            _taskWindow.TaskSelected -= OnTaskSelected;
            _taskWindow.AcceptRequested -= RequestAccept;
            _taskWindow.DeliverRequested -= RequestDeliver;
            _taskWindow.GiveUpRequested -= RequestGiveUp;
            _taskWindow.NavigateRequested -= RequestNavigate;
            _taskWindow.PageChanged -= OnPageChanged;
        }
        if (_quickPanel != null)
        {
            _quickPanel.TaskSelected -= OnTaskSelected;
            _quickPanel.PageChanged -= OnPageChanged;
            _quickPanel.TaskWindowRequested -= OnQuickPanelOpenTaskWindow;
            _quickPanel.NavigateRequested -= RequestNavigate;
            _quickPanel.GiveUpRequested -= RequestGiveUp;
        }
        if (TaskMgr.Instance != null)
        {
            TaskMgr.Instance.TaskDataChanged -= OnTaskDataChanged;
        }

    #region 交互响应
    #endregion
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_WorldQuest, OnWorldQuestReply);
        CancelQuestRoute();
        TaskMgr.Instance.SetTasks(null);
        _actionsRegistered = false;
    }

    /// <summary>任务数据变化后同时刷新主窗口与快捷面板，两者共用同一份数据。</summary>
    private void OnTaskDataChanged()
    {
        RefreshAll();
    }

    /// <summary>选中任务时切换追踪，主窗口与快捷面板共用同一追踪标识。</summary>
    /// <param name="data">被点击的任务展示数据。</param>
    private void OnTaskSelected(TaskViewData data)
    {
        if (data == null)
        {
            return;
        }
        if (TaskMgr.Instance == null)
        {
            return;
        }
        TaskMgr.Instance.SetTrackedTask(data.TaskId);
    }

    /// <summary>切换页签，窗口与快捷面板读取同一个当前页签。</summary>
    /// <param name="page">目标页签。</param>
    private void OnPageChanged(TaskPageFilter page)
    {
        if (TaskMgr.Instance == null)
        {

    #region 事件回调
    #endregion
            return;
        }
        TaskMgr.Instance.SetCurrentPage(page);
    }

    /// <summary>快捷面板请求打开任务窗口时联动展开折叠列表，保证任务条目可见。</summary>
    private void OnQuickPanelOpenTaskWindow()
    {
        if (_quickPanel != null)
        {
            _quickPanel.SetFolded(false);
        }
    }

    /// <summary>关闭任务主窗口请求，只隐藏窗口不改变业务状态。</summary>
    private void OnTaskWindowCloseRequested()

    #region 网络请求与消息
    #endregion
    {
        if (_taskWindow != null)
        {
            _taskWindow.CloseWindow();
        }
    }

    /// <summary>任务入口请求打开任务窗口：先经快捷面板事件联动展开折叠，再切换窗口显隐。</summary>
    public void RequestOpenTaskWindow()
    {
        if (_quickPanel != null)
        {
            _quickPanel.RequestOpenTaskWindow();
        }
        OpenTaskWindow();
    }

    /// <summary>在任务簿点击接取时前往该委托的发布人。</summary>
    /// <param name="data">当前任务簿选中的委托。</param>
    public void RequestAccept(TaskViewData data) { RequestNavigate(data); }

    /// <summary>在任务簿点击交付时前往交付人，由 NPC 对话确认领取。</summary>
    /// <param name="data">可交付的角色委托。</param>
    public void RequestDeliver(TaskViewData data) { RequestNavigate(data); }

    /// <summary>请求游戏服清除此支线的接取状态和累计数量。</summary>
    /// <param name="data">点击放弃按钮的委托。</param>
    public void RequestGiveUp(TaskViewData data)
    {
        if (data == null || !data.CanGiveUp) return;
        if (_routeQuestId == data.TaskId) CancelQuestRoute();
        SendQuest(WorldQuestAction.QuestAbandon, data.TaskId, 0);
    }
    #endregion

    /// <summary>切换任务主窗口显隐，关闭时只隐藏窗口不清理任务数据。</summary>
    public void OpenTaskWindow()
    {
        if (_taskWindow == null)
        {
            return;
        }
        if (_taskWindow.gameObject.activeSelf)
        {
            _taskWindow.CloseWindow();
            return;
        }
        ShowMainWindow(WindowType.TaskWindow);
    }

    /// <summary>刷新任务主窗口与快捷面板。</summary>
    public void RefreshAll()
    {
        if (_taskWindow != null)
        {
            _taskWindow.RefreshTaskList();
        }
        if (_quickPanel != null)
        {
            _quickPanel.RefreshTrackedTasks();
        }
    }
    /// <summary>把权威副本阶段转换为现有任务条目，复用任务列表、详情和快捷追踪。</summary>
    private void RefreshDungeonTasks()
    {
        // 当前完整阶段；空值只清理本来源，不清普通任务。
        DungeonFlowState flow = TeamData.Instance.DungeonFlow;
        if (flow == null)
        {
            TaskMgr.Instance.SetDungeonTasks(new TaskViewData[0]);
            return;
        }
        // 副本名称与服务器入场编号对应，不从任务标题或界面选中项猜测。
        DungeonsTable dungeon = LubanMgr.Instance.GetDungeonsTableById(TeamData.Instance.DungeonId);
        // 每个服务器目标对应一个既有任务子条目，编号在本实例内稳定。
        TaskViewData[] tasks = new TaskViewData[flow.Objectives.Count];
        // 按服务器当前阶段的目标顺序生成展示条目。
        for (int i = 0; i < flow.Objectives.Count; i++)
        {
            // 当前原始目标只提供权威编号、进度和完成状态。
            DungeonObjectiveData objective = flow.Objectives[i];
            // 文案使用同编号的 Luban 原始目标表，避免协议或场景 XML 维护第二份定义。
            DungeonTargetTable target = LubanMgr.Instance.GetDungeonTargetTableById(objective.Id);
            // 副本目标沿用现有任务展示结构，不提供普通任务接取和交付。
            TaskViewData task = new TaskViewData();
            task.TaskId = -objective.Id;
            task.IsDungeonObjective = true;
            task.Title = dungeon.DungeonsName + " · " + target.TargetDes;
            task.TypeName = "副本";
            task.Description = string.Empty;
            task.TargetText = target.TargetDes + "  " + objective.Current + "/" + objective.Total;
            // 现有任务页固定值为1，兼容MainLine/Adventure两套名称，不依赖未提交的页签改名。
            task.Page = (TaskPageFilter)1;
            task.State = TaskViewState.Accepted;
            if (objective.Completed)
            {
                task.State = TaskViewState.Finished;
                task.TargetText += "（已完成）";
            }
            if (flow.Finished && i == flow.Objectives.Count - 1)
                task.TargetText = "挑战完成，请前往出口传送";
            // 个人首领进度附在当前第一个阶段条目上，保留阶段本身的导航目标。
            DungeonBossProgressNtf bosses = TeamData.Instance.BossProgress;
            if (i == 0 && bosses != null)
            {
                task.TargetText += "\n首领进度 " + bosses.KilledBosses.Count + "/" + bosses.BossTotal;
                if (bosses.KilledBosses.Count > 0)
                    task.Description = "本周期已击败：" + string.Join("、", bosses.KilledBosses);
            }
            tasks[i] = task;
        }
        TaskMgr.Instance.SetDungeonTasks(tasks);
    }

}
