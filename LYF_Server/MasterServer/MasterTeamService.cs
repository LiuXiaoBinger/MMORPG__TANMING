using System.Collections.Generic;
using Google.Protobuf;

/// <summary>接收游戏服的队伍消息。框架注册命令要求实现现有的 IContainer 回调契约。</summary>
internal sealed class MasterTeamService : IContainer
{
    /// <summary>每条游戏服连接对应原游戏分区的队伍集合，分区间的角色、队伍和实例编号相互独立。</summary>
    private readonly Dictionary<int, GamePartition> _games = new Dictionary<int, GamePartition>();

    /// <summary>多节点部署共享一份区服业务，单节点开发沿用原连接分区。</summary>
    private readonly RealmTopology realm;
    /// <summary>按当前角色宿主路由的区服出口。</summary>
    private readonly RealmSocialLink cluster;

    /// <summary>为配置的区服建立一份社交权威。</summary><param name="config">启用多 GS 的区服配置。</param>
    internal MasterTeamService(RealmTopology config = null)
    {
        realm = config;
        if (realm == null) return;
        cluster = new RealmSocialLink();
        _games.Add(0, new GamePartition(cluster));
    }

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
            if (cluster != null)
            {
                if (basePackage.ProtoCode == SceneLineCode.Register)
                {
                    // 多台 GS 分别登记实际物理编号，全部加入同一份区服队伍目录。
                    SceneNodeRegistration game = SceneNodeRegistration.Parser.ParseFrom(basePackage.Data);
                    if (game.RealmId == realm.RealmId && realm.Games.ContainsKey(game.GameId))
                        cluster.Nodes[session.SessionID] = game.GameId;
                    return;
                }
                if (!cluster.Nodes.ContainsKey(session.SessionID)) return;
                cluster.Sender = session;
                if (basePackage.ProtoCode == SceneLineCode.Presence)
                {
                    cluster.Present(session, TeamMemberInfo.Parser.ParseFrom(basePackage.Data));
                    return;
                }
                partition = _games[0];
            }
            else if (!_games.TryGetValue(session.SessionID, out partition))
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
                if (cluster != null && !cluster.Remove(session, uid)) return;
                partition.Teams.OnRoleOffline(uid);
                partition.Chat.OnRoleOffline(uid);
                partition.Audio.Offline(uid);
                return;
            }
            partition.Teams.Handle(MasterTeamRolePacket.Parser.ParseFrom(basePackage.Data));
        }
    }

    /// <summary>核实私聊语音属于本角色刚上传的录音。</summary><param name="game">请求来源 GS 连接。</param><param name="role">发言人。</param><param name="audio">待发布的音频引用。</param>
    internal bool PreparePrivateAudio(int game, ulong role, AudioChat audio)
    {
        lock (_games)
        {
            if (cluster != null) game = 0;
            return _games.TryGetValue(game, out GamePartition partition) && partition.Audio.PrepareSocialMessage(role, audio);
        }
    }

    /// <summary>通过现有角色宿主路由，把私聊仅交给指定角色当前所在的 GS。</summary><param name="target">唯一收件角色。</param><param name="game">单机模式使用的来源分区。</param><param name="code">客户端好友命令。</param><param name="data">该收件人视角的协议正文。</param>
    internal void SendToContact(ulong target, int game, ushort code, ByteString data)
    {
        lock (_games)
        {
            // 正式多节点区从在线目录取目标宿主，离线时由中心库留待补读。
            Session owner;
            if (cluster != null) cluster.Owners.TryGetValue(target, out owner);
            else owner = SessionMgr.Instance.GetSession(game);
            if (owner == null) return;
            owner.Send(NetDefine.CMD_MasterTeamDelivery, new MasterTeamRolePacket { RoleId = target,
                Packet = new BasePackage { ProtoCode = code, Data = data } }.ToByteString());
        }
    }

    /// <summary>主循环推进进本确认时限，与网络请求使用同一分区锁。</summary>
    public void Update()
    {
        lock (_games)
        {
            if (cluster != null) cluster.Update();
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
            if (cluster == null) { _games.Remove(sessionId); return; }
            // 只清掉实际仍归属于失效节点的玩家，同区其他 GS 的队伍继续存在。
            List<ulong> ids = new List<ulong>(cluster.Owners.Keys);
            for (int index = 0; index < ids.Count; index++)
            {
                // 同一角色可能已经切换到另一物理节点。
                Session owner = cluster.Owners[ids[index]];
                if (owner.SessionID != sessionId) continue;
                cluster.Remove(owner, ids[index]);
                _games[0].Teams.OnRoleOffline(ids[index]);
                _games[0].Chat.OnRoleOffline(ids[index]);
                _games[0].Audio.Offline(ids[index]);
            }
            cluster.Lost(sessionId);
        }
    }

    /// <summary>监听端没有主动连接回包。</summary>
    /// <param name="serverBase">框架回调保留的主动连接。</param>
    /// <param name="basePackage">框架回调保留的消息包。</param>
    public void OnClientCommand(BaseLink serverBase, BasePackage basePackage) { }
}
