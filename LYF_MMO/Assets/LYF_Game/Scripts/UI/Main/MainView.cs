using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/**
* Title:主城场景相关view
* Descrpiton: 角色信息窗口 聊天信息窗口 小地图 背包信息窗口 技能信息窗口 ，Npc
*/

/// <summary>持有主城各窗口和常驻快捷栏，提供控制器访问入口，并登记窗口、绑定视图间引用。</summary>
public class MainView : UIBase
{
   /// <summary>主城聊天层，承载预制体中的聊天组件及运行时实例化的聊天模块。</summary>
   [SerializeField] private RectTransform _chatLayer;

   /// <summary>包含聊天栏 GM 按钮与窗口的正式预制体。</summary>
   [SerializeField] private GameObject _gmPrefab;
   /// <summary>此主城视图持有的 GM 子窗口，供控制器绑定。</summary>
   public GMPanel GM { get; private set; }

   /// <summary>主城功能层，承载背包、角色和任务等可以在同层内叠放的窗口。</summary>
   [SerializeField] private RectTransform _functionLayer;

   /// <summary>主城提示层，承载按预制体层级显示在功能窗口上方的提示窗口。</summary>
   [SerializeField] private RectTransform _tipsLayer;

   /// <summary>随本地角色移动的护盾提示，内容和资源引用保存在 UIRoot 预制体。</summary>
   [SerializeField] private RoleShieldWidget _roleShieldWidget;

   /// <summary>供主界面控制器下发权威护盾数值的显示入口。</summary>
   public RoleShieldWidget RoleShieldWidget => _roleShieldWidget;

   /// <summary>原魔神塔剧情黑幕，显隐沿主视图窗口生命周期管理。</summary>
   [SerializeField] private MoshentaBlackCurtainWindow _moshentaBlackCurtainWindow;
   /// <summary>提供给TeamCtrl的强类型黑幕展示入口。</summary>
   public MoshentaBlackCurtainWindow MoshentaBlackCurtainWindow => _moshentaBlackCurtainWindow;

   /// <summary>货币兑换窗口（兑铜币/兑银两/兑银票），元宝仅作支付来源。</summary>
   [SerializeField] private CurrencyExchangeWindow _currencyExchangeWindow;
   /// <summary>供兑换控制器访问的强类型窗口引用。</summary>
   public CurrencyExchangeWindow CurrencyExchangeWindow => _currencyExchangeWindow;

   /// <summary>副本死亡提示与返回出生点复活窗口，引用保存在UIRoot预制体。</summary>
   [SerializeField] private DeathWindow _deathWindow;
   /// <summary>提供给死亡控制器的正式窗口。</summary>
   public DeathWindow DeathWindow => _deathWindow;

   /// <summary>服务端副本目标及附近交互窗口，生命周期跟随主视图。</summary>
   [SerializeField] private DungeonFlowWindow _dungeonFlowWindow;
   /// <summary>供副本控制器绑定正式窗口。</summary>
   public DungeonFlowWindow DungeonFlowWindow => _dungeonFlowWindow;

   /// <summary>当前选中装备的详情窗口，提供穿脱和三特技槽选择。</summary>
   [SerializeField] private EquipmentDetailWindow _equipmentDetailWindow;
   /// <summary>供背包控制器绑定装备详情的展示与按钮事件。</summary>
   public EquipmentDetailWindow EquipmentDetailWindow => _equipmentDetailWindow;

   /// <summary>装备详情「强化」打开的全屏养成窗口，归主城功能层管理。</summary>
   [SerializeField] private EquipmentWorkshopWindow _equipmentWorkshopWindow;
   /// <summary>供背包控制器填充选中装备及其所属容器。</summary>
   public EquipmentWorkshopWindow EquipmentWorkshopWindow => _equipmentWorkshopWindow;

   /// <summary>功能菜单的交易入口，与 NPC 商店独立。</summary>
   [SerializeField] private Button _tradeMarketButton;
   /// <summary>主城功能层中持有的交易窗口。</summary>
   [SerializeField] private TradeMarketWindow _tradeMarketWindow;
   /// <summary>供主控制器注册交易按钮。</summary>
   public Button TradeMarketButton => _tradeMarketButton;
   /// <summary>供主控制器下发当前角色包裹。</summary>
   public TradeMarketWindow TradeMarketWindow => _tradeMarketWindow;
   /// <summary>功能菜单中的信封按钮。</summary>
   [SerializeField] internal Button emailButton;
   /// <summary>主城功能层持有的角色邮箱窗口。</summary>
   [SerializeField] internal EmailPanel emailPanel;
   /// <summary>信封按钮上显示本角色有未读邮件的红点。</summary>
   [SerializeField] internal GameObject emailHint;

