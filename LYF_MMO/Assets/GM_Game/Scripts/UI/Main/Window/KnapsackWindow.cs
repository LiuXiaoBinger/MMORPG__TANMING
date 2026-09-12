using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包窗口（虚拟列表实现）
///
/// 【核心设计：为什么用虚拟列表？】
/// 传统做法：背包有 500 个物品，就创建 500 个 Slot 格子。
///   问题：内存占用大、初始化慢、滚动时所有格子都在场景里。
///
/// 虚拟列表做法：只创建"可视区域 + 上下缓冲行"的格子（比如 5 行 x 5 列 = 25 个），
///   500 个物品也只用 25 个格子，滚动时复用这些格子，重新绑定不同物品的数据。
///   Content 的高度按 500 个格子算，这样 ScrollRect 能正确滚动。
///
/// 【关键组件分工】
/// - _content：所有格子的父节点，高度由物品总数决定（撑起滚动范围）
/// - _slotTemplate：格子模板，首次创建和对象池扩容时用
/// - _scrollRect：提供滚动事件和可视区域尺寸
/// - _gridLayout：只读配置（格子尺寸、间距、列数），但实际排版由代码手动计算
/// - _activeSlots：当前可见的格子列表（数量固定，不随物品数增长）
/// - _slotPool：被回收的格子栈，滚动时从这里取出来复用
///
/// 【滚动时发生了什么？】
/// 1. 用户拖动 ScrollRect → OnScrollChanged 触发
/// 2. 计算当前第一行是第几行（GetFirstVisibleRow）
/// 3. 如果行号没变，什么都不做（避免重复刷新）
/// 4. 如果行号变了，把 _activeSlots 里的格子依次绑定到新的物品数据（BindSlot）
/// 5. 格子的位置也按新的行号重新计算（左上角原点 + 行列偏移）
/// </summary>
public class KnapsackWindow : WindowBase
{
    /// <summary>窗口帧循环事件，由背包控制器消费当前页脏标记。</summary>
    public event Action FrameUpdated;
    // ============================================================
    // 一、Inspector 可配置字段
    // ============================================================

    // ScrollRect Content，所有背包格子都挂在该节点下。
    // 这个节点的高度会按物品总数设置，撑起整个滚动范围。
    [SerializeField, Header("背包 Slot 父组件")] private Transform _content;

    // 用于首次创建及对象池扩容的格子模板。
    // 模板本身也会被回收到对象池里复用（见 EnsureInitialized）。
    [SerializeField, Header("背包 Slot 模板")] private KnapsackSlotWidget _slotTemplate;

    // 提供滚动事件（onValueChanged）和可视区域尺寸（viewport.rect）。
    [SerializeField, Header("背包滚动框")] private ScrollRect _scrollRect;

    // 没有服务端容量数据时仍显示的空格子数量下限。
    // 正式逻辑以服务器返回的已开格子数为准，默认至少显示 81 格。
    [SerializeField, Header("空背包保底格子数"), Min(1)] private int _minimumSlotCount = 1;

    // 可视区域外额外保留的行数，减少滚动时的格子复用频率。
    // 比如可视区 5 行，缓冲 1 行，就创建 6 行格子，滚动半行内不需要换数据。
    [SerializeField, Header("虚拟列表缓冲行"), Min(0)] private int _bufferRowCount = 1;

    // 顶部货币显示
    [SerializeField, Header("金币")] private TMP_Text _texGold;
    [SerializeField, Header("灵石")] private TMP_Text _texLs;

    // ============================================================
    // 二、背包分类 Tab 按钮
    // ============================================================
    // 五个分类：普通背包、装备、消耗品、材料、当前装备。
    [Header("背包分类按钮")]
    [SerializeField] private Button _btnAll;
    [SerializeField] private Button _btnEquip;
    [SerializeField] private Button _btnConsume;
    [SerializeField] private Button _btnMaterial;
    [SerializeField] private Button _btnCurrtEquip;

