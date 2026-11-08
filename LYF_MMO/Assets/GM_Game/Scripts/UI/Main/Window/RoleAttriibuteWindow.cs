using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色装备窗口，只负责装备数据到固定槽位的布局映射。
/// </summary>
public class RoleAttriibuteWindow : WindowBase
{
    /// <summary>窗口的实际显隐通知，控制器据此订阅或解除基因列表事件。</summary>
    public event Action<bool> GeneVisibilityChanged;
    /// <summary>正式预制体中的基因滚动内容容器。</summary>
    [SerializeField] private RectTransform _geneContent;
    /// <summary>正式预制体中默认隐藏的完整基因行模板。</summary>
    [SerializeField] private RoleGeneItemWidget _geneRowTemplate;
    /// <summary>正式预制体中的无生效基因提示。</summary>
    [SerializeField] private TMP_Text _geneEmptyText;
    /// <summary>本窗口已创建的行，列表变化时按索引复用，窗口销毁时随层级释放。</summary>
    private readonly List<RoleGeneItemWidget> geneRows = new List<RoleGeneItemWidget>();
    /// <summary>当前可见且有有限寿命的行；永久基因不参加时间刷新。</summary>
    private readonly List<RoleGeneItemWidget> timedGeneRows = new List<RoleGeneItemWidget>();
    /// <summary>距上一次倒计时显示刷新的秒数。</summary>
    private float geneDisplayElapsed;

    /// <summary>真实窗口打开时通知控制器读取当前节点，重开也能显示最新列表。</summary>
    private void OnEnable() { GeneVisibilityChanged?.Invoke(true); }

    /// <summary>隐藏时停止订阅及时间刷新，并释放对旧角色节点的引用。</summary>
    private void OnDisable()
    {
        GeneVisibilityChanged?.Invoke(false);
        timedGeneRows.Clear();
        // 窗口对象可以复用，行本身保留但不再持有角色数据。
        for (int index = 0; index < geneRows.Count; index++) geneRows[index].Clear();
    }

    /// <summary>可见时每四分之一秒检查有限节点的显示秒数，属性列表不逐帧计算。</summary>
    private void Update()
    {
        if (timedGeneRows.Count == 0) return;
        geneDisplayElapsed += Time.unscaledDeltaTime;
        if (geneDisplayElapsed < 0.25f) return;
        geneDisplayElapsed = 0;
        // 每行仅当秒数改变时才写 TMP 文本。
        for (int index = 0; index < timedGeneRows.Count; index++) timedGeneRows[index].RefreshTime();
    }

    /// <summary>只在列表、层数或次数变化时更新当前可见行，滚动区域负责容纳全部根。</summary>
    /// <param name="items">控制器提供的当前完整展示列表，元素引用真实节点。</param>
    public void RenderGenes(IReadOnlyList<RoleGeneViewData> items)
    {
        timedGeneRows.Clear();
        geneDisplayElapsed = 0;
        _geneEmptyText.gameObject.SetActive(items.Count == 0);
        // 正式行模板的全部控件引用在预制体中绑定，运行时只实例化列表行。
        for (int index = 0; index < items.Count; index++)
        {
            if (index == geneRows.Count) geneRows.Add(Instantiate(_geneRowTemplate, _geneContent));
            // 同索引复用当前窗口的展示行，不给业务节点增加生命周期。
            RoleGeneItemWidget row = geneRows[index];
            row.gameObject.SetActive(true);
            row.Bind(items[index]);
            if (row.HasCountdown) timedGeneRows.Add(row);
        }
        // 删除项对应的旧行立即隐藏，避免同数量替换或卸装留下旧文字。
        for (int index = items.Count; index < geneRows.Count; index++)
        {
            geneRows[index].Clear();
            geneRows[index].gameObject.SetActive(false);
        }
    }

    /// <summary>已穿戴装备格点击事件，携带装备实例 UID。</summary>
    public event Action<long> EquipmentSelected;

    /// <summary>将装备格点击转交控制器打开详情。</summary>
    /// <param name="itemUid">被点击的已穿戴装备唯一编号。</param>
    private void OnEquipmentSelected(long itemUid)
    {
        EquipmentSelected?.Invoke(itemUid);
    }
    /// <summary>从装备区域寻找预制体固定槽位，建立部位到控件的映射。</summary>
    private void Start()
    {
        InitializeEquipSlots();
    }

    /// <summary>关闭角色属性，并沿I键关闭流程恢复打开前的角色镜头。</summary>
    public override void CloseWindow()
    {
        base.CloseWindow();
        CameraMgr.Instance.RoleAttrWindowAngle(this);
    }

    /// <summary>装备部位节点的共同父对象，子节点名按 EquipType 查找。</summary>
    [SerializeField, Header("穿戴装备父组件")] private Transform _content;

