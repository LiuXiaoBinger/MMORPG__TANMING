using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>区服级队伍与聊天的下行出口，按角色所在 GS 单播，按区服节点广播。</summary>
internal sealed class RealmSocialLink : BaseLink
{
    /// <summary>本区已经登记的 GS 连接号与物理节点编号。</summary>
    internal readonly Dictionary<int, int> Nodes = new Dictionary<int, int>();
    /// <summary>已进入场景的角色到当前 GS 连接的映射。</summary>
    internal readonly Dictionary<ulong, Session> Owners = new Dictionary<ulong, Session>();
    /// <summary>每个角色最近一次由宿主提交的队伍展示信息。</summary>
    private readonly Dictionary<ulong, TeamMemberInfo> profiles = new Dictionary<ulong, TeamMemberInfo>();
    /// <summary>本区控制服连接，用于按区服配置为新副本选服。</summary>
    private readonly BaseLink control;
    /// <summary>当前还没有收到建本结果的队伍。</summary>
    private readonly Dictionary<ulong, Opening> opening = new Dictionary<ulong, Opening>();
    /// <summary>选服到 GS 回报之间保留的一次真实建本请求。</summary>
    private sealed class Opening
    {
        /// <summary>队员确认的实例及有资格传送的名单。</summary>
        internal MasterTeamDungeonOrder Order;
        /// <summary>调度已选定的 GS 会话，选服回执到达前为空。</summary>
        internal Session Host;
        /// <summary>等待选服与建本结果的 UTC 截止时间。</summary>
        internal DateTime Deadline;
    }
    /// <summary>本区权威队伍，在宿主确认后更新实例号。</summary>
    internal TeamManager Teams;
    /// <summary>已经创建的副本实例始终由同一台 GS 运行。</summary>
    private readonly Dictionary<ulong, Opening> dungeonHosts = new Dictionary<ulong, Opening>();
    /// <summary>同步处理中的原始请求连接，供聊天和语音按原会话回包。</summary>
    internal Session Sender;

    /// <summary>沿当前区服的调度链路申请副本宿主。</summary><param name="scheduler">本区控制服连接。</param>
    internal RealmSocialLink(BaseLink scheduler) { control = scheduler; }

    /// <summary>角色首次上线或迁移完成后更新实际宿主。</summary>
    /// <param name="session">持有角色的 GS 连接。</param><param name="info">来自该 GS 的角色展示数据。</param>
    internal void Present(Session session, TeamMemberInfo info)
    {
        Owners[info.RoleId] = session;
        profiles[info.RoleId] = info;
    }

    /// <summary>移除仍属于此连接的角色，迟到的旧宿主下线不能删除新归属。</summary>
    /// <param name="session">下线事件来源。</param><param name="id">持久角色编号。</param>
    internal bool Remove(Session session, ulong id)
    {
        // 同一区服的当前角色宿主。
        Session owner;
        if (!Owners.TryGetValue(id, out owner) || owner != session) return false;
        Owners.Remove(id);
        profiles.Remove(id);
        return true;
    }

    /// <summary>按既有 MS 消息类型执行区服内投递。</summary><param name="packet">内部队伍或聊天通知。</param>
    /// <param name="code">由 BaseLink 重载传入的业务协议编号。</param><param name="data">待发送的业务载荷。</param>
    public override bool Send(BasePackage packet, int code = -1, ByteString data = null)
    {
        if (code >= 0) packet.ProtoCode = code;
        if (data != null) packet.Data = data;
        switch (packet.ProtoCode)
        {
            case NetDefine.CMD_MasterTeamDelivery:
                return ToRole(MasterTeamRolePacket.Parser.ParseFrom(packet.Data).RoleId, packet);
            case NetDefine.CMD_MasterTeamExit:
                return ToRole(LeaveTeamArg.Parser.ParseFrom(packet.Data).RoleId, packet);
            case NetDefine.CMD_MasterTeamEnter:
                return Open(MasterTeamDungeonOrder.Parser.ParseFrom(packet.Data));
            case NetDefine.CMD_MasterTeamRoster:
                // 本区只有一份成员顺序和身份信息，跨宿主成员也有昵称、职业和血量。
                TeamInfoNtf roster = TeamInfoNtf.Parser.ParseFrom(packet.Data);
                for (int i = 0; i < roster.Members.Count; i++)
                {
                    // 保留原队伍权威确定的队长标志和入本状态。
                    TeamMemberInfo member = roster.Members[i];
                    TeamMemberInfo profile;
                    if (!profiles.TryGetValue(member.RoleId, out profile)) continue;
                    member.Nickname = profile.Nickname;
                    member.JobId = profile.JobId;
                    member.Level = profile.Level;
                    member.CurrHp = profile.CurrHp;
                    member.MaxHp = profile.MaxHp;
                    member.SceneId = profile.SceneId;
                }
                packet.Data = roster.ToByteString();
                return Broadcast(packet);
            case NetDefine.PtcM2T_ChatBroadCastM2T:
                return Broadcast(packet);
            default:
                return Sender != null && Sender.Send(packet);
        }
    }