   /// <summary>功能菜单中的珍宝阁入口。</summary>
   [SerializeField] private Button _treasurePavilionButton;
   /// <summary>主城功能层中的珍宝阁面板。</summary>
   [SerializeField] private TreasurePavilionWindow _treasurePavilionWindow;
   /// <summary>供控制器绑定珍宝阁入口按钮。</summary>
   public Button TreasurePavilionButton => _treasurePavilionButton;
   /// <summary>控制器打开窗口时传入当前角色。</summary>
   public TreasurePavilionWindow TreasurePavilionWindow => _treasurePavilionWindow;

   /// <summary>主角血蓝、资源弧条、技能快捷栏及当前目标文字区域。</summary>
   [SerializeField,Header("角色当前信息window")] private RoleCurrInfoWindow _roleCurrInfoWindow;
   /// <summary>技能列表和快捷技能槽窗口，数据由技能控制器提供。</summary>
   [SerializeField,Header("技能信息window")] private SkillInfoWindow _skillInfoWindow;
   /// <summary>可滚动的背包格子窗口。</summary>
   [SerializeField,Header("背包")] private KnapsackWindow _knapsackWindow;
   /// <summary>与当前 NPC 交谈的文本窗口。</summary>
   [SerializeField,Header("交谈相关Window")] private TalkWindow _talkWindow;
   /// <summary>展示当前 NPC 商品的商城窗口。</summary>
   [SerializeField,Header("NPC商城")] private NpcShopWindow _shopWindow;
   /// <summary>按装备部位显示角色穿戴的窗口。</summary>
   [SerializeField,Header("角色信息window")] private RoleAttriibuteWindow _roleAttriibuteWindow;
   /// <summary>聊天输入和消息展示窗口，可从子节点补找旧预制体引用。</summary>
   [SerializeField,Header("聊天window")] private ChatWindows _chatWindow;
   /// <summary>可选的江湖聊天模块预制体，包含入口按钮和独立窗口。</summary>
   [SerializeField] private GameObject _yimengChatPrefab;
   /// <summary>当前 MainView 下保留的新版聊天子窗口。</summary>
   public YimengChatWindow YimengChat { get; private set; }
   /// <summary>跟踪当前角色与场景辅助点的小地图窗口。</summary>
   [SerializeField,Header("小地图window")] private MinMapWindow _minMapWindow;

   /// <summary>设置窗口，只承载RO设置界面与页签切换。</summary>
   [SerializeField, Header("设置窗口")] private SettingPanel _settingPanel;

   /// <summary>任务主窗口，绑定 Task 预制体，由主界面持有生命周期。</summary>
   [SerializeField, Header("任务窗口")] private TaskWindow _taskWindow;

   /// <summary>快捷任务面板，绑定 QuickTaskPanel 预制体，常驻主界面。</summary>
   [SerializeField, Header("快捷任务面板")] private QuickTaskPanelWidget _quickTaskPanel;

   /// <summary>日常任务窗口，绑定 DailyTask 预制体，独立于任务主窗口。</summary>
   [SerializeField, Header("日常任务窗口")] private DailyTaskWindow _dailyTaskWindow;

   /// <summary>主题副本选择窗口，直接使用 ThemeDungeonArround 预制体实例作为窗口根。</summary>
   [SerializeField, Header("主题副本窗口")] private ThemeDungeonArroundWindow _themeDungeonArroundWindow;

   /// <summary>主题副本详情窗口，对应 RO 的 Theme_Story，由选择窗口翻开书位后打开。</summary>
   [SerializeField, Header("副本详情窗口")] private ThemeStoryWindow _themeStoryWindow;

