using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 快捷任务面板中的单条任务控件，绑定 QuickTaskPanel 预制体里的 TaskTpl 节点及其子节点。
/// 只负责按展示数据刷新控件与转发点击，不修改任何任务业务状态。
/// </summary>
public class QuickTaskItemWidget : MonoBehaviour
{
    /// <summary>标题文本，绑定 TaskTpl/Title/TitleText。</summary>
    [SerializeField] private Text _titleText;
    /// <summary>目标文本一，绑定 TaskTpl/Target/TargetText1，首期唯一启用的目标行。</summary>
    [SerializeField] private Text _targetText1;
    /// <summary>目标文本二到五，首期保持隐藏，保留原节点不删除。</summary>
    [SerializeField] private Text[] _otherTargetTexts;
    /// <summary>描述文本，绑定 TaskTpl/Des/DesText。</summary>
    [SerializeField] private Text _desText;
    /// <summary>截止时间行根节点，无时限时隐藏，绑定 TaskTpl/Deadline。</summary>
    [SerializeField] private GameObject _deadline;
    /// <summary>截止时间文本，绑定 TaskTpl/Deadline 自身 Text，显示任务的截止时间或日期。</summary>
    [SerializeField] private Text _deadlineText;
    /// <summary>限制说明行根节点，无内容时隐藏，绑定 TaskTpl/Limit。</summary>
    [SerializeField] private GameObject _limit;
    /// <summary>限制说明文本，绑定 TaskTpl/Limit/LimitText。</summary>
    [SerializeField] private Text _limitText;
    /// <summary>奖励展示根节点，仅选中且有奖励时显示，绑定 TaskTpl/AwardItemBox。</summary>
    [SerializeField] private GameObject _awardItemBox;
    /// <summary>选中高亮图，绑定 TaskTpl/SelectTaskImg。</summary>
    [SerializeField] private GameObject _selectTaskImg;
    /// <summary>放弃按钮，仅在任务可放弃时显示，绑定 TaskTpl/Title/GiveUpBtn。</summary>
    [SerializeField] private Button _giveUpButton;
    /// <summary>整条任务的点击入口，克隆后由面板注册点击，绑定 TaskTpl 自身 Button。</summary>
    [SerializeField] private Button _clickButton;

    /// <summary>本条控件当前绑定的任务展示数据。</summary>
    private TaskViewData _data;
    /// <summary>本条控件被点击时触发，参数为当前绑定的任务数据。</summary>
    public event Action<TaskViewData> ItemClicked;
    /// <summary>本条控件的放弃按钮被点击时触发，参数为当前绑定的任务数据。</summary>
    public event Action<TaskViewData> GiveUpClicked;

    /// <summary>初始化本条控件的按钮事件，模板克隆后由面板调用一次，避免重复订阅。</summary>
    public void InitItem()
    {
        if (_clickButton != null)
        {
            _clickButton.onClick.RemoveListener(OnItemClick);
            _clickButton.onClick.AddListener(OnItemClick);
        }
        if (_giveUpButton != null)
        {
            _giveUpButton.onClick.RemoveListener(OnGiveUpClick);
            _giveUpButton.onClick.AddListener(OnGiveUpClick);
        }
    }

    /// <summary>绑定展示数据并刷新控件，数据为空时整条隐藏。</summary>
    /// <param name="data">任务展示数据。</param>
    /// <param name="isTracked">是否为当前追踪任务。</param>
    public void Bind(TaskViewData data, bool isTracked)
    {
        _data = data;
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        if (_titleText != null)
        {
            _titleText.text = data.Title;
        }
        if (_targetText1 != null)
        {
            _targetText1.text = data.TargetText;
        }
        HideOtherTargets();
        SetDescription(data);
        SetDeadline(data);
        SetLimit(data);
        if (_awardItemBox != null)
        {
            _awardItemBox.SetActive(data.HasReward && isTracked);
        }
        if (_selectTaskImg != null)
        {
            _selectTaskImg.SetActive(isTracked);
        }
        if (_giveUpButton != null)
        {
            _giveUpButton.gameObject.SetActive(data.CanGiveUp);
        }
    }

    /// <summary>点击入口回调，转发为带数据的点击事件。</summary>
    public void OnItemClick()
    {
        if (_data == null)
        {
            return;
        }
        if (ItemClicked != null)
        {
            ItemClicked.Invoke(_data);
        }
    }

    /// <summary>放弃按钮回调，转发为带数据的放弃事件，不本地删除任务。</summary>
    public void OnGiveUpClick()
    {
        if (_data == null)
        {
            return;
        }
        if (GiveUpClicked != null)
        {
            GiveUpClicked.Invoke(_data);
        }
    }

    /// <summary>首期只保留第一个目标行，其余目标行整体隐藏但不删除节点。</summary>
    private void HideOtherTargets()
    {
        if (_otherTargetTexts == null)
        {
            return;
        }
        // 循环索引：遍历首期保留之外的目标行文本，逐个隐藏。
        for (int i = 0; i < _otherTargetTexts.Length; i++)
        {
            if (_otherTargetTexts[i] != null)
            {
                _otherTargetTexts[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>写入描述文本，无描述时隐藏描述节点。</summary>
    /// <param name="data">任务展示数据。</param>
    private void SetDescription(TaskViewData data)
    {
        if (_desText == null)
        {
            return;
        }
        // 是否有描述内容：空描述时隐藏整行，避免展示空白说明。
        bool hasDescription = string.IsNullOrEmpty(data.Description) == false;
        _desText.gameObject.SetActive(hasDescription);
        _desText.text = data.Description;
    }

    /// <summary>按有无时限显示或隐藏截止时间行，有时限时写入截止文本，无时限时清空。</summary>
    /// <param name="data">任务展示数据。</param>
    private void SetDeadline(TaskViewData data)
    {
        if (_deadline == null)
        {
            return;
        }
        // 是否有时限信息：无时限时隐藏整行，避免展示占位内容。
        bool hasDeadline = data.HasDeadline;
        _deadline.SetActive(hasDeadline);
        if (_deadlineText == null)
        {
            return;
        }
        if (hasDeadline)
        {
            _deadlineText.text = data.DeadlineText;
        }
        else
        {
            // 隐藏截止行时同步清空文本，避免再次显示时残留上一次任务的内容。
            _deadlineText.text = string.Empty;
        }
    }

    /// <summary>按有无限制说明显示或隐藏限制行，并写入文本。</summary>
    /// <param name="data">任务展示数据。</param>
    private void SetLimit(TaskViewData data)
    {
        if (_limit != null)
        {
            _limit.SetActive(data.HasLimit);
        }
        if (_limitText != null)
        {
            _limitText.text = data.LimitText;
        }
    }
}
