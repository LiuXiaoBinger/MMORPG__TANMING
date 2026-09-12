using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仅用于客户端调试：构造与服务器 RoleKanpsackInfoRet 相同的背包快照。
/// 真实服务器回包会通过 MainCtrl 覆盖这份测试数据。
/// </summary>
public class KnapsackWindowMockServer : MonoBehaviour
{
    [SerializeField, Header("背包窗口")] private KnapsackWindow _knapsackWindow;
    [SerializeField, Header("启动时模拟服务器回包")] private bool _simulateOnStart = true;
    [SerializeField, Header("模拟回包延迟秒"), Min(0f)] private float _delaySeconds = 0.2f;
    [SerializeField, Header("每个独立包裹的模拟物品数"), Min(0)] private int _itemsPerPack = 12;
    [SerializeField, Header("模拟开格子按钮")] private Button _btnAddMockItem;
    [SerializeField, Header("点击时扩容的包裹")] private KnapsackType _addItemPack = KnapsackType.RolePackPlain;
    [SerializeField, Header("每次点击增加格子数（当前 9 列，默认一整行）"), Min(1)] private int _addItemCountPerClick = 9;

    private RoleKanpsackInfoRet _mockResponse;
    /// <summary>测试专用客户端角色，不写入正式角色世界。</summary>
    private ClientRole _clientRole;
    /// <summary>测试专用背包控制器，验证正式组件到窗口的数据流。</summary>
    private KnapsackCtrl _knapsackCtrl;

