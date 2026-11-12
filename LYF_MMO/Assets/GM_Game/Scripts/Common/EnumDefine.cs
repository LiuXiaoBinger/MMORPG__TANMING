using System.Collections.Generic;

/// <summary>
/// View 保存窗口时使用的编号，控制器据此查找并打开对应窗口。
/// </summary>
public enum WindowType
{
    LoginWindow,//登录窗口
    RegistWindow,//注册窗口
    GameServerWindow,//服务器窗口
    ServerListWindow,//服务器列表窗口
    CreateRoleWindow,//创建角色窗口
    SelectRoleWindow,//选择已有角色窗口
    RoleCurrtInfoWindow,//角色当前资源信息（生命/法力/真气/气力/怒气）窗口
    SkillInfoWindow,//技能信息展示窗口
    KnapsackWindow,//背包窗口
    TalkWindow ,//npc对话
    ShopWindow,//NPC商店窗口
    RoleAttriibuteWindow,//角色属性面板窗口
    SettingPanel,//设置界面窗口，仅展示设置页面
    TaskWindow,//任务窗口，展示任务详情、目标与奖励
    DailyTaskWindow,//日常任务窗口，RO原版日常面板，仅呈现与关闭
    ThemeDungeonArroundWindow,//主题副本选择窗口，展示副本书位、翻页与详情
    ThemeDungeonTeamCreateWindow,//主题副本建队窗口，创建副本队伍并展示已加入的成员
    ChatWindow, // 主界面聊天窗口，管理频道消息与语音控件。
    ThemeDungeonTeamWindow, // RO 全队进入副本的确认窗口。
    DungeonFlowWindow, // 服务端副本目标与附近门户、采集交互窗口。
    EquipmentDetailWindow, // 装备实例属性、特技和穿脱操作窗口。
    MoshentaBlackCurtainWindow, // 魔神塔原20002剧情黑幕，沿主界面窗口生命周期显示。
    DeathWindow // 副本死亡提示，点击后请求服务端返回出生点复活。
}

/// <summary>
/// 角色当前动作状态，供角色有限状态机驱动动画与移动。
/// </summary>
public enum RoleState
{

    Idle,//待机
    Run,//普通跑动
    FastRun,//加速奔跑
    Jump,//跳跃空中
    Slider,//滑铲/倒地滑行
    Attck,//攻击
    Hit//受击硬直
}
/// <summary>
/// 区分本地玩家、其他玩家、NPC 和怪物，供实体创建与交互判断使用。
/// </summary>
public enum RoleType
{
    MainRole,//本地玩家主控角色
    Monster,//怪物
    NPC,//非玩家交互角色
    OtherRole,//其他玩家角色

}
/// <summary>
/// 标记正在拖动的界面元素，接收方据此判断如何处理拖放。
/// </summary>
public enum DragType
{
    KanpsackSlot,//背包中的物品格子
}

/// <summary>
/// 客户端可以切换的场景；SceneMgr 将枚举名转换为资源路径。
/// </summary>
public enum SceneType  
{
    
    Scene_Login,//登录场景
    Scene_CreateRole,//创建角色场景
    Scene_MainCity,//主城
    Scene_Dungeon//副本场景类别；保留原枚举数值，具体地址按服务器 SceneID 读取资源登记。
}




