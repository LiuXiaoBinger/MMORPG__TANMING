using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务主窗口，组件挂 Task.prefab 整窗根节点，由 MainView 持有并注册到 WindowType.TaskWindow。
/// 显隐由窗口基类直接切换整窗根节点，保证 Show(true) 后 activeInHierarchy 为真。
/// 左侧任务列表用 TaskSelectPart/Toggroup 滚动 Content 内的 TaskParentItem/TaskChildItem 实际模板克隆生成，
/// 点击真实条目经事件交任务控制器联动追踪；接取、交付、放弃、导航只向外抛事件，不自行改业务状态。
/// 页签 Type_1..3 在 QuickTaskPanel.prefab 上，本窗口不持有页签开关。
/// </summary>
public class TaskWindow : WindowBase
{
    /// <summary>任务标题文本，绑定 TaskInfo/Task/TaskName。</summary>
    [SerializeField] private Text _taskName;
    /// <summary>任务类型文本，绑定 TaskInfo/Task/TaskTypeTxt。</summary>
    [SerializeField] private Text _taskTypeText;
    /// <summary>任务提示文本（剩余时间等），绑定 TaskInfo/Task/TextTips。</summary>
    [SerializeField] private Text _taskTipText;
    /// <summary>任务描述文本，绑定 Content/DescTitle/TargetDesc，显示任务的真实描述内容。</summary>
    [SerializeField] private Text _targetDesc;
    /// <summary>目标行文本，依次绑定 Content/TargetTips/Target1..5/Txt，首期只用第一行。</summary>
    [SerializeField] private Text[] _targetTexts;
    /// <summary>截止时间行根节点，无时限时隐藏，绑定 Content/Deadline。</summary>
    [SerializeField] private GameObject _deadline;
    /// <summary>奖励区域根节点，无奖励或任务未选中时隐藏，绑定 TaskInfo/FinishInfo。</summary>
    [SerializeField] private GameObject _rewardRoot;
    /// <summary>奖励条目模板根节点，绑定 FinishInfo/Reward/Viewport/RewardList/RewardItem。</summary>
    [SerializeField] private GameObject _rewardItem;
    /// <summary>奖励图标，绑定 RewardItem/ItemIcon。</summary>
    [SerializeField] private Image _rewardIcon;
    /// <summary>没有任务数据时的空态根节点，绑定 Task_Empty。</summary>
    [SerializeField] private GameObject _emptyRoot;
    /// <summary>可接任务筛选开关，绑定 TaskSelectPart/CanAccpetToggle，首期无可接数据源，只触发刷新。</summary>
    [SerializeField] private Toggle _acceptableToggle;
    /// <summary>任务列表滚动内容节点，克隆条目挂载父节点，绑定 TaskSelectPart/Toggroup/Obj_Toggle/Viewport/Content。</summary>
    [SerializeField] private RectTransform _listContent;
    /// <summary>列表模板容器根节点，运行时整体隐藏，绑定 Content/Templent。</summary>
    [SerializeField] private GameObject _listTemplateRoot;
    /// <summary>分组父条目模板，绑定 Templent/TaskParentItem。</summary>
    [SerializeField] private TaskParentItemWidget _parentTemplate;
    /// <summary>任务子条目模板，绑定 Templent/TaskChildItem。</summary>
    [SerializeField] private TaskChildItemWidget _childTemplate;
    /// <summary>开始（前往目标）按钮根节点，绑定 TaskInfo/Image/Btn_Start，无选中任务时隐藏。</summary>
    [SerializeField] private GameObject _startButtonRoot;
    /// <summary>取消（放弃任务）按钮根节点，绑定 TaskInfo/Image/Btn_Cancel，无可放弃任务时隐藏。</summary>
    [SerializeField] private GameObject _cancelButtonRoot;