    // 每个分类有"选中态"和"未选中态"两个 GameObject，切换时控制显隐
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

    // ============================================================
    // 三、运行时状态字段
    // ============================================================

    // 虚拟列表当前持有的可见格子，数量只和可视区域有关，不随物品总数增长。
    // 比如 500 个物品，这里可能只有 25 个格子对象。
    private readonly List<KnapsackSlotWidget> _activeSlots = new List<KnapsackSlotWidget>();

    // 格子对象池：切换分类、窗口关闭或可见行减少时，格子会回收并等待复用。
    // 滚动时新进入可视区的格子优先从这里取，避免反复 Instantiate。
    private readonly Stack<KnapsackSlotWidget> _slotPool = new Stack<KnapsackSlotWidget>();

    // 读取格子尺寸、间距、边距和列数配置。
    // 注意：GridLayoutGroup 保持启用（保存配置），但每个格子都 ignoreLayout = true，
    // 所以实际位置由代码手动计算，GridLayoutGroup 不参与排版。
    private GridLayoutGroup _gridLayout;

    // 当前页按格位保存展示数据，局部变化时无需重新生成整页列表。
    private readonly Dictionary<int, KnapsackSlotViewData> _displayItemsBySlot =
        new Dictionary<int, KnapsackSlotViewData>();

    // 当前页已经开启的格子数量，仅用于计算虚拟列表高度。
    private int _openedSlotCount;

    // 当前正在展示的独立背包分类（普通背包/装备/消耗品/材料/当前装备）。
    private KnapsackType _currtType = KnapsackType.RolePackPlain;

    // 用于计算 Content 高度的逻辑格子总数，即服务器确认已经开通的格子数量。
    private int _virtualSlotCount;

    // 当前布局可容纳的列数（由 GridLayoutGroup 配置或视口宽度计算）。
    private int _columnCount;

    // 当前视口内可显示的行数（视口高度 ÷ 每行高度）。
    private int _visibleRowCount;

    // 上次已绑定到对象池格子的首行，避免同一行重复刷新。
    // 滚动时如果首行没变，直接 return，不做任何更新。
    private int _firstVisibleRow = -1;

    // 防止 Awake、OnEnable 与 ReFreshUI 重复注册事件和回收模板。
    private bool _initialized;

    /// <summary>用户切换背包分类时通知背包控制器。</summary>
    public event Action<KnapsackType> CategorySelected;

    /// <summary>返回窗口当前选择的背包分类。</summary>
    public KnapsackType CurrentType
    {
        get { return _currtType; }
    }

    // ============================================================
    // 四、外部接口：接收服务器数据
    // ============================================================

    /// <summary>
    /// 接收控制器生成的当前页展示数据。
    /// </summary>
    /// <param name="items">当前分类的格子展示数据。</param>
    /// <param name="openedSlotCount">当前分类已经开启的格子数量。</param>
    /// <param name="scrollToNewest">新增物品时是否定位到列表底部。</param>
    public void Render(IReadOnlyList<KnapsackSlotViewData> items,
        int openedSlotCount, bool scrollToNewest)
    {
        EnsureInitialized();
        _displayItemsBySlot.Clear();
        if (items != null)
        {
            for (int index = 0; index < items.Count; index++)
            {
                KnapsackSlotViewData item = items[index];
                if (item != null)
                {
                    _displayItemsBySlot[item.SlotIndex] = item;
                }
            }
        }
        _openedSlotCount = Mathf.Max(0, openedSlotCount);
        RefreshCurrentType(scrollToNewest);
    }

