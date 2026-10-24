using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务主窗口列表的任务子条目，绑定 Task.prefab 中 Templent/TaskChildItem 模板。
/// 展示任务标题、类型与进行中标记，点击 ON/Off 任一态按钮都会抛出选中事件，
/// 由任务窗口转交任务控制器联动追踪；只读展示数据，不修改任务状态。
/// </summary>
public class TaskChildItemWidget : MonoBehaviour
{
    /// <summary>选中态标题文本，绑定 TogS/ON/Text。</summary>
    [SerializeField] private Text _titleOnText;
    /// <summary>未选中态标题文本，绑定 TogS/Off/Text。</summary>
    [SerializeField] private Text _titleOffText;
    /// <summary>任务类型文本，绑定 TogS/TaskType/Text。</summary>
    [SerializeField] private Text _typeText;
    /// <summary>选中态整节点，绑定 TogS/ON，选中时显示、未选中时隐藏。</summary>
    [SerializeField] private GameObject _onNode;
    /// <summary>未选中态整节点，绑定 TogS/Off，未选中时显示、选中时隐藏。</summary>
    [SerializeField] private GameObject _offNode;
    /// <summary>进行中标记节点，绑定 TogS/ON/Doing，任务处于已接取进行中状态时显示。</summary>
    [SerializeField] private GameObject _doingNode;
    /// <summary>选中态锁标记节点，绑定 TogS/ON/Lock，首期任务默认已解锁，始终隐藏。</summary>
    [SerializeField] private GameObject _lockOnNode;
    /// <summary>未选中态锁标记节点，绑定 TogS/Off/Lock，首期任务默认已解锁，始终隐藏。</summary>
    [SerializeField] private GameObject _lockOffNode;
    /// <summary>选中态按钮，绑定 TogS/ON，点击抛出选中事件。</summary>
    [SerializeField] private Button _onButton;
    /// <summary>未选中态按钮，绑定 TogS/Off，点击抛出选中事件。</summary>
    [SerializeField] private Button _offButton;

    /// <summary>条目被点击时抛出，参数为本条目绑定的任务展示数据。</summary>
    public event Action<TaskViewData> ItemClicked;

    /// <summary>本条目当前绑定的任务展示数据，未绑定时为空。</summary>
    public TaskViewData Data { get; private set; }

    /// <summary>注册选中态与未选中态按钮的点击事件，只注册一次。</summary>
    public void InitWidget()
    {
        if (_onButton != null)
        {
            _onButton.onClick.RemoveListener(OnButtonClick);
            _onButton.onClick.AddListener(OnButtonClick);
        }
        if (_offButton != null)
        {
            _offButton.onClick.RemoveListener(OnButtonClick);
            _offButton.onClick.AddListener(OnButtonClick);
        }
    }

    /// <summary>绑定任务展示数据并刷新标题、类型与状态标记。</summary>
    /// <param name="data">任务展示数据，为空时清空显示。</param>
    public void Bind(TaskViewData data)
    {
        Data = data;
        if (data == null)
        {
            SetText(_titleOnText, string.Empty);
            SetText(_titleOffText, string.Empty);
            SetText(_typeText, string.Empty);
            SetNodeVisible(_doingNode, false);
            return;
        }
        SetText(_titleOnText, data.Title);
        SetText(_titleOffText, data.Title);
        SetText(_typeText, data.TypeName);
        // 该任务是否处于已接取进行中状态，决定进行中标记显隐。
        bool doing = data.State == TaskViewState.Accepted;
        SetNodeVisible(_doingNode, doing);
        SetNodeVisible(_lockOnNode, false);
        SetNodeVisible(_lockOffNode, false);
    }

    /// <summary>设置条目选中态：选中显示 ON 节点、隐藏 Off 节点，未选中相反。</summary>
    /// <param name="selected">true 表示本条目是当前选中任务。</param>
    public void SetSelected(bool selected)
    {
        if (_onNode != null)
        {
            _onNode.SetActive(selected);
        }
        if (_offNode != null)
        {
            _offNode.SetActive(selected == false);
        }
    }

    /// <summary>点击任意态按钮都抛出本条目的选中事件。</summary>
    private void OnButtonClick()
    {
        if (Data == null)
        {
            return;
        }
        if (ItemClicked != null)
        {
            ItemClicked.Invoke(Data);
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

    /// <summary>控制节点显隐，节点未绑定时跳过。</summary>
    /// <param name="node">目标节点。</param>
    /// <param name="visible">true 显示，false 隐藏。</param>
    private void SetNodeVisible(GameObject node, bool visible)
    {
        if (node == null)
        {
            return;
        }
        node.SetActive(visible);
    }
}
