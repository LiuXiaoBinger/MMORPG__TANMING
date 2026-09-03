using System.Collections.Generic;
using UnityEngine;

/**
* Title:
* Descrpiton:
*/

public class RoleAttriibuteWindow : WindowBase
{
    // pan_equip 下的 12 个节点按 EquipType 匹配，不动态创建新的部位节点。
    [SerializeField, Header("穿戴装备父组件")] private Transform _content;

    // 保存部位与 UI 槽位的关系，服务器回包到达后可以直接刷新。
    private readonly Dictionary<EquipType, EquipSlotWidget> _equipSlots =
        new Dictionary<EquipType, EquipSlotWidget>();
    private RectTransform _rectTransform;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        InitializeEquipSlots();
    }

    public override void ReFreshUI(object obj)
    {
        InitializeEquipSlots();

        // RoleKanpsackInfoRet 同时包含背包和当前穿戴装备数据。
        RoleKanpsackInfoRet response = obj as RoleKanpsackInfoRet;
        if (response == null)
        {
            return;
        }

        Dictionary<EquipType, RoleItemInfo> currentEquipments =
            new Dictionary<EquipType, RoleItemInfo>();
        for (int index = 0; index < response.RoleCurrtEquipPack.Count; index++)
        {
            RoleItemInfo itemInfo = response.RoleCurrtEquipPack[index];
            EquipType equipType;
            // 优先使用服务器携带的 EquipInfo，兼容只有 ItemTypeId 的旧数据。
            if (itemInfo == null || !TryGetEquipType(itemInfo, out equipType))
            {
                continue;
            }

            currentEquipments[equipType] = itemInfo;
        }

        // 回包中没有的部位必须清空，避免角色换装后残留旧图标。
        foreach (KeyValuePair<EquipType, EquipSlotWidget> pair in _equipSlots)
        {
            RoleItemInfo itemInfo;
            if (currentEquipments.TryGetValue(pair.Key, out itemInfo))
            {
                pair.Value.RefreshUI(itemInfo);
            }
            else
            {
                pair.Value.Clear();
            }
        }
    }

    private void InitializeEquipSlots()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_content == null || _equipSlots.Count == 12)
        {
            return;
        }

        EquipType[] equipTypes = (EquipType[])System.Enum.GetValues(typeof(EquipType));
        for (int index = 0; index < equipTypes.Length; index++)
        {
            EquipType equipType = equipTypes[index];
            // WaistWaist 是枚举成员名，预制体节点也必须使用这个名称。
            Transform equipRoot = _content.Find(GetEquipRootName(equipType));
            if (equipRoot == null)
            {
                Debug.LogWarning($"RoleAttriibuteWindow: 找不到装备部位节点 {equipType}。", this);
                continue;
            }

            Transform slotRoot = equipRoot.Find("EquipSlotWidget");
            if (slotRoot == null)
            {
                Debug.LogWarning($"RoleAttriibuteWindow: {equipType} 下缺少 EquipSlotWidget。", equipRoot);
                continue;
            }

            EquipSlotWidget slot = slotRoot.GetComponent<EquipSlotWidget>();
            if (slot == null)
            {
                // 现有预制体只保存了视觉节点，首次运行时补充行为组件。
                slot = slotRoot.gameObject.AddComponent<EquipSlotWidget>();
            }

            slot.Initialize();
            slot.Clear();
            _equipSlots[equipType] = slot;
        }
    }

    private static string GetEquipRootName(EquipType equipType)
    {
        return equipType == EquipType.WaistWaist ? "WaistWaist" : equipType.ToString();
    }

    private static bool TryGetEquipType(RoleItemInfo itemInfo, out EquipType equipType)
    {
        if (itemInfo.EquipInfo != null && System.Enum.IsDefined(typeof(EquipType), itemInfo.EquipInfo.EquipType))
        {
            equipType = (EquipType)itemInfo.EquipInfo.EquipType;
            return true;
        }

        cfg.EquipInfo equipConfig = LubanMgr.Instance.GetEquipInfoById(itemInfo.ItemTypeId);
        if (equipConfig != null && System.Enum.IsDefined(typeof(EquipType), equipConfig.EquipType))
        {
            equipType = (EquipType)equipConfig.EquipType;
            return true;
        }

        equipType = default(EquipType);
        return false;
    }

}