    /// <summary>请求关闭本窗口。</summary>
    public event Action CloseRequested;
    /// <summary>选中某个任务，参数为被选中的任务数据。</summary>
    public event Action<TaskViewData> TaskSelected;
    /// <summary>请求接取任务。</summary>
    public event Action<TaskViewData> AcceptRequested;
    /// <summary>请求交付任务。</summary>
    public event Action<TaskViewData> DeliverRequested;
    /// <summary>请求放弃任务。</summary>
    public event Action<TaskViewData> GiveUpRequested;
    /// <summary>请求前往任务目标。</summary>
    public event Action<TaskViewData> NavigateRequested;

    /// <summary>当前窗口展示的任务数据序列，按页签筛选结果填充。</summary>
    private readonly List<TaskViewData> _pageTasks = new List<TaskViewData>();
    /// <summary>已克隆的任务子条目控件，按列表顺序持有，重建时统一解绑销毁。</summary>
    private readonly List<TaskChildItemWidget> _childItems = new List<TaskChildItemWidget>();
    /// <summary>当前列表的分组父条目控件，每次重建时替换。</summary>
    private TaskParentItemWidget _parentItem;
    /// <summary>可接筛选开关事件是否已注册，避免重复订阅。</summary>
    private bool _toggleRegistered;

    /// <summary>注册筛选开关、隐藏列表模板并刷新一次内容。</summary>
    public override void InitWindow()
    {
        RegisterAcceptableToggle();
        HideListTemplates();
        RefreshTaskList();
    }

    /// <summary>窗口被显隐切换时刷新列表与详情，保证打开时数据是最新快照。</summary>
    /// <param name="obj">框架透传的刷新数据，任务窗口不使用。</param>
    public override void ReFreshUI(object obj)
    {
        RefreshTaskList();
    }

    /// <summary>按当前页签重新取任务集合并重建列表、详情与空态。</summary>
    public void RefreshTaskList()
    {
        if (TaskMgr.Instance == null)
        {
            return;
        }
        _pageTasks.Clear();
        // 当前页签下筛选出的任务列表，作为本窗口列表与详情的数据源。
        List<TaskViewData> tasks = TaskMgr.Instance.GetPageTasks(TaskMgr.Instance.GetCurrentPage());
        // 循环索引：按筛选顺序把任务写入本页任务序列。
        for (int i = 0; i < tasks.Count; i++)
        {
            _pageTasks.Add(tasks[i]);
        }
        RebuildTaskList();
        // 当前应展示详情的任务，为空表示本页无任务，走空态。
        TaskViewData current = PickCurrentTask();
        ApplySelection(current);
        ShowDetail(current);
        UpdateActionButtons(current);
    }

    /// <summary>Inspector 入口：请求关闭任务窗口。</summary>
    public void OnCloseButtonClick()
    {
        if (CloseRequested != null)
        {
            CloseRequested.Invoke();
        }
    }

    /// <summary>Inspector 入口：请求接取当前任务。</summary>
    public void OnAcceptButtonClick()
    {
        RaiseAccept(PickCurrentTask());
    }

    /// <summary>Inspector 入口：请求交付当前任务。</summary>
    public void OnDeliverButtonClick()
    {
        // 当前选中并请求交付的任务，为空不响应。
        TaskViewData current = PickCurrentTask();
        if (current == null)
        {
            return;
        }
        if (DeliverRequested != null)
        {
            DeliverRequested.Invoke(current);
        }
    }

    /// <summary>Inspector 入口：取消按钮请求放弃当前任务，无选中任务时不响应。</summary>
    public void OnGiveUpButtonClick()
    {
        // 当前选中并请求放弃的任务，为空不响应。
        TaskViewData current = PickCurrentTask();
        if (current == null)
        {
            return;
        }
        if (GiveUpRequested != null)
        {
            GiveUpRequested.Invoke(current);
        }
    }