    /// <summary>只更新一个逻辑格子的展示数据，保持滚动位置和其他格子不变。</summary>
    public void RefreshSlot(int slotIndex, KnapsackSlotViewData itemData)
    {
        EnsureInitialized();
        KnapsackSlotViewData oldData = null;
        _displayItemsBySlot.TryGetValue(slotIndex, out oldData);
        if (itemData == null)
        {
            _displayItemsBySlot.Remove(slotIndex);
        }
        else
        {
            _displayItemsBySlot[slotIndex] = itemData;
        }

        int firstVirtualIndex = _firstVisibleRow * _columnCount;
        int poolIndex = slotIndex - firstVirtualIndex;
        if (poolIndex < 0 || poolIndex >= _activeSlots.Count)
        {
            return;
        }

        bool countOnly = oldData != null && itemData != null &&
            oldData.ItemUid == itemData.ItemUid &&
            oldData.ItemTypeId == itemData.ItemTypeId &&
            oldData.IconPath == itemData.IconPath;
        BindSlot(_activeSlots[poolIndex], slotIndex, countOnly);
    }

    /// <summary>更新当前分类容量；容量变化才重新计算虚拟列表高度。</summary>
    public void UpdateCapacity(int openedSlotCount)
    {
        EnsureInitialized();
        _openedSlotCount = Mathf.Max(0, openedSlotCount);
        RefreshCurrentType(false);
    }

    /// <summary>兼容 UIBase 的刷新入口，正式背包数据必须由 KnapsackCtrl 推送。</summary>
    public override void ReFreshUI(object obj)
    {
        if (obj != null)
        {
            Debug.LogWarning("背包窗口不再接收协议或业务数据，请通过 KnapsackCtrl 刷新。");
        }
    }

    // ============================================================
    // 五、生命周期
    // ============================================================

    private void Awake()
    {
        // 首次初始化：注册事件、回收模板、计算布局、刷新列表
        EnsureInitialized();
    }

    private void OnEnable()
    {
        // 窗口重新显示时再刷新一次，确保使用当前 ScrollRect 的实际尺寸。
        // （窗口可能在 Awake 时还没完成布局，尺寸不准确）
        if (_initialized)
        {
            RefreshCurrentType();
        }
    }

    private void OnDestroy()
    {
        if (_initialized)
        {
            UnbindTabButtons();
            UnbindScrollRect();
        }
        CategorySelected = null;
        FrameUpdated = null;
    }

