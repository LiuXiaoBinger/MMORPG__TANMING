using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 快捷任务面板控件，绑定 QuickTaskPanel.prefab 根节点。
/// 面板由主界面持有，克隆 TaskTpl 生成条目，只展示已接取且被追踪的任务；
/// 不创建独立窗口生命周期，也不自行修改任务数据。
/// 页签 Type_1..3 是 ON/Off 两个按钮组成的自定义页签，不是 Unity Toggle。
/// </summary>
public class QuickTaskPanelWidget : MonoBehaviour
{
    /// <summary>条目模板，运行时克隆使用，初始化后隐藏原型。</summary>
    [SerializeField] private QuickTaskItemWidget _taskTemplate;
    /// <summary>条目挂载父节点，绑定 TaskList/Viewport/TaskScrollView/Content。</summary>
    [SerializeField] private RectTransform _content;
    /// <summary>任务列表根节点，折叠时整体隐藏、页签保留，绑定 TaskList。</summary>
    [SerializeField] private RectTransform _taskListRoot;
    /// <summary>三个页签点击按钮，依次绑定 Type_1..3 的 Off 按钮（未选中态可点）。</summary>
    [SerializeField] private Button[] _pageButtons;
    /// <summary>三个页签选中态节点，依次绑定 Type_1..3 的 ON，选中时显示。</summary>
    [SerializeField] private GameObject[] _pageOnNodes;
    /// <summary>三个页签未选中态节点，依次绑定 Type_1..3 的 Off，未选中时显示。</summary>
    [SerializeField] private GameObject[] _pageOffNodes;
    /// <summary>
    /// 折叠按钮，绑定 QuickTaskPanel 根下正式新增的 Btn_Fold（复用 Btn_Small_02 项目视觉资源）。
    /// 点击在折叠与展开之间切换：折叠隐藏任务列表、保留页签，按钮绕 Z 轴旋转 180 度指示状态。
    /// </summary>
    [SerializeField] private Button _foldButton;

    /// <summary>点击任务条目切换追踪。</summary>
    public event Action<TaskViewData> TaskSelected;
    /// <summary>切换页签，携带目标页签。</summary>
    public event Action<TaskPageFilter> PageChanged;
    /// <summary>请求打开任务主窗口，由任务入口经任务控制器联动触发。</summary>
    public event Action TaskWindowRequested;
    /// <summary>请求前往任务目标。</summary>
    public event Action<TaskViewData> NavigateRequested;
    /// <summary>请求放弃任务，转交任务控制器收敛为具名接口边界。</summary>
    public event Action<TaskViewData> GiveUpRequested;

    /// <summary>已克隆的条目控件，按索引复用，不重复创建。</summary>
    private readonly List<QuickTaskItemWidget> _items = new List<QuickTaskItemWidget>();
    /// <summary>页签按钮事件是否已注册，避免重复订阅。</summary>
    private bool _pageButtonsRegistered;
    /// <summary>当前是否处于折叠状态，折叠时只保留页签、隐藏任务列表。</summary>
    private bool _isFolded;

    /// <summary>初始化模板与页签，由主界面视图调用一次。</summary>
    public void InitWidget()
    {
        if (_taskTemplate != null)
        {
            _taskTemplate.gameObject.SetActive(false);
        }
        RegisterPageButtons();
        RegisterFoldButton();
        SyncPageTabs();
        RotateFoldButton();
    }

    /// <summary>注册折叠按钮点击事件，先移除再添加保证重复初始化不叠加监听。</summary>
    private void RegisterFoldButton()
    {
        if (_foldButton == null)
        {
            return;
        }
        _foldButton.onClick.RemoveListener(OnFoldButtonClick);
        _foldButton.onClick.AddListener(OnFoldButtonClick);
    }

    /// <summary>按当前追踪任务刷新条目，数量不足时克隆、多余时隐藏。</summary>
    public void RefreshTrackedTasks()
    {
        if (TaskMgr.Instance == null)
        {
            return;
        }
        // 当前需要展示的追踪任务列表，来自任务管理器的统一数据源。
        List<TaskViewData> tracked = TaskMgr.Instance.GetTrackedTasks();
        EnsureItemCount(tracked.Count);
        // 当前追踪任务的 ID，为 0 表示没有追踪任务。
        int trackedId = 0;
        // 当前追踪任务数据，用于取 ID 判断高亮。
        TaskViewData trackedTask = TaskMgr.Instance.GetTrackedTask();
        if (trackedTask != null)
        {
            trackedId = trackedTask.TaskId;
        }
        // 循环索引：按条目顺序绑定数据，超出追踪数量的条目隐藏。
        for (int i = 0; i < _items.Count; i++)
        {
            // 该索引是否仍在追踪任务数量范围内。
            bool inRange = i < tracked.Count;
            if (inRange == false)
            {
                _items[i].gameObject.SetActive(false);
                continue;
            }
            // 该索引对应的追踪任务数据。
            TaskViewData data = tracked[i];
            // 该条目是否为当前追踪任务，决定选中高亮。
            bool isTracked = data.TaskId == trackedId;
            _items[i].Bind(data, isTracked);
        }
        SyncPageTabs();
    }

    /// <summary>设置面板折叠状态：折叠时隐藏任务列表、保留页签，展开时恢复并刷新。</summary>
    /// <param name="folded">true 折叠，false 展开。</param>
    public void SetFolded(bool folded)
    {
        _isFolded = folded;
        if (_taskListRoot != null)
        {
            _taskListRoot.gameObject.SetActive(folded == false);
        }
        RotateFoldButton();
        if (folded == false)
        {
            RefreshTrackedTasks();
        }
    }

