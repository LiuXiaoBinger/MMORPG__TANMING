using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>装备详情子窗口，只展示数据并通知控制器提交穿脱和特技选择。</summary>
public sealed class EquipmentDetailWindow : WindowBase
{
    /// <summary>显示装备名和强化等级的标题。</summary>
    [SerializeField] private TMP_Text _nameText;
    /// <summary>固定顶部的装备图标，使用现有资源加载入口。</summary>
    [SerializeField] private Image _icon;
    /// <summary>固定顶部的中文部位名称。</summary>
    [SerializeField] private TMP_Text _partText;
    /// <summary>基础属性左列名称，与右列数值逐行对齐。</summary>
    [SerializeField] private TMP_Text _attributesText;
    /// <summary>基础属性右列数值，采用与左列相同的行距。</summary>
    [SerializeField] private TMP_Text _attributeValuesText;
    /// <summary>实例词条分区中的正文。</summary>
    [SerializeField] private TMP_Text _traitsText;
    /// <summary>只滚动中间属性、词条和特技，顶部和底部按钮保持固定。</summary>
    [SerializeField] private ScrollRect _scroll;
    /// <summary>预制体中按槽0、1、2排列的特技说明。</summary>
    [SerializeField] private TMP_Text[] _specialSkillTexts;
    /// <summary>对应三个特技槽的启用按钮，引用保存在正式预制体。</summary>
    [SerializeField] private Button[] _activateButtons;
    /// <summary>未穿戴时显示的穿戴按钮。</summary>
    [SerializeField] private Button _wearButton;
    /// <summary>已穿戴时显示的卸下按钮。</summary>
    [SerializeField] private Button _takeOffButton;
    /// <summary>只关闭本窗口的按钮。</summary>
    [SerializeField] private Button _closeButton;

    /// <summary>玩家请求穿戴当前详情装备。</summary>
    public event Action WearRequested;
    /// <summary>玩家请求卸下当前详情装备。</summary>
    public event Action TakeOffRequested;
    /// <summary>玩家请求启用特技槽，参数范围0至2。</summary>
    public event Action<int> SpecialSkillRequested;

    /// <summary>注册正式预制体中的固定按钮，窗口生命周期内执行一次。</summary>
    public override void InitWindow()
    {
        _wearButton.onClick.AddListener(OnWearRequested);
        _takeOffButton.onClick.AddListener(OnTakeOffRequested);
        _closeButton.onClick.AddListener(CloseWindow);
        _activateButtons[0].onClick.AddListener(OnActivateFirst);
        _activateButtons[1].onClick.AddListener(OnActivateSecond);
        _activateButtons[2].onClick.AddListener(OnActivateThird);
    }

    /// <summary>刷新服务器已确认的装备详情，不提前修改穿戴或特技状态。</summary>
    /// <param name="data">控制器根据最新实例和配置生成的纯展示数据。</param>
    public void Render(EquipmentDetailViewData data)
    {
        _nameText.text = data.Name;
        _partText.text = data.PartName;
        _attributesText.text = data.Attributes;
        _attributeValuesText.text = data.AttributeValues;
        _traitsText.text = data.Traits;
        _icon.sprite = null;
        // 图标仅用于展示，装备身份和所有操作仍由控制器持有。
        ResourceMgr.Instance.LoadSpriteAsync(data.IconPath, sprite => _icon.sprite = sprite);
        _wearButton.gameObject.SetActive(!data.IsWorn);
        _takeOffButton.gameObject.SetActive(data.IsWorn);
        // 三个固定槽按服务器保存顺序渲染，空槽按钮停用。
        for (int index = 0; index < 3; index++)
        {
            _specialSkillTexts[index].text = data.SpecialSkillTexts[index];
            _activateButtons[index].interactable = data.CanActivateSpecialSkill[index];
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
    }

    /// <summary>每次打开详情从正文顶部阅读，不改变已经绑定的装备或按钮事件。</summary>
    private void OnEnable()
    {
        _scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>转交穿戴按钮事件。</summary>
    private void OnWearRequested() { WearRequested?.Invoke(); }
    /// <summary>转交卸下按钮事件。</summary>
    private void OnTakeOffRequested() { TakeOffRequested?.Invoke(); }
    /// <summary>转交第一个保存特技槽的启用请求。</summary>
    private void OnActivateFirst() { SpecialSkillRequested?.Invoke(0); }
    /// <summary>转交第二个保存特技槽的启用请求。</summary>
    private void OnActivateSecond() { SpecialSkillRequested?.Invoke(1); }
    /// <summary>转交第三个保存特技槽的启用请求。</summary>
    private void OnActivateThird() { SpecialSkillRequested?.Invoke(2); }
}