    private void Awake()
    {
        if (_btnAddMockItem == null)
        {
            // 仅在测试组件内按固定名称查找用户已放置的按钮，不创建运行时按钮。
            Button[] buttons;
            if (_knapsackWindow == null)
            {
                buttons = GetComponentsInChildren<Button>(true);
            }
            else
            {
                buttons = _knapsackWindow.GetComponentsInChildren<Button>(true);
            }
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].name == "MockAddItemButton")
                {
                    _btnAddMockItem = buttons[index];
                    break;
                }
            }
        }

        if (_btnAddMockItem != null)
        {
            _btnAddMockItem.onClick.AddListener(AddMockSlots);
        }
    }

    private void OnDestroy()
    {
        if (_btnAddMockItem != null)
        {
            _btnAddMockItem.onClick.RemoveListener(AddMockSlots);
        }
        if (_knapsackCtrl != null)
        {
            _knapsackCtrl.Dispose();
            _knapsackCtrl = null;
        }
        if (_clientRole != null)
        {
            _clientRole.Dispose();
            _clientRole = null;
        }
    }

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

    [ContextMenu("模拟服务器背包回包")]
    public void SendMockResponse()
    {
        if (_knapsackWindow == null)
        {
            Debug.LogWarning("KnapsackWindowMockServer: 请在 Inspector 中拖入 KnapsackWindow。", this);
            return;
        }

        _mockResponse = CreateMockResponse();
        EnsureTestRole();
        _clientRole.LoadRoleData(_mockResponse);
    }

    /// <summary>
    /// 供测试按钮和 Inspector 右键菜单调用：模拟服务器确认当前包裹新增一整行容量。
    /// </summary>
    [ContextMenu("模拟开一行背包格子")]
    public void AddMockSlots()
    {
        if (_knapsackWindow == null)
        {
            Debug.LogWarning("KnapsackWindowMockServer: 请在 Inspector 中拖入 KnapsackWindow。", this);
            return;
        }

        EnsureTestRole();
        RoItemComponent itemComponent;
        if (!_clientRole.TryGetComponent(out itemComponent))
        {
            return;
        }

        int currentCount = itemComponent.GetOpenedGridCount(_addItemPack);
        itemComponent.SetOpenedGridCount(_addItemPack,
            currentCount + Mathf.Max(1, _addItemCountPerClick));
    }

    /// <summary>创建测试角色并把背包控制器绑定到角色物品组件。</summary>
    private void EnsureTestRole()
    {
        if (_clientRole != null)
        {
            return;
        }

        MainRoleInfo roleInfo = new MainRoleInfo
        {
            BaseInfo = new RoleBaseInfo
            {
                RoleId = 1,
                Nickname = "背包测试角色"
            }
        };
        _clientRole = new ClientRole(roleInfo);
        _knapsackCtrl = new KnapsackCtrl(_knapsackWindow);
        _knapsackCtrl.BindRole(_clientRole);
    }

    private RoleKanpsackInfoRet CreateMockResponse()
    {
        RoleKanpsackInfoRet response = new RoleKanpsackInfoRet
        {
            CmdCode = CmdCode.Succeed,
            RoleKanpsackInfo = new RoleKanpsackInfo()
        };

        RoleKanpsackInfo info = response.RoleKanpsackInfo;

        // 五个列表彼此独立：全部包裹不再包含装备、消耗品或材料包裹中的对象。
        PopulatePack(info, KnapsackType.RolePackPlain, GetMockItemTypeIds(KnapsackType.RolePackPlain));
        PopulatePack(info, KnapsackType.RolePackEquip, GetMockItemTypeIds(KnapsackType.RolePackEquip));
        PopulatePack(info, KnapsackType.RolePackConsume, GetMockItemTypeIds(KnapsackType.RolePackConsume));
        PopulatePack(info, KnapsackType.RolePackMaterial, GetMockItemTypeIds(KnapsackType.RolePackMaterial));
        PopulatePack(info, KnapsackType.RoleCurrtEquipPack, GetMockItemTypeIds(KnapsackType.RoleCurrtEquipPack));

        AddPackCount(info, KnapsackType.RolePackPlain, GetInitialCapacity(info.RolePackPlain));
        AddPackCount(info, KnapsackType.RolePackEquip, GetInitialCapacity(info.RolePackEquip));
        AddPackCount(info, KnapsackType.RolePackConsume, GetInitialCapacity(info.RolePackConsume));
        AddPackCount(info, KnapsackType.RolePackMaterial, GetInitialCapacity(info.RolePackMaterial));
        AddPackCount(info, KnapsackType.RoleCurrtEquipPack, GetInitialCapacity(info.RoleCurrtEquipPack));

        return response;
    }

    private static int[] GetMockItemTypeIds(KnapsackType type)
    {
        switch (type)
        {
            case KnapsackType.RolePackConsume:
                return new[] { 2003, 2005, 2401 };
            case KnapsackType.RolePackMaterial:
                return new[] { 2501, 2601, 2701 };
            case KnapsackType.RoleCurrtEquipPack:
                return new[] { 2202 };
            default:
                return new[] { 2001, 2201, 2301 };
        }
    }

    private void PopulatePack(RoleKanpsackInfo response, KnapsackType type, int[] itemTypeIds)
    {
        for (int bagIndex = 0; bagIndex < _itemsPerPack; bagIndex++)
        {
            int itemTypeId = itemTypeIds[bagIndex % itemTypeIds.Length];
            int count = bagIndex % 99 + 1;
            AddItemToPack(response, CreateItem(type, bagIndex, itemTypeId, count));
        }
    }

    private static void AddItemToPack(RoleKanpsackInfo response, RoleItemInfo item)
    {
        // 只写入物品所属的那个包裹列表，禁止跨包裹追加。
        switch ((KnapsackType)item.BagType)
        {
            case KnapsackType.RolePackPlain:
                response.RolePackPlain.Add(item);
                break;
            case KnapsackType.RolePackEquip:
                response.RolePackEquip.Add(item);
                break;
            case KnapsackType.RolePackConsume:
                response.RolePackConsume.Add(item);
                break;
            case KnapsackType.RolePackMaterial:
                response.RolePackMaterial.Add(item);
                break;
            case KnapsackType.RoleCurrtEquipPack:
                response.RoleCurrtEquipPack.Add(item);
                break;
            default:
                break;
        }
    }

    private static RoleItemInfo CreateItem(KnapsackType type, int bagIndex, int itemTypeId, int count)
    {
        return new RoleItemInfo
        {
            ItemUid = itemTypeId * 100 + bagIndex,
            ItemTypeId = itemTypeId,
            Count = count,
            RoleId = 1,
            BagType = (int)type,
            BagIndex = bagIndex
        };
    }

    private static void AddPackCount(RoleKanpsackInfo response, KnapsackType type, int count)
    {
        response.KanpsackTypeCountLst.Add(new Kanpsacktypecount
        {
            Type = (int)type,
            Count = count
        });
    }

    /// <summary>
    /// Mock 容量至少为 81，且不得小于已生成物品占用的最高格子位置。
    /// </summary>
    private static int GetInitialCapacity(IList<RoleItemInfo> itemList)
    {
        int capacity = 81;
        for (int index = 0; index < itemList.Count; index++)
        {
            capacity = Mathf.Max(capacity, itemList[index].BagIndex + 1);
        }

        return capacity;
    }

}
