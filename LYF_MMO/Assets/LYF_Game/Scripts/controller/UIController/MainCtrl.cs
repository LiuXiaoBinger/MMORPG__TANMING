using System.Collections.Generic;
using Google.Protobuf;
using MMoRpgCommon;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 主界面的业务入口：创建商城、技能、角色信息、背包、任务和队伍控制器，统一处理相关协议与菜单交互。
/// </summary>
public class MainCtrl : CtrlBase
{

    /// <summary>副本阶段和场景交互窗口的控制器。</summary>
    private readonly DungeonFlowCtrl _dungeonFlowCtrl;
    /// <summary>监听主角死亡并处理主动复活的窗口控制器。</summary>
    private readonly DeathCtrl _deathCtrl;
    /// <summary>开发包主界面持有的 GM 控制器，在销毁时注销回执。</summary>
    private readonly GMCtrl _gmCtrl;
    /// <summary>当前主城收件箱的选择、阅读和领取交互。</summary>
    private readonly EmailHandler _emailHandler;

    #region 字段
    /// <summary>主界面视图集合。</summary>
    private readonly MainView _mainView;

    /// <summary>商城子控制器。</summary>
    private readonly ShopCtrl _shopCtrl;

    /// <summary>技能子控制器。</summary>
    private readonly SkillCtrl _skillCtrl;

    /// <summary>角色信息子控制器。</summary>
    private readonly RoleInfoCtrl _roleInfoCtrl;

    /// <summary>背包角色组件与背包窗口之间的控制器。</summary>
    private readonly KnapsackCtrl _knapsackCtrl;

    /// <summary>货币兑换控制器，依赖背包控制器读写虚拟货币。</summary>
    private readonly CurrencyExchangeCtrl _currencyExchangeCtrl;

    /// <summary>聊天窗口视图。</summary>
    private readonly ChatWindows _chatWindow;
    /// <summary>管理正式聊天窗口的文字及录音请求与回包。</summary>
    private readonly ChatMgr _chatMgr;

    /// <summary>可选江湖聊天模块，复用当前主界面的正式聊天服务。</summary>
    private readonly YimengChatCtrl _yimengChatCtrl;

    /// <summary>任务子控制器，组合任务窗口与快捷任务面板。</summary>
    private readonly TaskCtrl _taskCtrl;
    /// <summary>供当前副本传送回执在应用真实落点后续走本次追踪。</summary>
    internal TaskCtrl Tasks => _taskCtrl;

    /// <summary>主题副本队伍控制器，根据服务器通知刷新队伍，并在入场通知后切换场景。</summary>
    private readonly ThemeDungeonTeamCtrl _themeDungeonTeamCtrl;

    /// <summary>主界面快捷键是否已注册。</summary>
    private bool _mainInputRegistered;

    /// <summary>当前对话关联的 NPC，仅由控制器保存领域对象。</summary>
    private NpcEntity _currentTalkNpc;
    /// <summary>附近交谈扫描复用的场景实体引用，角色切图后随下一次扫描覆盖。</summary>
    private readonly List<MEntity> _nearbyTalkEntities = new List<MEntity>();
    /// <summary>本轮进入交谈距离的所有可见 NPC，同一列表重复用于每次距离检查。</summary>
    private readonly List<NpcEntity> _nearbySpeakers = new List<NpcEntity>();
    /// <summary>提示出现的三维半径，单位米；较短的距离用于区分相邻店铺。</summary>
    private const float NearbyTalkRadius = 4f;
    /// <summary>建筑、墙面和楼板所在的物理层，用于筛掉隔墙或隔层的说话者。</summary>
    private static readonly int TalkGeometryMask = LayerMask.GetMask("Geometry");
    #endregion

