using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备窗口显示测试：按 EquipType 构造 EquipmentViewData 列表并直接刷新窗口。
/// 优先采用 Luban 装备配置的名称和图标，不发送协议、不修改角色实际穿戴。
/// </summary>
public class RoleAttriibuteWindowMockServer : MonoBehaviour
{
    /// <summary>用于展示测试装备的窗口，未指定时从场景包括隐藏节点中寻找。</summary>
    [SerializeField, Header("角色属性窗口")] private RoleAttriibuteWindow _window;
    /// <summary>是否启动后自动生成一次装备展示列表。</summary>
    [SerializeField, Header("启动时模拟回包")] private bool _simulateOnStart = true;
    /// <summary>自动生成前等待的缩放时间秒数。</summary>
    [SerializeField, Header("模拟回包延迟秒"), Min(0f)] private float _delaySeconds = 0.5f;

    /// <summary>按配置等待后调用装备展示测试入口，不建立网络连接。</summary>
    private IEnumerator Start()
    {
        if (!_simulateOnStart)
        {
            yield break;
        }

        if (_delaySeconds > 0f)
        {
            yield return new WaitForSeconds(_delaySeconds);
        }

        SendMockResponse();
    }

    /// <summary>按装备部位生成展示数据并直接刷新窗口；优先使用 Luban 表中该部位第一项配置。</summary>
    [ContextMenu("模拟服务器当前装备回包")]
    public void SendMockResponse()
    {
        if (_window == null)
        {
            _window = FindObjectOfType<RoleAttriibuteWindow>(true);
        }

        if (_window == null)
        {
            Debug.LogWarning("RoleAttriibuteWindowMockServer: 找不到 RoleAttriibuteWindow。", this);
            return;
        }

        // 最终交给窗口的纯展示列表。
        List<EquipmentViewData> equipments = new List<EquipmentViewData>();
        // 当前 Luban 装备配置，用来给测试部位选择真实图标。
        Dictionary<int, cfg.EquipInfo> equipConfigMap = LubanMgr.Instance.GetEquipInfos();
        // 配置值引用快照，后续按索引查找匹配部位。
        List<cfg.EquipInfo> equipConfigs = new List<cfg.EquipInfo>();
        if (equipConfigMap != null)
        {
            equipConfigs.AddRange(equipConfigMap.Values);
        }

        // 按当前 EquipType 枚举逐部位生成展示数据，数量随枚举变化，不实际构造网络装备回包。
        // 所有当前装备部位枚举。
        Array equipTypes = Enum.GetValues(typeof(EquipType));
        for (int index = 0; index < equipTypes.Length; index++)
        {
            // 本轮要生成的装备部位。
            EquipType equipType = (EquipType)equipTypes.GetValue(index);
            // 找到的第一个同部位配置，找不到就使用测试缺省值。
            cfg.EquipInfo equipConfig = null;
            for (int configIndex = 0; configIndex < equipConfigs.Count; configIndex++)
            {
                if (equipConfigs[configIndex].EquipType == (int)equipType)
                {
                    equipConfig = equipConfigs[configIndex];
                    break;
                }
            }
            // 优先使用 Luban 装备表中的 ItemTypeId，保证测试显示真实配置图标。
            // 无同部位配置时使用的测试物品类型编号。
            int itemTypeId = 21001 + index;
            // 无配置时的可辨认测试名称。
            string equipName = "测试装备 " + itemTypeId;
            // 无配置时按默认图标命名规则拼出的路径。
            string iconPath = "Icon/Item/Item_" + itemTypeId;
            if (equipConfig != null)
            {
                itemTypeId = equipConfig.ItemTypeId;
                equipName = equipConfig.EquipName;
                iconPath = equipConfig.Icon;
            }
            equipments.Add(new EquipmentViewData(equipType, itemTypeId, equipName,
                iconPath, index + 1));
        }

        _window.ReFreshUI(equipments);
    }

    /// <summary>用空装备列表刷新窗口，检查全部固定装备槽能否清空。</summary>
    [ContextMenu("清空模拟当前装备")]
    public void ClearMockResponse()
    {
        if (_window == null)
        {
            _window = FindObjectOfType<RoleAttriibuteWindow>(true);
        }

        if (_window != null)
        {
            // 空列表代表服务器通知当前没有穿戴任何装备。
            _window.ReFreshUI(new List<EquipmentViewData>());
        }
    }
}
