using System.Collections;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包窗口专用测试组件，在独立 SceneEntityWorld 中创建测试玩家并绑定正式 KnapsackCtrl。
/// 构造五类背包快照和扩容变化，直接送入测试角色的物品组件；不模拟 Socket 收包，也不把测试角色加入正式世界。
/// </summary>
public class KnapsackWindowMockServer : MonoBehaviour
{

    #region 字段
    /// <summary>测试数据要显示到的背包窗口引用。</summary>
    [SerializeField, Header("背包窗口")] private KnapsackWindow _knapsackWindow;

    /// <summary>是否由 Start 自动注入一次测试背包数据。</summary>
    [SerializeField, Header("启动时模拟服务器回包")] private bool _simulateOnStart = true;

    /// <summary>自动注入前等待的缩放时间秒数，给测试界面留出初始化时间。</summary>
    [SerializeField, Header("模拟回包延迟秒"), Min(0f)] private float _delaySeconds = 0.2f;

    /// <summary>每类背包生成的测试物品条数，各类从格号零开始。</summary>
    [SerializeField, Header("每个独立包裹的模拟物品数"), Min(0)] private int _itemsPerPack = 12;

    /// <summary>扩容测试按钮，未指定时按 MockAddItemButton 名称查找。</summary>
    [SerializeField, Header("模拟开格子按钮")] private Button _btnAddMockItem;

    /// <summary>扩容按钮要修改的独立背包分类。</summary>
    [SerializeField, Header("点击时扩容的包裹")] private KnapsackType _addItemPack = KnapsackType.RolePackPlain;

    /// <summary>每次扩容增加的格数，默认九格；是否一整行取决于测试窗口当前列数。</summary>
    [SerializeField, Header("每次点击增加格子数（当前 9 列，默认一整行）"), Min(1)] private int _addItemCountPerClick = 9;

    /// <summary>最近构造的测试背包回包，直接交给测试角色加载，不经过网络。</summary>
    private RoleKanpsackInfoRet _mockResponse;

    /// <summary>测试专用客户端角色，不写入正式角色世界。</summary>
    private PlayerEntity _clientRole;

    /// <summary>测试专用实体世界，由世界统一分配玩家 UID。</summary>
    private SceneEntityWorld _testWorld;

    /// <summary>测试专用背包控制器，验证正式组件到窗口的数据流。</summary>
    private KnapsackCtrl _knapsackCtrl;
    #endregion

    #region 生命周期
    /// <summary>查找并绑定测试扩容按钮，不创建新的界面按钮。</summary>
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

    /// <summary>移除测试监听，释放独立控制器、测试角色及测试实体世界。</summary>
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
        if (_clientRole != null && _testWorld != null)
        {
            _testWorld.Remove(_clientRole.UID);
            _clientRole = null;
        }
        if (_testWorld != null)
        {
            _testWorld.Dispose();
            _testWorld = null;
        }
    }

    /// <summary>按开关和延迟决定是否自动注入测试数据。</summary>
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
    #endregion

    /// <summary>菜单入口：构造背包快照并交给独立测试角色，沿正式物品组件到窗口的链路展示。</summary>
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

    #region 创建与添加
    /// <summary>
    /// 供测试按钮和 Inspector 右键菜单调用：给配置的测试包裹直接增加指定格数，不向真实服务器发请求。
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
        // 独立测试玩家的正式物品组件，扩容变化由它通知绑定控制器。
        RoItemComponent itemComponent;
        if (!_clientRole.TryGetComponent(out itemComponent))
        {
            return;
        }

        // 该测试分类目前的已开格数。
        int currentCount = itemComponent.GetOpenedGridCount(_addItemPack);
        itemComponent.SetOpenedGridCount(_addItemPack,
            currentCount + Mathf.Max(1, _addItemCountPerClick));
    }

    /// <summary>构造五个独立分类的物品和容量数据，作为成功背包回包。</summary>
    private RoleKanpsackInfoRet CreateMockResponse()
    {
        // 只在内存中创建的成功测试回包。
        RoleKanpsackInfoRet response = new RoleKanpsackInfoRet
        {
            CmdCode = CmdCode.Succeed,
            RoleKanpsackInfo = new RoleKanpsackInfo()
        };

        // 回包中保存五类物品与容量的主体。
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

    /// <summary>按 item.BagType 把物品加入 response 的对应分类，未知类型不追加。</summary>
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

    /// <summary>为 type 分类的 bagIndex 格生成测试物品，使用 itemTypeId 类型及 count 数量，角色 ID 固定为一。</summary>
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

    /// <summary>向 response 的容量列表追加 type 分类已开 count 格的数据。</summary>
    private static void AddPackCount(RoleKanpsackInfo response, KnapsackType type, int count)
    {
        response.KanpsackTypeCountLst.Add(new Kanpsacktypecount
        {
            Type = (int)type,
            Count = count
        });
    }
    #endregion

    /// <summary>循环使用 itemTypeIds 为 type 分类填入指定条数的测试物品，格号从零递增。</summary>
    private void PopulatePack(RoleKanpsackInfo response, KnapsackType type, int[] itemTypeIds)
    {
        for (int bagIndex = 0; bagIndex < _itemsPerPack; bagIndex++)
        {
            // 根据格号循环取测试类型，避免每格都需要单独配置。
            int itemTypeId = itemTypeIds[bagIndex % itemTypeIds.Length];
            // 测试堆叠数量循环为 1 到 99。
            int count = bagIndex % 99 + 1;
            AddItemToPack(response, CreateItem(type, bagIndex, itemTypeId, count));
        }
    }

    #region 查询与判定
    /// <summary>返回该分类用于测试的固定物品编号组合，缺省分类使用同一组三种物品。</summary>
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

    /// <summary>
    /// Mock 容量至少为 81，且不得小于已生成物品占用的最高格子位置。
    /// </summary>
    private static int GetInitialCapacity(IList<RoleItemInfo> itemList)
    {
        // 初始保底容量，后续扩大到覆盖所有已填物品格号。
        int capacity = 81;
        for (int index = 0; index < itemList.Count; index++)
        {
            capacity = Mathf.Max(capacity, itemList[index].BagIndex + 1);
        }

        return capacity;
    }
    #endregion

    /// <summary>创建测试角色并把背包控制器绑定到角色物品组件。</summary>
    private void EnsureTestRole()
    {
        if (_clientRole != null)
        {
            return;
        }

        // 独立测试角色的最小基础数据，不设为正式场景本地主角。
        MainRoleInfo roleInfo = new MainRoleInfo
        {
            BaseInfo = new RoleBaseInfo
            {
                RoleId = 1,
                Nickname = "背包测试角色"
            }
        };
        if (_testWorld == null)
        {
            _testWorld = new SceneEntityWorld();
        }
        _clientRole = _testWorld.Create(EntityType.Player, 0) as PlayerEntity;
        if (_clientRole == null)
        {
            Debug.LogError("KnapsackWindowMockServer: 创建测试玩家实体失败。", this);
            return;
        }
        _clientRole.InitializeLocalRole(roleInfo);
        _clientRole.AfterAddedToWorld();
        _knapsackCtrl = new KnapsackCtrl(_knapsackWindow);
        _knapsackCtrl.BindRole(_clientRole);
    }

}