    /// <summary>Inspector 入口：开始按钮请求前往当前任务目标，无选中任务时不响应。</summary>
    public void OnNavigateButtonClick()
    {
        // 当前选中并请求导航的任务，为空不响应。
        TaskViewData current = PickCurrentTask();
        if (current == null)
        {
            return;
        }
        if (NavigateRequested != null)
        {
            NavigateRequested.Invoke(current);
        }
    }

    /// <summary>转发接取请求。</summary>
    /// <param name="data">待接取的任务数据，为空则不抛出。</param>
    private void RaiseAccept(TaskViewData data)
    {
        if (data == null)
        {
            return;
        }
        if (AcceptRequested != null)
        {
            AcceptRequested.Invoke(data);
        }
    }

    /// <summary>隐藏列表模板容器，克隆条目在容器外独立生成，不受模板隐藏影响。</summary>
    private void HideListTemplates()
    {
        if (_listTemplateRoot != null)
        {
            _listTemplateRoot.SetActive(false);
        }
    }

    /// <summary>按当前页任务数重建列表：空数据显示空态，有数据时生成一个分组父条目和全部子条目。</summary>
    private void RebuildTaskList()
    {
        ClearTaskList();
        // 本页是否有任务数据，决定显示列表还是空态。
        bool hasTask = _pageTasks.Count > 0;
        if (_emptyRoot != null)
        {
            _emptyRoot.SetActive(hasTask == false);
        }
        if (hasTask == false)
        {
            return;
        }
        if (_listContent == null || _parentTemplate == null || _childTemplate == null)
        {
            return;
        }
        // 克隆出的分组父条目节点，挂到列表滚动内容下参与布局。
        GameObject parentGo = UnityEngine.Object.Instantiate(_parentTemplate.gameObject, _listContent);
        parentGo.SetActive(true);
        _parentItem = parentGo.GetComponent<TaskParentItemWidget>();
        if (_parentItem == null)
        {
            DestroyRow(parentGo);
            return;
        }
        _parentItem.InitWidget();
        _parentItem.SetTitle(PageToTitle(TaskMgr.Instance.GetCurrentPage()));
        // 循环索引：按本页任务顺序逐条克隆子条目。
        for (int i = 0; i < _pageTasks.Count; i++)
        {
            // 当前索引对应的任务展示数据。
            TaskViewData task = _pageTasks[i];
            // 克隆出的任务子条目节点，与父条目同级挂载。
            GameObject childGo = UnityEngine.Object.Instantiate(_childTemplate.gameObject, _listContent);
            childGo.SetActive(true);
            // 克隆条目上的子条目控件，缺失时销毁该克隆行。
            TaskChildItemWidget item = childGo.GetComponent<TaskChildItemWidget>();
            if (item == null)
            {
                DestroyRow(childGo);
                continue;
            }
            item.InitWidget();
            item.Bind(task);
            item.ItemClicked += OnTaskItemClicked;
            _childItems.Add(item);
        }
        _parentItem.BindChildren(BuildChildSnapshot());
    }

    /// <summary>拷贝当前子条目列表，避免分组父条目持有重建期变化的活列表。</summary>
    /// <returns>当前子条目控件的只读快照列表。</returns>
    private List<TaskChildItemWidget> BuildChildSnapshot()
    {
        // 拷贝出的子条目快照列表，供分组父条目持有，避免活列表在重建期变化。
        List<TaskChildItemWidget> snapshot = new List<TaskChildItemWidget>();
        // 循环索引：按当前列表顺序拷贝子条目引用。
        for (int i = 0; i < _childItems.Count; i++)
        {
            snapshot.Add(_childItems[i]);
        }
        return snapshot;
    }

    /// <summary>销毁全部克隆条目并解绑事件，防止重建后残留订阅。</summary>
    private void ClearTaskList()
    {
        if (_parentItem != null)
        {
            DestroyRow(_parentItem.gameObject);
            _parentItem = null;
        }
        // 循环索引：按列表顺序逐个解绑并销毁子条目。
        for (int i = 0; i < _childItems.Count; i++)
        {
            // 当前索引对应的子条目控件，可能已被销毁，需判空。
            TaskChildItemWidget item = _childItems[i];
            if (item == null)
            {
                continue;
            }
            item.ItemClicked -= OnTaskItemClicked;
            DestroyRow(item.gameObject);
        }
        _childItems.Clear();
    }

