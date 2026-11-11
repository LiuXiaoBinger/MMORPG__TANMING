using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;

/// <summary>
/// 背包界面控制器，负责将角色物品组件转换成窗口展示数据。
/// </summary>
public sealed class KnapsackCtrl : IDisposable
{

    #region 字段
    /// <summary>计入套装的六个穿戴部位，顺序与详情展开区一致。</summary>
    private static readonly EquipType[] SetParts =
    {
        EquipType.Head, EquipType.Chest, EquipType.WaistWaist,
        EquipType.Pants, EquipType.Shoes, EquipType.Bracer
    };

    /// <summary>背包窗口，只负责渲染和派发页签事件。</summary>
    private readonly KnapsackWindow _window;

    /// <summary>持有并管理装备详情生命周期的现有主视图。</summary>
    private readonly MainView _mainView;
    /// <summary>显示当前装备详情的正式子窗口。</summary>
    private readonly EquipmentDetailWindow _detailWindow;
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
            _mainView = mainView;
            _detailWindow = mainView.EquipmentDetailWindow;
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
            _window.ItemSelected += OpenEquipmentDetail;
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
            _window.ItemSelected -= OpenEquipmentDetail;
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
        if (_role != role)
        {
            _selectedEquipmentUid = 0;
            if (_detailWindow != null) _detailWindow.CloseWindow();
        }
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
    /// <summary>请求卸下当前详情装备，业务结果由服务器回复。</summary>
    private void TakeOffSelectedEquipment() { SubmitSelectedOperation(EquipOperation.TakeOff, 0); }
    /// <summary>请求启用当前装备的指定保存特技槽。</summary>
    /// <param name="index">玩家点击的槽下标0至2。</param>
    private void SwitchSelectedSpecialSkill(int index) { SubmitSelectedOperation(EquipOperation.SwitchSpecialSkill, index); }

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
        if (equip.StrengthenLevel > 0) result.Name += " +" + equip.StrengthenLevel;
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
        int[] baseIds = { config.GeneId0, config.GeneId1, config.GeneId2, config.GeneId3, config.GeneId4, config.GeneId5 };
        // 六个静态基因的策划值，与基础编号保持同序。
        int[] baseValues = { config.GeneValue0, config.GeneValue1, config.GeneValue2, config.GeneValue3, config.GeneValue4, config.GeneValue5 };
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
            valueTarget.Append(baseValues[index]);
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
        cardAttr.Append("<color=#4E7E9C>基础贡献：—　强化贡献：—　进阶贡献：—</color>");
        result.AttributeCardText = cardAttr.ToString();
        // 铭缀与洗练暂无实例数据，写中性空态，不推算任何数值。
        result.InscribeRowText = "暂无信息";
        result.WashRowText = "暂无信息";
        // 宝石孔数取真实配置，必须先于说明文本赋值，否则界面会显示「共 0 孔」。
        result.GemSlotCount = config.GemSlotCount;
        // 镶嵌只报真实孔数与是否已有宝石，属性未知一律写暂无信息。
        result.GemInfoText = "共 " + config.GemSlotCount + " 孔，暂无宝石信息";
        FillSetDetails(result, _itemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack));
        result.ForgeExpandText = "暂无淬锋信息";
        // 三个保存槽逐一展示，只有当前选择且已穿戴的槽计算实际同名件数等级。
        for (int index = 0; index < 3; index++)
        {
            // 槽中保存的是稳定特技族 ID，不是某级配置行 ID。
            int familyId = equip.GetSpecialSkillId(index);
            if (familyId == 0)
            {
                result.SpecialSkillTexts[index] = "特技槽" + (index + 1) + "：空槽，未镶嵌特技";
                // 空槽说明卡只写空态，当前层与下一层都写暂无信息，不伪造效果。
                result.SpecialSkillCards[index] = BuildSpecialSkillCard(null, null, 0);
                // 空槽标题行只写槽位与「空」，不伪造已装配的特技名，也不画占位横线。
                result.SpecialSkillHeadlines[index] = "特技：空";
                result.SpecialSkillCardTitles[index] = "特技" + (index + 1) + "：空槽";
                result.SpecialSkillCardStates[index] = "未镶嵌";
                // 明确标记空槽，界面据此使用浅灰禁用色，避免把「已选但不可切换」误判成空槽。
                result.IsSpecialSkillEmpty[index] = true;
                continue;
            }
            // 本装备当前选择状态，未穿戴时不作为已生效显示。
            bool selected = equip.ActiveSpecialSkillIndex == index;
            // 同名已启用装备的真实件数，不对未启用槽进行换装模拟。
            int pieces = CountWornSpecialSkill(familyId);
            // 当前族的有效等级配置；没有穿戴时显示一级基础说明。
            cfg.EquipSpecialSkillInfo skill = ResolveSpecialSkill(familyId, pieces);
            if (skill == null) continue;
            // 当前槽状态说明，详情层保留完整措辞。
            string state = "未启用";
            if (selected)
            {
                state = "已选，未穿戴";
                if (result.IsWorn) state = "已启用，同名" + pieces + "件";
            }
            // 标题行只放短状态，窄行内不会被省略号截断而丢掉「未穿戴」。
            string shortState = "未启用";
            if (selected)
            {
                shortState = "已选·未穿";
                if (result.IsWorn) shortState = "已启用·" + pieces + "件";
            }
            result.SpecialSkillTexts[index] = skill.Name + " Lv." + skill.Level + "（" + state + "）\n" + skill.Description;
            // 主列表保持视频中的紧凑单行，只有已选槽附带次级状态，完整效果进入侧卡查看。
            result.SpecialSkillHeadlines[index] = "特技：<color=#267B8B>" + skill.Name + " +" + skill.Level + "级</color>";
            if (selected)
            {
                result.SpecialSkillHeadlines[index] += " <size=12><color=#627E94>（" + shortState + "）</color></size>";
            }
            result.SpecialSkillCardTitles[index] = "特技" + (index + 1) + "：" + skill.Name + " +" + skill.Level + "级";
            result.SpecialSkillCardStates[index] = "未使用";
            if (selected)
            {
                result.SpecialSkillCardStates[index] = "已选·未穿";
                if (result.IsWorn) result.SpecialSkillCardStates[index] = "当前生效";
            }
            // 说明卡按区展示：当前层取真实配置等级，下一层取同族下一等级行，缺失写暂无信息。
            int maxLevel = ResolveSpecialSkillMaxLevel(familyId);
            cfg.EquipSpecialSkillInfo nextSkill = ResolveSpecialSkillLevel(familyId, skill.Level + 1);
            result.SpecialSkillCards[index] = BuildSpecialSkillCard(skill, nextSkill, maxLevel);
            result.CanActivateSpecialSkill[index] = !selected;
        }
        // 头部角标只由服务器穿戴状态派生，不根据按钮显隐猜测。
        result.WornBadgeText = "未装备";
        if (result.IsWorn)
        {
            result.WornBadgeText = "装备中";
        }
        // 右上部位与品质、绑定拆成两个独立 TMP；品质与绑定没有真实字段，统一中性占位。
        result.HeaderPartText = EquipmentPartName((EquipType)config.EquipType) + " · 品质 —";
        result.HeaderBindText = "绑定 —";
        // 原元信息行改承担评分与需求，两行都在窄区内。
        result.MetaText = BuildMetaText(config);
        // 附加分区与底栏只用已存在的配置和实例字段；没有来源的统一中性占位，不写录屏数值。
        // 交易与耐久都没有真实字段，未知不等于不可交易，统一中性占位。
        result.FooterText = "交易：—    耐久：—/—";
        result.InfoLayerText = BuildInfoText(config);
        result.FooterExtraText = result.InfoLayerText;
        // 无真实宝石实例时只显示空槽，不断言已镶嵌或未镶嵌。
        result.GemDropText = "镶嵌：空槽";
        // 独立行控件的数据：强化取真实配置，其余保持中性占位；宝石孔数已在前文写入。
        result.EnhanceRowText = "强化 " + equip.StrengthenLevel + "/" + config.MaxStrengthenLevel + "  ·  阶位 —";
        // 淬锋与铭缀是两个子系统，淬锋行只写自身内容，不再拼成一大段混写文字。
        result.ForgeRowText = "淬锋：暂无数据";
        // 祭甲与甲片按用户要求本轮不展示，该行数值不再生成，控件保持隐藏。
        result.GarnishRowText = string.Empty;
        // 特技信息行取第一个真实特技槽文本，空槽时也沿用其「空」说明。
        result.FeatureRowText = result.SpecialSkillTexts[0];
        result.AppearanceRowText = "外观：—  ·  无额外养成";
        return result;
    }

    /// <summary>生成头部元信息：评分与需求等级两行，职业为中性占位，状态由角标单独承载。</summary>
    /// <param name="config">当前装备的静态配置。</param>
    /// <returns>头部元信息的两行展示文本。</returns>
    private static string BuildMetaText(cfg.EquipInfo config)
    {
        // meta 是逐段拼接的头部两行文本，避免为展示数据新建类型。
        StringBuilder meta = new StringBuilder();
        meta.Append("评分 —\n");
        meta.Append("需求 ").Append(config.NeedLevel).Append("级 · 职业 —");
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
    private static string ShortGeneName(string desc)
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

    /// <summary>只统计当前穿戴栏中启用该特技族的装备件数。</summary>
    /// <param name="familyId">特技稳定族编号。</param>
    private int CountWornSpecialSkill(int familyId)
    {
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
            if (equip != null && equip.GetActiveSpecialSkillId() == familyId) count++;
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
