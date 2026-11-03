using System;
using System.Collections.Generic;

/// <summary>
/// 背包界面控制器，负责将角色物品组件转换成窗口展示数据。
/// </summary>
public sealed class KnapsackCtrl : IDisposable
{

    #region 字段
    /// <summary>背包窗口，只负责渲染和派发页签事件。</summary>
    private readonly KnapsackWindow _window;

    /// <summary>当前绑定的角色物品组件。</summary>
    private RoItemComponent _itemComponent;

    /// <summary>当前绑定角色，用来查询哪些背包格子或分类需要刷新。</summary>
    private PlayerEntity _role;

    /// <summary>上次渲染的物品条目数；当前只保存或重置，没有用它决定滚动位置。</summary>
    private int _lastRenderedItemCount = -1;

    /// <summary>当前页按格位缓存的展示数据，增量事件只改动对应条目。</summary>
    private readonly Dictionary<int, KnapsackSlotViewData> _slotDataByIndex =
        new Dictionary<int, KnapsackSlotViewData>();

    /// <summary>当前页按物品 UID 索引格位，移动和删除时可快速定位旧格子。</summary>
    private readonly Dictionary<long, int> _slotIndexByUid =
        new Dictionary<long, int>();
    #endregion

    #region 构造与初始化
    /// <summary>创建背包控制器并绑定当前本地角色。</summary>
    /// <param name="mainView">提供背包窗口引用的主界面。</param>
    public KnapsackCtrl(MainView mainView)
    {
        if (mainView != null)
        {
            _window = mainView.KnapsackWindow;
        }
        Initialize();
    }

    /// <summary>为专用测试组件创建背包控制器。</summary>
    /// <param name="window">测试专用窗口实例。</param>
    public KnapsackCtrl(KnapsackWindow window)
    {
        _window = window;
        Initialize();
    }

    /// <summary>背包窗口每帧调用：可见时读取当前分类的变化标记，刷新后清除本次标记。</summary>
    /// <param name="deltaMilliseconds">保留的帧间隔参数，当前不参与刷新判断。</param>
    public void Update(int deltaMilliseconds)
    {
        if (_window == null || !_window.isActiveAndEnabled)
        {
            return;
        }
        if (_role == null || _role.DirtyTracker == null)
        {
            return;
        }
        // 当前分类积累的整页刷新标记或格子下标，成功绘制后由角色清除。
        ClientDirtySnapshot snapshot;
        if (!_role.DirtyTracker.TryGetKnapsack(_window.CurrentType, out snapshot))
        {
            return;
        }

        if (snapshot.FullRefresh)
        {
            RefreshCurrentCategory(false);
        }
        else
        {
            RefreshDirtySlots(snapshot);
        }
        _role.DirtyTracker.ClearKnapsack(_window.CurrentType);
    }

    /// <summary>注册窗口事件并绑定当前本地角色。</summary>
    private void Initialize()
    {
        if (_window != null)
        {
            _window.CategorySelected += OnCategorySelected;
            _window.FrameUpdated += OnWindowFrameUpdated;
        }

        PlayerEntity localRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        BindRole(localRole);
    }

    /// <summary>解除窗口事件，清空角色引用和格子缓存；当前没有订阅角色组件事件。</summary>
    public void Dispose()
    {
        if (_window != null)
        {
            _window.CategorySelected -= OnCategorySelected;
            _window.FrameUpdated -= OnWindowFrameUpdated;
        }
        _itemComponent = null;
        _role = null;
        _slotDataByIndex.Clear();
        _slotIndexByUid.Clear();
    }
    #endregion

    #region 界面刷新、交互响应等
    /// <summary>先重建当前分类的物品列表，再把标记中的格子交给窗口刷新，最后更新容量和布局。</summary>
    /// <param name="snapshot">这一帧待处理的背包变化摘要。</param>
    private void RefreshDirtySlots(ClientDirtySnapshot snapshot)
    {
        // 当前分类的最新完整列表，用它查询受影响格子的现状。
        List<KnapsackSlotViewData> items = BuildItems(_window.CurrentType);
        // 按格子下标查最新数据；查不到表示这个格子已经为空。
        Dictionary<int, KnapsackSlotViewData> latest =
            new Dictionary<int, KnapsackSlotViewData>();
        for (int index = 0; index < items.Count; index++)
        {
            latest[items[index].SlotIndex] = items[index];
        }
        for (int index = 0; index < snapshot.DirtySlots.Count; index++)
        {
            int slotIndex = snapshot.DirtySlots[index];
            KnapsackSlotViewData data;
            latest.TryGetValue(slotIndex, out data);
            if (data == null)
            {
                RemoveSlot(slotIndex);
            }
            else
            {
                SetSlot(data);
            }
            _window.RefreshSlot(slotIndex, data);
        }
        _window.UpdateCapacity(GetOpenedSlotCount(_window.CurrentType));
    }

