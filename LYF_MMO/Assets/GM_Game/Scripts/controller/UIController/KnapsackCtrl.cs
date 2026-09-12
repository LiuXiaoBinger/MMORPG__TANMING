using System;
using System.Collections.Generic;

/// <summary>
/// 背包界面控制器，负责将角色物品组件转换成窗口展示数据。
/// </summary>
public sealed class KnapsackCtrl : IDisposable
{
    /// <summary>背包窗口，只负责渲染和派发页签事件。</summary>
    private readonly KnapsackWindow _window;

    /// <summary>当前绑定的角色物品组件。</summary>
    private RoItemComponent _itemComponent;
    /// <summary>当前绑定角色，用于读取角色级脏标记。</summary>
    private ClientRole _role;

    /// <summary>上次渲染的物品数量，用于判断是否需要定位到新物品。</summary>
    private int _lastRenderedItemCount = -1;
    /// <summary>当前页按格位缓存的展示数据，增量事件只改动对应条目。</summary>
    private readonly Dictionary<int, KnapsackSlotViewData> _slotDataByIndex =
        new Dictionary<int, KnapsackSlotViewData>();
    /// <summary>当前页按物品 UID 索引格位，移动和删除时可快速定位旧格子。</summary>
    private readonly Dictionary<long, int> _slotIndexByUid =
        new Dictionary<long, int>();

    /// <summary>创建背包控制器并绑定当前本地角色。</summary>
    public KnapsackCtrl(MainView mainView)
    {
        if (mainView != null)
        {
            _window = mainView.KnapsackWindow;
        }
        Initialize();
    }

    /// <summary>为专用测试组件创建背包控制器。</summary>
    public KnapsackCtrl(KnapsackWindow window)
    {
        _window = window;
        Initialize();
    }

    /// <summary>注册窗口事件并绑定当前本地角色。</summary>
    private void Initialize()
    {
        if (_window != null)
        {
            _window.CategorySelected += OnCategorySelected;
            _window.FrameUpdated += OnWindowFrameUpdated;
        }

        ClientRole localRole = null;
        if (Global.Instance != null && Global.Instance.RoleWorld != null)
        {
            localRole = Global.Instance.RoleWorld.LocalRole;
        }
        BindRole(localRole);
    }

    /// <summary>绑定本地角色，并监听其背包模型变化。</summary>
    public void BindRole(ClientRole role)
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

    /// <summary>接收窗口帧事件并消费当前页脏标记。</summary>
    private void OnWindowFrameUpdated()
    {
        Update(0);
    }

    /// <summary>背包窗口帧循环调用，按当前页消费脏标记。</summary>
    public void Update(int deltaMilliseconds)
    {
        if (_window == null || !_window.isActiveAndEnabled || _role == null ||
            _role.DirtyTracker == null)
        {
            return;
        }
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

    /// <summary>只刷新脏摘要中的格位，避免打开页面前产生 UI 工作。</summary>
    private void RefreshDirtySlots(ClientDirtySnapshot snapshot)
    {
        List<KnapsackSlotViewData> items = BuildItems(_window.CurrentType);
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

    /// <summary>只修改当前页受影响的 UID 和格位，不重新生成整页列表。</summary>
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

    /// <summary>构造不可变展示数据，控制器不把 ItemBase 传入窗口。</summary>
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

    /// <summary>将展示数据写入当前页的格位和 UID 索引。</summary>
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

    /// <summary>从当前页缓存删除指定格位。</summary>
    private void RemoveSlot(int slotIndex)
    {
        if (_slotDataByIndex.TryGetValue(slotIndex, out KnapsackSlotViewData data))
        {
            _slotIndexByUid.Remove(data.ItemUid);
        }
        _slotDataByIndex.Remove(slotIndex);
    }

    /// <summary>窗口切换页签后从组件重新生成对应页面。</summary>
    private void OnCategorySelected(KnapsackType type)
    {
        _lastRenderedItemCount = -1;
        if (_window != null)
        {
            _window.ResetScrollToTop();
        }
        RefreshCurrentCategory(false);
    }

    /// <summary>生成当前页展示数据并交给窗口渲染。</summary>
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

    /// <summary>从角色物品容器构建当前分类的展示列表。</summary>
    private List<KnapsackSlotViewData> BuildItems(KnapsackType type)
    {
        List<KnapsackSlotViewData> result = new List<KnapsackSlotViewData>();
        if (_itemComponent == null)
        {
            return result;
        }

        HashSet<long> itemUids = new HashSet<long>();
        // 每个页签只对应一个独立容器，普通背包不再聚合装备、消耗品和材料。
        AppendContainer(result, itemUids, type);

        result.Sort(CompareSlot);
        return result;
    }

    /// <summary>读取指定独立容器中的物品，并按实例编号去重后生成展示数据。</summary>
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

    /// <summary>读取当前分类已经开启的格子数量。</summary>
    private int GetOpenedSlotCount(KnapsackType type)
    {
        if (_itemComponent == null)
        {
            return 0;
        }
        return _itemComponent.GetOpenedGridCount(type);
    }

    /// <summary>按背包格子位置和实例编号稳定排序展示数据。</summary>
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

    /// <summary>解除窗口和角色组件事件订阅。</summary>
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
}