   /// <summary>主题副本建队窗口，复制 RO 快捷队伍面板的建队分支，由详情页挑战打开。</summary>
   [SerializeField, Header("建队窗口")] private ThemeDungeonTeamCreateWindow _themeDungeonTeamCreateWindow;
   /// <summary>RO 全队进本确认窗口，成员答复由 MS 统一处理。</summary>
   [SerializeField] private ThemeDungeonTeamWindow _themeDungeonTeamWindow;

   /// <summary>主界面左侧常驻快捷队伍栏，复制 RO QuickTeamPanel 的原层级与布局。</summary>
   [SerializeField, Header("快捷队伍栏")] private QuickTeamWidget _quickTeamWidget;

   /// <summary>RO 快捷区外层页签，只切换任务和队伍内容。</summary>
   [SerializeField, Header("任务队伍快捷页签")] private QuickPanelWidget _quickPanel;

   /// <summary>功能菜单中的日常入口，打开日常任务窗口。</summary>
   [SerializeField, Header("日常入口")] private Button _dailyTaskButton;

   /// <summary>功能菜单中的副本入口，打开主题副本选择窗口。</summary>
   [SerializeField, Header("副本入口")] private Button _themeDungeonButton;

   /// <summary>主界面任务入口，点击后切换任务窗口，事件由 MainCtrl 注册。</summary>
   [SerializeField, Header("任务入口")] private Button _taskButton;

   /// <summary>左上功能菜单内的设置入口，点击事件由MainCtrl管理。</summary>
   [SerializeField, Header("设置入口")] private Button _settingButton;

   /// <summary>左上角始终可见的功能菜单开关，由主控制器接收点击。</summary>
   [SerializeField, Header("功能菜单开关")] private Button _functionMenuButton;

   /// <summary>功能入口按钮组的容器，收起时整组隐藏且不接收点击。</summary>
   [SerializeField, Header("功能菜单内容")] private GameObject _functionMenuContent;

   /// <summary>开关内的箭头，收起时朝右提示可向右展开，展开时朝左提示可收回。</summary>
   [SerializeField, Header("功能菜单箭头")] private RectTransform _functionMenuArrow;

   /// <summary>功能菜单中的背包入口，复用B键的窗口与镜头流程。</summary>
   [SerializeField, Header("背包入口")] private Button _knapsackButton;

   /// <summary>功能菜单中的角色入口，复用I键的窗口与镜头流程。</summary>
   [SerializeField, Header("角色入口")] private Button _roleButton;

   /// <summary>功能菜单中的技能入口，打开现有技能窗口。</summary>
   [SerializeField, Header("技能入口")] private Button _skillButton;

   /// <summary>战斗快捷栏下方的角色状态底栏，展示职业剑形标记、等级与经验进度。</summary>
   [SerializeField, Header("角色状态底栏")] private RoleStatusFooterWidget _roleStatusFooter;

   /// <summary>竞技设置窗口，仅展示攻击与保护模式外观，初始隐藏。</summary>
   [SerializeField, Header("竞技设置窗口")] private ArenaSettingsWindow _arenaSettingsWindow;

   /// <summary>供角色信息控制器刷新底栏的职业、等级与经验。</summary>
   public RoleStatusFooterWidget RoleStatusFooter => _roleStatusFooter;

   /// <summary>供主界面控制器注册与注销设置入口点击事件。</summary>
   public Button SettingButton => _settingButton;
   /// <summary>战斗 HUD 蓝色状态图标上的详情按钮。</summary>
   [SerializeField] private Button _combatStatusButton;

   /// <summary>登记到主视图窗口表的角色状态详情。</summary>
   [SerializeField] private CombatStatusWindow _combatStatusWindow;

   /// <summary>供主控制器绑定状态详情的开关事件。</summary>
   public Button CombatStatusButton => _combatStatusButton;

   /// <summary>供主控制器向状态列表同步角色的实际护盾状态。</summary>
   public CombatStatusWindow CombatStatus => _combatStatusWindow;


   /// <summary>供主控制器注册与注销功能菜单开关事件。</summary>
   public Button FunctionMenuButton => _functionMenuButton;

   /// <summary>供主控制器注册与注销背包入口事件。</summary>
   public Button KnapsackButton => _knapsackButton;

   /// <summary>供主控制器注册与注销角色入口事件。</summary>
   public Button RoleButton => _roleButton;

   /// <summary>供主控制器注册与注销技能入口事件。</summary>
   public Button SkillButton => _skillButton;