    /// <summary>生成当前页展示数据并交给窗口渲染。</summary>
    /// <param name="detectNewItem">保留参数，当前未读取；刷新始终保留现有滚动位置。</param>
    private void RefreshCurrentCategory(bool detectNewItem)
    {
        if (_window == null)
        {
            return;
        }

        KnapsackType type = _window.CurrentType;
        List<KnapsackSlotViewData> items = BuildItems(type);
        int openedSlotCount = GetOpenedSlotCount(type);
        _lastRenderedItemCount = items.Count;
        _slotDataByIndex.Clear();
        _slotIndexByUid.Clear();
        for (int index = 0; index < items.Count; index++)
        {
            SetSlot(items[index]);
        }
        // 普通增删改和扩容刷新保持当前滚动位置，不因数据重建跳转。
        _window.Render(items, openedSlotCount, false);
    }

    /// <summary>窗口切换页签后从组件重新生成对应页面。</summary>
    /// <param name="type">窗口报告的新分类；实际绘制读取窗口已保存的 CurrentType。</param>
    private void OnCategorySelected(KnapsackType type)
    {
        _lastRenderedItemCount = -1;
        if (_window != null)
        {
            _window.ResetScrollToTop();
        }
        RefreshCurrentCategory(false);
    }

    /// <summary>接收窗口帧事件并消费当前页脏标记。</summary>
    private void OnWindowFrameUpdated()
    {
        Update(0);
    }
    #endregion

    #region 创建与添加
    /// <summary>构造不可变展示数据，控制器不把 ItemBase 传入窗口。</summary>
    /// <param name="change">旧增量事件中的物品信息；缺图标时从物品配置补齐。</param>
    private static KnapsackSlotViewData CreateViewData(KnapsackChangedEventArgs change)
    {
        string iconPath = change.IconPath;
        if (string.IsNullOrEmpty(iconPath))
        {
            ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(change.ItemTypeId);
            if (config != null)
            {
                iconPath = config.Icon;
            }
        }
        return new KnapsackSlotViewData(change.ItemUid, change.ItemTypeId,
            change.Count, change.BagIndex, iconPath);
    }

    /// <summary>读取指定独立容器中的物品，并按实例编号去重后生成展示数据。</summary>
    /// <param name="result">接收整理后展示条目的列表。</param>
    /// <param name="itemUids">本次已加入的实例编号，避免重复展示同一物品。</param>
    /// <param name="type">本次读取的独立背包分类。</param>
    private void AppendContainer(List<KnapsackSlotViewData> result,
        HashSet<long> itemUids, KnapsackType type)
    {
        RoItemContainer container = _itemComponent.GetContainer(type);
        if (container == null || container.GetItemMap() == null)
        {
            return;
        }

        List<ItemBase> items = new List<ItemBase>(container.GetItemMap().Values);
        items.Sort(CompareItem);
        for (int index = 0; index < items.Count; index++)
        {
            ItemBase item = items[index];
            if (item == null || item.GetItemUID() <= 0 ||
                !itemUids.Add(item.GetItemUID()))
            {
                continue;
            }

            string iconPath = string.Empty;
            ItemConfigBase config = LubanMgr.Instance.GetItemConfigById(item.GetItemID());
            if (config != null)
            {
                iconPath = config.Icon;
            }
            result.Add(new KnapsackSlotViewData(item.GetItemUID(), item.GetItemID(),
                item.GetItemCount(), item.GetBagIndex(), iconPath));
        }
    }
    #endregion

    /// <summary>从当前页缓存删除指定格位。</summary>
    /// <param name="slotIndex">需要从当前分类缓存中移除的格子下标。</param>
    private void RemoveSlot(int slotIndex)
    {
        if (_slotDataByIndex.TryGetValue(slotIndex, out KnapsackSlotViewData data))
        {
            _slotIndexByUid.Remove(data.ItemUid);
        }
        _slotDataByIndex.Remove(slotIndex);
    }

    #region 查询与判定
    /// <summary>读取当前分类已经开启的格子数量。</summary>
    /// <param name="type">需要查询容量的背包分类。</param>
    private int GetOpenedSlotCount(KnapsackType type)
    {
        if (_itemComponent == null)
        {
            return 0;
        }
        return _itemComponent.GetOpenedGridCount(type);
    }

    /// <summary>按背包格子位置和实例编号稳定排序展示数据。</summary>
    /// <param name="left">参与排序的前一个展示条目。</param>
    /// <param name="right">参与排序的后一个展示条目。</param>
    private static int CompareSlot(KnapsackSlotViewData left,
        KnapsackSlotViewData right)
    {
        int slotResult = left.SlotIndex.CompareTo(right.SlotIndex);
        if (slotResult != 0)
        {
            return slotResult;
        }
        return left.ItemUid.CompareTo(right.ItemUid);
    }

