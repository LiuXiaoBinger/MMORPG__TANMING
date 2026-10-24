using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务主窗口列表的分组父条目，绑定 Task.prefab 中 Templent/TaskParentItem 模板。
/// 只提供分组标题、选中态显隐与展开折叠：点击在 ON/Off 两个按钮上都会触发折叠切换，
/// 折叠时隐藏挂接的子条目、保留自身行，展开时恢复；不持有任务数据，也不改业务状态。
/// </summary>
public class TaskParentItemWidget : MonoBehaviour
{
    /// <summary>选中态标题文本，绑定 TogL/ON/Text。</summary>
    [SerializeField] private Text _titleOnText;
    /// <summary>未选中态标题文本，绑定 TogL/Off/Text。</summary>
    [SerializeField] private Text _titleOffText;
    /// <summary>选中态整节点，绑定 TogL/ON，选中时显示、未选中时隐藏。</summary>
    [SerializeField] private GameObject _onNode;
    /// <summary>未选中态整节点，绑定 TogL/Off，未选中时显示、选中时隐藏。</summary>
    [SerializeField] private GameObject _offNode;
    /// <summary>选中态箭头节点，绑定 TogL/ON/Arror，折叠时绕 Z 轴旋转 180 度。</summary>
    [SerializeField] private RectTransform _arrowOn;
    /// <summary>未选中态箭头节点，绑定 TogL/Off/Arror，折叠时绕 Z 轴旋转 180 度。</summary>
    [SerializeField] private RectTransform _arrowOff;
    /// <summary>选中态按钮，绑定 TogL/ON，点击切换展开折叠。</summary>
    [SerializeField] private Button _onButton;
    /// <summary>未选中态按钮，绑定 TogL/Off，点击切换展开折叠。</summary>
    [SerializeField] private Button _offButton;

    /// <summary>展开或折叠切换时抛出，参数为切换后的展开状态。</summary>
    public event Action<bool> ExpandToggled;

    /// <summary>挂接在本分组下的子条目控件列表，折叠时统一隐藏。</summary>
    private readonly List<TaskChildItemWidget> _children = new List<TaskChildItemWidget>();
    /// <summary>当前是否展开，初始为展开，保证列表生成后条目默认可见。</summary>
    private bool _expanded = true;
    /// <summary>按钮事件是否已注册，避免重复订阅。</summary>
    private bool _registered;

    /// <summary>注册选中态与未选中态按钮的点击事件，只注册一次。</summary>
    public void InitWidget()
    {
        if (_registered)
        {
            return;
        }
        if (_onButton != null)
        {
            _onButton.onClick.AddListener(OnButtonClick);
        }
        if (_offButton != null)
        {
            _offButton.onClick.AddListener(OnButtonClick);
        }
        _registered = true;
    }

    /// <summary>写入分组标题，选中态与未选中态文本保持一致。</summary>
    /// <param name="title">分组标题文本。</param>
    public void SetTitle(string title)
    {
        if (_titleOnText != null)
        {
            _titleOnText.text = title;
        }
        if (_titleOffText != null)
        {
            _titleOffText.text = title;
        }
    }

    /// <summary>挂接子条目列表，随后按当前展开状态刷新子条目显隐。</summary>
    /// <param name="children">本分组下的任务子条目控件列表。</param>
    public void BindChildren(List<TaskChildItemWidget> children)
    {
        _children.Clear();
        if (children != null)
        {
            // 循环索引：按传入顺序拷贝子条目引用，避免分组持有调用方的活列表。
            for (int i = 0; i < children.Count; i++)
            {
                _children.Add(children[i]);
            }
        }
        ApplyChildrenVisible();
    }

    /// <summary>设置分组选中态：选中显示 ON 节点、隐藏 Off 节点，未选中相反。</summary>
    /// <param name="selected">true 表示本组内有选中任务。</param>
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

    /// <summary>切换展开折叠并刷新子条目显隐与箭头朝向。</summary>
    /// <param name="expanded">true 展开，false 折叠。</param>
    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        ApplyChildrenVisible();
        RotateArrows();
        if (ExpandToggled != null)
        {
            ExpandToggled.Invoke(_expanded);
        }
    }

    /// <summary>点击任意态按钮都在展开与折叠之间切换。</summary>
    private void OnButtonClick()
    {
        SetExpanded(_expanded == false);
    }

    /// <summary>按展开状态刷新全部子条目显隐，已销毁的条目跳过。</summary>
    private void ApplyChildrenVisible()
    {
        // 循环索引：按挂接顺序刷新子条目显隐。
        for (int i = 0; i < _children.Count; i++)
        {
            // 当前索引对应的子条目控件，可能已被销毁，需判空。
            TaskChildItemWidget child = _children[i];
            if (child == null)
            {
                continue;
            }
            child.gameObject.SetActive(_expanded);
        }
    }

    /// <summary>按展开状态旋转两个态的箭头：展开朝上，折叠绕 Z 轴旋转 180 度朝下。</summary>
    private void RotateArrows()
    {
        // 展开态箭头角度，保持原始朝向（朝上）。
        Vector3 expandedAngle = Vector3.zero;
        // 折叠态箭头角度，绕 Z 轴旋转 180 度朝下。
        Vector3 foldedAngle = new Vector3(0f, 0f, 180f);
        // 当前应应用的箭头角度，默认展开态。
        Vector3 targetAngle = expandedAngle;
        if (_expanded == false)
        {
            targetAngle = foldedAngle;
        }
        if (_arrowOn != null)
        {
            _arrowOn.localEulerAngles = targetAngle;
        }
        if (_arrowOff != null)
        {
            _arrowOff.localEulerAngles = targetAngle;
        }
    }
}
