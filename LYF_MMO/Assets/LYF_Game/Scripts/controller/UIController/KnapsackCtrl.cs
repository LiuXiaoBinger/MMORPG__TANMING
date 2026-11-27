using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 背包界面控制器，负责将角色物品组件转换成窗口展示数据。
/// </summary>
public sealed class KnapsackCtrl : IDisposable
{

    #region 字段
    /// <summary>装备表品质值对应的详情标签，索引 0 供未标注品质的既有装备使用。</summary>
    private static readonly string[] EquipmentQualityNames = { "—", "白色", "绿色", "蓝色", "紫色", "金色" };

    /// <summary>计入套装的六个穿戴部位，顺序与详情展开区一致。</summary>
    private static readonly EquipType[] SetParts =
    {
        EquipType.Head, EquipType.Chest, EquipType.WaistWaist,
        EquipType.Pants, EquipType.Shoes, EquipType.Bracer
    };

    /// <summary>背包窗口，只负责渲染和派发页签事件。</summary>
    private readonly KnapsackWindow _window;
    /// <summary>主界面两个可点击的药品位，背包关闭时也接收库存变化。</summary>
    private readonly PotionBarWidget _potionBar;
    /// <summary>本机当前角色装备的生命药配置编号，零表示还未装备。</summary>
    private int _healthPotionId;
    /// <summary>本机当前角色装备的法力药配置编号，零表示还未装备。</summary>
    private int _manaPotionId;

    // 四格货币对应的虚拟物品配置编号，与服务器 ItemEnum 现有取值一致：
    // 铜币 101、银子 102、元宝 103、银票 106。这里只用于读取余额，不改任何消费映射。
    /// <summary>铜币的虚拟物品配置编号。</summary>
    private const int CopperItemId = 101;
    /// <summary>银子（旧 zeny 币）的虚拟物品配置编号。</summary>
    private const int SilverItemId = 102;
    /// <summary>银票的虚拟物品配置编号。</summary>
    private const int TicketItemId = 106;
    /// <summary>元宝（旧钻石/金币）的虚拟物品配置编号。</summary>
    private const int YuanbaoItemId = 103;

    /// <summary>持有并管理装备详情生命周期的现有主视图。</summary>
    private readonly MainView _mainView;
    /// <summary>显示当前装备详情的正式子窗口。</summary>
    private readonly EquipmentDetailWindow _detailWindow;
    /// <summary>展示当前选中装备及五个养成分页的主城子窗口。</summary>
    private readonly EquipmentWorkshopWindow _workshopWindow;
    /// <summary>显示辉银金装合成与实例成长的作坊。</summary>
    private readonly GoldWorkshopWindow _goldWorkshop;
    /// <summary>珍宝阁商会与工坊共享铜钱余额、宝石购买命令和配额回执。</summary>
    private readonly TreasurePavilionWindow _pavilionWindow;
    /// <summary>已穿戴装备槽的点击来源。</summary>
    private readonly RoleAttriibuteWindow _equipmentWindow;
    /// <summary>当前详情所选实例的 UID，切换角色时清空。</summary>
    private long _selectedEquipmentUid;

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
            _potionBar = mainView.GetComponentInChildren<PotionBarWidget>(true);
            _mainView = mainView;
            _detailWindow = mainView.EquipmentDetailWindow;
            _workshopWindow = mainView.EquipmentWorkshopWindow;
            _goldWorkshop = mainView.GoldWorkshopWindow;
            _pavilionWindow = mainView.TreasurePavilionWindow;
            _equipmentWindow = mainView.RoleAttributeWindow;
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
        // 虚拟币不属于任何分类页，先消费它的脏标记再处理当前页。
        ClientDirtySnapshot moneySnapshot;
        if (_role.DirtyTracker.TryGetKnapsack(KnapsackType.RoleVirtualItemPack, out moneySnapshot))
        {
            RefreshMoney();
            _role.DirtyTracker.ClearKnapsack(KnapsackType.RoleVirtualItemPack);
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
        if (_potionBar != null) _potionBar.UseRequested += UseEquippedPotion;
        if (_window != null)
        {
            _window.CategorySelected += OnCategorySelected;
            _window.FrameUpdated += OnWindowFrameUpdated;
            _window.ItemSelected += OpenBagItem;
            _window.MoneyExchangeRequested += OnMoneyExchangeRequested;
        }
        if (_equipmentWindow != null)
        {
            _equipmentWindow.EquipmentSelected += OpenEquipmentDetail;
        }
        if (_detailWindow != null)
        {
            _detailWindow.WearRequested += WearSelectedEquipment;
            _detailWindow.TakeOffRequested += TakeOffSelectedEquipment;
            _detailWindow.SpecialSkillRequested += SwitchSelectedSpecialSkill;
            _detailWindow.StrengthenRequested += OpenEquipmentWorkshop;
            _detailWindow.WashRequested += OpenWashWorkshop;
            _detailWindow.AdvanceRequested += OpenGoldAdvance;
            _detailWindow.GrowthRequested += SubmitDetailGrowth;
        }
        if (_workshopWindow != null)
        {
            _workshopWindow.SourceSelected += ChangeWorkshopSource;
            _workshopWindow.EquipmentSelected += RenderWorkshopEquipment;
            _workshopWindow.GoldUpgradeRequested += OpenGoldUpgrade;
            _workshopWindow.StrengthRequested += StrengthenEquipment;
            _workshopWindow.StockRequested += RenderWorkshopStock;
            _workshopWindow.SocketRequested += SocketWorkshopGem;
            _workshopWindow.Owned = OwnedGem;
            _workshopWindow.SendTrade = SendGemTrade;
        }
        if (_goldWorkshop != null)
        {
            _mainView.GoldWorkshopButton.onClick.AddListener(OpenGoldCatalog);
            _goldWorkshop.Owned = GoldStock;
            _goldWorkshop.Equipment = GoldEquipment;
            _goldWorkshop.Submit = SendGemTrade;
        }
        if (_pavilionWindow != null) _pavilionWindow.SendGemTrade = SendGemTrade;

        PlayerEntity localRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        BindRole(localRole);
    }

