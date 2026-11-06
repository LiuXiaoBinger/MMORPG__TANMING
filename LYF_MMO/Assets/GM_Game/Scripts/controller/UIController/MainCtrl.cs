using Google.Protobuf;
using MMoRpgCommon;
using UnityEngine;

/// <summary>
/// 主界面的业务入口：创建商城、技能、角色信息、背包、任务和队伍控制器，统一处理相关协议与菜单交互。
/// </summary>
public class MainCtrl : CtrlBase
{

    /// <summary>副本阶段和场景交互窗口的控制器。</summary>
    private readonly DungeonFlowCtrl _dungeonFlowCtrl;

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
        _shopCtrl = new ShopCtrl(_mainView);
        _skillCtrl = new SkillCtrl(_mainView);
        _roleInfoCtrl = new RoleInfoCtrl(_mainView);
        _knapsackCtrl = new KnapsackCtrl(_mainView);
        _taskCtrl = new TaskCtrl(_mainView);
        _themeDungeonTeamCtrl = new ThemeDungeonTeamCtrl(_mainView);
        _dungeonFlowCtrl = new DungeonFlowCtrl(_mainView);
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            Global.Instance.SceneWorld.LocalRoleChanged += OnLocalRoleChanged;
        }
        BindTrackedRole();
        _chatWindow = _mainView.ChatWindow;
        if (_chatWindow != null) _chatMgr = new ChatMgr(_chatWindow);
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
    }

    /// <summary>注册功能菜单、NPC对话和聊天视图事件。</summary>
    private void RegisterViewActions()
    {
        _mainView.FunctionMenuButton.onClick.AddListener(OnFunctionMenuRequested);
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
        if (_mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested += OnShopRequested;
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
        _dungeonFlowCtrl?.Dispose();
        _mainView.FunctionMenuButton.onClick.RemoveListener(OnFunctionMenuRequested);
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
        if (_mainInputRegistered && PlayerInputCtr.Instance != null)
        {
            PlayerInputCtr.Instance.MainUIKeyHandler -= MainUIKeyHandler;
            _mainInputRegistered = false;
        }
        if (_mainView != null && _mainView.TalkWindow != null)
        {
            _mainView.TalkWindow.ShopRequested -= OnShopRequested;
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
        _currentTalkNpc = null;
    }
    #endregion

    #region 界面显示与开关
    /// <summary>对话窗口先把 NPC 转为展示数据，商店交给 ShopCtrl 准备商品；其他窗口沿用基类显隐流程。</summary>
    /// <param name="windowType">需要切换的主界面子窗口。</param>
    /// <param name="obj">窗口数据；对话和商店入口要求传入 NpcEntity。</param>
    public override void ShowMainWindow(WindowType windowType, object obj = null)
    {
        if (windowType == WindowType.TalkWindow)
        {
            _currentTalkNpc = obj as NpcEntity;
            NpcTalkViewData viewData = CreateNpcTalkViewData(_currentTalkNpc);
            base.ShowMainWindow(windowType, viewData);
            return;
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
        }
    }
    #endregion

    #region 交互响应
    /// <summary>角色切换或断线清理时重新绑定背包控制器。</summary>
    /// <param name="role">新的本地角色，null 表示角色已离场。</param>
    private void OnLocalRoleChanged(PlayerEntity role)
    {
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

    /// <summary>处理 NPC 对话窗口发出的商城请求。</summary>
    private void OnShopRequested()
    {
        if (_currentTalkNpc == null)
        {
            TipsMgr.Instance.ShowSystemTips("当前没有可交互的 NPC");
            return;
        }
        _shopCtrl.OpenNpcShop(_currentTalkNpc);
    }

    /// <summary>把 NPC 领域对象转换为对话展示数据。</summary>
    /// <param name="npcEntity">当前交互 NPC，null 时返回空数据。</param>
    private NpcTalkViewData CreateNpcTalkViewData(NpcEntity npcEntity)
    {
        if (npcEntity == null)
        {
            return null;
        }
        return new NpcTalkViewData(npcEntity.NpcID, npcEntity.Name, npcEntity.Talk);
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
