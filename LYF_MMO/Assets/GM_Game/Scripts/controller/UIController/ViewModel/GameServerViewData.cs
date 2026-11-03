/// <summary>
/// 游戏服务器展示数据，不向 View 暴露网络协议对象。
/// </summary>
public sealed class GameServerViewData
{
    /// <summary>服务器唯一编号。</summary>
    public int ServerId { get; }

    /// <summary>服务器显示名称。</summary>
    public string ServerName { get; }

    /// <summary>服务器状态码：1 爆满、2 拥挤、3 正常，由列表控件映射为颜色。</summary>
    public int RunState { get; }

    /// <summary>是否显示新服标记。</summary>
    public bool IsNew { get; }

    /// <summary>创建服务器展示数据。</summary>
    /// <param name="serverId">选择服务器后发包使用的编号。</param>
    /// <param name="serverName">列表中显示的服务器名称。</param>
    /// <param name="runState">用于状态颜色的服务器状态码。</param>
    /// <param name="isNew">是否显示新服标记。</param>
    public GameServerViewData(int serverId, string serverName, int runState, bool isNew)
    {
        ServerId = serverId;
        ServerName = serverName;
        RunState = runState;
        IsNew = isNew;
    }
}