    /// <summary>按容器格子位置和实例编号稳定排序物品。</summary>
    /// <param name="left">参与排序的前一个物品，可为空。</param>
    /// <param name="right">参与排序的后一个物品，可为空。</param>
    private static int CompareItem(ItemBase left, ItemBase right)
    {
        if (left == null)
        {
            return 1;
        }
        if (right == null)
        {
            return -1;
        }
        int slotResult = left.GetBagIndex().CompareTo(right.GetBagIndex());
        if (slotResult != 0)
        {
            return slotResult;
        }
        return left.GetItemUID().CompareTo(right.GetItemUID());
    }
    #endregion

    #region 核心逻辑
    /// <summary>换绑本地角色和物品组件，清空旧缓存、标记当前分类需要刷新，并立即绘制一次。</summary>
    /// <param name="role">新的本地角色，null 表示解除绑定并清空显示。</param>
    public void BindRole(PlayerEntity role)
    {
        if (_itemComponent != null)
        {
            // 背包数据变化不再直接驱动窗口刷新，改由帧循环消费角色脏标记。
        }

        _role = role;
        _itemComponent = null;
        _lastRenderedItemCount = -1;
        _slotDataByIndex.Clear();
        _slotIndexByUid.Clear();
        if (role != null)
        {
            role.TryGetComponent(out _itemComponent);
        }
        if (_role != null && _role.DirtyTracker != null)
        {
            KnapsackType type = KnapsackType.RolePackPlain;
            if (_window != null)
            {
                type = _window.CurrentType;
            }
            _role.DirtyTracker.MarkKnapsack(type, true, -1,
                new ClientChangeContext("KnapsackBind"));
        }
        RefreshCurrentCategory(false);
    }

    /// <summary>保留的旧事件增量处理方法，当前未被调用；按增删移事件修改对应格子，不重建列表。</summary>
    /// <param name="change">旧事件描述的物品新增、删除、数量变化或移动。</param>
    private void ApplyIncrementalChange(KnapsackChangedEventArgs change)
    {
        if (_window == null)
        {
            return;
        }

        if (change.ChangeType == KnapsackChangeType.Moved)
        {
            bool oldPage = change.PreviousBagType == _window.CurrentType;
            bool newPage = change.BagType == _window.CurrentType;
            if (oldPage && newPage && change.PreviousBagIndex == change.BagIndex)
            {
                return;
            }
            if (oldPage && change.PreviousBagIndex != change.BagIndex)
            {
                RemoveSlot(change.PreviousBagIndex);
                _window.RefreshSlot(change.PreviousBagIndex, null);
            }
            if (newPage && change.Count > 0)
            {
                KnapsackSlotViewData movedData = CreateViewData(change);
                SetSlot(movedData);
                _window.RefreshSlot(change.BagIndex, movedData);
            }
            return;
        }

        if (change.BagType != _window.CurrentType)
        {
            return;
        }

        if (change.ChangeType == KnapsackChangeType.Removed || change.Count <= 0)
        {
            int oldIndex = change.BagIndex;
            if (_slotIndexByUid.TryGetValue(change.ItemUid, out int cachedIndex))
            {
                oldIndex = cachedIndex;
            }
            RemoveSlot(oldIndex);
            _window.RefreshSlot(oldIndex, null);
            return;
        }

        KnapsackSlotViewData data = CreateViewData(change);
        SetSlot(data);
        _window.RefreshSlot(change.BagIndex, data);
    }

    /// <summary>将展示数据写入当前页的格位和 UID 索引。</summary>
    /// <param name="data">将要占用指定格子的物品展示数据，替换旧 UID 与格子对应关系。</param>
    private void SetSlot(KnapsackSlotViewData data)
    {
        if (data == null)
        {
            return;
        }
        if (_slotIndexByUid.TryGetValue(data.ItemUid, out int oldIndex) && oldIndex != data.SlotIndex)
        {
            _slotDataByIndex.Remove(oldIndex);
        }
        KnapsackSlotViewData occupiedData;
        if (_slotDataByIndex.TryGetValue(data.SlotIndex, out occupiedData) &&
            occupiedData.ItemUid != data.ItemUid)
        {
            _slotIndexByUid.Remove(occupiedData.ItemUid);
        }
        _slotDataByIndex[data.SlotIndex] = data;
        _slotIndexByUid[data.ItemUid] = data.SlotIndex;
    }

    /// <summary>从角色物品容器构建当前分类的展示列表。</summary>
    /// <param name="type">本次要显示的独立背包分类。</param>
    private List<KnapsackSlotViewData> BuildItems(KnapsackType type)
    {
        List<KnapsackSlotViewData> result = new List<KnapsackSlotViewData>();
        if (_itemComponent == null)
        {
            return result;
        }

        // 本次列表已经收录的物品实例编号，只用于去重。
        HashSet<long> itemUids = new HashSet<long>();
        // 每个页签只对应一个独立容器，普通背包不再聚合装备、消耗品和材料。
        AppendContainer(result, itemUids, type);

        result.Sort(CompareSlot);
        return result;
    }
    #endregion
}
