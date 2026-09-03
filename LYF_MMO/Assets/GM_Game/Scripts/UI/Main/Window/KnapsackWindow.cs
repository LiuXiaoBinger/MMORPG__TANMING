using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KnapsackWindow : WindowBase
{
    // ScrollRect Content，所有背包格子都挂在该节点下。
    [SerializeField, Header("背包 Slot 父组件")] private Transform _content;
    // 用于首次创建及对象池扩容的格子模板。
    [SerializeField, Header("背包 Slot 模板")] private KnapsackSlotWidget _slotTemplate;
    // 提供滚动事件和可视区域尺寸。
    [SerializeField, Header("背包滚动框")] private ScrollRect _scrollRect;
    // 没有物品时仍显示的空格子数量下限。
    [SerializeField, Header("空背包保底格子数"), Min(1)] private int _minimumSlotCount = 1;
    // 可视区域外额外保留的行数，减少滚动时的格子复用频率。
    [SerializeField, Header("虚拟列表缓冲行"), Min(0)] private int _bufferRowCount = 1;

    [SerializeField, Header("金币")] private TMP_Text _texGold;
    [SerializeField, Header("灵石")] private TMP_Text _texLs;

    [Header("背包分类按钮")]
    [SerializeField] private Button _btnAll;
    [SerializeField] private Button _btnEquip;
    [SerializeField] private Button _btnConsume;
    [SerializeField] private Button _btnMaterial;
    [SerializeField] private Button _btnCurrtEquip;

    [Header("背包分类选中状态")]
    [SerializeField] private GameObject _allOff;
    [SerializeField] private GameObject _allOn;
    [SerializeField] private GameObject _equipOff;
    [SerializeField] private GameObject _equipOn;
    [SerializeField] private GameObject _consumeOff;
    [SerializeField] private GameObject _consumeOn;
    [SerializeField] private GameObject _materialOff;
    [SerializeField] private GameObject _materialOn;
    [SerializeField] private GameObject _currtEquipOff;
    [SerializeField] private GameObject _currtEquipOn;

    // 虚拟列表当前持有的可见格子，数量只和可视区域有关，不随物品总数增长。
    private readonly List<KnapsackSlotWidget> _activeSlots = new List<KnapsackSlotWidget>();
    // 格子对象池：切换分类、窗口关闭或可见行减少时，格子会回收并等待复用。
    private readonly Stack<KnapsackSlotWidget> _slotPool = new Stack<KnapsackSlotWidget>();
    // 读取格子尺寸、间距、边距和列数配置。
    private GridLayoutGroup _gridLayout;
    // 最近一次服务端返回的背包数据快照。
    private RoleKanpsackInfoRet _knapsackInfo;
    // 当前正在展示的背包分类。
    private KnapsackType _currtType = KnapsackType.RolePackAll;
    // 用于计算 Content 高度的逻辑格子总数，包含空白补位格子。
    private int _virtualSlotCount;
    // 当前布局可容纳的列数。
    private int _columnCount;
    // 当前视口内可显示的行数。
    private int _visibleRowCount;
    // 上次已绑定到对象池格子的首行，避免同一行重复刷新。
    private int _firstVisibleRow = -1;
    // 用于识别同一响应对象是否新增了当前分类的物品。
    private int _lastRenderedItemCount = -1;
    // 防止 Awake、OnEnable 与 ReFreshUI 重复注册事件和回收模板。
    private bool _initialized;

    /// <summary>
    /// 接收服务器背包快照，并刷新当前分类。
    /// </summary>
    public override void ReFreshUI(object obj)
    {
        EnsureInitialized();

        if (obj is RoleKanpsackInfoRet roleKanpsackInfo)
        {
            // MockServer 会原地追加到同一个响应对象，因此使用上一次渲染的数量作比较。
            int previousItemCount = _knapsackInfo == null ? -1 : _lastRenderedItemCount;
            int incomingItemCount = GetItemList(roleKanpsackInfo, _currtType).Count;
            bool addedToCurrentPack = previousItemCount >= 0 && incomingItemCount > previousItemCount;
            _knapsackInfo = roleKanpsackInfo;
            RefreshCurrentType(addedToCurrentPack);
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        // 窗口重新显示时再刷新一次，确保使用当前 ScrollRect 的实际尺寸。
        if (_initialized)
        {
            RefreshCurrentType();
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _gridLayout = _content != null ? _content.GetComponent<GridLayoutGroup>() : null;

        // GridLayoutGroup 保持启用，供预制体继续保存格子尺寸、间距和列数配置。
        // 每个可复用格子都标记为忽略布局，位置由虚拟列表按当前滚动行计算。
        PrepareSlotForVirtualLayout(_slotTemplate);

        BindTabButtons();
        BindScrollRect();
        SetTabVisuals();

        if (_slotTemplate != null)
        {
            // 预制体中的模板也是对象池的第一个可复用对象。
            ReleaseSlot(_slotTemplate);
        }

        RefreshCurrentType();
    }

    private void OnDestroy()
    {
        if (_initialized)
        {
            UnbindTabButtons();
            UnbindScrollRect();
        }
    }

    private void BindTabButtons()
    {
        if (_btnAll != null) _btnAll.onClick.AddListener(SelectAll);
        if (_btnEquip != null) _btnEquip.onClick.AddListener(SelectEquip);
        if (_btnConsume != null) _btnConsume.onClick.AddListener(SelectConsume);
        if (_btnMaterial != null) _btnMaterial.onClick.AddListener(SelectMaterial);
        if (_btnCurrtEquip != null) _btnCurrtEquip.onClick.AddListener(SelectCurrtEquip);
    }

    private void UnbindTabButtons()
    {
        if (_btnAll != null) _btnAll.onClick.RemoveListener(SelectAll);
        if (_btnEquip != null) _btnEquip.onClick.RemoveListener(SelectEquip);
        if (_btnConsume != null) _btnConsume.onClick.RemoveListener(SelectConsume);
        if (_btnMaterial != null) _btnMaterial.onClick.RemoveListener(SelectMaterial);
        if (_btnCurrtEquip != null) _btnCurrtEquip.onClick.RemoveListener(SelectCurrtEquip);
    }

    private void BindScrollRect()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }
    }

    private void UnbindScrollRect()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }
    }

    private void SelectAll() => SelectType(KnapsackType.RolePackAll);
    private void SelectEquip() => SelectType(KnapsackType.RolePackEquip);
    private void SelectConsume() => SelectType(KnapsackType.RolePackConsume);
    private void SelectMaterial() => SelectType(KnapsackType.RolePackMaterial);
    private void SelectCurrtEquip() => SelectType(KnapsackType.RoleCurrtEquipPack);

    private void SelectType(KnapsackType type)
    {
        if (_currtType == type)
        {
            return;
        }

        _currtType = type;
        SetTabVisuals();
        RefreshCurrentType();
    }

    private void SetTabVisuals()
    {
        SetTabVisual(_allOff, _allOn, _currtType == KnapsackType.RolePackAll);
        SetTabVisual(_equipOff, _equipOn, _currtType == KnapsackType.RolePackEquip);
        SetTabVisual(_consumeOff, _consumeOn, _currtType == KnapsackType.RolePackConsume);
        SetTabVisual(_materialOff, _materialOn, _currtType == KnapsackType.RolePackMaterial);
        SetTabVisual(_currtEquipOff, _currtEquipOn, _currtType == KnapsackType.RoleCurrtEquipPack);
    }

    private static void SetTabVisual(GameObject off, GameObject on, bool selected)
    {
        if (off != null)
        {
            off.SetActive(!selected);
        }

        if (on != null)
        {
            on.SetActive(selected);
        }
    }

    private void RefreshCurrentType(bool scrollToNewest = false)
    {
        if (_slotTemplate == null || _content == null)
        {
            return;
        }

        IList<RoleItemInfo> itemList = _knapsackInfo == null ? null : GetItemList(_currtType);
        int itemCount = itemList == null ? 0 : itemList.Count;
        _columnCount = GetColumnCount(_gridLayout);
        _virtualSlotCount = GetVirtualSlotCount(itemCount);
        _visibleRowCount = GetVisibleRowCount();

        // 500 件物品只会创建首屏加缓冲行的 Slot，剩余物品由 Content 高度表示。
        EnsureVisibleSlotCount();
        UpdateContentHeight(_virtualSlotCount);
        if (scrollToNewest)
        {
            ResetScrollToBottom();
        }
        else
        {
            ResetScrollToTop();
        }
        RefreshVisibleSlots(true);
        _lastRenderedItemCount = itemCount;
    }

    private IList<RoleItemInfo> GetItemList(KnapsackType type)
    {
        return GetItemList(_knapsackInfo, type);
    }

    private static IList<RoleItemInfo> GetItemList(RoleKanpsackInfoRet info, KnapsackType type)
    {
        if (info == null)
        {
            return null;
        }

        switch (type)
        {
            case KnapsackType.RolePackEquip:
                return info.RolePackEquip;
            case KnapsackType.RolePackConsume:
                return info.RolePackConsume;
            case KnapsackType.RolePackMaterial:
                return info.RolePackMaterial;
            case KnapsackType.RoleCurrtEquipPack:
                return info.RoleCurrtEquipPack;
            default:
                return info.RolePackAll;
        }
    }

    private int GetMinimumSlotCount()
    {
        int fallbackCount = Mathf.Max(1, _minimumSlotCount);
        if (_content == null || _scrollRect == null || _scrollRect.viewport == null)
        {
            return fallbackCount;
        }

        if (_gridLayout == null)
        {
            return fallbackCount;
        }

        int columnCount = GetColumnCount(_gridLayout);
        float availableHeight = _scrollRect.viewport.rect.height - _gridLayout.padding.top - _gridLayout.padding.bottom;
        float rowHeight = _gridLayout.cellSize.y + _gridLayout.spacing.y;
        int rowCount = rowHeight <= 0f ? 1 : Mathf.Max(1, Mathf.CeilToInt(availableHeight / rowHeight));
        return Mathf.Max(fallbackCount, columnCount * rowCount);
    }

    private int GetVirtualSlotCount(int itemCount)
    {
        int minimumSlotCount = GetMinimumSlotCount();
        if (itemCount <= 0)
        {
            return minimumSlotCount;
        }

        // 最后一行也补成完整网格，避免剩余几格显示成白色空洞。
        int completedRowSlotCount = Mathf.CeilToInt((float)itemCount / _columnCount) * _columnCount;
        return Mathf.Max(minimumSlotCount, completedRowSlotCount);
    }

    private int GetColumnCount(GridLayoutGroup grid)
    {
        if (grid == null)
        {
            return 1;
        }

        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            return Mathf.Max(1, grid.constraintCount);
        }

        float availableWidth = _scrollRect.viewport.rect.width - grid.padding.left - grid.padding.right;
        float columnWidth = grid.cellSize.x + grid.spacing.x;
        return columnWidth <= 0f ? 1 : Mathf.Max(1, Mathf.FloorToInt((availableWidth + grid.spacing.x) / columnWidth));
    }

    private int GetVisibleRowCount()
    {
        if (_gridLayout == null || _scrollRect == null || _scrollRect.viewport == null)
        {
            return 1;
        }

        float rowHeight = _gridLayout.cellSize.y + _gridLayout.spacing.y;
        return rowHeight <= 0f
            ? 1
            : Mathf.Max(1, Mathf.CeilToInt(_scrollRect.viewport.rect.height / rowHeight));
    }

    private KnapsackSlotWidget GetSlot()
    {
        // 优先复用隐藏格子，只有对象池为空时才实例化新对象。
        KnapsackSlotWidget slot = _slotPool.Count > 0
            ? _slotPool.Pop()
            : Instantiate(_slotTemplate, _content);

        slot.transform.SetParent(_content, false);
        slot.transform.SetSiblingIndex(_activeSlots.Count);
        PrepareSlotForVirtualLayout(slot);
        return slot;
    }

    private static void PrepareSlotForVirtualLayout(KnapsackSlotWidget slot)
    {
        if (slot == null)
        {
            return;
        }

        LayoutElement layoutElement = slot.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = slot.gameObject.AddComponent<LayoutElement>();
        }

        // 关闭 GridLayoutGroup 对该格子的自动排版，改为 BindSlot 手动定位。
        layoutElement.ignoreLayout = true;
    }

    private void EnsureVisibleSlotCount()
    {
        // 活跃格子数只覆盖可视行与缓冲行，而非整个背包。
        int maxVisibleSlots = _columnCount * (_visibleRowCount + _bufferRowCount);
        int requiredSlotCount = Mathf.Min(_virtualSlotCount, maxVisibleSlots);

        while (_activeSlots.Count < requiredSlotCount)
        {
            _activeSlots.Add(GetSlot());
        }

        while (_activeSlots.Count > requiredSlotCount)
        {
            int lastIndex = _activeSlots.Count - 1;
            ReleaseSlot(_activeSlots[lastIndex]);
            _activeSlots.RemoveAt(lastIndex);
        }
    }

    private void ReleaseSlot(KnapsackSlotWidget slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.Clear();
        slot.gameObject.SetActive(false);
        _slotPool.Push(slot);
    }

    private void OnScrollChanged(Vector2 _)
    {
        // ScrollRect 发生位移时，只更新那一小组可见 Slot 绑定的数据。
        RefreshVisibleSlots(false);
    }

    private void RefreshVisibleSlots(bool forceRefresh)
    {
        if (_activeSlots.Count == 0 || _gridLayout == null)
        {
            return;
        }

        int firstVisibleRow = GetFirstVisibleRow();
        if (!forceRefresh && firstVisibleRow == _firstVisibleRow)
        {
            return;
        }

        _firstVisibleRow = firstVisibleRow;
        // 将对象池第一个格子映射为当前首行的第一个逻辑格子。
        int firstVirtualIndex = firstVisibleRow * _columnCount;
        IList<RoleItemInfo> itemList = _knapsackInfo == null ? null : GetItemList(_currtType);
        int itemCount = itemList == null ? 0 : itemList.Count;

        for (int poolIndex = 0; poolIndex < _activeSlots.Count; poolIndex++)
        {
            int virtualIndex = firstVirtualIndex + poolIndex;
            BindSlot(_activeSlots[poolIndex], virtualIndex, itemList, itemCount);
        }
    }

    private int GetFirstVisibleRow()
    {
        if (!(_content is RectTransform contentRect) || _gridLayout == null)
        {
            return 0;
        }

        float rowHeight = _gridLayout.cellSize.y + _gridLayout.spacing.y;
        int rowCount = Mathf.CeilToInt((float)_virtualSlotCount / _columnCount);
        int maxFirstRow = Mathf.Max(0, rowCount - _visibleRowCount);
        int firstRow = rowHeight <= 0f
            ? 0
            : Mathf.FloorToInt(Mathf.Max(0f, contentRect.anchoredPosition.y) / rowHeight);
        return Mathf.Clamp(firstRow, 0, maxFirstRow);
    }

    private void BindSlot(KnapsackSlotWidget slot, int virtualIndex, IList<RoleItemInfo> itemList, int itemCount)
    {
        bool hasVirtualSlot = virtualIndex < _virtualSlotCount;
        slot.gameObject.SetActive(hasVirtualSlot);
        if (!hasVirtualSlot)
        {
            slot.Clear();
            return;
        }

        // 使用左上角为原点，让逻辑索引稳定映射为网格的行列位置。
        RectTransform slotRect = slot.transform as RectTransform;
        int column = virtualIndex % _columnCount;
        int row = virtualIndex / _columnCount;
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);
        slotRect.sizeDelta = _gridLayout.cellSize;
        slotRect.anchoredPosition = new Vector2(
            _gridLayout.padding.left + column * (_gridLayout.cellSize.x + _gridLayout.spacing.x),
            -_gridLayout.padding.top - row * (_gridLayout.cellSize.y + _gridLayout.spacing.y));

        if (virtualIndex < itemCount)
        {
            slot.RefreshUI(itemList[virtualIndex]);
        }
        else
        {
            slot.Clear();
        }
    }

    private void UpdateContentHeight(int slotCount)
    {
        if (!(_content is RectTransform contentRect) || _scrollRect == null || _scrollRect.viewport == null)
        {
            return;
        }

        if (_gridLayout == null)
        {
            return;
        }

        int columnCount = GetColumnCount(_gridLayout);
        int rowCount = Mathf.Max(1, Mathf.CeilToInt((float)slotCount / columnCount));
        float requiredHeight = _gridLayout.padding.top + _gridLayout.padding.bottom
            + rowCount * _gridLayout.cellSize.y
            + Mathf.Max(0, rowCount - 1) * _gridLayout.spacing.y;

        // Content 的锚点和横向宽度由预制体固定。虚拟列表只改变高度，避免破坏原有布局。
        contentRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(requiredHeight, _scrollRect.viewport.rect.height));
    }

    private void ResetScrollToTop()
    {
        // Content 尺寸变更后，先让 Canvas 更新，再回到滚动框顶部。
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        _firstVisibleRow = -1;
    }

    private void ResetScrollToBottom()
    {
        // 新增物品后定位到最后一行，让刚添加的格子立即可见。
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        _firstVisibleRow = -1;
    }

}
