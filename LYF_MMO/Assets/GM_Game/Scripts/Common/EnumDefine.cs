using System.Collections.Generic;

/// <summary>
/// 窗口类型
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
    RoleAttriibuteWindow//角色属性面板窗口
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
/// 角色类型
/// </summary>
public enum RoleType
{
    MainRole,//本地玩家主控角色
    Monster,//怪物
    NPC,//非玩家交互角色
    OtherRole,//其他玩家角色

}
/// <summary>
/// 拖拽的类型
/// </summary>
public enum DragType
{
    KanpsackSlot,//背包Slot
}

/// <summary>
/// 场景类型
/// </summary>
public enum SceneType  
{
    
    Scene_Login,//登录场景
    Scene_CreateRole,//创建角色场景
    Scene_MainCity//主城
}




