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
    CreateRoleWindow,
    SelectRoleWindow,
    RoleCurrtInfoWindow,
    SkillInfoWindow,
    KnapsackWindow,
    TalkWindow ,//npc对话
    ShopWindow,
    RoleAttriibuteWindow
}

public enum RoleState
{
    
    Idle,
    Run,
    FastRun,
    Jump,
    Slider,
    Attck,
    Hit
}
/// <summary>
/// 角色类型
/// </summary>
public enum RoleType
{
    MainRole,
    Monster,
    NPC,
    OtherRole,
    
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

public enum RoleJobtype
{
    MJS = 1,
}