    /// <summary>销毁列表行节点，按项目正常运行态的对象销毁流程处理。</summary>
    /// <param name="row">要销毁的条目节点。</param>
    private void DestroyRow(GameObject row)
    {
        if (row == null)
        {
            return;
        }
        UnityEngine.Object.Destroy(row);
    }

    /// <summary>点击任务子条目：把选中数据交给任务控制器联动追踪与详情刷新。</summary>
    /// <param name="data">被点击的任务展示数据。</param>
    private void OnTaskItemClicked(TaskViewData data)
    {
        if (data == null)
        {
            return;
        }
        if (TaskSelected != null)
        {
            TaskSelected.Invoke(data);
        }
    }

    /// <summary>按当前选中任务刷新列表高亮：子条目按任务 ID 比对，父条目按是否有选中任务置态。</summary>
    /// <param name="current">当前展示的任务数据，为空表示全部不高亮。</param>
    private void ApplySelection(TaskViewData current)
    {
        // 当前选中任务的 ID，为 0 表示没有选中任务，全部条目不高亮。
        int currentId = 0;
        if (current != null)
        {
            currentId = current.TaskId;
        }
        // 循环索引：按列表顺序逐条刷新子条目高亮。
        for (int i = 0; i < _childItems.Count; i++)
        {
            // 当前索引对应的子条目控件。
            TaskChildItemWidget item = _childItems[i];
            if (item == null)
            {
                continue;
            }
            // 该条目是否为当前选中任务，按任务 ID 比对。
            bool selected = item.Data != null && item.Data.TaskId == currentId;
            item.SetSelected(selected);
        }
        if (_parentItem != null)
        {
            _parentItem.SetSelected(current != null);
        }
    }

    /// <summary>按选中任务控制操作按钮显隐：无任务时全部隐藏，有任务时显示开始按钮，可放弃时显示取消按钮。</summary>
    /// <param name="current">当前展示的任务数据，为空表示无数据。</param>
    private void UpdateActionButtons(TaskViewData current)
    {
        if (_startButtonRoot != null)
        {
            _startButtonRoot.SetActive(current != null);
        }
        if (_cancelButtonRoot != null)
        {
            // 当前任务是否允许放弃，无可放弃任务时隐藏取消按钮。
            bool canGiveUp = false;
            if (current != null)
            {
                canGiveUp = current.CanGiveUp;
            }
            _cancelButtonRoot.SetActive(canGiveUp);
        }
    }

    /// <summary>取当前应展示的任务：只在本页签列表内优先追踪任务，其次本页第一条，不跨页签取。</summary>
    /// <returns>当前任务数据，无数据时返回 null。</returns>
    private TaskViewData PickCurrentTask()
    {
        if (TaskMgr.Instance == null)
        {
            return null;
        }
        // 追踪中的任务数据，为空或不在本页签时不作为当前展示任务。
        TaskViewData tracked = TaskMgr.Instance.GetTrackedTask();
        if (tracked != null && tracked.Page == TaskMgr.Instance.GetCurrentPage())
        {
            return tracked;
        }
        if (_pageTasks.Count > 0)
        {
            return _pageTasks[0];
        }
        return null;
    }

    /// <summary>把页签枚举转换成分组父条目标题文本。</summary>
    /// <param name="page">目标页签。</param>
    /// <returns>页签对应的中文标题。</returns>
    private string PageToTitle(TaskPageFilter page)
    {
        if (page == TaskPageFilter.MainLine)
        {
            return "主线任务";
        }
        if (page == TaskPageFilter.Daily)
        {
            return "日常任务";
        }
        return "活动任务";
    }