    /// <summary>原角色信息区的昵称文本。</summary>
    [SerializeField] private TMP_Text _nicknameText;
    /// <summary>原职业区域中的职业名文本。</summary>
    [SerializeField] private TMP_Text _jobNameText;
    /// <summary>原生命条上的当前值与上限文本。</summary>
    [SerializeField] private TMP_Text _hpText;
    /// <summary>原法力条上的当前值与上限文本。</summary>
    [SerializeField] private TMP_Text _mpText;
    /// <summary>原角色生命条，值使用0至1比例。</summary>
    [SerializeField] private Slider _hpSlider;
    /// <summary>原角色法力条，值使用0至1比例。</summary>
    [SerializeField] private Slider _mpSlider;
    /// <summary>按 RoleAttributeViewData 约定顺序保存原16个属性数值文本。</summary>
    [SerializeField] private TMP_Text[] _attributeValues;

    /// <summary>在原属性面板中渲染控制器提供的最终属性和资源。</summary>
    /// <param name="data">服务器最终属性转换成的展示值，不含客户端概率推算。</param>
    public void RenderAttributes(RoleAttributeViewData data)
    {
        _nicknameText.text = data.Nickname;
        _jobNameText.text = data.JobName;
        _hpText.text = data.HpText;
        _mpText.text = data.MpText;
        _hpSlider.value = data.HpRatio;
        _mpSlider.value = data.MpRatio;
        // 原16个固定属性行与纯展示数组同序，不创建额外属性控件。
        for (int index = 0; index < _attributeValues.Length; index++)
        {
            _attributeValues[index].text = data.AttributeValues[index];
        }
    }

    /// <summary>已经找到并初始化的固定装备槽，刷新时先全部清空再填入当前穿戴。</summary>
    private readonly Dictionary<EquipType, EquipSlotWidget> _equipSlots =
        new Dictionary<EquipType, EquipSlotWidget>();

    /// <summary>用 obj 中的装备展示列表重填固定部位槽；未穿戴部位保持空槽。</summary>
    public override void ReFreshUI(object obj)
    {
        InitializeEquipSlots();
        // 控制器提供的当前穿戴列表。
        List<EquipmentViewData> equipments = obj as List<EquipmentViewData>;
        if (equipments == null)
        {
            return;
        }

        // 已登记部位的快照，用来先清掉上一次展示。
        List<EquipType> slotTypes = new List<EquipType>(_equipSlots.Keys);
        for (int index = 0; index < slotTypes.Count; index++)
        {
            _equipSlots[slotTypes[index]].Clear();
        }
        for (int index = 0; index < equipments.Count; index++)
        {
            // 当前待显示的装备摘要。
            EquipmentViewData equipment = equipments[index];
            // 与该装备部位对应的预制体槽位。
            EquipSlotWidget slot;
            if (equipment != null && _equipSlots.TryGetValue(equipment.EquipType, out slot))
            {
                slot.RefreshUI(equipment);
            }
        }
    }

    /// <summary>首次按枚举查找已有部位节点，必要时给槽位补组件；建立映射后不重复初始化。</summary>
    private void InitializeEquipSlots()
    {
        if (_content == null || _equipSlots.Count > 0)
        {
            return;
        }
        // 所有装备部位枚举，预制体未提供的部位会跳过。
        EquipType[] equipTypes = (EquipType[])Enum.GetValues(typeof(EquipType));
        for (int index = 0; index < equipTypes.Length; index++)
        {
            // 当前处理的装备部位。
            EquipType equipType = equipTypes[index];
            // 按部位名称定位外层节点。
            Transform equipRoot = _content.Find(GetEquipRootName(equipType));
            if (equipRoot == null)
            {
                continue;
            }
            // 该部位下固定命名的装备格子节点。
            Transform slotRoot = equipRoot.Find("EquipSlotWidget");
            if (slotRoot == null)
            {
                continue;
            }
            // 槽位展示组件，旧预制体未挂载时在此补上。
            EquipSlotWidget slot = slotRoot.GetComponent<EquipSlotWidget>();
            if (slot == null)
            {
                slot = slotRoot.gameObject.AddComponent<EquipSlotWidget>();
            }
            slot.Initialize();
            slot.EquipmentSelected += OnEquipmentSelected;
            slot.Clear();
            _equipSlots[equipType] = slot;
        }
    }

    /// <summary>把 equipType 转成预制体节点名，腰部沿用现有 WaistWaist 拼写。</summary>
    private static string GetEquipRootName(EquipType equipType)
    {
        if (equipType == EquipType.WaistWaist)
        {
            return "WaistWaist";
        }
        return equipType.ToString();
    }
}
