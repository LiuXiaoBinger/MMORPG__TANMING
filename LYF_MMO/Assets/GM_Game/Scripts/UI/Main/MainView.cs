using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/**
* Title:主城场景相关view
* Descrpiton: 角色信息窗口 聊天信息窗口 小地图 背包信息窗口 技能信息窗口 ，Npc
*/

public class MainView : UIBase
{
   [SerializeField,Header("角色当前信息window")] private RoleCurrInfoWindow _roleCurrInfoWindow;
   [SerializeField,Header("技能信息window")] private SkillInfoWindow _skillInfoWindow;
   [SerializeField,Header("背包")] private KnapsackWindow _knapsackWindow;
   [SerializeField,Header("交谈相关Window")] private TalkWindow _talkWindow;
   [SerializeField,Header("NPC商城")] private NpcShopWindow _shopWindow;
   [SerializeField,Header("角色信息window")] private RoleAttriibuteWindow _roleAttriibuteWindow;
   [SerializeField,Header("聊天window")] private ChatWindow _chatWindow;
   [SerializeField,Header("小地图window")] private MinMapWindow _minMapWindow;

   /// <summary>设置窗口，只承载RO设置界面与页签切换。</summary>
   [SerializeField, Header("设置窗口")] private SettingPanel _settingPanel;

   /// <summary>任务主窗口，绑定 Task 预制体，由主界面持有生命周期。</summary>
   [SerializeField, Header("任务窗口")] private TaskWindow _taskWindow;

   /// <summary>快捷任务面板，绑定 QuickTaskPanel 预制体，常驻主界面。</summary>
   [SerializeField, Header("快捷任务面板")] private QuickTaskPanelWidget _quickTaskPanel;

   /// <summary>主界面任务入口，点击后切换任务窗口，事件由 MainCtrl 注册。</summary>
   [SerializeField, Header("任务入口")] private Button _taskButton;

   /// <summary>左上功能菜单内的设置入口，点击事件由MainCtrl管理。</summary>
   [SerializeField, Header("设置入口")] private Button _settingButton;

   /// <summary>左上角始终可见的功能菜单开关，由主控制器接收点击。</summary>
   [SerializeField, Header("功能菜单开关")] private Button _functionMenuButton;

   /// <summary>四个功能入口的容器，收起时整组隐藏且不接收点击。</summary>
   [SerializeField, Header("功能菜单内容")] private GameObject _functionMenuContent;

   /// <summary>开关内的RO下箭头，收起时朝下，展开时绕Z轴旋转180度朝上。</summary>
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

   /// <summary>切换左上功能菜单，显隐状态直接取正式容器的当前状态。</summary>
   public void ToggleFunctionMenu()
   {
      SetFunctionMenuVisible(!_functionMenuContent.activeSelf);
   }

   /// <summary>控制入口组显隐及箭头朝向，不改变任何业务窗口。</summary>
   /// <param name="visible">true展开四个入口，false只保留功能开关。</param>
   public void SetFunctionMenuVisible(bool visible)
   {
      _functionMenuContent.SetActive(visible);
      if (visible)
      {
         _functionMenuArrow.localEulerAngles = new Vector3(0f, 0f, 180f);
      }
      else
      {
         _functionMenuArrow.localEulerAngles = Vector3.zero;
      }
   }

   public RoleCurrInfoWindow RoleCurrentWindow => _roleCurrInfoWindow;
   public SkillInfoWindow SkillWindow => _skillInfoWindow;
   public NpcShopWindow ShopWindow => _shopWindow;
   public RoleAttriibuteWindow RoleAttributeWindow => _roleAttriibuteWindow;
   public KnapsackWindow KnapsackWindow => _knapsackWindow;
   public TalkWindow TalkWindow => _talkWindow;
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
   public ChatWindow ChatWindow
   {
      get
      {
         if (_chatWindow == null)
         {
            _chatWindow = GetComponentInChildren<ChatWindow>(true);
         }
         return _chatWindow;
      }
   }
   /// <summary>登记主界面已有窗口，并将左上功能入口初始化为收起状态。</summary>
   public override void InitView()
   {
      base.InitView();
      windowDic[WindowType.RoleCurrtInfoWindow] = _roleCurrInfoWindow;
      windowDic[WindowType.SkillInfoWindow] = _skillInfoWindow;
      windowDic[WindowType.KnapsackWindow] = _knapsackWindow;
      windowDic[WindowType.TalkWindow] = _talkWindow;
      windowDic[WindowType.ShopWindow] = _shopWindow;
      windowDic[WindowType.RoleAttriibuteWindow] = _roleAttriibuteWindow;
      windowDic[WindowType.SettingPanel] = _settingPanel;
      windowDic[WindowType.TaskWindow] = _taskWindow;
      SetFunctionMenuVisible(false);
      if (_taskWindow != null)
      {
         _taskWindow.InitWindow();
      }
      if (_quickTaskPanel != null)
      {
         _quickTaskPanel.InitWidget();
      }
      _roleCurrInfoWindow.InitWindow();
   }
}