   /// <summary>供主控制器注册与注销任务入口事件。</summary>
   public Button TaskButton => _taskButton;

   /// <summary>任务主窗口实例，供任务控制器绑定事件与刷新。</summary>
   public TaskWindow TaskWindow => _taskWindow;

   /// <summary>快捷任务面板实例，供任务控制器绑定事件与刷新。</summary>
   public QuickTaskPanelWidget QuickTaskPanel => _quickTaskPanel;

   /// <summary>日常任务窗口实例，供主控制器初始化与显隐。</summary>
   public DailyTaskWindow DailyTaskWindow => _dailyTaskWindow;

   /// <summary>主题副本选择窗口实例，供主控制器初始化与显隐。</summary>
   public ThemeDungeonArroundWindow ThemeDungeonArroundWindow => _themeDungeonArroundWindow;

   /// <summary>主题副本详情窗口实例，供建队控制器在进入副本前收起。</summary>
   public ThemeStoryWindow ThemeStoryWindow => _themeStoryWindow;

   /// <summary>建队窗口实例，供建队控制器初始化、刷新与显隐。</summary>
   public ThemeDungeonTeamCreateWindow ThemeDungeonTeamCreateWindow => _themeDungeonTeamCreateWindow;
   /// <summary>供副本控制器接入服务器询问与成员答复展示。</summary>
   public ThemeDungeonTeamWindow ThemeDungeonTeamWindow => _themeDungeonTeamWindow;

   /// <summary>快捷队伍栏实例，供建队控制器刷新成员信息。</summary>
   public QuickTeamWidget QuickTeamWidget => _quickTeamWidget;

   /// <summary>任务/队伍快捷区，供建队成功后切换到本人成员行。</summary>
   public QuickPanelWidget QuickPanel => _quickPanel;

   /// <summary>供主控制器注册与注销日常入口事件。</summary>
   public Button DailyTaskButton => _dailyTaskButton;

   /// <summary>供主控制器注册与注销副本入口事件。</summary>
   public Button ThemeDungeonButton => _themeDungeonButton;

   /// <summary>切换左上功能菜单，显隐状态直接取正式容器的当前状态。</summary>
   public void ToggleFunctionMenu()
   {
      SetFunctionMenuVisible(!_functionMenuContent.activeSelf);
   }

   /// <summary>控制入口组显隐及箭头朝向，不改变任何业务窗口。</summary>
   /// <param name="visible">true 展开入口组，false 只保留功能开关。</param>
   public void SetFunctionMenuVisible(bool visible)
   {
      _functionMenuContent.SetActive(visible);
      if (visible)
      {
         // 入口组在箭头右侧展开后，箭头转为朝左，提示可以收回。
         _functionMenuArrow.localEulerAngles = new Vector3(0f, 0f, 90f);
      }
      else
      {
         // 收起时箭头朝右，提示入口组会从右侧展开。
         _functionMenuArrow.localEulerAngles = new Vector3(0f, 0f, -90f);
      }
   }

   /// <summary>供控制器刷新角色状态与锁定目标信息。</summary>
   public RoleCurrInfoWindow RoleCurrentWindow => _roleCurrInfoWindow;
   /// <summary>供控制器刷新技能列表和顶部职业、可用技能点数。</summary>
   public SkillInfoWindow SkillWindow => _skillInfoWindow;
   /// <summary>供商城控制器绑定商品点击与刷新列表。</summary>
   public NpcShopWindow ShopWindow => _shopWindow;
   /// <summary>供装备控制器刷新穿戴槽位。</summary>
   public RoleAttriibuteWindow RoleAttributeWindow => _roleAttriibuteWindow;
   /// <summary>供背包控制器设置容量、分类和物品列表。</summary>
   public KnapsackWindow KnapsackWindow => _knapsackWindow;
   /// <summary>供交互控制器显示 NPC 文本并绑定商店按钮。</summary>
   public TalkWindow TalkWindow => _talkWindow;

   /// <summary>主 HUD 中列出附近所有可交谈者的小窗，默认隐藏。</summary>
   [SerializeField] private NpcTalkPromptWindow _npcTalkPrompt;
   /// <summary>下一次检查附近说话者的本地秒数，主视图隐藏期间无需扫描。</summary>
   private float _nextNpcTalkScan;
   /// <summary>供主控制器更新人物列表并订阅逐人交谈事件。</summary>
   internal NpcTalkPromptWindow NpcTalkPrompt => _npcTalkPrompt;