    /// <summary>按当前宿主找到定向通知的接收者。</summary><param name="id">目标角色。</param><param name="packet">内部通知。</param>
    private bool ToRole(ulong id, BasePackage packet)
    {
        // 玩家在切线后可由另一台 GS 接收同一条队伍通知。
        Session owner;
        return Owners.TryGetValue(id, out owner) && owner.Send(packet);
    }

    /// <summary>向本区所有已登记 GS 发送，GS 继续按其本地玩家筛选。</summary><param name="packet">频道消息或队伍名单。</param>
    private bool Broadcast(BasePackage packet)
    {
        // 网络连接断开清理与本次分发均由 MS 同一把分区锁串行处理。
        List<int> ids = new List<int>(Nodes.Keys);
        for (int index = 0; index < ids.Count; index++)
            SessionMgr.Instance.GetSession(ids[index])?.Send(packet);
        return true;
    }

    /// <summary>队伍确认后开始选服；只有已建实例才直接沿用先前宿主。</summary>
    /// <param name="order">MS 分配的实例号及同意进本的成员。</param>
    private bool Open(MasterTeamDungeonOrder order)
    {
        if (opening.ContainsKey(order.TeamId)) return false;
        // 一次入场申请共用同一个选服和建本截止时间。
        Opening entry = new Opening { Order = order, Deadline = DateTime.UtcNow.AddSeconds(SceneLineCode.DungeonEntrySeconds) };
        opening.Add(order.TeamId, entry);
        // 可重入的实例必须回到创建它的 GS。
        Opening previous;
        if (dungeonHosts.TryGetValue(order.InstanceId, out previous))
        {
            entry.Host = previous.Host;
            if (entry.Host.Send(SceneLineCode.PrepareDungeon, order.ToByteString())) return true;
        }
        else if (control.Send(SceneLineCode.ChooseDungeonHost, order.ToByteString())) return true;
        Cancel(order.TeamId);
        return false;
    }

    /// <summary>根据控制服返回的物理编号向对应的注册连接建本。</summary>
    /// <param name="result">控制服针对一次建本请求给出的宿主。</param>
    internal void Selected(MasterDungeonHostResult result)
    {
        // 名单变更或超时后，原选服回执无需再创建场景。
        Opening entry;
        if (!opening.TryGetValue(result.TeamId, out entry) || entry.Order.InstanceId != result.InstanceId) return;
        if (result.Success)
        {
            // 取出注册了所选物理编号的真实 GS 会话。
            List<int> connections = new List<int>(Nodes.Keys);
            for (int index = 0; index < connections.Count; index++)
            {
                if (Nodes[connections[index]] != result.GameId) continue;
                entry.Host = SessionMgr.Instance.GetSession(connections[index]);
                if (entry.Host != null && entry.Host.Send(SceneLineCode.PrepareDungeon, entry.Order.ToByteString())) return;
            }
        }
        Cancel(result.TeamId);
    }