    #region 构造、初始化与事件注册
    /// <summary>创建主界面控制器并组合各业务子控制器。</summary>
    /// <param name="view">持有所有主界面子窗口的 MainView。</param>
    public MainCtrl(UIBase view) : base(view)
    {
        _mainView = view as MainView;
        if (_mainView == null)
        {
            return;
        }

        _mainView.InitView();
        if (_mainView.MinMapWindow != null)
        {
            _mainView.MinMapWindow.LineSelected += SelectSceneLine;
            _mainView.MinMapWindow.LinesRequested += RequestSceneLines;
            _mainView.MinMapWindow.PortalSelected += NavigateCityPortal;
            _mainView.MinMapWindow.RegionRequested += OpenAreaMap;
        }
        if (_mainView.AreaMap != null)
        {
            _mainView.AreaMap.TravelRequested += NavigateMapPoint;
            _mainView.AreaMap.StopRequested += StopMapNavigation;
        }
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.State, OnSceneLineState);
        SocketDispatcher.Instance.AddEventHandler(StallCode.Result, OnStallResult);
        SocketDispatcher.Instance.AddEventHandler(StallCode.Settled, OnStallDeal);
        SocketDispatcher.Instance.AddEventHandler(StallCode.StallSellItemCancelResult, OnStallSellItemCancelRsp);
        if (_mainView.TradeMarketWindow != null)
        {
            _mainView.TradeMarketWindow.StallRequested += RequestStall;
            _mainView.TradeMarketWindow.StallDealRequested += SendStallDeal;
            _mainView.TradeMarketWindow.StallCancelRequested += SendStallSellItemCancelReq;
        }
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.List, OnSceneLineList);
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.Change, OnSceneLineChanged);
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.Portal, OnCityPortal);
        _shopCtrl = new ShopCtrl(_mainView);
        _skillCtrl = new SkillCtrl(_mainView);
        _roleInfoCtrl = new RoleInfoCtrl(_mainView);
        _knapsackCtrl = new KnapsackCtrl(_mainView);
        _currencyExchangeCtrl = new CurrencyExchangeCtrl(_mainView, _knapsackCtrl);
        _taskCtrl = new TaskCtrl(_mainView);
        _themeDungeonTeamCtrl = new ThemeDungeonTeamCtrl(_mainView);
        _dungeonFlowCtrl = new DungeonFlowCtrl(_mainView);
        _deathCtrl = new DeathCtrl(_mainView);
        if (_mainView.GM != null) _gmCtrl = new GMCtrl(_mainView);
        if (_mainView.emailPanel != null)
        {
            _emailHandler = new EmailHandler(_mainView.emailPanel, _mainView.emailHint);
            _mainView.emailButton.onClick.AddListener(OpenEmail);
            _emailHandler.BindRole(Global.Instance?.SceneWorld?.LocalRole?.BaseInfo?.RoleId ?? 0);
        }
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            Global.Instance.SceneWorld.LocalRoleChanged += OnLocalRoleChanged;
        }
        BindTrackedRole();
        _chatWindow = _mainView.ChatWindow;
        if (_chatWindow != null) _chatMgr = new ChatMgr(_chatWindow);
        if (_mainView.YimengChat != null)
            _yimengChatCtrl = new YimengChatCtrl(_mainView.YimengChat, _chatWindow, ToggleYimengChat, OnChatInputFocusChanged);
        RegisterCommands();
        RegisterViewActions();
    }

    /// <summary>注册主界面统一接收的协议回包。</summary>
    private void RegisterCommands()
    {
        // 背包快照只由主控制器注册一次，再分发到各业务控制器和模型。
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleKnapsackInfoCode,
            RoleKnapsackInfoHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_OpenKnapsackGridCode,
            OpenKnapsackGridHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_BuyShopItemCode,
            BuyShopItemHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_SC_UpdateItemInfoCode,
            UpdateItemInfoHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_CountUpdateSyncInfoCode,
            CountUpdateSyncInfoHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_EquipOperationCode,
            EquipOperationHandle);
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleAttributeUpdateCode,
            RoleAttributeUpdateHandle);
    }

    /// <summary>注册功能菜单、NPC对话和聊天视图事件。</summary>
    private void RegisterViewActions()
    {
        if (_mainView.NpcTalkPrompt != null)
            _mainView.NpcTalkPrompt.TalkRequested += OnNearbyTalkRequested;
        _mainView.FunctionMenuButton.onClick.AddListener(OnFunctionMenuRequested);
        _mainView.TradeMarketButton.onClick.AddListener(OnTradeMarketRequested);
        _mainView.TreasurePavilionButton.onClick.AddListener(OnTreasurePavilionRequested);
        _mainView.KnapsackButton.onClick.AddListener(OnKnapsackRequested);
        _mainView.RoleButton.onClick.AddListener(OnRoleRequested);
        _mainView.SkillButton.onClick.AddListener(OnSkillRequested);
        _mainView.SettingButton.onClick.AddListener(OnSettingRequested);
        if (_mainView.TaskButton != null)
        {
            _mainView.TaskButton.onClick.AddListener(OnTaskRequested);
        }
        if (_mainView.DailyTaskButton != null)
        {
            _mainView.DailyTaskButton.onClick.AddListener(OnDailyTaskRequested);
        }
        if (_mainView.ThemeDungeonButton != null)
        {
            _mainView.ThemeDungeonButton.onClick.AddListener(OnThemeDungeonRequested);
        }
        // 底栏安全入口绑定竞技设置窗口的切换处理。
        if (_mainView.RoleStatusFooter != null && _mainView.RoleStatusFooter.SafetyButton != null)
        {
            _mainView.RoleStatusFooter.SafetyButton.onClick.AddListener(OnSafetyButtonClicked);
        }
        if (_mainView.CombatStatusButton != null)
        {
            _mainView.CombatStatusButton.onClick.AddListener(OnCombatStatusClicked);
        }
        if (_mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested += OnNpcServiceRequested;
            _mainView.TalkWindow.MoshentaBattleRequested += OnMoshentaBattleRequested;
            _mainView.TalkWindow.TalkLeaveRequested += CloseNpcTalk;
            _mainView.TalkWindow.Closed += OnNpcTalkClosed;
        }
        if (_chatWindow != null)
        {
            _chatWindow.InputFocusChanged += OnChatInputFocusChanged;
        }
    }

    /// <summary>注册主界面快捷键；已注册时不重复订阅，同时重新绑定小地图和技能输入。</summary>
    public void RegisterMainInput()
    {
        // 主角可能晚于主界面控制器创建，因此每次主 UI 注册时重新绑定小地图。
        BindTrackedRole();
        _skillCtrl.RegisterInput();
        if (_mainInputRegistered || PlayerInputCtr.Instance == null)
        {
            return;
        }
        PlayerInputCtr.Instance.MainUIKeyHandler += MainUIKeyHandler;
        _mainInputRegistered = true;
    }

    /// <summary>注销主控制器持有的全部协议、输入和视图事件。</summary>
    public override void Dispose()
    {
        if (_mainView != null && _mainView.MinMapWindow != null)
        {
            _mainView.MinMapWindow.LineSelected -= SelectSceneLine;
            _mainView.MinMapWindow.LinesRequested -= RequestSceneLines;
            _mainView.MinMapWindow.PortalSelected -= NavigateCityPortal;
            _mainView.MinMapWindow.RegionRequested -= OpenAreaMap;
        }
        if (_mainView != null && _mainView.AreaMap != null)
        {
            _mainView.AreaMap.TravelRequested -= NavigateMapPoint;
            _mainView.AreaMap.StopRequested -= StopMapNavigation;
        }
        if (_mainView.NpcTalkPrompt != null)
        {
            _mainView.NpcTalkPrompt.TalkRequested -= OnNearbyTalkRequested;
            _mainView.NpcTalkPrompt.SetSpeakers(null);
        }
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.State, OnSceneLineState);
        SocketDispatcher.Instance.RemoveEventHandler(StallCode.Result, OnStallResult);
        SocketDispatcher.Instance.RemoveEventHandler(StallCode.Settled, OnStallDeal);
        SocketDispatcher.Instance.RemoveEventHandler(StallCode.StallSellItemCancelResult, OnStallSellItemCancelRsp);
        if (_mainView.TradeMarketWindow != null)
        {
            _mainView.TradeMarketWindow.StallRequested -= RequestStall;
            _mainView.TradeMarketWindow.StallDealRequested -= SendStallDeal;
            _mainView.TradeMarketWindow.StallCancelRequested -= SendStallSellItemCancelReq;
        }
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.List, OnSceneLineList);
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.Change, OnSceneLineChanged);
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.Portal, OnCityPortal);
        _yimengChatCtrl?.Dispose();
        _dungeonFlowCtrl?.Dispose();
        _mainView.FunctionMenuButton.onClick.RemoveListener(OnFunctionMenuRequested);
        _mainView.TradeMarketButton.onClick.RemoveListener(OnTradeMarketRequested);
        _mainView.TreasurePavilionButton.onClick.RemoveListener(OnTreasurePavilionRequested);
        _mainView.KnapsackButton.onClick.RemoveListener(OnKnapsackRequested);
        _mainView.RoleButton.onClick.RemoveListener(OnRoleRequested);
        _mainView.SkillButton.onClick.RemoveListener(OnSkillRequested);
        _mainView.SettingButton.onClick.RemoveListener(OnSettingRequested);
        if (_mainView != null && _mainView.TaskButton != null)
        {
            _mainView.TaskButton.onClick.RemoveListener(OnTaskRequested);
        }
        if (_mainView != null && _mainView.DailyTaskButton != null)
        {
            _mainView.DailyTaskButton.onClick.RemoveListener(OnDailyTaskRequested);
        }
        if (_mainView != null && _mainView.ThemeDungeonButton != null)
        {
            _mainView.ThemeDungeonButton.onClick.RemoveListener(OnThemeDungeonRequested);
        }
        // 解绑底栏安全入口，避免控制器销毁后残留点击监听。
        if (_mainView != null && _mainView.RoleStatusFooter != null && _mainView.RoleStatusFooter.SafetyButton != null)
        {
            _mainView.RoleStatusFooter.SafetyButton.onClick.RemoveListener(OnSafetyButtonClicked);
        }
        if (_mainView.CombatStatusButton != null)
        {
            _mainView.CombatStatusButton.onClick.RemoveListener(OnCombatStatusClicked);
        }
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_RoleKnapsackInfoCode, RoleKnapsackInfoHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_OpenKnapsackGridCode, OpenKnapsackGridHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_BuyShopItemCode, BuyShopItemHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_SC_UpdateItemInfoCode, UpdateItemInfoHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_CountUpdateSyncInfoCode, CountUpdateSyncInfoHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_EquipOperationCode, EquipOperationHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_RoleAttributeUpdateCode, RoleAttributeUpdateHandle);
        _mainView.RoleShieldWidget?.Clear();
        if (_mainInputRegistered && PlayerInputCtr.Instance != null)
        {
            PlayerInputCtr.Instance.MainUIKeyHandler -= MainUIKeyHandler;
            _mainInputRegistered = false;
        }
        if (_mainView != null && _mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested -= OnNpcServiceRequested;
            _mainView.TalkWindow.MoshentaBattleRequested -= OnMoshentaBattleRequested;
            _mainView.TalkWindow.TalkLeaveRequested -= CloseNpcTalk;
            _mainView.TalkWindow.Closed -= OnNpcTalkClosed;
        }
        if (_chatWindow != null)
        {
            _chatWindow.InputFocusChanged -= OnChatInputFocusChanged;
            _chatMgr.Dispose();
        }
        if (_shopCtrl != null)
        {
            _shopCtrl.Dispose();
        }
        if (_skillCtrl != null)
        {
            _skillCtrl.Dispose();
        }
        if (_roleInfoCtrl != null)
        {
            _roleInfoCtrl.Dispose();
        }
        if (_knapsackCtrl != null)
        {
            _knapsackCtrl.Dispose();
        }
        if (_currencyExchangeCtrl != null)
        {
            _currencyExchangeCtrl.Dispose();
        }
        if (_taskCtrl != null)
        {
            _taskCtrl.Dispose();
        }
        if (_themeDungeonTeamCtrl != null)
        {
            _themeDungeonTeamCtrl.Dispose();
        }
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            Global.Instance.SceneWorld.LocalRoleChanged -= OnLocalRoleChanged;
        }
        _gmCtrl?.Dispose();
        _deathCtrl.Dispose();
        if (_emailHandler != null)
        {
            _mainView.emailButton.onClick.RemoveListener(OpenEmail);
            _emailHandler.Dispose();
        }
        if (_currentTalkNpc != null) CloseNpcTalk();
        OnNpcTalkClosed();
    }
    #endregion

    #region 界面显示与开关
    /// <summary>展开或收起江湖聊天，沿主界面既有窗口路由管理显隐。</summary>
    private void ToggleYimengChat()
    {
        ShowMainWindow(WindowType.YimengChatWindow);
    }

    /// <summary>对话窗口先把 NPC 转为展示数据，商店交给 ShopCtrl 准备商品；其他窗口沿用基类显隐流程。</summary>
    /// <param name="windowType">需要切换的主界面子窗口。</param>
    /// <param name="obj">窗口数据；对话和商店入口要求传入 NpcEntity。</param>
    public override void ShowMainWindow(WindowType windowType, object obj = null)
    {
        if (windowType == WindowType.TalkWindow)
        {
            // 窗口基类沿用 object，实际交互始终以当前点击的 NPC 为准。
            NpcEntity speaker = obj as NpcEntity;
            if (speaker == null || !speaker.Model.IsLoaded)
            {
                CloseNpcTalk();
                return;
            }
            _currentTalkNpc = speaker;
            _mainView.NpcTalkPrompt?.SetSpeakers(null);
            // 切换说话者时刷新正在显示的窗口，避免通用窗口入口将它反向关闭。
            NpcTalkViewData dialogue = CreateNpcTalkViewData(speaker);
            if (_mainView.TalkWindow.gameObject.activeSelf) _mainView.TalkWindow.ReFreshUI(dialogue);
            else base.ShowMainWindow(windowType, dialogue);
            if (!speaker.IsSceneObject) CameraMgr.Instance?.BeginNpcTalk(speaker, CloseNpcTalk);
            return;
        }
        if (windowType == WindowType.TreasurePavilionWindow)
        {
            // 珍宝阁只读取本角色的背包和等级，操作意图由窗口事件传出。
            PlayerEntity role = Global.Instance?.SceneWorld?.LocalRole;
            // 未加载角色时仍可浏览公开陈列。
            RoItemComponent inventory = null;
            if (role != null) role.TryGetComponent(out inventory);
            _mainView.TreasurePavilionWindow.BindRole(inventory, role?.BaseInfo?.Level ?? 0);
        }
        if (windowType == WindowType.TradeMarketWindow)
        {
            // 交易目录和出售包裹读取本地角色当前的背包组件。
            PlayerEntity role = Global.Instance?.SceneWorld?.LocalRole;
            RoItemComponent items = null;
            if (role != null) role.TryGetComponent(out items);
            _mainView.TradeMarketWindow.BindInventory(items);
        }
        if (windowType == WindowType.ShopWindow)
        {
            _shopCtrl.OpenNpcShop(obj as NpcEntity);
            return;
        }
        base.ShowMainWindow(windowType, obj);
    }

    /// <summary>使用服务端返回的容量刷新对应背包分类。</summary>
    /// <param name="data">扩容回包，包含背包分类和服务端确认的已开格数。</param>
    private void OpenKnapsackGridHandle(ByteString data)
    {
        OpenKnapsackGridRet result = OpenKnapsackGridRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed)
        {
            return;
        }

        // 接收扩容结果的本地玩家背包组件。
        RoItemComponent itemComponent;
        // 当前场景中的本地玩家；角色尚未创建或已被断线清理时为空。
        PlayerEntity localRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        if (localRole != null && localRole.TryGetComponent(out itemComponent))
        {
            itemComponent.SetOpenedGridCount((KnapsackType)result.KnapsackType,
                result.CurrentOpenedGridCount);
        }
    }
    #endregion

    #region 界面刷新
    /// <summary>重画任务主窗口和快捷任务面板；这里只读取现有 TaskMgr 数据，不请求或装载服务端数据。</summary>
    public void RefreshTaskUI()
    {
        if (_taskCtrl == null)
        {
            return;
        }
        _taskCtrl.RefreshAll();
    }

    /// <summary>接收服务器物品增量并交给角色背包组件更新客户端模型。</summary>
    /// <param name="data">服务端推送的单次物品增量。</param>
    private void UpdateItemInfoHandle(ByteString data)
    {
        // 本次物品增量回包，成功后更新当前角色的背包数据。
        UpdateItemRet result = UpdateItemRet.Parser.ParseFrom(data);

        if (result == null || result.CmdCode != CmdCode.Succeed || result.ItemDate == null)
        {
            return;
        }

        PlayerEntity localRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        if (localRole == null)
        {
            return;
        }

        RoItemComponent itemComponent;
        if (localRole.TryGetComponent(out itemComponent))
        {
            itemComponent.ApplyServerItemDelta(result.ItemDate);
            _roleInfoCtrl.RefreshEquipment(itemComponent);
            _knapsackCtrl.RefreshEquipmentDetail();
        }
    }

    /// <summary>装备操作回包只报告失败；成功后的模型由物品和属性通知更新。</summary>
    /// <param name="data">服务器返回的 EquipOperationRet 消息体。</param>
    private void EquipOperationHandle(ByteString data)
    {
        // 直接解析协议，不吞掉协议错误。
        EquipOperationRet result = EquipOperationRet.Parser.ParseFrom(data);
        if (result.Operation == EquipOperation.GemMarket || result.Operation == EquipOperation.ComposeGem || result.Operation == EquipOperation.BuyGem)
        {
            _knapsackCtrl.ApplyGemTrade(result);
            return;
        }
        if (result.CmdCode != CmdCode.Succeed)
        {
            TipsMgr.Instance.ShowSystemTips("装备操作失败：" + result.CmdCode);
        }
    }

    /// <summary>应用服务器最终属性，同时刷新实体资源和现有角色窗口。</summary>
    /// <param name="data">包含最终属性、护盾数值与基因完整或增量列表的权威通知。</param>
    private void RoleAttributeUpdateHandle(ByteString data)
    {
        // 服务器已结算装备与基因，客户端直接采用最终属性。
        RoleAttributeUpdateNtf result = RoleAttributeUpdateNtf.Parser.ParseFrom(data);
        // 当前本地角色是此通知的唯一应用目标。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null || !localRole.ApplyAttributeUpdate(result)) return;
        // 旧属性通知没有经验字段，登录时的本级经验仍然有效。
        if (result.HasExp) localRole.RoleInfo.Exp = result.Exp;
        _mainView.RoleShieldWidget?.SetShield(localRole, result.ShieldRemaining, result.ShieldCapacity);
        _mainView.CombatStatus?.SetShield(result.ShieldRemaining, result.ShieldCapacity);
        // 兼容已有攻击入口通过角色控制器读取基础属性的流程。
        RoleCtrlBase controller = localRole.GetEntityCtrl();
        if (controller != null) controller.InitCtrl(controller._roleType, localRole.BaseInfo);
        _roleInfoCtrl.RefreshCurrentRole();
    }
    #endregion

    #region 交互响应
    /// <summary>角色切换或断线清理时重新绑定背包控制器。</summary>
    /// <param name="role">新的本地角色，null 表示角色已离场。</param>
    private void OnLocalRoleChanged(PlayerEntity role)
    {
        _emailHandler?.BindRole(role?.BaseInfo?.RoleId ?? 0);
        _mainView.RoleShieldWidget?.Clear();
        _mainView.CombatStatus?.SetShield(0, 0);
        _roleInfoCtrl.BindGeneRole(role);
        if (_knapsackCtrl != null)
        {
            _knapsackCtrl.BindRole(role);
        }
        BindTrackedRole();
    }

    /// <summary>聊天获得焦点时调用输入控制器 OnDisable，失焦时调用 OnEnable；不修改组件 enabled 属性。</summary>
    /// <param name="focused">聊天框是否正在接收键盘输入。</param>
    private void OnChatInputFocusChanged(bool focused)
    {
        if (PlayerInputCtr.Instance == null)
        {
            return;
        }
        if (focused)
        {
            PlayerInputCtr.Instance.OnDisable();
        }
        else
        {
            PlayerInputCtr.Instance.OnEnable();
        }
    }
    #endregion

    #region 事件回调、创建与添加等
    /// <summary>经游戏网关提交玩家确认的摆摊操作。</summary><param name="request">背包堆或挂牌和本次数量、单价。</param>
    private void SendStallDeal(StallDealArg request)
    {
        MNetClient.Instance.Send(StallCode.Deal, request.ToByteString());
    }

    /// <summary>将已结算的回执交给打开的商城。</summary><param name="data">网关返回的摆摊结算结果。</param>
    private void OnStallDeal(ByteString data)
    {
        _mainView.TradeMarketWindow?.OnStallDeal(StallDeal.Parser.ParseFrom(data));
    }

    /// <summary>用当前网关连接发起玩家摆摊查询，角色身份由服务端确定。</summary><param name="query">窗口选择的视图、物品与游标。</param>
    private void RequestStall(StallQuery query)
    {
        MNetClient.Instance.Send(StallCode.Browse, query.ToByteString());
    }

    /// <summary>参照 RO 的卖家撤单请求，只传选中行的挂牌号。</summary><param name="listing">本次要撤下的挂牌号。</param>
    private void SendStallSellItemCancelReq(long listing)
    {
        MNetClient.Instance.Send(StallCode.StallSellItemCancel, new StallSellItemCancelArg { ListingId = listing }.ToByteString());
    }

    /// <summary>将中心服确认的撤单结果交回出售页。</summary><param name="data">网关回送的撤单结果。</param>
    private void OnStallSellItemCancelRsp(ByteString data)
    {
        _mainView.TradeMarketWindow?.OnStallSellItemCancelRsp(StallSellItemCancelRes.Parser.ParseFrom(data));
    }

    /// <summary>把网关返回的本服市场结果交给当前窗口。</summary><param name="data">市场查询回包。</param>
    private void OnStallResult(ByteString data)
    {
        _mainView.TradeMarketWindow?.ApplyStall(StallResult.Parser.ParseFrom(data));
    }

    /// <summary>从主城功能菜单切换珍宝阁。</summary>
    private void OnTreasurePavilionRequested()
    {
        ShowMainWindow(WindowType.TreasurePavilionWindow);
    }

    /// <summary>从主城信封入口走既有 WindowType 显隐流程。</summary>
    private void OpenEmail() { ShowMainWindow(WindowType.EmailPanel); }

    /// <summary>从主城功能菜单打开或收起交易窗口。</summary>
    private void OnTradeMarketRequested()
    {
        ShowMainWindow(WindowType.TradeMarketWindow);
    }

    /// <summary>切换左上入口组，展开和收起只修改视图展示。</summary>
    private void OnFunctionMenuRequested()
    {
        _mainView.ToggleFunctionMenu();
    }

    /// <summary>点击背包时保持菜单展开，沿B键原流程切换背包及调整镜头。</summary>
    private void OnKnapsackRequested()
    {
        MainUIKeyHandler("B");
    }

    /// <summary>点击角色时保持菜单展开，沿I键原流程切换属性及调整镜头。</summary>
    private void OnRoleRequested()
    {
        MainUIKeyHandler("I");
    }

    /// <summary>点击技能时保持菜单展开，沿L键原流程切换技能窗口。</summary>
    private void OnSkillRequested()
    {
        MainUIKeyHandler("L");
    }

    /// <summary>点击设置时保持菜单展开，切换既有设置窗口。</summary>
    private void OnSettingRequested()
    {
        ShowMainWindow(WindowType.SettingPanel);
    }

    /// <summary>点击任务入口时经任务控制器联动展开快捷面板并切换任务窗口显隐。</summary>
    private void OnTaskRequested()
    {
        if (_taskCtrl == null)
        {
            return;
        }
        _taskCtrl.RequestOpenTaskWindow();
    }

    /// <summary>点击日常入口时切换日常任务窗口显隐，日常面板独立于任务主窗口。</summary>
    private void OnDailyTaskRequested()
    {
        ShowMainWindow(WindowType.DailyTaskWindow);
    }

    /// <summary>点击副本入口时打开主题副本选择窗口，窗口实例由 MainView 注册。</summary>
    private void OnThemeDungeonRequested()
    {
        ShowMainWindow(WindowType.ThemeDungeonArroundWindow);
    }

    /// <summary>点击底栏安全入口时切换竞技设置窗口，窗口仅展示外观选项。</summary>
    private void OnSafetyButtonClicked()
    {
        ShowMainWindow(WindowType.ArenaSettingsWindow);
    }

    /// <summary>点击战斗 HUD 的状态图标时，通过主视图开关状态详情。</summary>
    private void OnCombatStatusClicked()
    {
        ShowMainWindow(WindowType.CombatStatusWindow);
    }

    /// <summary>列出交谈距离内的每个可见人物，交由窗口按姓名分别显示。</summary>
    internal void RefreshNearbyTalkPrompt()
    {
        // 角色卸载、正在说话或浏览货架时隐藏世界交互入口。
        SceneWorldSystem world = Global.Instance?.SceneWorld;
        if (world?.LocalRole == null || !world.LocalRole.Model.IsLoaded || _currentTalkNpc != null)
        {
            _mainView.NpcTalkPrompt.SetSpeakers(null);
            return;
        }
        if (_mainView.ShopWindow != null && _mainView.ShopWindow.gameObject.activeInHierarchy)
        {
            _mainView.NpcTalkPrompt.SetSpeakers(null);
            return;
        }

        // 使用主角的实际世界坐标，距离比较同时计入楼层高差。
        Vector3 origin = world.LocalRole.Position;
        _nearbySpeakers.Clear();
        world.CopyEntities(_nearbyTalkEntities);
        for (int index = 0; index < _nearbyTalkEntities.Count; index++)
        {
            // 只从已实例化的 NPC 中挑选，角色、怪物和场景特效跳过。
            NpcEntity npc = _nearbyTalkEntities[index] as NpcEntity;
            if (npc == null || !npc.Model.IsLoaded) continue;
            // 移动中的巡卫使用当前模型位置。
            float distance = (npc.Position - origin).sqrMagnitude;
            if (distance > NearbyTalkRadius * NearbyTalkRadius) continue;
            if (CanSpeakToNearbyNpc(npc, origin)) _nearbySpeakers.Add(npc);
        }
        _mainView.NpcTalkPrompt.SetSpeakers(_nearbySpeakers);
    }

    /// <summary>检查面前的实体是否属于当前场景、能说话且未被建筑挡住。</summary>
    /// <param name="npc">已载入模型的候选居民或有车夫的马车。</param>
    /// <param name="origin">本地玩家脚下的世界坐标，单位米。</param>
    private bool CanSpeakToNearbyNpc(NpcEntity npc, Vector3 origin)
    {
        if (npc.Model.GameObject.NativeObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) return false;
        if (npc.IsSceneObject && !_mainView.TalkWindow.Catalog.HasNearbySpeaker(npc.NpcID)) return false;
        // 在胸口高度检测建筑遮挡，允许看见矮台或台阶边站立的人。
        Vector3 chest = Vector3.up * 1.15f;
        return !Physics.Linecast(origin + chest, npc.Position + chest, TalkGeometryMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>按下交谈时核对屏幕上那位说话者，再进入与点击模型共用的导航事件。</summary>
    /// <param name="speaker">实际点到的按钮所显示的 NPC，不随其他人的距离变化。</param>
    private void OnNearbyTalkRequested(NpcEntity speaker)
    {
        // 玩家和巡卫可在两次提示刷新之间移动，因此按实际位置确认本次点击。
        PlayerEntity player = Global.Instance?.SceneWorld?.LocalRole;
        if (player == null || speaker == null || !speaker.Model.IsLoaded) return;
        if ((speaker.Position - player.Position).sqrMagnitude > NearbyTalkRadius * NearbyTalkRadius ||
            !CanSpeakToNearbyNpc(speaker, player.Position))
        {
            RefreshNearbyTalkPrompt();
            return;
        }
        TouchObject.singleton.ClickNpc(speaker);
    }

    /// <summary>角色到达交互范围后，让当前 NPC 面向角色并打开既有对话窗。</summary>
    /// <param name="npc">本次点击并走近的场景 NPC 实体。</param>
    internal void OpenNpcTalk(NpcEntity npc)
    {
        // 导航到达时仍处于本地场景中的主角，位置以实体实际模型为准。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        if (player == null || !player.Model.IsLoaded)
        {
            TipsMgr.Instance.ShowSystemTips("主角尚未创建完成");
            return;
        }
        if (Vector3.Distance(player.Position, npc.Position) > NpcEntity.TalkDistance)
        {
            TipsMgr.Instance.ShowSystemTips("距离过远，请靠近");
            return;
        }
        // 固定设施维持朝向；巡卫停步和转身由 GS 统一通知。
        if (!npc.IsSceneObject)
        {
            if (npc.Model.GameObject.Transform.TryGetComponent(out MSceneNpcPatrol patrol))
                patrol.SetTalking(true);
            else npc.Model.GameObject.Transform.LookAtTarget(player.Model.GameObject.Transform);
        }
        ShowMainWindow(WindowType.TalkWindow, npc);
    }

    /// <summary>按对话选中的功能进入商店、委托窗口或场景传送。</summary>
    /// <param name="service">按钮配置的功能、货架或本图门号。</param>
    private void OnNpcServiceRequested(NpcDialogueCatalog.Service service)
    {
        if (_currentTalkNpc == null)
        {
            TipsMgr.Instance.ShowSystemTips("当前没有可交互的 NPC");
            return;
        }
        // 关窗会释放当前说话者，先保留货架需要的实体。
        NpcEntity merchant = _currentTalkNpc;
        if (service.Function == NpcFunction.QuestAction)
        {
            _taskCtrl.ConfirmNpcQuest(merchant, service.QuestId);
            CloseNpcTalk();
            return;
        }
        if (service.Function == NpcFunction.Task)
        {
            ShowNpcCommissions(merchant);
            return;
        }
        CloseNpcTalk();
        switch (service.Function)
        {
            case NpcFunction.PropertyShop:
                _shopCtrl.OpenNpcShop(merchant, service);
                break;
            case NpcFunction.TaskJournal:
                _taskCtrl.RequestOpenTaskWindow();
                break;
            case NpcFunction.Transfer:
                MNetClient.Instance.Send(SceneLineCode.Portal,
                    new ScenePortalRequest { DoorId = service.DoorId }.ToByteString());
                break;
        }
    }

    /// <summary>以接取和交付 NPC 编号查询委托，只展示策划表中与说话者有关的条目。</summary>
    /// <param name="npc">当前对话的真实人物，使用稳定的出生表编号。</param>
    private void ShowNpcCommissions(NpcEntity npc)
    {
        // 任务模板用于介绍，实际接取、完成和奖励仍由任务系统决定。
        Dictionary<int, cfg.QuestInfo> quests = LubanMgr.Instance.GetQuestInfos();
        // 以任务编号顺序翻页，委托入口在尚未配置剧情时也可使用。
        List<int> ids = new List<int>(quests.Keys);
        ids.Sort();
        List<string> lines = new List<string>();
        for (int i = 0; i < ids.Count; i++)
        {
            // 一项委托可由本 NPC 发布，也可在本 NPC 处交付。
            cfg.QuestInfo quest = quests[ids[i]];
            if (quest.AcceptNpcId == npc.NpcID)
                lines.Add("发布委托 · " + quest.Name + "\n" + quest.Description);
            else if (quest.SubmitNpcId == npc.NpcID)
                lines.Add("交付委托 · " + quest.Name + "\n" + quest.Description);
        }
        if (lines.Count == 0) lines.Add("我这里暂时没有委托。");
        // 查看本人的委托后仍可进入已有任务簿查看角色的任务进度。
        NpcDialogueCatalog.Service journal = new NpcDialogueCatalog.Service
            { Label = "打开任务簿", Function = NpcFunction.TaskJournal };
        _mainView.TalkWindow.ReFreshUI(new NpcTalkViewData(npc.NpcID, npc.Name,
            string.Join("_", lines), services: _taskCtrl.GetQuestServices(npc.NpcID, new[] { journal })));
    }

    /// <summary>仅原NPC20186帮助选项发送BossEnterCombat，黑幕结束不调用此入口。</summary>
    private void OnMoshentaBattleRequested()
    {
        if (!IsMoshentaBossNpc(_currentTalkNpc)) return;
        // 对应原NPC/2000000.txt的danatuosi_boss_enter_combat，后续状态由SceneAI原树决定。
        TeamCtrl.RequestDungeonCommandEvent("BossEnterCombat");
        CloseNpcTalk();
    }

    /// <summary>沿原stoptalk/end语义结束当前对话，不自动推进副本条件。</summary>
    private void CloseNpcTalk()
    {
        _mainView.TalkWindow.CloseWindow();
    }

    /// <summary>对话从任何路径隐藏时归还世界相机，并释放本次说话者。</summary>
    private void OnNpcTalkClosed()
    {
        CameraMgr.Instance?.EndNpcTalk();
        // 正常关闭、Esc 或外部切场都会从这个窗口回调释放巡卫。
        if (_currentTalkNpc != null && _currentTalkNpc.Model.IsLoaded)
        {
            if (_currentTalkNpc.Model.GameObject.Transform.TryGetComponent(out MSceneNpcPatrol patrol))
                patrol.SetTalking(false);
        }
        _currentTalkNpc = null;
    }

    /// <summary>识别本次原魔神塔波47的同一NPC实例，普通NPC保持已有商店/任务分支。</summary>
    /// <param name="npc">当前真实交互的NPC领域实体。</param>
    private static bool IsMoshentaBossNpc(NpcEntity npc)
    {
        if (npc == null || npc.NpcID != 20186 || npc.LifecycleState != EntityLifecycleState.Active) return false;
        if (TeamData.Instance.DungeonId != 4060) return false;
        return npc.DungeonWaveId == 47;
    }

    /// <summary>把 NPC 领域对象转换为对话展示数据。</summary>
    /// <param name="npcEntity">当前交互 NPC，null 时返回空数据。</param>
    private NpcTalkViewData CreateNpcTalkViewData(NpcEntity npcEntity)
    {
        if (npcEntity == null)
        {
            return null;
        }
        if (IsMoshentaBossNpc(npcEntity))
        {
            // 原NPC/2000000.txt:596的正文与597的两个选项，不执行无关剧情脚本。
            return new NpcTalkViewData(npcEntity.NpcID, npcEntity.Name,
                "啊啊！！啊啊啊啊！！！我好痛苦，让我死个痛快！！！", true);
        }
        // 已单独配置的 NPC 使用自己的服务顺序，旧商人保留默认货架。
        NpcDialogueCatalog.Service[] services = _mainView.TalkWindow.Catalog.GetServices(npcEntity.NpcID, npcEntity.ShopId);
        return new NpcTalkViewData(npcEntity.NpcID, npcEntity.Name, npcEntity.Talk,
            services: _taskCtrl.GetQuestServices(npcEntity.NpcID, services));
    }

    /// <summary>接收服务器次数增量并写入当前角色次数组件。</summary>
    /// <param name="data">服务器下发的次数计数更新。</param>
    private void CountUpdateSyncInfoHandle(ByteString data)
    {
        // 服务器推送的次数增量，用于同步当前角色的次数组件。
        CountUpdateSyncInfo result = CountUpdateSyncInfo.Parser.ParseFrom(data);
        if (result == null)
        {
            return;
        }
        PlayerEntity localRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        if (localRole == null)
        {
            return;
        }
        RoleCountComponent countComponent;
        if (localRole.TryGetComponent(out countComponent))
        {
            countComponent.ApplyServerSync(result);
        }
    }

    /// <summary>将技能协议回包交给正式 11080 解析和当前角色装载链。</summary>
    /// <param name="payload">已序列化的 RoleSkillInfoRet protobuf 消息体。</param>
    /// <returns>协议有效且回包角色与本地主角匹配时返回 true。</returns>
    public bool LoadSkillResponse(ByteString payload)
    {
        if (_skillCtrl == null)
        {
            return false;
        }
        return _skillCtrl.ApplyRoleSkillInfoPayload(payload);
    }
    #endregion

    #region 核心逻辑
    /// <summary>在主角创建完成后把跟踪对象交给小地图 View。</summary>
    private void BindTrackedRole()
    {
        if (_mainView == null || _mainView.MinMapWindow == null || Global.Instance == null)
        {
            return;
        }
        // 小地图直接跟踪场景世界中的本地玩家实体，不再依赖旧 MainRoleCtrl。
        PlayerEntity localRole = null;
        if (Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        _mainView.MinMapWindow.SetTrackedRole(localRole);
        if (_mainView.AreaMap != null) _mainView.AreaMap.Track(localRole);
    }

    /// <summary>沿既有主视图窗口栈打开当前地区的地图。</summary>
    private void OpenAreaMap()
    {
        ShowMainWindow(WindowType.AreaMapWindow);
    }

    /// <summary>从点图位置取得可走地面，沿本地玩家已有的导航事件移动。</summary>
    /// <param name="point">点在区域图上的位置，XZ 是场景米制坐标。</param>
    private void NavigateMapPoint(Vector3 point)
    {
        // 导航与手动移动使用同一个本地角色及同一张场景导航网。
        PlayerEntity role = Global.Instance.SceneWorld.LocalRole;
        if (role == null) return;
        // 俯视图没有高度；先沿实际地形取高度，未命中碰撞体时沿角色当前的步行高度找最近面。
        point.y = role.Position.y;
        if (MNavigationMgr.singleton.TryRaycastTerrain(point + Vector3.up * 512f,
            point + Vector3.down * 512f, out Vector3 ground)) point.y = ground.y;
        if (!MNavigationMgr.singleton.TrySetHeight(ref point))
        {
            _mainView.AreaMap.ShowUnreachable();
            return;
        }
        role.DispatchEvent(MEventType.MEvent_Navigation, MNavigationEventArgs.Create(point));
        if (!role.NavComp.IsNavigation && (role.Position - point).sqrMagnitude > 1f)
            _mainView.AreaMap.ShowUnreachable();
    }

    /// <summary>中止已有角色组件持有的路线，主角留在当前位置。</summary>
    private void StopMapNavigation()
    {
        // 由移动组件切回待机，与键盘中止寻路的行为一致。
        PlayerEntity role = Global.Instance.SceneWorld.LocalRole;
        if (role != null && role.NavComp != null) role.NavComp.Interrupt();
    }

    /// <summary>点击传送标记后沿实际场景导航走到门心，触碰时再传送。</summary>
    /// <param name="point">传送门在导航平台上的世界坐标。</param>
    private void NavigateCityPortal(Vector3 point)
    {
        // 这里已知门所在楼层，不从空中向下射线，避免选中上一层屋顶。
        PlayerEntity role = Global.Instance.SceneWorld.LocalRole;
        if (role == null) return;
        role.DispatchEvent(MEventType.MEvent_Navigation, MNavigationEventArgs.Create(point));
    }

    /// <summary>收到游戏服正式接管的城市后，用现有场景加载器携带本地角色进城。</summary>
    /// <param name="data">包含目的地图与线路的成功回执，或本次拒绝原因。</param>
    private void OnCityPortal(ByteString data)
    {
        // 地图编号由实际接管玩家的区服返回。
        SceneLineResult result = SceneLineResult.Parser.ParseFrom(data);
        if (result.Reason.Length > 0)
        {
            _taskCtrl.CancelQuestRoute();
            TipsMgr.Instance.ShowSystemTips(result.Reason);
            return;
        }
        if (result.Lines.Count == 0) return;
        // 服务端确认的目的地图及将由场景加载器携带的常驻玩家。
        int mapId = result.Lines[0].MapId;
        PlayerEntity role = Global.Instance.SceneWorld.LocalRole;
        role.BaseInfo.MapId = mapId;
        // 客户端和 GS 使用同一个回执落点，从室内出来时站在刚离开的那栋建筑外。
        if (result.ArrivalPosition.Length > 0) role.BaseInfo.Pos = result.ArrivalPosition;
        SceneMgr.Instance.LoadPublicMap(mapId, result.ArrivalPosition, _taskCtrl.OnQuestMapLoaded);
    }

    /// <summary>从小地图提交所选线路；地图和角色归属由区服确定。</summary><param name="line">目标线路编号。</param>
    private void SelectSceneLine(int line)
    {
        MNetClient.Instance.Send(SceneLineCode.Change, new SceneLineRequest { LineId = line }.ToByteString());
    }

    /// <summary>打开线路菜单或进入公共地图时查询当前可用线路及人数。</summary>
    private void RequestSceneLines()
    {
        MNetClient.Instance.Send(SceneLineCode.List, ByteString.Empty);
    }

    /// <summary>进入公共地图后请求线路人数，副本内直接收起选择器。</summary><param name="data">网关确认的场景状态。</param>
    private void OnSceneLineState(ByteString data)
    {
        if (SceneLineResult.Parser.ParseFrom(data).CurrentLine == 0) _mainView.MinMapWindow?.ShowLines(null);
        else
        {
            RequestSceneLines();
            // 同图切线不会重建 Unity 场景，向新宿主重新订阅本线巡逻。
            MNetClient.Instance.Send(SceneLineCode.NpcPatrol, ByteString.Empty);
        }
    }

    /// <summary>将本地图真实目录交给已注册的小地图窗口。</summary><param name="data">控制服返回的线路目录。</param>
    private void OnSceneLineList(ByteString data)
    {
        _mainView.MinMapWindow?.ShowLines(SceneLineResult.Parser.ParseFrom(data));
    }

    /// <summary>拒绝请求后提示原因并恢复权威选择；成功时等待同一网关的场景状态刷新。</summary>
    /// <param name="data">本次手动切线回执。</param>
    private void OnSceneLineChanged(ByteString data)
    {
        // 失败原因由区服容量或角色当前状态决定。
        string reason = SceneLineResult.Parser.ParseFrom(data).Reason;
        if (reason.Length == 0) return;
        TipsMgr.Instance.ShowSystemTips(reason);
        MNetClient.Instance.Send(SceneLineCode.List, ByteString.Empty);
    }

    /// <summary>把商城购买回包交给商城模型解析。</summary>
    /// <param name="data">商城购买结果的序列化字节。</param>
    private void BuyShopItemHandle(ByteString data)
    {
        ShopMgr.Instance.HandleResponse(data);
    }

    /// <summary>分发角色背包快照到背包、商城和角色信息模块。</summary>
    /// <param name="data">包含当前角色各背包数据的完整回包。</param>
    private void RoleKnapsackInfoHandle(ByteString data)
    {
        RoleKanpsackInfoRet result = RoleKanpsackInfoRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed ||
            result.RoleKanpsackInfo == null)
        {
            return;
        }

        // 服务端返回的背包完整快照，用于刷新角色背包和装备界面。
        RoleKanpsackInfo info = result.RoleKanpsackInfo;
        // 当前场景中的本地玩家；Global 或 SceneWorld 已清理时保持为空并忽略旧回包。
        PlayerEntity localRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            localRole = Global.Instance.SceneWorld.LocalRole;
        }
        if (localRole == null || !localRole.LoadRoleData(result))
        {
            return;
        }
        _knapsackCtrl.BindRole(localRole);
        _roleInfoCtrl.RefreshEquipment(info);
        _roleInfoCtrl.RefreshCurrentRole();
    }

    /// <summary>处理主界面快捷键。</summary>
    /// <param name="key">L 切技能、B 切背包、I 切角色属性；其他字符不处理。</param>
    public void MainUIKeyHandler(string key)
    {
        if (_currentTalkNpc != null) return;
        switch (key)
        {
            case "L":
                ShowMainWindow(WindowType.SkillInfoWindow);
                break;
            case "B":
                ShowMainWindow(WindowType.KnapsackWindow);
                CameraMgr.Instance.KnapsackWindowAngle(
                    _mainView.GetWindow(WindowType.KnapsackWindow));
                break;
            case "E":
                if (_currencyExchangeCtrl != null)
                {
                    _currencyExchangeCtrl.Open(0);
                }
                break;
            case "I":
                ShowMainWindow(WindowType.RoleAttriibuteWindow);
                CameraMgr.Instance.RoleAttrWindowAngle(
                    _mainView.GetWindow(WindowType.RoleAttriibuteWindow));
                break;
        }
    }
    #endregion
}
