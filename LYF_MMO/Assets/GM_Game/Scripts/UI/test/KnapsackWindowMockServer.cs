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
    [SerializeField, Header("每个独立包裹的模拟物品数"), Min(1)] private int _itemsPerPack = 500;
    [SerializeField, Header("模拟添加物品按钮")] private Button _btnAddMockItem;
    [SerializeField, Header("点击时追加到的包裹")] private KnapsackType _addItemPack = KnapsackType.RolePackAll;
    [SerializeField, Header("每次点击添加数量（当前 9 列，默认添加一整行）"), Min(1)] private int _addItemCountPerClick = 9;

    private RoleKanpsackInfoRet _mockResponse;

    private void Awake()
    {
        if (_btnAddMockItem == null)
        {
            // 允许场景中的测试按钮通过固定名称自动绑定，避免每次替换预制体都要手动拖引用。
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].name == "MockAddItemButton")
                {
                    _btnAddMockItem = buttons[index];
                    break;
                }
            }
        }

        if (_btnAddMockItem == null)
        {
            _btnAddMockItem = CreateRuntimeAddButton();
        }

        if (_btnAddMockItem != null)
        {
            _btnAddMockItem.onClick.AddListener(AddMockItems);
        }
    }

    private Button CreateRuntimeAddButton()
    {
        if (_knapsackWindow == null)
        {
            return null;
        }

        Transform bg = _knapsackWindow.transform.Find("BG");
        if (bg == null)
        {
            return null;
        }

        GameObject buttonObject = new GameObject(
            "MockAddItemButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = 5;
        buttonObject.transform.SetParent(bg, false);

        RectTransform buttonRect = buttonObject.transform as RectTransform;
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-80f, -282f);
        buttonRect.sizeDelta = new Vector2(110f, 52f);

        Image image = buttonObject.GetComponent<Image>();
        Transform styleSource = bg.Find("Btn_Kuaijie");
        Image sourceImage = styleSource == null ? null : styleSource.GetComponent<Image>();
        if (sourceImage != null)
        {
            image.sprite = sourceImage.sprite;
            image.type = sourceImage.type;
            image.material = sourceImage.material;
            image.color = sourceImage.color;
        }
        else
        {
            image.color = new Color(0.19f, 0.52f, 0.88f, 1f);
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TMPro.TextMeshProUGUI));
        labelObject.layer = 5;
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.transform as RectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI label = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
        label.text = "模拟添加";
        label.fontSize = 18f;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        TMPro.TMP_Text[] sourceTexts = bg.GetComponentsInChildren<TMPro.TMP_Text>(true);
        for (int index = 0; index < sourceTexts.Length; index++)
        {
            if (sourceTexts[index] != label)
            {
                label.font = sourceTexts[index].font;
                break;
            }
        }

        return button;
    }

    private void OnDestroy()
    {
        if (_btnAddMockItem != null)
        {
            _btnAddMockItem.onClick.RemoveListener(AddMockItems);
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
        _knapsackWindow.ReFreshUI(_mockResponse);
    }

    /// <summary>
    /// 供 UI 按钮和 Inspector 右键菜单调用：在指定独立包裹末尾模拟服务器新增物品。
    /// </summary>
    [ContextMenu("模拟添加物品")]
    public void AddMockItems()
    {
        if (_knapsackWindow == null)
        {
            Debug.LogWarning("KnapsackWindowMockServer: 请在 Inspector 中拖入 KnapsackWindow。", this);
            return;
        }

        if (_mockResponse == null)
        {
            _mockResponse = CreateMockResponse();
        }

        int[] itemTypeIds = GetMockItemTypeIds(_addItemPack);
        int startBagIndex = GetPackItems(_mockResponse, _addItemPack).Count;
        for (int offset = 0; offset < _addItemCountPerClick; offset++)
        {
            int bagIndex = startBagIndex + offset;
            int itemTypeId = itemTypeIds[bagIndex % itemTypeIds.Length];
            AddItemToPack(_mockResponse, CreateItem(_addItemPack, bagIndex, itemTypeId, bagIndex % 99 + 1));
        }

        UpdatePackCount(_mockResponse, _addItemPack);
        _knapsackWindow.ReFreshUI(_mockResponse);
    }

    private RoleKanpsackInfoRet CreateMockResponse()
    {
        RoleKanpsackInfoRet response = new RoleKanpsackInfoRet
        {
            CmdCode = CmdCode.Succeed
        };

        // 五个列表彼此独立：全部包裹不再包含装备、消耗品或材料包裹中的对象。
        PopulatePack(response, KnapsackType.RolePackAll, GetMockItemTypeIds(KnapsackType.RolePackAll));
        PopulatePack(response, KnapsackType.RolePackEquip, GetMockItemTypeIds(KnapsackType.RolePackEquip));
        PopulatePack(response, KnapsackType.RolePackConsume, GetMockItemTypeIds(KnapsackType.RolePackConsume));
        PopulatePack(response, KnapsackType.RolePackMaterial, GetMockItemTypeIds(KnapsackType.RolePackMaterial));
        PopulatePack(response, KnapsackType.RoleCurrtEquipPack, GetMockItemTypeIds(KnapsackType.RoleCurrtEquipPack));

        AddPackCount(response, KnapsackType.RolePackAll, response.RolePackAll.Count);
        AddPackCount(response, KnapsackType.RolePackEquip, response.RolePackEquip.Count);
        AddPackCount(response, KnapsackType.RolePackConsume, response.RolePackConsume.Count);
        AddPackCount(response, KnapsackType.RolePackMaterial, response.RolePackMaterial.Count);
        AddPackCount(response, KnapsackType.RoleCurrtEquipPack, response.RoleCurrtEquipPack.Count);

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

    private static IList<RoleItemInfo> GetPackItems(RoleKanpsackInfoRet response, KnapsackType type)
    {
        switch (type)
        {
            case KnapsackType.RolePackEquip:
                return response.RolePackEquip;
            case KnapsackType.RolePackConsume:
                return response.RolePackConsume;
            case KnapsackType.RolePackMaterial:
                return response.RolePackMaterial;
            case KnapsackType.RoleCurrtEquipPack:
                return response.RoleCurrtEquipPack;
            default:
                return response.RolePackAll;
        }
    }

    private void PopulatePack(RoleKanpsackInfoRet response, KnapsackType type, int[] itemTypeIds)
    {
        for (int bagIndex = 0; bagIndex < _itemsPerPack; bagIndex++)
        {
            int itemTypeId = itemTypeIds[bagIndex % itemTypeIds.Length];
            int count = bagIndex % 99 + 1;
            AddItemToPack(response, CreateItem(type, bagIndex, itemTypeId, count));
        }
    }

    private static void AddItemToPack(RoleKanpsackInfoRet response, RoleItemInfo item)
    {
        // 只写入物品所属的那个包裹列表，禁止跨包裹追加。
        switch ((KnapsackType)item.BagType)
        {
            case KnapsackType.RolePackAll:
                response.RolePackAll.Add(item);
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
            ItemId = itemTypeId * 100 + bagIndex,
            ItemTypeId = itemTypeId,
            Count = count,
            RoleId = 1,
            BagType = (int)type,
            BagIndex = bagIndex
        };
    }

    private static void AddPackCount(RoleKanpsackInfoRet response, KnapsackType type, int count)
    {
        response.KanpsackTypeCountLst.Add(new Kanpsacktypecount
        {
            Type = (int)type,
            Count = count
        });
    }

    private static void UpdatePackCount(RoleKanpsackInfoRet response, KnapsackType type)
    {
        int count = GetPackItems(response, type).Count;
        for (int index = 0; index < response.KanpsackTypeCountLst.Count; index++)
        {
            Kanpsacktypecount packCount = response.KanpsackTypeCountLst[index];
            if (packCount.Type == (int)type)
            {
                packCount.Count = count;
                return;
            }
        }

        AddPackCount(response, type, count);
    }
}
