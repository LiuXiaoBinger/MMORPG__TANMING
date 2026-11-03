/// <summary>
/// 记录角色当前在线或离线；存档请求是否进行中由 OnlineRole 的独立字段管理。
/// </summary>
public enum OnlineRoleState
{
    /// <summary>角色当前处于在线状态。</summary>
    Online = 1,
    /// <summary>角色当前已离线，可能仍等待最终存档完成。</summary>
    Offline = 2,
}