   /// <summary>主界面可见时每 0.2 秒更新附近交谈对象，移动和巡逻共用该频率。</summary>
   private void Update()
   {
      if (_npcTalkPrompt == null || Time.unscaledTime < _nextNpcTalkScan) return;
      _nextNpcTalkScan = Time.unscaledTime + 0.2f;
      UIRoot.Instance?.MainCtrl?.RefreshNearbyTalkPrompt();
   }
   /// <summary>取得小地图窗口；未手动绑定时查找包括隐藏节点在内的子组件。</summary>
   public MinMapWindow MinMapWindow
   {
      get
      {
         if (_minMapWindow == null)
         {
            _minMapWindow = GetComponentInChildren<MinMapWindow>(true);
         }
         return _minMapWindow;
      }
   }

   /// <summary>常驻主视图引用的正式区域地图预制体。</summary>
   [SerializeField] private AreaMapWindow _areaMapPrefab;
   /// <summary>随主视图创建一次并登记在功能层的地图窗口实例。</summary>
   private AreaMapWindow _areaMap;
   /// <summary>主界面控制器沿此引用提交地图输入和绑定本地玩家。</summary>
   public AreaMapWindow AreaMap => _areaMap;

   /// <summary>获取聊天窗口，兼容尚未绑定序列化字段的旧预制体。</summary>
   public ChatWindows ChatWindow
   {
      get
      {
         if (_chatWindow == null)
         {
            _chatWindow = GetComponentInChildren<ChatWindows>(true);
         }
         return _chatWindow;
      }
   }
   /// <summary>绑定功能与提示窗口所属层，登记主城窗口并收起功能菜单。初始化任务、副本和快捷栏，再连接副本选择页与详情页。</summary>
   public override void InitView()
   {
      base.InitView();
      if (_areaMap == null && _areaMapPrefab != null)
         _areaMap = Instantiate(_areaMapPrefab, _functionLayer, false);
      SetLayerPanelParentData(_functionLayer);
      SetLayerPanelParentData(_tipsLayer);
      if (_areaMap != null)
      {
         windowDic[WindowType.AreaMapWindow] = _areaMap;
         _areaMap.InitWindow();
         _areaMap.Show(false);
      }
      // 竞技设置窗口登记到主视图窗口表并初始化关闭按钮，初始保持隐藏。
      if (_arenaSettingsWindow != null)
      {
         windowDic[WindowType.ArenaSettingsWindow] = _arenaSettingsWindow;
         _arenaSettingsWindow.InitWindow();
      if (_combatStatusWindow != null)
      {
         windowDic[WindowType.CombatStatusWindow] = _combatStatusWindow;
         _combatStatusWindow.InitWindow();
         _combatStatusWindow.Show(false);
      }
         _arenaSettingsWindow.Show(false);
      }
      if (_currencyExchangeWindow != null)
      {
         windowDic[WindowType.CurrencyExchangeWindow] = _currencyExchangeWindow;
         _currencyExchangeWindow.InitWindow();
      }
      windowDic[WindowType.DeathWindow] = _deathWindow;
      _deathWindow.InitWindow();
      windowDic[WindowType.MoshentaBlackCurtainWindow] = _moshentaBlackCurtainWindow;
      _moshentaBlackCurtainWindow.InitWindow();
      windowDic[WindowType.EquipmentDetailWindow] = _equipmentDetailWindow;
      _equipmentDetailWindow.InitWindow();
      windowDic[WindowType.EquipmentWorkshopWindow] = _equipmentWorkshopWindow;
      _equipmentWorkshopWindow.InitWindow();
      windowDic[WindowType.DungeonFlowWindow] = _dungeonFlowWindow;
      _dungeonFlowWindow.InitWindow();
      if (ChatWindow != null) windowDic[WindowType.ChatWindow] = ChatWindow;
      if (_yimengChatPrefab != null && ChatWindow != null)
      {
         if (YimengChat == null)
         {
            // 聊天模块根节点挂在聊天层，主城重新初始化时复用这一实例。
            GameObject module = Instantiate(_yimengChatPrefab, _chatLayer, false);
            YimengChat = module.GetComponentInChildren<YimengChatWindow>(true);
            YimengChat.InitWindow();
         }
         windowDic[WindowType.YimengChatWindow] = YimengChat;
      }
      // RO 的客户端包开关映射到 Unity 编辑器、开发包或显式启用 GM 的构建。
#if UNITY_EDITOR || DEVELOPMENT_BUILD || LYF_CLIENT_GM
      if (GM == null && _gmPrefab != null)
      {
         // 聊天入口保留在聊天层，窗口归主功能层管理。
         GameObject gmModule = Instantiate(_gmPrefab, _chatLayer, false);
         GM = gmModule.GetComponentInChildren<GMPanel>(true);
         GM.transform.SetParent(_functionLayer, false);
         GM.SetPanelParentData(_functionLayer);
      }
      if (GM != null) windowDic[WindowType.GMPanel] = GM;
#endif
      windowDic[WindowType.RoleCurrtInfoWindow] = _roleCurrInfoWindow;
      windowDic[WindowType.SkillInfoWindow] = _skillInfoWindow;
      windowDic[WindowType.KnapsackWindow] = _knapsackWindow;
      windowDic[WindowType.TalkWindow] = _talkWindow;
      if (_npcTalkPrompt != null)
      {
         windowDic[WindowType.NpcTalkPromptWindow] = _npcTalkPrompt;
         _npcTalkPrompt.SetSpeakers(null);
      }
      windowDic[WindowType.ShopWindow] = _shopWindow;
      windowDic[WindowType.TradeMarketWindow] = _tradeMarketWindow;
      windowDic[WindowType.EmailPanel] = emailPanel;
      windowDic[WindowType.TreasurePavilionWindow] = _treasurePavilionWindow;
      windowDic[WindowType.RoleAttriibuteWindow] = _roleAttriibuteWindow;
      windowDic[WindowType.SettingPanel] = _settingPanel;
      windowDic[WindowType.TaskWindow] = _taskWindow;
      windowDic[WindowType.DailyTaskWindow] = _dailyTaskWindow;
      windowDic[WindowType.ThemeDungeonArroundWindow] = _themeDungeonArroundWindow;
      windowDic[WindowType.ThemeDungeonTeamCreateWindow] = _themeDungeonTeamCreateWindow;
      windowDic[WindowType.ThemeDungeonTeamWindow] = _themeDungeonTeamWindow;
      _themeDungeonTeamWindow.InitWindow();
      SetFunctionMenuVisible(false);
      if (_taskWindow != null)
      {
         _taskWindow.InitWindow();
      }
      if (_dailyTaskWindow != null)
      {
         _dailyTaskWindow.InitWindow();
      }
      if (_themeDungeonArroundWindow != null)
      {
         _themeDungeonArroundWindow.InitWindow();
      }
      if (_themeStoryWindow != null)
      {
         _themeStoryWindow.InitWindow();
      }
      if (_themeDungeonTeamCreateWindow != null)
      {
         _themeDungeonTeamCreateWindow.InitWindow();
      }
      if (_themeDungeonArroundWindow != null && _themeStoryWindow != null)
      {
         _themeDungeonArroundWindow.SetStoryWindow(_themeStoryWindow);
      }
      if (_quickTaskPanel != null)
      {
         _quickTaskPanel.InitWidget();
      }
      if (_quickTeamWidget != null)
      {
         // 快捷队伍栏常驻主界面，初始化只绑定一次按钮监听并复位为未建队展示。
         _quickTeamWidget.InitWidget();
      }
      _quickPanel.InitWidget();
      _roleCurrInfoWindow.InitWindow();
   }

   /// <summary>按预制体层级绑定直接子窗口的父容器，供窗口显示时更新同级顺序。</summary>
   /// <param name="layer">UIRoot 预制体绑定的主城功能层或提示层。</param>
   private void SetLayerPanelParentData(RectTransform layer)
   {
      // i 是当前层的直接子节点索引，绑定期间子节点顺序不变。
      for (int i = 0; i < layer.childCount; i++)
      {
         // 子节点上的窗口组件保存所属层，供后续显示时使用。
         if (layer.GetChild(i).TryGetComponent(out WindowBase window))
         {
            window.SetPanelParentData(layer);
         }
      }
   }
}