    /// <summary>请求打开任务主窗口，供任务入口按钮调用，事件由任务控制器消费。</summary>
    public void RequestOpenTaskWindow()
    {
        if (TaskWindowRequested != null)
        {
            TaskWindowRequested.Invoke();
        }
    }

    /// <summary>注册三个页签按钮的点击事件，只注册一次。</summary>
    private void RegisterPageButtons()
    {
        if (_pageButtonsRegistered)
        {
            return;
        }
        if (_pageButtons == null)
        {
            return;
        }
        // 循环索引：按数组顺序注册三个页签按钮。
        for (int i = 0; i < _pageButtons.Length; i++)
        {
            // 该索引对应的页签按钮。
            Button button = _pageButtons[i];
            if (button == null)
            {
                continue;
            }
            // 该按钮对应的页签枚举，闭包捕获供回调使用。
            TaskPageFilter page = ToPageByIndex(i);
            button.onClick.AddListener(delegate ()
            {
                OnPageButtonClick(page);
            });
        }
        _pageButtonsRegistered = true;
    }

    /// <summary>页签按钮回调，把目标页签上报给任务控制器。</summary>
    /// <param name="page">该按钮对应的页签。</param>
    private void OnPageButtonClick(TaskPageFilter page)
    {
        if (PageChanged != null)
        {
            PageChanged.Invoke(page);
        }
    }

    /// <summary>把页签按钮索引转换为页签枚举。</summary>
    /// <param name="index">按钮在数组中的下标，从 0 开始。</param>
    /// <returns>对应的页签枚举。</returns>
    private TaskPageFilter ToPageByIndex(int index)
    {
        if (index == 0)
        {
            return TaskPageFilter.MainLine;
        }
        if (index == 1)
        {
            return TaskPageFilter.Daily;
        }
        return TaskPageFilter.Activity;
    }

    /// <summary>按当前页签同步 ON/Off 显隐，不触发重复事件上报。</summary>
    private void SyncPageTabs()
    {
        if (TaskMgr.Instance == null)
        {
            return;
        }
        if (_pageOnNodes == null || _pageOffNodes == null)
        {
            return;
        }
        // 任务管理器记录的当前页签，ON/Off 显隐与它比对。
        TaskPageFilter current = TaskMgr.Instance.GetCurrentPage();
        // 循环索引：依次同步三个页签的选中态节点显隐。
        for (int i = 0; i < _pageOnNodes.Length; i++)
        {
            // 该索引对应的选中态节点。
            GameObject onNode = _pageOnNodes[i];
            if (onNode != null)
            {
                onNode.SetActive(ToPageByIndex(i) == current);
            }
        }
        // 循环索引：依次同步三个页签的未选中态节点显隐。
        for (int i = 0; i < _pageOffNodes.Length; i++)
        {
            // 该索引对应的未选中态节点。
            GameObject offNode = _pageOffNodes[i];
            if (offNode != null)
            {
                offNode.SetActive(ToPageByIndex(i) != current);
            }
        }
    }

    /// <summary>折叠按钮回调，在折叠与展开之间切换。</summary>
    private void OnFoldButtonClick()
    {
        SetFolded(_isFolded == false);
    }

    /// <summary>按折叠状态旋转折叠按钮：展开朝上，折叠时绕 Z 轴旋转 180 度朝下。</summary>
    private void RotateFoldButton()
    {
        if (_foldButton == null)
        {
            return;
        }
        // 折叠按钮自身的矩形变换，旋转它指示折叠或展开朝向。
        RectTransform foldRect = _foldButton.GetComponent<RectTransform>();
        if (foldRect == null)
        {
            return;
        }
        if (_isFolded)
        {
            foldRect.localEulerAngles = new Vector3(0f, 0f, 180f);
        }
        else
        {
            foldRect.localEulerAngles = Vector3.zero;
        }
    }

    /// <summary>保证克隆条目数量满足需求，不足时克隆并注册事件。</summary>
    /// <param name="needCount">当前需要展示的条目数量。</param>
    private void EnsureItemCount(int needCount)
    {
        while (_items.Count < needCount)
        {
            if (_taskTemplate == null)
            {
                return;
            }
            // 克隆出的条目节点，挂到滚动内容下参与布局。
            GameObject clone = Instantiate(_taskTemplate.gameObject, _content);
            // 克隆条目上的控件，缺失时放弃该克隆。
            QuickTaskItemWidget item = clone.GetComponent<QuickTaskItemWidget>();
            if (item == null)
            {
                clone.SetActive(false);
                return;
            }
            item.InitItem();
            item.ItemClicked += OnItemClicked;
            item.GiveUpClicked += OnItemGiveUp;
            _items.Add(item);
        }
    }

    /// <summary>条目点击回调，切换追踪并请求导航入口。</summary>
    /// <param name="data">被点击的任务展示数据。</param>
    private void OnItemClicked(TaskViewData data)
    {
        if (TaskSelected != null)
        {
            TaskSelected.Invoke(data);
        }
        if (NavigateRequested != null)
        {
            NavigateRequested.Invoke(data);
        }
    }

    /// <summary>条目放弃回调，原样转交任务控制器，不本地删除任务。</summary>
    /// <param name="data">被请求放弃的任务展示数据。</param>
    private void OnItemGiveUp(TaskViewData data)
    {
        if (GiveUpRequested != null)
        {
            GiveUpRequested.Invoke(data);
        }
    }
}
