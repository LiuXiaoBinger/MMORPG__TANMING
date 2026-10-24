using System;

/// <summary>
/// 任务控制器，对应 RO 的 TaskCtrl。
/// 负责组合任务主窗口与快捷任务面板、转发界面点击到任务数据，
/// 并把尚未实现的接取、交付、放弃、导航能力收敛为具名接口边界。
/// </summary>
public class TaskCtrl : CtrlBase
{
    /// <summary>主界面视图，持有任务窗口与快捷面板。</summary>
    private readonly MainView _mainView;
    /// <summary>任务主窗口，绑定 Task.prefab。</summary>
    private readonly TaskWindow _taskWindow;
    /// <summary>快捷任务面板，绑定 QuickTaskPanel.prefab。</summary>
    private readonly QuickTaskPanelWidget _quickPanel;
    /// <summary>界面事件是否已注册，避免重复订阅。</summary>
    private bool _actionsRegistered;

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
        _actionsRegistered = true;
    }

    /// <summary>注销网络、输入和视图事件，控制器销毁时调用。</summary>
    public override void Dispose()
    {
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
        _actionsRegistered = false;
    }

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

    /// <summary>任务入口请求打开任务窗口：先经快捷面板事件联动展开折叠，再切换窗口显隐。</summary>
    public void RequestOpenTaskWindow()
    {
        if (_quickPanel != null)
        {
            _quickPanel.RequestOpenTaskWindow();
        }
        OpenTaskWindow();
    }

    /// <summary>快捷面板请求打开任务窗口时联动展开折叠列表，保证任务条目可见。</summary>
    private void OnQuickPanelOpenTaskWindow()
    {
        if (_quickPanel != null)
        {
            _quickPanel.SetFolded(false);
        }
    }

    /// <summary>任务数据变化后同时刷新主窗口与快捷面板，两者共用同一份数据。</summary>
    private void OnTaskDataChanged()
    {
        RefreshAll();
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

    /// <summary>关闭任务主窗口请求，只隐藏窗口不改变业务状态。</summary>
    private void OnTaskWindowCloseRequested()
    {
        if (_taskWindow != null)
        {
            _taskWindow.CloseWindow();
        }
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
            return;
        }
        TaskMgr.Instance.SetCurrentPage(page);
    }

    /// <summary>
    /// 接取任务请求边界：需要服务端任务接取协议与角色任务组件后实现。
    /// 当前没有正式任务服务，方法保留为具名接口，不本地伪造接取结果。
    /// </summary>
    /// <param name="data">待接取的任务展示数据。</param>
    public void RequestAccept(TaskViewData data)
    {
    }

    /// <summary>
    /// 交付任务请求边界：需要服务端交付校验与奖励事务后实现。
    /// 未完成前不修改任务状态、不发放奖励、不提示成功。
    /// </summary>
    /// <param name="data">待交付的任务展示数据。</param>
    public void RequestDeliver(TaskViewData data)
    {
    }

    /// <summary>
    /// 放弃任务请求边界：需要服务端放弃协议后实现，当前不本地删除任务数据。
    /// </summary>
    /// <param name="data">待放弃的任务展示数据。</param>
    public void RequestGiveUp(TaskViewData data)
    {
    }

    /// <summary>
    /// 任务导航请求边界：需沿现有导航模块前往目标，且到达不自动完成任务。
    /// 当前未接入任务导航回调，方法保留为具名接口。
    /// </summary>
    /// <param name="data">需要导航到的任务展示数据。</param>
    public void RequestNavigate(TaskViewData data)
    {
    }
}
