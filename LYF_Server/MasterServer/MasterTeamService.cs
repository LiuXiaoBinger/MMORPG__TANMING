using System.Collections.Generic;

/// <summary>接收游戏服的队伍消息。框架注册命令要求实现现有的 IContainer 回调契约。</summary>
internal sealed class MasterTeamService : IContainer
{
    /// <summary>每条游戏服连接对应原游戏分区的队伍集合，分区间的角色、队伍和实例编号相互独立。</summary>
    private readonly Dictionary<int, GamePartition> _games = new Dictionary<int, GamePartition>();

    /// <summary>同一个 GS 连接共享队伍权威与聊天频道，不跨游戏分区广播。</summary>
    private sealed class GamePartition
    {
        /// <summary>真实成员和队长的唯一记录。</summary>
        internal readonly TeamManager Teams;
        /// <summary>使用同一份队伍名单的聊天规则。</summary>
        internal readonly CChatMgr Chat;
        /// <summary>该分区正在上传和等待发布的语音记录。</summary>
        internal readonly ChatAudioRecordMgr Audio;
        /// <summary>启动该连接的分区业务。</summary><param name="link">对应 GS 的内部连接。</param>
        internal GamePartition(BaseLink link)
        {
            Teams = new TeamManager(link);
            Audio = new ChatAudioRecordMgr(link);
            Chat = new CChatMgr(link, Teams, Audio);
        }
    }

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
            GamePartition partition;
            if (!_games.TryGetValue(session.SessionID, out partition))
            {
                partition = new GamePartition(session);
                _games.Add(session.SessionID, partition);
            }
            // 先结束到期确认，迟到的答复不能越过服务端时限。
            partition.Teams.Update(System.DateTime.UtcNow);
            if (basePackage.ProtoCode == NetDefine.PtcG2M_ChatAudioTransfer)
            {
                partition.Audio.Handle(MasterTeamRolePacket.Parser.ParseFrom(basePackage.Data));
                return;
            }
            if (basePackage.ProtoCode == NetDefine.RpcC2M_SendChatMsg)
            {
                partition.Chat.SendMsg(basePackage);
                return;
            }
            if (basePackage.ProtoCode == NetDefine.CMD_MasterTeamOffline)
            {
                // 同一角色的社交状态一并释放。
                ulong uid = LeaveTeamArg.Parser.ParseFrom(basePackage.Data).RoleId;
                partition.Teams.OnRoleOffline(uid);
                partition.Chat.OnRoleOffline(uid);
                partition.Audio.Offline(uid);
                return;
            }
            partition.Teams.Handle(MasterTeamRolePacket.Parser.ParseFrom(basePackage.Data));
        }
    }

    /// <summary>主循环推进进本确认时限，与网络请求使用同一分区锁。</summary>
    public void Update()
    {
        lock (_games)
        {
            // 分区快照持有原对象引用；断链移除分区同样受此锁保护。
            List<GamePartition> partitions = new List<GamePartition>(_games.Values);
            for (int index = 0; index < partitions.Count; index++)
                partitions[index].Teams.Update(System.DateTime.UtcNow);
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