    /// <summary>收到所选 GS 的真实建本结果后才向全部成员发出私有实例传送。</summary>
    /// <param name="host">发送结果的 GS 连接。</param><param name="result">已建实例及是否成功。</param>
    internal void Prepared(Session host, MasterDungeonHostResult result)
    {
        // 实例号和会话都须与正在创建的队伍副本对应。
        Opening entry;
        if (!opening.TryGetValue(result.TeamId, out entry) || entry.Order.InstanceId != result.InstanceId) return;
        if (entry.Host != host) return;
        // 全员都有有效路由时才开始发出第一条传送。
        MasterTeamDungeonOrder order = entry.Order;
        if (!result.Success) { Cancel(order.TeamId); return; }
        for (int index = 0; index < order.MemberIds.Count; index++)
            if (!Owners.ContainsKey(order.MemberIds[index])) { Cancel(order.TeamId); return; }
        opening.Remove(order.TeamId);
        dungeonHosts[order.InstanceId] = entry;
        Teams.DungeonReady(order);
        for (int index = 0; index < order.MemberIds.Count; index++)
        {
            // 独立副本用负路由号，与公共场景 UID 分开；物理宿主取自回执来源。
            ulong member = order.MemberIds[index];
            Owners[member].Send(SceneLineCode.Gather, new SceneRouteRequest { RoleId = (int)member,
                GameId = Nodes[host.SessionID], RequireGame = true, SceneUid = -(long)order.InstanceId }.ToByteString());
        }
    }

    /// <summary>GS 已释放最后一人离开的实例或无人进入的预建实例。</summary>
    /// <param name="host">实际销毁实例的 GS。</param><param name="result">释放的队伍和实例号。</param>
    internal void Closed(Session host, MasterDungeonHostResult result)
    {
        // 只清掉这个实例的原宿主登记。
        Opening previous;
        if (!dungeonHosts.TryGetValue(result.InstanceId, out previous) || previous.Host != host) return;
        dungeonHosts.Remove(result.InstanceId);
        Teams.DungeonClosed(result.TeamId, result.InstanceId);
    }

    /// <summary>名单变动或建本失败时停止传送并通知仍在线的成员。</summary><param name="teamId">发起入场的队伍。</param>
    internal void Cancel(ulong teamId)
    {
        // 实际正在等建本结果的成员才需要收到失败提示。
        Opening entry;
        if (!opening.TryGetValue(teamId, out entry)) return;
        opening.Remove(teamId);
        for (int index = 0; index < entry.Order.MemberIds.Count; index++)
        {
            // 通知经成员当前持有的 GS 投递给客户端。
            MasterTeamRolePacket error = new MasterTeamRolePacket { RoleId = entry.Order.MemberIds[index],
                Packet = new BasePackage { ProtoCode = NetDefine.CMD_ErrCode, Data = new ErrMsg { CmdCode = CmdCode.ServerError }.ToByteString() } };
            ToRole(error.RoleId, new BasePackage { ProtoCode = NetDefine.CMD_MasterTeamDelivery, Data = error.ToByteString() });
        }
    }

    /// <summary>清理失联导致没有返回建本结果的请求。</summary>
    internal void Update()
    {
        if (opening.Count == 0) return;
        // 取消会修改字典，按本轮待开队伍的快照遍历。
        List<Opening> entries = new List<Opening>(opening.Values);
        for (int index = 0; index < entries.Count; index++)
            if (entries[index].Deadline <= DateTime.UtcNow) Cancel(entries[index].Order.TeamId);
    }

    /// <summary>宿主断开时释放该机登记的副本宿主地址。</summary><param name="sessionId">本次失效的连接号。</param>
    internal void Lost(int sessionId)
    {
        Nodes.Remove(sessionId);
        // 游戏服重启后先前内存中的副本实体已经释放。
        List<ulong> ids = new List<ulong>(dungeonHosts.Keys);
        for (int index = 0; index < ids.Count; index++)
        {
            // 该 GS 内存里已消失的世界不能再被队伍当作可重入实例。
            Opening entry = dungeonHosts[ids[index]];
            if (entry.Host.SessionID == sessionId)
                Closed(entry.Host, new MasterDungeonHostResult { TeamId = entry.Order.TeamId, InstanceId = ids[index] });
        }
    }
}
