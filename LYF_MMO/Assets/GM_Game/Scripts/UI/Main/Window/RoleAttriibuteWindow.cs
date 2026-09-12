using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色装备窗口，只负责装备数据到固定槽位的布局映射。
/// </summary>
public class RoleAttriibuteWindow : WindowBase
{
    [SerializeField, Header("穿戴装备父组件")] private Transform _content;

    private readonly Dictionary<EquipType, EquipSlotWidget> _equipSlots =
        new Dictionary<EquipType, EquipSlotWidget>();

    private void Start()
    {
        InitializeEquipSlots();
    }

    public override void ReFreshUI(object obj)
    {
        InitializeEquipSlots();
        List<EquipmentViewData> equipments = obj as List<EquipmentViewData>;
        if (equipments == null)
        {
            return;
        }

        List<EquipType> slotTypes = new List<EquipType>(_equipSlots.Keys);
        for (int index = 0; index < slotTypes.Count; index++)
        {
            _equipSlots[slotTypes[index]].Clear();
        }
        for (int index = 0; index < equipments.Count; index++)
        {
            EquipmentViewData equipment = equipments[index];
            EquipSlotWidget slot;
            if (equipment != null && _equipSlots.TryGetValue(equipment.EquipType, out slot))
            {
                slot.RefreshUI(equipment);
            }
        }
    }

    private void InitializeEquipSlots()
    {
        if (_content == null || _equipSlots.Count > 0)
        {
            return;
        }
        EquipType[] equipTypes = (EquipType[])Enum.GetValues(typeof(EquipType));
        for (int index = 0; index < equipTypes.Length; index++)
        {
            EquipType equipType = equipTypes[index];
            Transform equipRoot = _content.Find(GetEquipRootName(equipType));
            if (equipRoot == null)
            {
                continue;
            }
            Transform slotRoot = equipRoot.Find("EquipSlotWidget");
            if (slotRoot == null)
            {
                continue;
            }
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

    private static string GetEquipRootName(EquipType equipType)
    {
        if (equipType == EquipType.WaistWaist)
        {
            return "WaistWaist";
        }
        return equipType.ToString();
    }
}