    /// <summary>窗口激活期间检查背包脏标记，关闭时不触发控制器刷新。</summary>
    private void Update()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        Action handler = FrameUpdated;
        if (handler != null)
        {
            handler.Invoke();
        }
    }

    // ============================================================
    // 六、初始化
    // ============================================================

    /// <summary>
    /// 一次性初始化：只执行一次，由 _initialized 标记保护。
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        // 读取 GridLayoutGroup 配置（格子尺寸、间距、列数）
        if (_content != null)
        {
            _gridLayout = _content.GetComponent<GridLayoutGroup>();
        }
        else
        {
            _gridLayout = null;
        }

        // GridLayoutGroup 保持启用，供预制体继续保存格子尺寸、间距和列数配置。
        // 每个可复用格子都标记为忽略布局，位置由虚拟列表按当前滚动行计算。
        PrepareSlotForVirtualLayout(_slotTemplate);

        // 注册分类按钮和滚动事件
        BindTabButtons();
        BindScrollRect();
        SetTabVisuals();

        if (_slotTemplate != null)
        {
            // 预制体中的模板也是对象池的第一个可复用对象。
            // 模板默认是显示的，这里回收到池里并隐藏，等需要时再取出来。
            ReleaseSlot(_slotTemplate);
        }

        // 首次刷新列表
        RefreshCurrentType();
    }

    // ============================================================
    // 七、分类 Tab 按钮事件绑定
    // ============================================================

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
            // 滚动位置变化时触发，参数是归一化坐标 (0~1)，这里用不到所以叫 _
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

    // ============================================================
    // 八、分类切换
    // ============================================================

    // 五个按钮的快捷方法，都调用 SelectType 传入对应的独立容器类型。
    // SelectAll 保留旧方法名以兼容预制体事件绑定，实际映射为普通背包页。
    private void SelectAll() => SelectType(KnapsackType.RolePackPlain);
    private void SelectEquip() => SelectType(KnapsackType.RolePackEquip);
    private void SelectConsume() => SelectType(KnapsackType.RolePackConsume);
    private void SelectMaterial() => SelectType(KnapsackType.RolePackMaterial);
    private void SelectCurrtEquip() => SelectType(KnapsackType.RoleCurrtEquipPack);

    /// <summary>
    /// 切换背包分类：更新选中态、重新计算布局、回到顶部、刷新可见格子。
    /// </summary>
    private void SelectType(KnapsackType type)
    {
        // 点当前已选中的分类，不做任何事
        if (_currtType == type)
        {
            return;
        }

        _currtType = type;
        SetTabVisuals();       // 更新按钮选中/未选中图片
        Action<KnapsackType> handler = CategorySelected;
        if (handler != null)
        {
            handler.Invoke(type);
        }
    }

    /// <summary>
    /// 根据当前分类更新所有 Tab 的选中/未选中显示。
    /// </summary>
    private void SetTabVisuals()
    {
        SetTabVisual(_allOff, _allOn, _currtType == KnapsackType.RolePackPlain);
        SetTabVisual(_equipOff, _equipOn, _currtType == KnapsackType.RolePackEquip);
        SetTabVisual(_consumeOff, _consumeOn, _currtType == KnapsackType.RolePackConsume);
        SetTabVisual(_materialOff, _materialOn, _currtType == KnapsackType.RolePackMaterial);
        SetTabVisual(_currtEquipOff, _currtEquipOn, _currtType == KnapsackType.RoleCurrtEquipPack);
    }

    /// <summary>
    /// 单个 Tab 的显隐控制：selected=true 显示 on，隐藏 off；反之亦然。
    /// </summary>
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

    // ============================================================
    // 九、核心：刷新当前分类（虚拟列表主流程）
    // ============================================================

    /// <summary>
    /// 刷新当前分类的完整流程：
    /// 1. 获取当前分类的物品列表
    /// 2. 计算列数、逻辑格子总数、可见行数
    /// 3. 确保可见格子数量足够（不够就创建，多了就回收）
    /// 4. 更新 Content 高度（撑起滚动范围）
    /// 5. 仅在明确要求时滚动到底部，普通刷新保持当前位置
    /// 6. 刷新可见格子的数据绑定
    /// </summary>
    /// <param name="scrollToNewest">true=明确要求定位到底部，false=保持当前位置</param>
    private void RefreshCurrentType(bool scrollToNewest = false)
    {
        if (_slotTemplate == null || _content == null)
        {
            return;
        }

        // 2. 计算布局参数
        _columnCount = GetColumnCount(_gridLayout);           // 列数
        _virtualSlotCount = GetVirtualSlotCount(); // 组件确认的已开格子总数
        _visibleRowCount = GetVisibleRowCount();               // 可视行数

        // 3. 500 件物品只会创建首屏加缓冲行的 Slot，剩余物品由 Content 高度表示。
        EnsureVisibleSlotCount();

        // 4. 更新 Content 高度，让 ScrollRect 知道总共有多少可滚动内容
        UpdateContentHeight(_virtualSlotCount);

        // 5. 滚动定位
        if (scrollToNewest)
        {
            ResetScrollToBottom();  // 有新物品时滚到底部
        }
        // 6. 把可见格子绑定到对应物品数据
        RefreshVisibleSlots(true);

    }

    // ============================================================
    // 十一、布局计算工具方法
    // ============================================================

    /// <summary>
    /// 计算默认最小格子数。默认背包容量为 81 格，避免旧服务器未返回容量时界面缩小。
    /// </summary>
    private int GetMinimumSlotCount()
    {
        return Mathf.Max(81, _minimumSlotCount);
    }

    /// <summary>
    /// 计算逻辑格子总数（用于设置 Content 高度）。
    /// 把物品数向上取整到完整行，比如 12 个物品 5 列 → 15 格（3 整行）。
    /// 这样最后一行不会出现"缺几格显示成白色空洞"的问题。
    /// </summary>
    private int GetVirtualSlotCount()
    {
        int highestOccupiedSlotCount = GetHighestOccupiedSlotCount();
        return Mathf.Max(GetMinimumSlotCount(), _openedSlotCount,
            highestOccupiedSlotCount);
    }

    /// <summary>
    /// 背包位置由 BagIndex 决定，不应依赖服务器列表的排列顺序。
    /// </summary>
    private int GetHighestOccupiedSlotCount()
    {
        int highestSlotCount = 0;
        List<KnapsackSlotViewData> itemList =
            new List<KnapsackSlotViewData>(_displayItemsBySlot.Values);
        for (int index = 0; index < itemList.Count; index++)
        {
            KnapsackSlotViewData item = itemList[index];
            if (item != null)
            {
                highestSlotCount = Mathf.Max(highestSlotCount, item.SlotIndex + 1);
            }
        }

        return highestSlotCount;
    }

    /// <summary>
    /// 计算列数：
    /// - 如果 GridLayoutGroup 设了固定列数，直接用
    /// - 否则按视口宽度动态计算（能放几列放几列）
    /// </summary>
    private int GetColumnCount(GridLayoutGroup grid)
    {
        if (grid == null)
        {
            return 1;
        }

        // 固定列数模式：直接用配置的 constraintCount
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            return Mathf.Max(1, grid.constraintCount);
        }

        // 灵活列数模式：按视口宽度计算
        float availableWidth =
            _scrollRect.viewport.rect.width
            - grid.padding.left - grid.padding.right;
        float columnWidth = grid.cellSize.x + grid.spacing.x;
        if (columnWidth <= 0f)
        {
            return 1;
        }
        return Mathf.Max(1, Mathf.FloorToInt((availableWidth + grid.spacing.x) / columnWidth));
    }

    /// <summary>
    /// 计算视口内能显示的行数（视口高度 ÷ 每行高度，向上取整）。
    /// </summary>
    private int GetVisibleRowCount()
    {
        if (_gridLayout == null || _scrollRect == null || _scrollRect.viewport == null)
        {
            return 1;
        }

        float rowHeight = _gridLayout.cellSize.y + _gridLayout.spacing.y;
        if (rowHeight <= 0f)
        {
            return 1;
        }
        return Mathf.Max(1, Mathf.CeilToInt(
            _scrollRect.viewport.rect.height / rowHeight));
    }

    // ============================================================
    // 十二、格子对象池：获取/回收
    // ============================================================

    /// <summary>
    /// 从对象池取一个格子，池为空时才 Instantiate 新对象。
    /// 取出来后挂到 Content 下，排到活跃列表末尾，并设置为忽略布局。
    /// </summary>
    private KnapsackSlotWidget GetSlot()
    {
        // 优先复用隐藏格子，只有对象池为空时才实例化新对象。
        KnapsackSlotWidget slot;
        if (_slotPool.Count > 0)
        {
            slot = _slotPool.Pop();
        }
        else
        {
            slot = Instantiate(_slotTemplate, _content);
        }

        slot.transform.SetParent(_content, false);
        slot.transform.SetSiblingIndex(_activeSlots.Count);  // 保持层级顺序
        PrepareSlotForVirtualLayout(slot);
        return slot;
    }

    /// <summary>
    /// 给格子添加 LayoutElement 并设置 ignoreLayout = true。
    /// 这样 GridLayoutGroup 不会自动排版这个格子，位置由 BindSlot 手动计算。
    /// </summary>
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

    /// <summary>
    /// 确保活跃格子数量 = min(逻辑格子总数, 可视行+缓冲行)。
    /// - 不够：从对象池取（或新建）
    /// - 多了：回收到对象池
    /// 这是虚拟列表的关键：格子数量永远只和屏幕大小有关，和物品总数无关。
    /// </summary>
    private void EnsureVisibleSlotCount()
    {
        // 活跃格子数只覆盖可视行与缓冲行，而非整个背包。
        int maxVisibleSlots = _columnCount * (_visibleRowCount + _bufferRowCount);
        int requiredSlotCount = Mathf.Min(_virtualSlotCount, maxVisibleSlots);

        // 不够就创建/取出
        while (_activeSlots.Count < requiredSlotCount)
        {
            _activeSlots.Add(GetSlot());
        }

        // 多了就回收
        while (_activeSlots.Count > requiredSlotCount)
        {
            int lastIndex = _activeSlots.Count - 1;
            ReleaseSlot(_activeSlots[lastIndex]);
            _activeSlots.RemoveAt(lastIndex);
        }
    }

    /// <summary>
    /// 回收格子：清空数据、隐藏、压入对象池栈顶。
    /// </summary>
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

    // ============================================================
    // 十三、滚动刷新：虚拟列表的核心
    // ============================================================

    /// <summary>
    /// ScrollRect 滚动时回调。只更新可见 Slot 绑定的数据，不创建新对象。
    /// </summary>
    private void OnScrollChanged(Vector2 _)
    {
        // ScrollRect 发生位移时，只更新那一小组可见 Slot 绑定的数据。
        RefreshVisibleSlots(false);
    }

    /// <summary>
    /// 刷新可见格子的数据绑定。
    /// 核心优化：如果首行号没变，直接 return，不做任何更新。
    /// </summary>
    /// <param name="forceRefresh">true=强制刷新（切换分类/新数据时），false=只在行号变化时刷新</param>
    private void RefreshVisibleSlots(bool forceRefresh)
    {
        if (_activeSlots.Count == 0 || _gridLayout == null)
        {
            return;
        }

        // 计算当前滚动位置对应的第一行是第几行
        int firstVisibleRow = GetFirstVisibleRow();

        // 首行没变 → 可见格子绑定的数据也没变，什么都不做
        if (!forceRefresh && firstVisibleRow == _firstVisibleRow)
        {
            return;
        }

        _firstVisibleRow = firstVisibleRow;

        // 将对象池第一个格子映射为当前首行的第一个逻辑格子。
        // 比如首行是第 3 行、5 列，那么第一个活跃格子对应逻辑索引 15。
        int firstVirtualIndex = firstVisibleRow * _columnCount;

        // 遍历所有活跃格子，依次绑定到 firstVirtualIndex, +1, +2, ...
        for (int poolIndex = 0; poolIndex < _activeSlots.Count; poolIndex++)
        {
            int virtualIndex = firstVirtualIndex + poolIndex;
            BindSlot(_activeSlots[poolIndex], virtualIndex, false);
        }
    }

    /// <summary>
    /// 根据 Content 的当前位置计算第一行是第几行。
    ///
    /// 原理：Content 的 anchoredPosition.y 就是向上滚动了多少像素。
    /// 除以每行高度 = 滚动了多少行，向下取整 = 当前第一行的行号。
    ///
    /// 比如每行 100px，Content 向上滚了 250px → 2.5 行 → 第一行是第 2 行。
    /// </summary>
    private int GetFirstVisibleRow()
    {
        if (!(_content is RectTransform contentRect) || _gridLayout == null)
        {
            return 0;
        }

        float rowHeight = _gridLayout.cellSize.y + _gridLayout.spacing.y;

        // 总行数（用于限制首行最大值，避免滚到空白区域）
        int rowCount = Mathf.CeilToInt((float)_virtualSlotCount / _columnCount);
        int maxFirstRow = Mathf.Max(0, rowCount - _visibleRowCount);

        // 核心公式：滚动距离 ÷ 行高 = 当前首行
        int firstRow = 0;
        if (rowHeight > 0f)
        {
            firstRow = Mathf.FloorToInt(
                Mathf.Max(0f, contentRect.anchoredPosition.y) / rowHeight);
        }

        // 限制在合法范围内
        return Mathf.Clamp(firstRow, 0, maxFirstRow);
    }

    // ============================================================
    // 十四、格子绑定：位置计算 + 数据填充
    // ============================================================

    /// <summary>
    /// 把一个格子绑定到指定逻辑索引：
    /// 1. 计算该索引对应的行列位置
    /// 2. 设置格子的锚点、尺寸、坐标（左上角原点）
    /// 3. 如果索引范围内有物品，刷新物品数据；否则清空显示空格子
    /// </summary>
    /// <param name="slot">要绑定的格子</param>
    /// <param name="virtualIndex">逻辑索引（0 = 第一行第一列）</param>
    /// <param name="itemList">当前分类的物品列表</param>
    private void BindSlot(KnapsackSlotWidget slot, int virtualIndex, bool countOnly)
    {
        // 超出逻辑格子总数的格子（缓冲行多出来的）直接隐藏
        bool hasVirtualSlot = virtualIndex < _virtualSlotCount;
        slot.gameObject.SetActive(hasVirtualSlot);
        if (!hasVirtualSlot)
        {
            slot.Clear();
            return;
        }

        // 使用左上角为原点，让逻辑索引稳定映射为网格的行列位置。
        RectTransform slotRect = slot.transform as RectTransform;

        // 逻辑索引 → 行列：第 7 格、5 列 → 列 2，行 1
        int column = virtualIndex % _columnCount;
        int row = virtualIndex / _columnCount;

        // 锚点和 pivot 都设为左上角 (0,1)，这样坐标计算最直观
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);

        // 格子尺寸用 GridLayoutGroup 配置的 cellSize
        slotRect.sizeDelta = _gridLayout.cellSize;

        // 位置 = 边距 + 列/行 * (格子尺寸 + 间距)
        // 注意 y 是负的，因为 Unity UI 中向下是 y 减小方向
        slotRect.anchoredPosition = new Vector2(
            _gridLayout.padding.left + column * (_gridLayout.cellSize.x + _gridLayout.spacing.x),
            -_gridLayout.padding.top - row * (_gridLayout.cellSize.y + _gridLayout.spacing.y));

        KnapsackSlotViewData item = null;
        _displayItemsBySlot.TryGetValue(virtualIndex, out item);
        if (item != null)
        {
            if (countOnly)
            {
                slot.RefreshCount(item.Count);
            }
            else
            {
                slot.RefreshUI(item);
            }
        }
        else
        {
            slot.Clear();
        }
    }

    // ============================================================
    // 十五、Content 高度与滚动定位
    // ============================================================

    /// <summary>
    /// 根据逻辑格子总数设置 Content 的高度。
    /// 这是虚拟列表能滚动的关键：Content 很高（比如 500 格的高度），
    /// 但实际只有 25 个格子对象挂在上面。
    /// </summary>
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

        // 计算总行数
        int columnCount = GetColumnCount(_gridLayout);
        int rowCount = Mathf.Max(1, Mathf.CeilToInt((float)slotCount / columnCount));

        // 总高度 = 上下边距 + 行数 * 格子高度 + (行数-1) * 行间距
        float requiredHeight = _gridLayout.padding.top + _gridLayout.padding.bottom
            + rowCount * _gridLayout.cellSize.y
            + Mathf.Max(0, rowCount - 1) * _gridLayout.spacing.y;

        // Content 的锚点和横向宽度由预制体固定。虚拟列表只改变高度，避免破坏原有布局。
        // 高度至少为视口高度，避免内容太少时 ScrollRect 无法滚动
        contentRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(requiredHeight, _scrollRect.viewport.rect.height));
    }

    /// <summary>
    /// 切换背包页签时把滚动位置重置到顶部。
    /// verticalNormalizedPosition = 1 表示顶部，0 表示底部。
    /// </summary>
    public void ResetScrollToTop()
    {
        // Content 尺寸变更后，先让 Canvas 更新，再回到滚动框顶部。
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        // 重置首行标记，强制下次刷新
        _firstVisibleRow = -1;
    }

    /// <summary>
    /// 滚动到底部。获得新物品时调用，让新添加的格子立即可见。
    /// </summary>
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
