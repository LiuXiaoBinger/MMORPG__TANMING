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
   /// <summary>服务端副本目标及附近交互窗口，生命周期跟随主视图。</summary>
   [SerializeField] private DungeonFlowWindow _dungeonFlowWindow;
   /// <summary>供副本控制器绑定正式窗口。</summary>
   public DungeonFlowWindow DungeonFlowWindow => _dungeonFlowWindow;

   /// <summary>当前选中装备的详情窗口，提供穿脱和三特技槽选择。</summary>
   [SerializeField] private EquipmentDetailWindow _equipmentDetailWindow;
   /// <summary>供背包控制器绑定装备详情的展示与按钮事件。</summary>
   public EquipmentDetailWindow EquipmentDetailWindow => _equipmentDetailWindow;

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

   /// <summary>供主界面控制器注册与注销设置入口点击事件。</summary>
   public Button SettingButton => _settingButton;

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
   /// <summary>登记主城窗口并收起功能菜单；初始化任务、副本和快捷栏，再连接副本选择页与详情页。</summary>
   public override void InitView()
   {
      base.InitView();
      windowDic[WindowType.EquipmentDetailWindow] = _equipmentDetailWindow;
      _equipmentDetailWindow.InitWindow();
      windowDic[WindowType.DungeonFlowWindow] = _dungeonFlowWindow;
      _dungeonFlowWindow.InitWindow();
      if (ChatWindow != null) windowDic[WindowType.ChatWindow] = ChatWindow;
      windowDic[WindowType.RoleCurrtInfoWindow] = _roleCurrInfoWindow;
      windowDic[WindowType.SkillInfoWindow] = _skillInfoWindow;
      windowDic[WindowType.KnapsackWindow] = _knapsackWindow;
      windowDic[WindowType.TalkWindow] = _talkWindow;
      windowDic[WindowType.ShopWindow] = _shopWindow;
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
}
