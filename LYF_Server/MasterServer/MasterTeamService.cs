using System.Collections.Generic;

/// <summary>接收游戏服的队伍消息。框架注册命令要求实现现有的 IContainer 回调契约。</summary>
internal sealed class MasterTeamService : IContainer
{
    /// <summary>每条游戏服连接对应原游戏分区的队伍集合，分区间的角色、队伍和实例编号相互独立。</summary>
    private readonly Dictionary<int, TeamManager> _games = new Dictionary<int, TeamManager>();

    /// <summary>队伍集合在对应游戏服第一次提交业务时创建。</summary>
    public void OnInit() { }

    /// <summary>处理来自已接入游戏服的组队请求或玩家下线事件。</summary>
    /// <param name="serverBase">本队伍服接受的游戏服会话。</param>
    /// <param name="basePackage">仅此服务端口注册的内部组队协议。</param>
    public void OnServerCommand(BaseLink serverBase, BasePackage basePackage)
    {
        // 监听端的接收器必为 Session，其编号标识这个游戏服连接的生命周期。
        Session session = (Session)serverBase;
        lock (_games)
        {
            // 该游戏服分区的唯一组队权威，成员变更与连接清理按顺序执行。
            TeamManager teams;
            if (!_games.TryGetValue(session.SessionID, out teams))
            {
                teams = new TeamManager(session);
                _games.Add(session.SessionID, teams);
            }
            if (basePackage.ProtoCode == NetDefine.CMD_MasterTeamOffline)
            {
                teams.OnRoleOffline(LeaveTeamArg.Parser.ParseFrom(basePackage.Data).RoleId);
                return;
            }
            teams.Handle(MasterTeamRolePacket.Parser.ParseFrom(basePackage.Data));
        }
    }

    /// <summary>释放已断开游戏服的临时队伍、邀请和申请。</summary>
    /// <param name="sessionId">断开的内网 TCP 会话号。</param>
    public void RemoveGame(int sessionId)
    {
        lock (_games)
        {
            _games.Remove(sessionId);
        }
    }

    /// <summary>监听端没有主动连接回包。</summary>
    /// <param name="serverBase">框架回调保留的主动连接。</param>
    /// <param name="basePackage">框架回调保留的消息包。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage) { }
}
