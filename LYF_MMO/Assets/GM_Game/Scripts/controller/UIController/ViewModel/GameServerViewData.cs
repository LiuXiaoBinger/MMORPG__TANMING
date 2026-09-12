/// <summary>
/// 游戏服务器展示数据，不向 View 暴露网络协议对象。
/// </summary>
public sealed class GameServerViewData
{
    /// <summary>服务器唯一编号。</summary>
    public int ServerId { get; }

    /// <summary>服务器显示名称。</summary>
    public string ServerName { get; }

    /// <summary>服务器运行状态。</summary>
    public int RunState { get; }

    /// <summary>是否显示新服标记。</summary>
    public bool IsNew { get; }

    /// <summary>创建服务器展示数据。</summary>
    public GameServerViewData(int serverId, string serverName, int runState, bool isNew)
    {
        ServerId = serverId;
        ServerName = serverName;
        RunState = runState;
        IsNew = isNew;
    }
}
