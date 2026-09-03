using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// 在不连接服务器时，模拟服务器返回当前已穿戴装备列表。
/// </summary>
public class RoleAttriibuteWindowMockServer : MonoBehaviour
{
    [SerializeField, Header("角色属性窗口")] private RoleAttriibuteWindow _window;
    [SerializeField, Header("启动时模拟回包")] private bool _simulateOnStart = true;
    [SerializeField, Header("模拟回包延迟秒"), Min(0f)] private float _delaySeconds = 0.5f;

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

        RoleKanpsackInfoRet response = new RoleKanpsackInfoRet
        {
            CmdCode = CmdCode.Succeed
        };

        // 按枚举生成 12 条当前穿戴装备，模拟服务器的 RoleCurrtEquipPack。
        Array equipTypes = Enum.GetValues(typeof(EquipType));
        for (int index = 0; index < equipTypes.Length; index++)
        {
            EquipType equipType = (EquipType)equipTypes.GetValue(index);
            cfg.EquipInfo equipConfig = LubanMgr.Instance.GetEquipInfos()?.Values
                .FirstOrDefault(info => info.EquipType == (int)equipType);
            // 优先使用 Luban 装备表中的 ItemTypeId，保证测试显示真实配置图标。
            int itemTypeId = equipConfig == null ? 21001 + index : equipConfig.ItemTypeId;

            response.RoleCurrtEquipPack.Add(new RoleItemInfo
            {
                ItemId = 900000 + index,
                Count = 1,
                RoleId = 1,
                ItemTypeId = itemTypeId,
                BagType = (int)KnapsackType.RoleCurrtEquipPack,
                BagIndex = index,
                EquipInfo = new RoleEquipItemInfo
                {
                    ItemId = 900000 + index,
                    RoleId = 1,
                    StrengthenLevel = index + 1,
                    EquipType = (int)equipType
                }
            });
        }

        _window.ReFreshUI(response);
    }

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
            _window.ReFreshUI(new RoleKanpsackInfoRet { CmdCode = CmdCode.Succeed });
        }
    }
}
