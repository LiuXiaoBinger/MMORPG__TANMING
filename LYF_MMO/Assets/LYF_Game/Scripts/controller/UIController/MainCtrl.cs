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

    /// <summary>聊天窗口视图。</summary>
    private readonly ChatWindows _chatWindow;
    /// <summary>管理正式聊天窗口的文字及录音请求与回包。</summary>
    private readonly ChatMgr _chatMgr;

    /// <summary>可选江湖聊天模块，复用当前主界面的正式聊天服务。</summary>
    private readonly YimengChatCtrl _yimengChatCtrl;

    /// <summary>任务子控制器，组合任务窗口与快捷任务面板。</summary>
    private readonly TaskCtrl _taskCtrl;

    /// <summary>主题副本队伍控制器，根据服务器通知刷新队伍，并在入场通知后切换场景。</summary>
    private readonly ThemeDungeonTeamCtrl _themeDungeonTeamCtrl;

    /// <summary>主界面快捷键是否已注册。</summary>
    private bool _mainInputRegistered;

    /// <summary>当前对话关联的 NPC，仅由控制器保存领域对象。</summary>
    private NpcEntity _currentTalkNpc;
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
        if (_mainView.MinMapWindow != null) _mainView.MinMapWindow.LineSelected += SelectSceneLine;
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.State, OnSceneLineState);
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.List, OnSceneLineList);
        SocketDispatcher.Instance.AddEventHandler(SceneLineCode.Change, OnSceneLineChanged);
        _shopCtrl = new ShopCtrl(_mainView);
        _skillCtrl = new SkillCtrl(_mainView);
        _roleInfoCtrl = new RoleInfoCtrl(_mainView);
        _knapsackCtrl = new KnapsackCtrl(_mainView);
        _taskCtrl = new TaskCtrl(_mainView);
        _themeDungeonTeamCtrl = new ThemeDungeonTeamCtrl(_mainView);
        _dungeonFlowCtrl = new DungeonFlowCtrl(_mainView);
        _deathCtrl = new DeathCtrl(_mainView);
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
        _mainView.FunctionMenuButton.onClick.AddListener(OnFunctionMenuRequested);
        _mainView.TradeMarketButton.onClick.AddListener(OnTradeMarketRequested);
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
        if (_mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested += OnShopRequested;
            _mainView.TalkWindow.MoshentaBattleRequested += OnMoshentaBattleRequested;
        if (_mainView.CombatStatusButton != null)
        {
            _mainView.CombatStatusButton.onClick.AddListener(OnCombatStatusClicked);
        }
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
        if (_mainView != null && _mainView.MinMapWindow != null) _mainView.MinMapWindow.LineSelected -= SelectSceneLine;
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.State, OnSceneLineState);
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.List, OnSceneLineList);
        SocketDispatcher.Instance.RemoveEventHandler(SceneLineCode.Change, OnSceneLineChanged);
        _yimengChatCtrl?.Dispose();
        _dungeonFlowCtrl?.Dispose();
        _mainView.FunctionMenuButton.onClick.RemoveListener(OnFunctionMenuRequested);
        _mainView.TradeMarketButton.onClick.RemoveListener(OnTradeMarketRequested);
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
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_RoleKnapsackInfoCode, RoleKnapsackInfoHandle);
        SocketDispatcher.Instance.RemoveEventHandler(
            NetDefine.CMD_OpenKnapsackGridCode, OpenKnapsackGridHandle);
        if (_mainView.CombatStatusButton != null)
        {
            _mainView.CombatStatusButton.onClick.RemoveListener(OnCombatStatusClicked);
        }
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
            _mainView.TalkWindow.ShopRequested -= OnShopRequested;
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
        _deathCtrl.Dispose();
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
            // 切换说话者时刷新正在显示的窗口，避免通用窗口入口将它反向关闭。
            NpcTalkViewData dialogue = CreateNpcTalkViewData(speaker);
            if (_mainView.TalkWindow.gameObject.activeSelf) _mainView.TalkWindow.ReFreshUI(dialogue);
            else base.ShowMainWindow(windowType, dialogue);
            CameraMgr.Instance?.BeginNpcTalk(speaker, CloseNpcTalk);
            return;
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
        npc.Model.GameObject.Transform.LookAtTarget(player.Model.GameObject.Transform);
        ShowMainWindow(WindowType.TalkWindow, npc);
    }

    /// <summary>处理 NPC 对话窗口发出的商城请求。</summary>
    /// <param name="service">所选按钮对应的商店和商品清单。</param>
    private void OnShopRequested(NpcDialogueCatalog.Service service)
    {
        if (_currentTalkNpc == null)
        {
            TipsMgr.Instance.ShowSystemTips("当前没有可交互的 NPC");
            return;
        }
        // 关窗会释放当前说话者，先保留本次开店的真实 NPC。
        NpcEntity merchant = _currentTalkNpc;
        CloseNpcTalk();
        _shopCtrl.OpenNpcShop(merchant, service);
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
        return new NpcTalkViewData(npcEntity.NpcID, npcEntity.Name, npcEntity.Talk, services: services);
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
    }

    /// <summary>从小地图提交所选线路；地图和角色归属由区服确定。</summary><param name="line">目标线路编号。</param>
    private void SelectSceneLine(int line)
    {
        MNetClient.Instance.Send(SceneLineCode.Change, new SceneLineRequest { LineId = line }.ToByteString());
    }

    /// <summary>进入公共地图后请求线路人数，副本内直接收起选择器。</summary><param name="data">网关确认的场景状态。</param>
    private void OnSceneLineState(ByteString data)
    {
        if (SceneLineResult.Parser.ParseFrom(data).CurrentLine == 0) _mainView.MinMapWindow?.ShowLines(null);
        else MNetClient.Instance.Send(SceneLineCode.List, ByteString.Empty);
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
            case "I":
                ShowMainWindow(WindowType.RoleAttriibuteWindow);
                CameraMgr.Instance.RoleAttrWindowAngle(
                    _mainView.GetWindow(WindowType.RoleAttriibuteWindow));
                break;
        }
    }
    #endregion
}
