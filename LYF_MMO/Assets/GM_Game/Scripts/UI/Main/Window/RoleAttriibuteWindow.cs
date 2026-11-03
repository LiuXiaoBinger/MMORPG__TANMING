using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色装备窗口，只负责装备数据到固定槽位的布局映射。
/// </summary>
public class RoleAttriibuteWindow : WindowBase
{
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