    /// <summary>把任务数据写入详情控件，数据为空时清空文本并进入空态。</summary>
    /// <param name="data">当前展示的任务数据。</param>
    private void ShowDetail(TaskViewData data)
    {
        if (data == null)
        {
            SetText(_taskName, string.Empty);
            SetText(_taskTypeText, string.Empty);
            SetText(_taskTipText, string.Empty);
            SetText(_targetDesc, string.Empty);
            ClearTargetTexts();
            if (_deadline != null)
            {
                _deadline.SetActive(false);
            }
            if (_rewardRoot != null)
            {
                _rewardRoot.SetActive(false);
            }
            if (_rewardItem != null)
            {
                _rewardItem.SetActive(false);
            }
            return;
        }
        SetText(_taskName, data.Title);
        SetText(_taskTypeText, data.TypeName);
        // 描述栏写入真实任务描述，与目标栏的目标文本分开，避免同一内容重复展示。
        SetText(_targetDesc, data.Description);
        ShowTargets(data);
        if (_taskTipText != null)
        {
            // 该任务是否有限时信息，无限时隐藏提示行。
            bool hasDeadline = data.HasDeadline;
            _taskTipText.gameObject.SetActive(hasDeadline);
            _taskTipText.text = data.DeadlineText;
        }
        if (_deadline != null)
        {
            _deadline.SetActive(data.HasDeadline);
        }
        // 该任务是否有奖励展示内容，无奖励时隐藏奖励区域。
        bool showReward = data.HasReward;
        if (_rewardRoot != null)
        {
            _rewardRoot.SetActive(showReward);
        }
        if (_rewardItem != null)
        {
            _rewardItem.SetActive(showReward);
        }
    }

    /// <summary>首期只把第一个目标行写入文本，其余目标行隐藏但保留节点。</summary>
    /// <param name="data">当前展示的任务数据。</param>
    private void ShowTargets(TaskViewData data)
    {
        if (_targetTexts == null)
        {
            return;
        }
        // 循环索引：按目标行顺序刷新，首期只显示第一行。
        for (int i = 0; i < _targetTexts.Length; i++)
        {
            // 当前索引对应的目标行文本控件。
            Text target = _targetTexts[i];
            if (target == null)
            {
                continue;
            }
            // 该行是否是第一行，只有第一行显示并写入目标描述。
            bool isFirst = i == 0;
            target.gameObject.SetActive(isFirst);
            if (isFirst)
            {
                target.text = data.TargetText;
            }
        }
    }

    /// <summary>清空并隐藏全部目标行。</summary>
    private void ClearTargetTexts()
    {
        if (_targetTexts == null)
        {
            return;
        }
        // 循环索引：按目标行顺序逐个清空文本并隐藏。
        for (int i = 0; i < _targetTexts.Length; i++)
        {
            if (_targetTexts[i] != null)
            {
                _targetTexts[i].text = string.Empty;
                _targetTexts[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>写入文本控件，控件未绑定时跳过。</summary>
    /// <param name="target">目标文本控件。</param>
    /// <param name="content">要写入的文本。</param>
    private void SetText(Text target, string content)
    {
        if (target == null)
        {
            return;
        }
        target.text = content;
    }

    /// <summary>注册可接筛选开关的切换事件，只注册一次。</summary>
    private void RegisterAcceptableToggle()
    {
        if (_toggleRegistered)
        {
            return;
        }
        if (_acceptableToggle == null)
        {
            return;
        }
        _acceptableToggle.onValueChanged.AddListener(OnAcceptableToggleChanged);
        _toggleRegistered = true;
    }

    /// <summary>可接筛选开关回调：首期没有可接任务数据源，只按当前页签重新刷新，不伪造筛选结果。</summary>
    /// <param name="isOn">开关是否选中。</param>
    private void OnAcceptableToggleChanged(bool isOn)
    {
        RefreshTaskList();
    }
}
