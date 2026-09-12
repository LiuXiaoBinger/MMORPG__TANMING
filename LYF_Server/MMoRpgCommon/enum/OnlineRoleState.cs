/// <summary>
/// 在线角色生命周期状态。后续断线重连、离线保存均以该状态扩展。
/// </summary>
public enum OnlineRoleState
{
    Online = 1,
    Offline = 2,
}