    /// <summary>解除窗口和材料库存事件，清空角色引用与格子缓存。</summary>
    public void Dispose()
    {
        if (_potionBar != null) _potionBar.UseRequested -= UseEquippedPotion;
        if (_itemComponent != null) _itemComponent.KnapsackChangedDetailed -= OnPotionStockChanged;
        if (_window != null)
        {
            _window.CategorySelected -= OnCategorySelected;
            _window.FrameUpdated -= OnWindowFrameUpdated;
            _window.ItemSelected -= OpenBagItem;
            _window.MoneyExchangeRequested -= OnMoneyExchangeRequested;
        }
        if (_equipmentWindow != null)
        {
            _equipmentWindow.EquipmentSelected -= OpenEquipmentDetail;
        }
        if (_detailWindow != null)
        {
            _detailWindow.WearRequested -= WearSelectedEquipment;
            _detailWindow.TakeOffRequested -= TakeOffSelectedEquipment;
            _detailWindow.SpecialSkillRequested -= SwitchSelectedSpecialSkill;
            _detailWindow.StrengthenRequested -= OpenEquipmentWorkshop;
            _detailWindow.WashRequested -= OpenWashWorkshop;
            _detailWindow.AdvanceRequested -= OpenGoldAdvance;
            _detailWindow.GrowthRequested -= SubmitDetailGrowth;
        }
        if (_workshopWindow != null)
        {
            _workshopWindow.SourceSelected -= ChangeWorkshopSource;
            _workshopWindow.EquipmentSelected -= RenderWorkshopEquipment;
            _workshopWindow.GoldUpgradeRequested -= OpenGoldUpgrade;
            _workshopWindow.StrengthRequested -= StrengthenEquipment;
            _workshopWindow.StockRequested -= RenderWorkshopStock;
            _workshopWindow.SocketRequested -= SocketWorkshopGem;
            _workshopWindow.Owned = null;
            _workshopWindow.SendTrade = null;
        }
        if (_goldWorkshop != null)
        {
            _mainView.GoldWorkshopButton.onClick.RemoveListener(OpenGoldCatalog);
            _goldWorkshop.Owned = null;
            _goldWorkshop.Equipment = null;
            _goldWorkshop.Submit = null;
        }
        if (_pavilionWindow != null) _pavilionWindow.SendGemTrade = null;
        if (_itemComponent != null) _itemComponent.KnapsackChangedDetailed -= OnWorkshopStockChanged;
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
        if (_role != role)
        {
            _selectedEquipmentUid = 0;
            if (_window != null) _window.CloseManualItem();
            if (_detailWindow != null) _detailWindow.CloseWindow();
            if (_workshopWindow != null) _workshopWindow.CloseWindow();
            if (_goldWorkshop != null) _goldWorkshop.CloseWindow();
        }
        if (_itemComponent != null)
        {
            _itemComponent.KnapsackChangedDetailed -= OnWorkshopStockChanged;
            _itemComponent.KnapsackChangedDetailed -= OnPotionStockChanged;
        }

        _role = role;
        _healthPotionId = 0;
        _manaPotionId = 0;
        _itemComponent = null;
        _lastRenderedItemCount = -1;
        _slotDataByIndex.Clear();
        _slotIndexByUid.Clear();
        if (role != null)
        {
            role.TryGetComponent(out _itemComponent);
            if (_potionBar != null && _itemComponent != null)
            {
                _healthPotionId = PlayerPrefs.GetInt(PotionPreference(false));
                _manaPotionId = PlayerPrefs.GetInt(PotionPreference(true));
                _itemComponent.KnapsackChangedDetailed += OnPotionStockChanged;
            }
            // 只有持有工坊或商会窗口的控制器订阅库存变动。
            bool hasWorkshop = _workshopWindow != null || _goldWorkshop != null || _pavilionWindow != null;
            if (_itemComponent != null && hasWorkshop)
                _itemComponent.KnapsackChangedDetailed += OnWorkshopStockChanged;
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
        RefreshMoney();
        RenderPotionSlot(false);
        RenderPotionSlot(true);
    }

    /// <summary>读取虚拟物品背包中的四种货币余额交给窗口，未绑定角色时四格清零。</summary>
    public void RefreshMoney()
    {
        if (_window == null)
        {
            return;
        }
        if (_itemComponent == null)
        {
            _window.RenderMoney(0L, 0L, 0L, 0L);
            return;
        }
        _window.RenderMoney(_itemComponent.GetVirtualItemCount(CopperItemId),
            _itemComponent.GetVirtualItemCount(SilverItemId),
            _itemComponent.GetVirtualItemCount(TicketItemId),
            _itemComponent.GetVirtualItemCount(YuanbaoItemId));
    }

    /// <summary>读取四种货币余额，供兑换窗口渲染；顺序为铜币、银子、银票、元宝。</summary>
    public void ReadMoney(out long copper, out long silver, out long ticket, out long yuanbao)
    {
        copper = 0L;
        silver = 0L;
        ticket = 0L;
        yuanbao = 0L;
        if (_itemComponent == null)
        {
            return;
        }
        copper = _itemComponent.GetVirtualItemCount(CopperItemId);
        silver = _itemComponent.GetVirtualItemCount(SilverItemId);
        ticket = _itemComponent.GetVirtualItemCount(TicketItemId);
        yuanbao = _itemComponent.GetVirtualItemCount(YuanbaoItemId);
    }

    /// <summary>原子完成一次货币兑换：先扣支付货币，再发产出货币；发放失败时回滚。</summary>
    /// <returns>扣减与发放都成功返回 true，随后会刷新背包货币栏。</returns>
    public bool ApplyCurrencyExchange(int fromItemId, long fromCount, int toItemId, long toCount)
    {
        if (_itemComponent == null || fromCount <= 0L || toCount <= 0L)
        {
            return false;
        }
        // 元宝是充值货币，任何兑换都不允许把它作为产出。
        if (toItemId == YuanbaoItemId || toCount > int.MaxValue)
        {
            return false;
        }
        if (!_itemComponent.TakeVirtualItem(fromItemId, fromCount))
        {
            return false;
        }
        if (!_itemComponent.GiveVirtualItem(toItemId, toCount))
        {
            _itemComponent.GiveVirtualItem(fromItemId, fromCount);
            return false;
        }
        RefreshMoney();
        return true;
    }

    /// <summary>货币栏加号入口：打开兑换窗口并选中对应页签；窗口已打开时只切页不关闭。</summary>
    private void OnMoneyExchangeRequested(int page)
    {
        if (_mainView == null)
        {
            return;
        }
        CurrencyExchangeWindow window = _mainView.CurrencyExchangeWindow;
        if (window != null)
        {
            // 与角标上的持久监听走同一个幂等入口：窗口已开时只切页，不会再被 toggle 关掉。
            window.Open(page);
        }
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

    /// <summary>按 UID 提交服务器装备操作；成功仅表示请求已发送，模型等待物品增量。</summary>
    /// <param name="itemUid">装备实例 UID，不能使用配置 ID 代替。</param>
    /// <param name="operation">穿戴、卸下或切换特技的操作类型。</param>
    /// <param name="specialSkillIndex">切换操作选择的保存槽0至2，其他操作忽略。</param>
    public static bool SendEquipmentOperation(long itemUid, EquipOperation operation, int specialSkillIndex = 0)
    {
        // 只发送本次明确选择，不在客户端移动物品或预算属性。
        EquipOperationReq request = new EquipOperationReq
        {
            ItemUid = itemUid,
            Operation = operation,
            SpecialSkillIndex = specialSkillIndex
        };
        return MNetClient.Instance.Send(NetDefine.CMD_EquipOperationCode, request.ToByteString());
    }

    /// <summary>装备读取实例属性，其余格子按物品表的类别和按钮顺序打开背包详情。</summary>
    /// <param name="itemUid">被点击堆叠的实例编号。</param>
    private void OpenBagItem(long itemUid)
    {
        // 绑定、限时及实际数量属于实例，同名的两叠物品可以有不同的操作。
        ItemBase stack = _itemComponent?.GetItemByUID(itemUid);
        if (stack == null) return;
        if (stack is EquipBase)
        {
            _window.CloseManualItem();
            OpenEquipmentDetail(itemUid);
            return;
        }
        // 介绍、来源和动作顺序使用同一物品行，装备仍沿用独立属性表。
        cfg.ItemInfo item = LubanMgr.Instance.GetItemInfoById(stack.GetItemID());
        if (item == null) return;
        // 总数含该角色不同格位持有的绑定和未绑定物品。
        int held = _itemComponent.GetItemNum(item.ItemTypeId, ItemBind.kItemBindIs, true);
        // 碎片沿用原有的合成进度展示；整本及其他物品显示持有量。
        string owned = "持有数量  " + held;
        // 详情按钮按表中顺序在卡片右侧排列。
        List<KeyValuePair<string, Action>> actions = new List<KeyValuePair<string, Action>>();
        // 只有书类物品才需要配套书目中的书号及合成份数。
        cfg.MartialManualInfo book = null;
        if (item.DetailActions.Contains("培养") || item.DetailActions.Contains("合成秘笈") || item.DetailActions.Contains("领悟"))
        {
            // 书目键是书号，同一本的碎片与成书在背包各有独立物品编号。
            List<cfg.MartialManualInfo> books = LubanMgr.Instance.ConfigTables.TbMartialManualInfo.DataList;
            for (int index = 0; index < books.Count; index++)
            {
                if (books[index].VolumeItem != item.ItemTypeId && books[index].FragmentId != item.ItemTypeId) continue;
                book = books[index];
                if (book.FragmentId == item.ItemTypeId) owned = "合成进度  " + held + " / " + book.FragmentCount;
                break;
            }
        }
        // 稳定的动作键在物品表配置；只有当前实例能使用的入口进入按钮列表。
        string[] keys = item.DetailActions.Split('|');
        for (int index = 0; index < keys.Length; index++)
        {
            // 按钮文字默认与表中动作键一致，具体操作可共用游戏内惯用的短标题。
            string label = keys[index];
            // 每次点击重新绑定，只捕获当前格子的 UID、表行或书目。
            Action run = null;
            switch (label)
            {
                case "饮用":
                    if (RecoveryPotion.TryRead(item, out _, out _)) { label = "使用"; run = () => UsePotion(itemUid); }
                    break;
                case "装备药品":
                    if (RecoveryPotion.TryRead(item, out bool mana, out _)) { label = "装备"; run = () => EquipPotion(item, mana); }
                    break;
                case "合成秘笈":
                case "领悟":
                    if (book == null) break;
                    // 碎片合成与整本领悟都由原秘笈协议确认和扣发。
                    ManualAction operation = ManualAction.ManualLearn;
                    if (label == "合成秘笈") { operation = ManualAction.ManualCompose; label = "合成"; }
                    run = () => MNetClient.Instance.Send(NetDefine.CMD_MartialManual,
                        new ManualRequest { Book = book.Id, Action = operation }.ToByteString());
                    break;
                case "培养":
                    if (book != null) run = () => OpenManualTraining(book.Id);
                    break;
                case "摆摊出售":
                case "集市出售":
                    // 当前堆叠的标志与市场交易规则还会在出售窗口和服务端检查。
                    bool local = label == "集市出售";
                    if (TradeMarketWindow.CanSellStack(stack, local)) run = () => OpenBagSale(itemUid, local);
                    break;
                case "强化":
                    run = () => OpenBagWorkshop(EquipmentWorkshopWindow.Page.Strengthen);
                    break;
                case "锁定词条":
                    run = () => { OpenBagWorkshop(EquipmentWorkshopWindow.Page.Refine); _workshopWindow.SelectWashJade(item.ItemTypeId); };
                    break;
                case "洗练":
                    run = () => { OpenBagWorkshop(EquipmentWorkshopWindow.Page.Refine); _workshopWindow.SelectWashStone(item.ItemTypeId); };
                    break;
                case "镶嵌":
                    run = () => OpenBagWorkshop(EquipmentWorkshopWindow.Page.Socket);
                    break;
                case "合成宝石":
                    if (EquipmentGemRules.TryRead(item.ItemTypeId, out int family, out int grade) && grade < EquipmentGemRules.ComposeCap)
                    {
                        label = "合成";
                        run = () => { OpenBagWorkshop(EquipmentWorkshopWindow.Page.Socket); _workshopWindow.OpenSynthesis(family, grade + 1); };
                    }
                    break;
                case "金装作坊":
                case "金装进阶":
                    // 进阶石定位到成长页，其他材料打开既有金装配方目录。
                    bool advance = label == "金装进阶";
                    run = () =>
                    {
                        _window.CloseWindow();
                        if (!_goldWorkshop.gameObject.activeSelf) _mainView.ShowMainWindow(WindowType.GoldWorkshopWindow);
                        if (advance) _goldWorkshop.Open(1, GoldEquipment(true).Count == 0, 0);
                        else _goldWorkshop.Open(0, false, 0);
                    };
                    break;
            }
            if (run != null) actions.Add(new KeyValuePair<string, Action>(label, run));
        }
        // 绑定优先显示在当前堆叠上；宝石、强化石和禁交易铁牌也显示其限制。
        string restriction = string.Empty;
        if (stack.IsBind() == ItemBind.kItemBindIs) restriction = "已绑定 · 不可交易";
        else if (StallCode.TradeRestriction(item.ItemTypeId, false).Length > 0 && StallCode.TradeRestriction(item.ItemTypeId, true).Length > 0)
            restriction = "不可交易";
        _window.ShowBagItem(item, owned, restriction, actions);
    }

    /// <summary>从物品卡进入出售页，并把被点击的原堆叠带入上架表单。</summary>
    /// <param name="uid">待出售堆叠的实例编号。</param><param name="local">金特技所用的本服集市为真。</param>
    private void OpenBagSale(long uid, bool local)
    {
        // 按点击时的最新背包状态取堆叠，售罄或绑定后不能打开出售表单。
        ItemBase stack = _itemComponent.GetItemByUID(uid);
        if (!TradeMarketWindow.CanSellStack(stack, local)) return;
        _window.CloseWindow();
        if (!_mainView.TradeMarketWindow.gameObject.activeSelf) _mainView.ShowMainWindow(WindowType.TradeMarketWindow);
        _mainView.TradeMarketWindow.OpenBagSale(stack, local);
    }

    /// <summary>从强化石或宝石详情进入现有强化、镶嵌或合成页面。</summary>
    /// <param name="page">物品配置动作所对应的装备养成页。</param>
    private void OpenBagWorkshop(EquipmentWorkshopWindow.Page page)
    {
        _window.CloseWindow();
        if (!_workshopWindow.gameObject.activeSelf) _mainView.ShowMainWindow(WindowType.EquipmentWorkshopWindow);
        _workshopWindow.SelectPage(page);
        _workshopWindow.RenderMoney(_itemComponent.GetVirtualItemCount(CopperItemId),
            _itemComponent.GetVirtualItemCount(SilverItemId), _itemComponent.GetVirtualItemCount(TicketItemId),
            _itemComponent.GetVirtualItemCount(YuanbaoItemId));
        FillWorkshopSource(_itemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack).GetItemMap().Count > 0, 0);
    }

    /// <summary>按角色编号分开保存两个药位，换角色后从该角色自己的配置恢复。</summary>
    /// <param name="mana">真为法力药位，假为生命药位。</param>
    private string PotionPreference(bool mana)
    {
        // 药位是偏好设置，物品堆叠和数量仍在服务器背包里。
        string slot = GameHotkeys.HealthPotion;
        if (mana) slot = GameHotkeys.ManaPotion;
        return "LYF.Potion." + _role.BaseInfo.RoleId + "." + slot;
    }

    /// <summary>把药品类型记入对应的快捷位，更换同类药不消耗库存。</summary>
    /// <param name="item">背包点击的药品配置。</param><param name="mana">该药是否恢复法力。</param>
    private void EquipPotion(cfg.ItemInfo item, bool mana)
    {
        // 快捷栏保留物品编号，堆叠耗尽后补货也能继续使用。
        string command = GameHotkeys.HealthPotion;
        if (mana) { _manaPotionId = item.ItemTypeId; command = GameHotkeys.ManaPotion; }
        else _healthPotionId = item.ItemTypeId;
        PlayerPrefs.SetInt(PotionPreference(mana), item.ItemTypeId);
        PlayerPrefs.Save();
        RenderPotionSlot(mana);
        TipsMgr.Instance.ShowSystemTips(item.Name + "已装备到 " + GameHotkeys.CommandLabel(GameHotkeys.Kind.Potion, command));
    }

    /// <summary>从实际拥有的堆叠中取一瓶，鼠标点击和设置中的物理按键共用入口。</summary>
    /// <param name="mana">真表示点击或按下法力药位。</param>
    public void UseEquippedPotion(bool mana)
    {
        if (_itemComponent == null) return;
        // 同种药可能分属多个格子，按原背包扣料顺序优先选择绑定堆叠。
        int id = _healthPotionId;
        if (mana) id = _manaPotionId;
        if (id == 0) { TipsMgr.Instance.ShowSystemTips("请先在背包中装备药品"); return; }
        List<ItemBase> stacks = _itemComponent.GetItemForReduce(id, ItemBind.kItemBindIs, -2);
        if (stacks.Count == 0) { TipsMgr.Instance.ShowSystemTips("药品已用完"); return; }
        UsePotion(stacks[0].GetItemUID());
    }

    /// <summary>两种入口都只提交当前堆叠，由游戏服检查资源、扣药并广播变化。</summary>
    /// <param name="uid">当前角色背包中的物品堆叠实例编号。</param>
    private void UsePotion(long uid)
    {
        if (!MNetClient.Instance.Send(RecoveryPotion.UseCommand, new UsePotionRequest { ItemUid = uid }.ToByteString()))
            TipsMgr.Instance.ShowSystemTips("连接未就绪，请稍后再试");
    }

    /// <summary>背包增删及完整快照到达后刷新受影响的快捷位，即使背包窗口已关闭。</summary>
    /// <param name="change">已经写入本地库存的变化，零物品号表示完整刷新。</param>
    private void OnPotionStockChanged(KnapsackChangedEventArgs change)
    {
        if (change.ItemTypeId == 0 || change.ItemTypeId == _healthPotionId) RenderPotionSlot(false);
        if (change.ItemTypeId == 0 || change.ItemTypeId == _manaPotionId) RenderPotionSlot(true);
    }

    /// <summary>按当前角色装备的配置号查询真实合计，空位不展示装饰药瓶。</summary>
    /// <param name="mana">要刷新的快捷位，真为法力药。</param>
    private void RenderPotionSlot(bool mana)
    {
        if (_potionBar == null) return;
        // 角色解绑会令物品编号归零，并把原角色药瓶从 HUD 清掉。
        int id = _healthPotionId;
        if (mana) id = _manaPotionId;
        cfg.ItemInfo item = LubanMgr.Instance.GetItemInfoById(id);
        if (item == null) { _potionBar.Render(mana, string.Empty, 0); return; }
        int count = _itemComponent.GetItemNum(id, ItemBind.kItemBindIs, true);
        _potionBar.Render(mana, item.Icon, Math.Max(0, count));
    }

    /// <summary>关闭背包镜头并沿主视图窗口入口进入所点书籍的培养页。</summary>
    /// <param name="bookId">碎片或整本所属的稳定书号。</param>
    private void OpenManualTraining(int bookId)
    {
        _window.CloseWindow();
        if (!_mainView.SkillWindow.gameObject.activeSelf) _mainView.ShowMainWindow(WindowType.SkillInfoWindow);
        _mainView.SkillWindow.OpenManualTraining(bookId);
    }

    /// <summary>由背包或已穿戴槽打开同一装备详情窗口。</summary>
    /// <param name="itemUid">玩家实际点击的物品 UID。</param>
    public void OpenEquipmentDetail(long itemUid)
    {
        if (_itemComponent == null || _detailWindow == null) return;
        // 普通物品继续使用既有物品提示，只有装备打开此窗口。
        EquipBase equip = _itemComponent.GetItemByUID(itemUid) as EquipBase;
        if (equip == null) return;
        _selectedEquipmentUid = itemUid;
        RefreshEquipmentDetail();
        if (!_detailWindow.gameObject.activeSelf)
        {
            _mainView.ShowMainWindow(WindowType.EquipmentDetailWindow);
        }
    }

    /// <summary>服务器增量应用后重新读取当前实例，确保穿脱和特技状态来自服务器。</summary>
    public void RefreshEquipmentDetail()
    {
        if (_detailWindow == null || _itemComponent == null || _selectedEquipmentUid <= 0) return;
        // 增量替换后的装备引用，不能缓存上一次已被移出的实例。
        EquipBase equip = _itemComponent.GetItemByUID(_selectedEquipmentUid) as EquipBase;
        if (equip == null)
        {
            _detailWindow.CloseWindow();
            _selectedEquipmentUid = 0;
            return;
        }
        _detailWindow.Render(BuildEquipmentDetail(equip));
    }

    /// <summary>请求穿戴当前详情装备，业务结果由服务器回复。</summary>
    private void WearSelectedEquipment() { SubmitSelectedOperation(EquipOperation.Wear, 0); }

    /// <summary>从详情携带装备 UID 打开强化页，并读取该角色当前四种货币的余额。</summary>
    private void OpenEquipmentWorkshop() { OpenSelectedWorkshop(EquipmentWorkshopWindow.Page.Strengthen); }

    /// <summary>从当前装备的洗练方案进入工坊。</summary>
    private void OpenWashWorkshop() { OpenSelectedWorkshop(EquipmentWorkshopWindow.Page.Refine); }

    /// <summary>按详情中的实际装备定位到目标养成页。</summary>
    /// <param name="page">强化或洗练页签。</param>
    private void OpenSelectedWorkshop(EquipmentWorkshopWindow.Page page)
    {
        // 详情和养成页指向同一件实例，同名的背包装备也能正确定位。
        EquipBase equip = _itemComponent.GetItemByUID(_selectedEquipmentUid) as EquipBase;
        if (equip == null) return;
        _detailWindow.CloseWindow();
        _mainView.ShowMainWindow(WindowType.EquipmentWorkshopWindow);
        _workshopWindow.SelectPage(page);
        _workshopWindow.RenderMoney(_itemComponent.GetVirtualItemCount(CopperItemId),
            _itemComponent.GetVirtualItemCount(SilverItemId), _itemComponent.GetVirtualItemCount(TicketItemId),
            _itemComponent.GetVirtualItemCount(YuanbaoItemId));
        FillWorkshopSource(equip.GetBagType() == (int)KnapsackType.RoleCurrtEquipPack, equip.GetItemUID());
    }

    /// <summary>玩家切换左侧来源后选择新列表中的第一件装备。</summary>
    /// <param name="worn">为真时列出穿戴栏，为假时列出背包内的装备实例。</param>
    private void ChangeWorkshopSource(bool worn) { FillWorkshopSource(worn, 0); }

    /// <summary>按实际物品编号统计每个等级的所有背包堆叠，包含绑定材料。</summary>
    /// <param name="firstId">当前栏位中一级强化石或一级宝石的配置编号。</param>
    /// <param name="levels">强化石栏有五级，每个宝石品种有十级。</param>
    private void RenderWorkshopStock(int firstId, int levels)
    {
        // index 为当前物品等级减一，对应连续的物品 ID。
        for (int index = 0; index < levels; index++)
        {
            // 统计角色全部背包，同级材料拆成多个堆叠时在此相加。
            int count = _itemComponent.GetItemNum(firstId + index, ItemBind.kItemBindIs, true);
            _workshopWindow.RenderStockCount(index, count);
        }
    }

    /// <summary>物品变动或整包同步后更新打开的材料栏，背包自身继续使用原有脏标记。</summary>
    /// <param name="change">已完成写入的物品变更，整包同步携带的物品编号为零。</param>
    private void OnWorkshopStockChanged(KnapsackChangedEventArgs change)
    {
        if (_goldWorkshop != null && _goldWorkshop.isActiveAndEnabled)
            _goldWorkshop.Refresh(change.ItemTypeId == 0 || GoldWorkshopRecipe.IsGold(change.ItemTypeId));
        if (_pavilionWindow != null && _pavilionWindow.isActiveAndEnabled)
        {
            if (change.ItemTypeId == CopperItemId || change.ItemTypeId == 0) _pavilionWindow.RefreshWallet();
        }
        if (_workshopWindow == null || !_workshopWindow.isActiveAndEnabled) return;
        if (change.ItemTypeId == CopperItemId || change.ItemTypeId == 0)
            _workshopWindow.RenderMoney(_itemComponent.GetVirtualItemCount(CopperItemId), _itemComponent.GetVirtualItemCount(SilverItemId),
                _itemComponent.GetVirtualItemCount(TicketItemId), _itemComponent.GetVirtualItemCount(YuanbaoItemId));
        _workshopWindow.RefreshStock(change.ItemTypeId);
        // 只在当前装备被服务器更新、或收到整包快照时重绘宝石孔。
        long uid = _workshopWindow.SelectedItemUid;
        if (uid != 0 && (change.ItemUid == uid || change.ItemTypeId == 0)) RenderWorkshopEquipment(uid);
    }

    /// <summary>统一统计角色背包里可选中的绑定与未绑定宝石。</summary>
    /// <param name="id">包含品种与等级的真实物品编号。</param>
    private int OwnedGem(int id) { return _itemComponent.GetItemNum(id, ItemBind.kItemBindIs, true); }

    /// <summary>通过现有装备命令发送材料或商会查询，断线时允许玩家重新提交。</summary>
    /// <param name="request">合成、购买或查询的强类型消息。</param>
    private bool SendGemTrade(EquipOperationReq request)
    {
        if (MNetClient.Instance.Send(NetDefine.CMD_EquipOperationCode, request.ToByteString())) return true;
        TipsMgr.Instance.ShowSystemTips("未连接服务器");
        return false;
    }

    /// <summary>将服务器的个人配额和真实交易结果转发到养成界面。</summary>
    /// <param name="reply">本角色通过现有装备命令收到的交易回执。</param>
    public void ApplyGemTrade(EquipOperationRet reply)
    {
        if (_workshopWindow != null) _workshopWindow.RenderTrade(reply);
        if (_pavilionWindow != null) _pavilionWindow.RenderGemTrade(reply);
        if (reply.GemMessage.Length > 0) TipsMgr.Instance.ShowSystemTips(reply.GemMessage);
        else if (reply.CmdCode != CmdCode.Succeed) TipsMgr.Instance.ShowSystemTips("宝石操作失败：" + reply.CmdCode);
    }

    /// <summary>把实际结果发回洗练窗口，失败时展示服务端给出的具体原因。</summary>
    /// <param name="reply">游戏服完成或拒绝本次洗练后返回的状态。</param>
    public void ApplyWash(EquipOperationRet reply)
    {
        if (_workshopWindow != null) _workshopWindow.RenderWash(reply);
        if (!string.IsNullOrEmpty(reply.WashMessage)) TipsMgr.Instance.ShowSystemTips(reply.WashMessage);
    }

    /// <summary>从主城菜单进入作坊，空背包也能预览和合成成品。</summary>
    private void OpenGoldCatalog()
    {
        _mainView.SetFunctionMenuVisible(false);
        _mainView.ShowMainWindow(WindowType.GoldWorkshopWindow);
        _goldWorkshop.Open(0, false, 0);
    }

    /// <summary>从装备详情进入当前实例的进阶页。</summary>
    private void OpenGoldAdvance()
    {
        // 现有金装使用逐阶材料配方，普通装备按当前实例阶数支付铜币。
        EquipBase equip = _itemComponent.GetItemByUID(_selectedEquipmentUid) as EquipBase;
        if (equip == null) return;
        if (GoldWorkshopRecipe.IsGold(equip.GetItemID())) { OpenGoldInstance(1, _selectedEquipmentUid); return; }
        // 初始为一阶，下一目标是已进阶次数加二。
        int target = equip.GoldTierSteps + 2;
        if (target > EquipmentGrowth.MaxTier) { TipsMgr.Instance.ShowSystemTips("已达十阶"); return; }
        _detailWindow.ShowGrowth(EquipOperation.Advance, "进阶", "提升至 " + target + " 阶，消耗 " + EquipmentGrowth.Cost(EquipOperation.Advance, target) + " 铜币");
    }

    /// <summary>提交小窗里确认的普通装备进阶或重铸。</summary>
    /// <param name="operation">详情页选择的服务器操作。</param>
    private void SubmitDetailGrowth(EquipOperation operation) { SubmitGrowth(_selectedEquipmentUid, operation); }

    /// <summary>按工坊列表当前选中的真实实例提交一级强化。</summary>
    /// <param name="uid">工坊选中装备的唯一编号。</param>
    private void StrengthenEquipment(long uid) { SubmitGrowth(uid, EquipOperation.Strengthen); }

    /// <summary>只发送实例编号与当前看到的目标档，数值和价格由服务器计算。</summary>
    /// <param name="uid">从详情或养成列表选中的实例。</param>
    /// <param name="operation">一次普通进阶、强化或重铸。</param>
    private void SubmitGrowth(long uid, EquipOperation operation)
    {
        // 物品通知可能替换实例引用，提交时按唯一编号读取。
        EquipBase equip = _itemComponent.GetItemByUID(uid) as EquipBase;
        if (equip == null) return;
        // 重铸没有目标档，强化和进阶分别提交所见下一档。
        int target = 0;
        if (operation == EquipOperation.Strengthen) target = equip.StrengthenLevel + 1;
        if (operation == EquipOperation.Advance) target = equip.GoldTierSteps + 2;
        SendGemTrade(new EquipOperationReq { ItemUid = uid, GoldItemId = equip.GetItemID(), GoldTarget = target, Operation = operation });
    }

    /// <summary>从现有养成窗口的升级页签进入当前金装的升级页。</summary>
    /// <param name="uid">养成左栏当前选中的实例。</param>
    private void OpenGoldUpgrade(long uid) { OpenGoldInstance(2, uid); }

    /// <summary>保持原穿戴或背包来源，打开详情中选定的同一件金装。</summary>
    /// <param name="page">进阶为1，升级为2。</param>
    /// <param name="uid">详情或养成窗口的装备实例。</param>
    private void OpenGoldInstance(int page, long uid)
    {
        // 物品增量可能替换客户端对象，这里按 UID 取得最新实例。
        EquipBase equip = _itemComponent?.GetItemByUID(uid) as EquipBase;
        if (equip == null || !GoldWorkshopRecipe.IsGold(equip.GetItemID()))
        { TipsMgr.Instance.ShowSystemTips("当前作坊只开放魔剑士辉银金装"); return; }
        _detailWindow.CloseWindow();
        _workshopWindow.CloseWindow();
        _mainView.ShowMainWindow(WindowType.GoldWorkshopWindow);
        _goldWorkshop.Open(page, equip.GetBagType() != (int)KnapsackType.RoleCurrtEquipPack, uid);
    }

    /// <summary>材料跨角色物品包统计，虚拟钱包内以相同绑定规则读取工费余额。</summary>
    /// <param name="id">材料或货币的物品配置编号。</param>
    private long GoldStock(int id)
    {
        if (_itemComponent == null) return 0;
        if (ItemDesc.IsVirtualItem(id)) return _itemComponent.GetItemNum(id, ItemBind.kItemBindIs);
        return _itemComponent.GetItemNum(id, ItemBind.kItemBindIs, true);
    }

    /// <summary>按格位读取所属容器内真实的金装实例。</summary>
    /// <param name="worn">为真时从穿戴栏读取，否则从装备包读取。</param>
    private List<EquipBase> GoldEquipment(bool worn)
    {
        // 当前来源下的辉银装备，同名的多件各占一行。
        List<EquipBase> result = new List<EquipBase>();
        if (_itemComponent == null) return result;
        // 当前来源的背包类别。
        KnapsackType source = KnapsackType.RolePackEquip;
        if (worn) source = KnapsackType.RoleCurrtEquipPack;
        // 与其他装备窗口采用相同的格位顺序。
        List<ItemBase> entries = new List<ItemBase>(_itemComponent.GetContainer(source).GetItemMap().Values);
        if (!worn) entries.AddRange(_itemComponent.GetContainer(KnapsackType.RolePackPlain).GetItemMap().Values);
        entries.Sort(CompareItem);
        for (int index = 0; index < entries.Count; index++)
        {
            // 原物品实例保留在角色容器中，窗口只读。
            EquipBase equip = entries[index] as EquipBase;
            if (equip != null && GoldWorkshopRecipe.IsGold(equip.GetItemID())) result.Add(equip);
        }
        return result;
    }

    /// <summary>把金装结算回执返回到作坊，库存与装备仍走原物品更新。</summary>
    /// <param name="reply">角色服的实际结算结果。</param>
    public void ApplyGoldWorkshop(EquipOperationRet reply) { _goldWorkshop.Finish(reply); }

    /// <summary>向服务器请求一次镶嵌，材料库存与孔位均沿用服务器物品增量更新。</summary>
    /// <param name="uid">玩家选中的装备实例。</param>
    /// <param name="slot">当前装备从左到右的宝石孔下标。</param>
    /// <param name="gemId">按品种和等级区分的背包物品编号。</param>
    private void SocketWorkshopGem(long uid, int slot, int gemId)
    {
        // 客户端只表达选材，不自行修改装备或扣减库存。
        EquipOperationReq request = new EquipOperationReq
        {
            ItemUid = uid, Operation = EquipOperation.SocketGem, GemSocketIndex = slot, GemItemId = gemId
        };
        if (!MNetClient.Instance.Send(NetDefine.CMD_EquipOperationCode, request.ToByteString()))
            TipsMgr.Instance.ShowSystemTips("镶嵌请求发送失败");
    }

    /// <summary>按穿戴栏或背包实际格位顺序生成列表，并传递本次需要定位的实例。</summary>
    /// <param name="worn">当前选择的来源是否为穿戴栏。</param>
    /// <param name="selectedUid">打开入口时的真实实例 UID，切换来源时为零。</param>
    private void FillWorkshopSource(bool worn, long selectedUid)
    {
        // 从角色物品组件拿到的原始格位，用已有排序与装备详情保持一致。
        List<ItemBase> items = new List<ItemBase>();
        if (worn)
        {
            items.AddRange(_itemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack).GetItemMap().Values);
        }
        else
        {
            items.AddRange(_itemComponent.GetContainer(KnapsackType.RolePackEquip).GetItemMap().Values);
            items.AddRange(_itemComponent.GetContainer(KnapsackType.RolePackPlain).GetItemMap().Values);
        }
        items.Sort(CompareItem);
        // 列表只带实际装备和图标，完整属性按玩家选中的那一行读取。
        List<EquipmentViewData> entries = new List<EquipmentViewData>();
        // index 遍历选定来源中已经占用的格位。
        for (int index = 0; index < items.Count; index++)
        {
            // 普通背包里非装备物品不进入养成列表。
            EquipBase equip = items[index] as EquipBase;
            if (equip == null) continue;
            // 正式装备表提供名称、部位、图标路径和穿戴需求。
            cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equip.GetItemID());
            entries.Add(new EquipmentViewData((EquipType)config.EquipType, equip.GetItemID(),
                config.EquipName, config.Icon, equip.StrengthenLevel, equip.GetItemUID(), config.NeedLevel));
        }
        _workshopWindow.RenderItems(entries, worn, selectedUid);
    }

    /// <summary>把左侧选中的真实装备属性交给中央分页，全程只读取模型。</summary>
    /// <param name="itemUid">这次选中的装备实例 UID。</param>
    private void RenderWorkshopEquipment(long itemUid)
    {
        // 与装备详情使用同一份配置、属性格式及宝石孔数。
        EquipBase equip = _itemComponent.GetItemByUID(itemUid) as EquipBase;
        if (equip == null) return;
        // 强化等级上限来自装备表，强化材料和升级数值留待玩法定义。
        cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equip.GetItemID());
        _workshopWindow.RenderDetail(BuildEquipmentDetail(equip), equip.StrengthenLevel, config.MaxStrengthenLevel);
        _workshopWindow.RenderGems(equip, config.MaxGemLevel);
    }

    /// <summary>请求卸下当前详情装备，业务结果由服务器回复。</summary>
    private void TakeOffSelectedEquipment() { SubmitSelectedOperation(EquipOperation.TakeOff, 0); }
    /// <summary>请求启用当前装备的指定保存特技槽；该槽未开孔时改为提交开孔请求。</summary>
    /// <param name="index">玩家点击的槽下标0至2。</param>
    private void SwitchSelectedSpecialSkill(int index)
    {
        // 当前选择实例可能已被增量更新替换，取最新引用再判断孔位状态。
        EquipBase equip = null;
        if (_itemComponent != null && _selectedEquipmentUid > 0)
        {
            equip = _itemComponent.GetItemByUID(_selectedEquipmentUid) as EquipBase;
        }
        if (equip == null)
        {
            SubmitSelectedOperation(EquipOperation.SwitchSpecialSkill, index);
            return;
        }
        // 只有正好是下一个孔时才是开孔请求；未开孔的后续孔由服务端拒绝，不会扣费。
        if (index >= equip.OpenedSpecialSkillCount)
        {
            SubmitSelectedOperation(EquipOperation.OpenSpecialSkillSlot, index);
            return;
        }
        SubmitSelectedOperation(EquipOperation.SwitchSpecialSkill, index);
    }

    /// <summary>提交当前选择并仅在连接发送失败时显示提示。</summary>
    /// <param name="operation">当前按钮对应的服务器装备操作。</param>
    /// <param name="index">切换特技时的目标保存槽下标。</param>
    private void SubmitSelectedOperation(EquipOperation operation, int index)
    {
        if (!SendEquipmentOperation(_selectedEquipmentUid, operation, index))
        {
            TipsMgr.Instance.ShowSystemTips("装备操作发送失败，连接不可用");
        }
    }

    /// <summary>将基础属性、当前套装穿戴状态和三个特技槽合并为详情展示数据。</summary>
    /// <param name="equip">当前服务器装备实例。</param>
    private EquipmentDetailViewData BuildEquipmentDetail(EquipBase equip)
    {
        // 装备静态配置提供基础属性和宝石孔数，特技仍从装备实例读取。
        cfg.EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equip.GetItemID());
        // 当前装备的纯展示结果。
        EquipmentDetailViewData result = new EquipmentDetailViewData();
        result.Name = config.EquipName;
        result.IconPath = config.Icon;
        result.PartName = EquipmentPartName((EquipType)config.EquipType);
        result.IsWorn = equip.GetBagType() == (int)KnapsackType.RoleCurrtEquipPack;
        // 属性按录屏改成双列：左列三行先填满，剩余行落到右列，名称与数值行号一一对应。
        StringBuilder leftNames = new StringBuilder();
        // 左列数值，保留百分数语义。
        StringBuilder leftValues = new StringBuilder();
        // 右列名称，与左列行序连续。
        StringBuilder rightNames = new StringBuilder();
        // 右列数值，与右列名称同序。
        StringBuilder rightValues = new StringBuilder();
        // 静态装备配置中的六个基因编号及对应覆盖值。
        int[] baseIds = EquipmentGrowth.Genes(config);
        // 六个静态基因的策划值，与基础编号保持同序。
        int[] baseValues = EquipmentGrowth.Templates(config);
        // 属性眼显示当前这件装备的进度及逐槽拆分。
        StringBuilder breakdown = new StringBuilder();
        breakdown.Append("强化 ").Append(equip.StrengthenLevel).Append("/50 · ").Append(equip.GoldTierSteps + 1).Append("阶\n基础值（+强化 +进阶）\n");
        // 重铸卡显示达到极所用的原始值，不含强化或进阶。
        StringBuilder reforge = new StringBuilder("各项达到满值的 90% 即为「极」\n");
        // visible 是已配置基因在双列中的实际填写行数，0~2 落左列，3 起落右列。
        int visible = 0;
        for (int index = 0; index < baseIds.Length; index++)
        {
            if (baseIds[index] == 0) continue;
            // 正式基因配置提供显示名和百分数类型，此处只拆分四列展示文字。
            cfg.Gene gene = LubanMgr.Instance.GetGeneInfoById(baseIds[index]);
            if (gene == null) continue;
            // nameTarget 与 valueTarget 是当前行应写入的列缓冲，左列满三行后切到右列。
            StringBuilder nameTarget = leftNames;
            // valueTarget 是同名列对应的数值缓冲，切换规则与名称一致。
            StringBuilder valueTarget = leftValues;
            if (visible >= 3)
            {
                nameTarget = rightNames;
                valueTarget = rightValues;
            }
            nameTarget.Append(ShortGeneName(gene.EffectDesc)).Append('\n');
            // 本槽两项养成贡献分别取整，与服务器挂载的基因值一致。
            int value = EquipmentGrowth.Base(equip, baseValues[index], index);
            // 百分比词条的三个数值均以百分点表示。
            string unit = string.Empty;
            if (gene.ValueType == 1) unit = "%";
            // 单条满值用于重铸进度与满属性标记。
            int cap = EquipmentGrowth.Maximum(baseValues[index]);
            breakdown.Append(ShortGeneName(gene.EffectDesc)).Append("　").Append(value).Append(unit)
                .Append("（<color=#267D68>+").Append(EquipmentGrowth.Strength(value, equip.StrengthenLevel)).Append(unit)
                .Append("</color> <color=#936323>+").Append(EquipmentGrowth.Tier(value, equip.GoldTierSteps)).Append(unit).Append("</color>）\n");
            reforge.Append(ShortGeneName(gene.EffectDesc)).Append("　").Append(value).Append(unit).Append(" / ").Append(cap).Append(unit);
            if (value == cap && cap > 0) reforge.Append("　<color=#267D68>满</color>");
            reforge.Append('\n');
            valueTarget.Append(EquipmentGrowth.Total(equip, baseValues[index], index));
            if (gene.ValueType == 1) valueTarget.Append('%');
            valueTarget.Append('\n');
            visible++;
        }
        result.AttributeLeftNames = leftNames.ToString().TrimEnd('\n');
        result.AttributeLeftValues = leftValues.ToString().TrimEnd('\n');
        // 右列没有配置时补中性占位，避免右侧 TMP 空白。
        result.AttributeRightNames = rightNames.ToString().TrimEnd('\n');
        result.AttributeRightValues = rightValues.ToString().TrimEnd('\n');
        if (string.IsNullOrEmpty(result.AttributeRightNames)) result.AttributeRightNames = "—";
        if (string.IsNullOrEmpty(result.AttributeRightValues)) result.AttributeRightValues = "—";
        // 旧字段保持左列语义，其它引用方不会读到空值。
        result.Attributes = result.AttributeLeftNames;
        result.AttributeValues = result.AttributeLeftValues;
        // 属性卡正文按左列、右列逐行配对，名称与数值来自同一行号，缺失写占位。
        StringBuilder cardAttr = new StringBuilder();
        string[] cardNames = (result.AttributeLeftNames + "\n" + result.AttributeRightNames).Split('\n');
        string[] cardValues = (result.AttributeLeftValues + "\n" + result.AttributeRightValues).Split('\n');
        for (int index = 0; index < cardNames.Length; index++)
        {
            // 空行与占位行不写进卡片，避免出现孤立破折号。
            if (cardNames[index] == "—") continue;
            if (string.IsNullOrEmpty(cardNames[index])) continue;
            // 对应行数值，行号对齐左列在前右列在后。
            string cardValue = "—";
            if (index < cardValues.Length) cardValue = cardValues[index];
            cardAttr.Append("<b>").Append(cardNames[index]).Append("</b>　").Append(cardValue).Append('\n');
        }
        if (cardAttr.Length == 0) cardAttr.Append("暂无信息\n");
        result.BaseAttributeText = cardAttr.ToString().TrimEnd('\n');
        result.AttributeCardText = breakdown.ToString().TrimEnd('\n');
        result.ReforgeText = reforge.Append("重铸将替换现有基础值，消耗 ").Append(EquipmentGrowth.ReforgeCost).Append(" 铜币").ToString();
        // 铭缀与洗练暂无实例数据，写中性空态，不推算任何数值。
        result.InscribeRowText = "暂无信息";
        result.WashRowText = EquipmentWashPanel.Describe(EquipmentWash.Snapshot(equip));
        // 宝石孔数取真实配置，必须先于说明文本赋值，否则界面会显示「共 0 孔」。
        result.GemSlotCount = config.GemSlotCount;
        // 收集各孔中实际存在的宝石，顺序与装备实例相同。
        List<string> gemNames = new List<string>();
        for (int slot = 0; slot < result.GemSlotCount && slot < equip.GemItemIds.Length; slot++)
        {
            // 零是空孔，旧存档还原时也按空孔处理。
            int gemId = equip.GemItemIds[slot];
            if (gemId == 0) continue;
            // 物品表中保存该颗宝石的带等级名称。
            cfg.ItemInfo gem = LubanMgr.Instance.GetItemInfoById(gemId);
            if (gem == null) continue;
            gemNames.Add(gem.Name);
            // 品种决定透明孔内图，背包里的等级底色只用于选材。
            if (_workshopWindow != null && EquipmentGemRules.TryRead(gemId, out int family, out _))
                result.GemSprites[slot] = _workshopWindow.GetGemArt(family);
        }
        result.GemInfoText = "已镶嵌 " + gemNames.Count + " / " + result.GemSlotCount;
        result.GemDropText = "镶嵌：空槽";
        if (gemNames.Count > 0) result.GemDropText = string.Join("、", gemNames);
        FillSetDetails(result, _itemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack));
        result.ForgeExpandText = "暂无淬锋信息";
        // 已开孔数量来自服务器权威实例，决定哪些槽可以安装特技。
        int openedSkillCount = equip.OpenedSpecialSkillCount;
        // 开孔费用与服务端扣费使用同一个常量，客户端只负责展示。
        result.SpecialSkillOpenCostText = "开孔需 " + EquipBase.SpecialSkillSlotOpenCost + " 铜币";
        // 已开孔数量同步进视图数据，供后续分区展示使用。
        result.OpenedSpecialSkillCount = openedSkillCount;
        // 三个保存槽逐一展示，只有当前选择且已穿戴的槽计算实际同名件数等级。
        for (int index = 0; index < 3; index++)
        {
            // 槽中保存的是稳定特技族 ID，不是某级配置行 ID。
            int familyId = equip.GetSpecialSkillId(index);
            // 未开孔的槽即使存有历史数据也不参与启用，界面按未开孔展示。
            if (index >= openedSkillCount)
            {
                result.SpecialSkillTexts[index] = "特技槽" + (index + 1) + "：未开孔，需先开启前一孔并消耗铜币";
                result.SpecialSkillCards[index] = "该槽尚未开孔。开孔顺序固定为第一、第二、第三孔，每次消耗铜币。";
                result.SpecialSkillHeadlines[index] = "特技：<color=#627E94>未开孔</color>";
                result.SpecialSkillCardTitles[index] = "特技" + (index + 1) + "：未开孔";
                result.SpecialSkillCardStates[index] = "未开孔";
                result.IsSpecialSkillEmpty[index] = true;
                // 只有正好等于已开孔数的那个槽可以提交开孔，跳孔一律不给入口。
                result.CanOpenSpecialSkill[index] = index == openedSkillCount;
                continue;
            }
            if (familyId == 0)
            {
                result.SpecialSkillTexts[index] = "特技槽" + (index + 1) + "：已开孔，空槽，未镶嵌特技";
                // 空槽说明卡只写空态，当前层与下一层都写暂无信息，不伪造效果。
                result.SpecialSkillCards[index] = BuildSpecialSkillCard(null, null, 0);
                // 空槽标题行只写槽位与「已开孔·空槽」，不伪造已装配的特技名，也不画占位横线。
                result.SpecialSkillHeadlines[index] = "特技：<color=#627E94>已开孔·空槽</color>";
                result.SpecialSkillCardTitles[index] = "特技" + (index + 1) + "：已开孔·空槽";
                result.SpecialSkillCardStates[index] = "已开孔";
                // 明确标记空槽，界面据此使用浅灰禁用色，避免把「已选但不可切换」误判成空槽。
                result.IsSpecialSkillEmpty[index] = true;
                continue;
            }
            // 本装备当前选择状态，未穿戴时不作为已生效显示。
            bool selected = equip.ActiveSpecialSkillIndex == index;
            // 一级行同时提供本族的部位限制，无穿戴时仍可查看基础效果。
            cfg.EquipSpecialSkillInfo skill = ResolveSpecialSkillLevel(familyId, 1);
            if (skill == null) continue;
            // 只计入穿在合法部位且选中同族的实例，按件数读取该族的唯一生效等级。
            int pieces = CountWornSpecialSkill(skill);
            if (pieces > 1) skill = ResolveSpecialSkill(familyId, pieces);
            // 旧装备可能保留部位不符的槽位，详情仍可查看该条说明。
            bool partAllowed = EquipBase.IsSpecialSkillPartAllowed(skill.AllowedEquipTypeMask, equip.EquipType);
            // 当前槽状态说明，详情层保留完整措辞。
            string state = "未启用";
            // 标题行只放短状态，窄行内不会被省略号截断而丢掉「未穿戴」。
            string shortState = "未启用";
            // 侧卡同步使用本槽在当前装备上的生效状态。
            string cardState = "未使用";
            if (!partAllowed || skill.GeneId == 0)
            {
                state = "部位不符";
                if (partAllowed) state = "效果未配置";
                shortState = state;
                cardState = state;
            }
            else if (selected && result.IsWorn)
            {
                state = "已启用，同名" + pieces + "件";
                shortState = "已启用·" + pieces + "件";
                cardState = "当前生效";
            }
            else if (selected)
            {
                state = "已选，未穿戴";
                shortState = "已选·未穿";
                cardState = shortState;
            }
            result.SpecialSkillTexts[index] = skill.Name + " Lv." + skill.Level + "（" + state + "）\n" + skill.Description;
            // 主列表保持视频中的紧凑单行，只有已选槽附带次级状态，完整效果进入侧卡查看。
            result.SpecialSkillHeadlines[index] = "特技：<color=#267B8B>" + skill.Name + " +" + skill.Level + "级</color>";
            if (selected)
            {
                result.SpecialSkillHeadlines[index] += " <size=12><color=#627E94>（" + shortState + "）</color></size>";
            }
            result.SpecialSkillCardTitles[index] = "特技" + (index + 1) + "：" + skill.Name + " +" + skill.Level + "级";
            result.SpecialSkillCardStates[index] = cardState;
            // 说明卡按区展示：当前层取真实配置等级，下一层取同族下一等级行，缺失写暂无信息。
            int maxLevel = ResolveSpecialSkillMaxLevel(familyId);
            cfg.EquipSpecialSkillInfo nextSkill = ResolveSpecialSkillLevel(familyId, skill.Level + 1);
            result.SpecialSkillCards[index] = BuildSpecialSkillCard(skill, nextSkill, maxLevel);
            result.CanActivateSpecialSkill[index] = !selected && partAllowed;
        }
        // 头部角标只由服务器穿戴状态派生，不根据按钮显隐猜测。
        result.WornBadgeText = "未装备";
        if (result.IsWorn)
        {
            result.WornBadgeText = "装备中";
        }
        // 品质按该装备的配置展示，旧表中未标注的装备保留占位。
        string qualityName = EquipmentQualityNames[0];
        if (config.Quality > 0 && config.Quality < EquipmentQualityNames.Length)
        {
            qualityName = EquipmentQualityNames[config.Quality];
        }
        // 重铸评级独立于白、绿、蓝、紫、金的装备颜色。
        string grade = "珍品";
        result.IsExtreme = EquipmentGrowth.IsExtreme(equip, config);
        if (result.IsExtreme) grade = "极";
        result.HeaderPartText = result.PartName + " · " + grade;
        if (config.Quality > 0) result.HeaderPartText = result.PartName + " · " + qualityName + " · " + grade;
        result.HeaderBindText = "绑定 —";
        // 原元信息行改承担评分与需求，两行都在窄区内。
        result.MetaText = BuildMetaText(config);
        // 附加分区与底栏只用已存在的配置和实例字段；没有来源的统一中性占位，不写录屏数值。
        // 交易与耐久都没有真实字段，未知不等于不可交易，统一中性占位。
        result.FooterText = "交易：—    耐久：—/—";
        result.InfoLayerText = BuildInfoText(config);
        result.FooterExtraText = result.InfoLayerText;
        // 独立行控件的数据：强化取真实配置，其余保持中性占位；宝石孔数已在前文写入。
        result.EnhanceRowText = "强化 " + equip.StrengthenLevel + "/" + config.MaxStrengthenLevel + "  ·  " + (equip.GoldTierSteps + 1) + "阶";
        if (GoldWorkshopRecipe.IsGold(equip.GetItemID()))
            result.EnhanceRowText = "强化 " + equip.StrengthenLevel + "/" + config.MaxStrengthenLevel +
                "  ·  " + GoldWorkshopRecipe.Level(equip.GoldLevelSteps) + "级  ·  " + (equip.GoldTierSteps + 1) + "阶";
        // 淬锋与铭缀是两个子系统，淬锋行只写自身内容，不再拼成一大段混写文字。
        result.ForgeRowText = "淬锋：暂无数据";
        // 祭甲与甲片按用户要求本轮不展示，该行数值不再生成，控件保持隐藏。
        result.GarnishRowText = string.Empty;
        // 特技信息行取第一个真实特技槽文本，空槽时也沿用其「空」说明。
        result.FeatureRowText = result.SpecialSkillTexts[0];
        result.AppearanceRowText = "外观：—  ·  无额外养成";
        return result;
    }

    /// <summary>生成装备头部的评分、需求等级与装备表指定的职业名称。</summary>
    /// <param name="config">当前装备的静态配置。</param>
    /// <returns>头部元信息的两行展示文本。</returns>
    private static string BuildMetaText(cfg.EquipInfo config)
    {
        // meta 是逐段拼接的头部两行文本，避免为展示数据新建类型。
        StringBuilder meta = new StringBuilder();
        meta.Append("评分 —\n");
        // 职业名称取自穿戴限制引用的职业配置，缺少匹配项时沿用占位符。
        cfg.ProfessionInfo profession = LubanMgr.Instance.GetProfessionInfoById(config.Job);
        meta.Append("需求 ").Append(config.NeedLevel).Append("级 · 职业 ").Append(profession?.Name ?? "—");
        return meta.ToString();
    }

    /// <summary>生成信息层正文，只用静态配置里已有的说明与获取途径，不编造装备背景。</summary>
    /// <param name="config">当前装备的静态配置。</param>
    /// <returns>说明与获取途径两行文本。</returns>
    private static string BuildInfoText(cfg.EquipInfo config)
    {
        // desc 是配置里的装备说明，缺失时用中性占位。
        string desc = "暂无数据";
        if (!string.IsNullOrEmpty(config.Des))
        {
            desc = config.Des;
        }
        // source 是配置里的获取途径，缺失时用中性占位。
        string source = "暂无数据";
        if (!string.IsNullOrEmpty(config.Source))
        {
            source = config.Source;
        }
        // text 是说明层两行正文，职业编号不映射职业名，避免用数字冒充中文职业。
        StringBuilder text = new StringBuilder();
        text.Append("说明：").Append(desc).Append('\n');
        text.Append("获取途径：").Append(source);
        return text.ToString();
    }

    /// <summary>把已有装备部位枚举转换为详情顶部的中文标签。</summary>
    /// <param name="part">当前装备正式配置中的部位。</param>
    private static string EquipmentPartName(EquipType part)
    {
        switch (part)
        {
            case EquipType.MainWeapon: return "主武器";
            case EquipType.OffhandWeapon: return "副武器";
            case EquipType.Head: return "头部";
            case EquipType.Chest: return "上衣";
            case EquipType.Pants: return "下装";
            case EquipType.Shoes: return "鞋履";
            case EquipType.WaistWaist: return "腰带";
            case EquipType.Bracer: return "护腕";
            case EquipType.Necklace: return "项链";
            case EquipType.Ring: return "戒指";
            case EquipType.Bracelet: return "手镯";
            case EquipType.JadePendant: return "玉佩";
            default: return "装备";
        }
    }

    /// <summary>把过长的属性名压到四列可读长度，只做等价缩写，不改变含义。</summary>
    /// <param name="desc">配置里的原始属性描述。</param>
    /// <returns>不超过四字的展示名，未命中的名称原样返回。</returns>
    internal static string ShortGeneName(string desc)
    {
        if (string.IsNullOrEmpty(desc)) return "—";
        // 命中表里只放常见长名，缩写后语义不变，避免四列各 77 宽时被截断。
        if (desc == "外功攻击最小值") return "外攻下限";
        if (desc == "外功攻击最大值") return "外攻上限";
        if (desc == "内功攻击最小值") return "内攻下限";
        if (desc == "内功攻击最大值") return "内攻上限";
        if (desc == "外功攻击") return "外攻";
        if (desc == "内功攻击") return "内攻";
        if (desc == "命中属性值") return "命中";
        if (desc == "暴击属性值") return "暴击";
        if (desc == "暴击伤害") return "暴伤";
        if (desc == "外功防御") return "外防";
        if (desc == "内功防御") return "内防";
        return desc;
    }

    /// <summary>按穿戴栏位置统计六个套装部位，并生成展开区的逐部位状态。</summary>
    /// <param name="result">当前详情的展示数据，写入套装摘要和展开正文。</param>
    /// <param name="worn">当前角色已同步的穿戴容器，位置使用装备部位枚举值。</param>
    private static void FillSetDetails(EquipmentDetailViewData result, RoItemContainer worn)
    {
        // count 是六个指定部位中已穿戴的件数，范围为零至六。
        int count = 0;
        // status 每行显示两个部位，顺序与 SetParts 相同。
        StringBuilder status = new StringBuilder();
        // index 是套装部位的展示下标，偶数位置开始新的一行。
        for (int index = 0; index < SetParts.Length; index++)
        {
            if (index > 0)
            {
                if (index % 2 == 0)
                {
                    status.Append('\n');
                }
                else
                {
                    status.Append("　　");
                }
            }
            // part 同时决定中文部位名称和穿戴容器中的实际格位。
            EquipType part = SetParts[index];
            status.Append(EquipmentPartName(part)).Append('：');
            if (worn.GetItemByPos((int)part) is EquipBase)
            {
                count++;
                status.Append("已穿戴");
            }
            else
            {
                status.Append("未穿戴");
            }
        }
        result.SetRowText = "套装：件数 " + count + "/" + SetParts.Length;
        result.SetExpandText = status.ToString();
    }

    /// <summary>统计当前穿戴栏中在允许部位启用同族特技的不同装备。</summary>
    /// <param name="skill">同族一级配置，提供稳定族号、允许部位与已接入的基因。</param>
    private int CountWornSpecialSkill(cfg.EquipSpecialSkillInfo skill)
    {
        if (skill.GeneId == 0) return 0;
        // 当前穿戴装备容器，服务器增量后已经是权威状态。
        RoItemContainer container = _itemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack);
        // 用原装备引用的快照按索引遍历。
        List<ItemBase> items = new List<ItemBase>(container.GetItemMap().Values);
        // 同名生效装备件数，品质不参与此计算。
        int count = 0;
        for (int index = 0; index < items.Count; index++)
        {
            // 当前穿戴条目必须是完整装备实例。
            EquipBase equip = items[index] as EquipBase;
            if (equip == null || equip.GetActiveSpecialSkillId() != skill.SpecialSkillId) continue;
            if (EquipBase.IsSpecialSkillPartAllowed(skill.AllowedEquipTypeMask, equip.EquipType)) count++;
        }
        return count;
    }

    /// <summary>按当前件数选取不超过件数的最高等级，并自然封顶配置最高级。</summary>
    /// <param name="familyId">稳定特技族编号。</param>
    /// <param name="pieces">已穿戴且启用同名特技的装备件数，0时展示一级说明。</param>
    private static cfg.EquipSpecialSkillInfo ResolveSpecialSkill(int familyId, int pieces)
    {
        // 未穿戴时用基础一级展示，不推算装备后的组合收益。
        int level = Math.Max(1, pieces);
        // 该族最终选中的配置等级。
        cfg.EquipSpecialSkillInfo selected = null;
        // 真实Luban特技等级行，按Level筛选而非按品质计算。
        List<cfg.EquipSpecialSkillInfo> configs = LubanMgr.Instance.GetEquipSpecialSkillInfos();
        for (int index = 0; index < configs.Count; index++)
        {
            // 当前候选等级记录。
            cfg.EquipSpecialSkillInfo config = configs[index];
            if (config.SpecialSkillId != familyId || config.Level > level) continue;
            if (selected == null || config.Level > selected.Level) selected = config;
        }
        return selected;
    }

    /// <summary>取同一特技族的配置行：指定等级有则返回该行，没有则返回空。</summary>
    /// <param name="familyId">稳定特技族编号。</param>
    /// <param name="level">要取的配置等级，下一层为当前层加一。</param>
    private static cfg.EquipSpecialSkillInfo ResolveSpecialSkillLevel(int familyId, int level)
    {
        // 该族该等级的配置行，缺失时保持空，界面写暂无信息。
        cfg.EquipSpecialSkillInfo matched = null;
        // 真实Luban特技等级行，只按族编号与等级精确匹配。
        List<cfg.EquipSpecialSkillInfo> configs = LubanMgr.Instance.GetEquipSpecialSkillInfos();
        for (int index = 0; index < configs.Count; index++)
        {
            // 当前候选配置行。
            cfg.EquipSpecialSkillInfo config = configs[index];
            if (config.SpecialSkillId != familyId || config.Level != level) continue;
            matched = config;
            break;
        }
        return matched;
    }

    /// <summary>取同一特技族配置里的最高等级，用于标题里的「当前级/满级」。</summary>
    /// <param name="familyId">稳定特技族编号。</param>
    private static int ResolveSpecialSkillMaxLevel(int familyId)
    {
        // 该族已配置的最高等级；没有任何配置时返回0，由调用方转成中性占位。
        int maxLevel = 0;
        // 真实Luban特技等级行。
        List<cfg.EquipSpecialSkillInfo> configs = LubanMgr.Instance.GetEquipSpecialSkillInfos();
        for (int index = 0; index < configs.Count; index++)
        {
            // 当前候选配置行。
            cfg.EquipSpecialSkillInfo config = configs[index];
            if (config.SpecialSkillId != familyId || config.Level <= maxLevel) continue;
            maxLevel = config.Level;
        }
        return maxLevel;
    }

    /// <summary>生成特技说明卡正文，按标题、当前层、下一层、同名说明分区，无数据写暂无信息。</summary>
    /// <param name="skill">当前生效等级配置，空槽传空。</param>
    /// <param name="next">下一层配置，没有下一层时为空。</param>
    /// <param name="maxLevel">该族配置最高等级，0表示没拿到配置。</param>
    private static string BuildSpecialSkillCard(cfg.EquipSpecialSkillInfo skill, cfg.EquipSpecialSkillInfo next, int maxLevel)
    {
        // card 是说明卡全文，分区之间用细横线隔开，和录屏说明卡的层级一致。
        StringBuilder card = new StringBuilder();
        if (skill == null)
        {
            card.Append("<color=#4E7E9C>此槽未镶嵌特技</color>\n");
            card.Append("当前层效果：暂无信息\n");
            card.Append("下一层效果：暂无信息\n");
            card.Append("——");
            return card.ToString();
        }
        // maxText 是标题里的满级数字，没有配置时写占位符。
        string maxText = "—";
        if (maxLevel > 0) maxText = maxLevel.ToString();
        card.Append("<color=#2E6E8E>◇ ").Append(skill.Name).Append(skill.Level).Append("级/").Append(maxText).Append("级</color>\n");
        card.Append("　").Append(skill.Description).Append('\n');
        card.Append("<color=#2E6E8E>◇ 下一层</color>\n");
        if (next == null)
        {
            card.Append("　暂无信息\n");
        }
        else
        {
            card.Append("　").Append(next.Name).Append(next.Level).Append("级：").Append(next.Description).Append('\n');
        }
        card.Append("——\n");
        card.Append("装备带有的多条特技，仅可选1条生效\n");
        card.Append("穿戴包含相同特技的装备可提升该特技的等级");
        return card.ToString();
    }
    #endregion
}
