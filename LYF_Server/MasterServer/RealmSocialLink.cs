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
    /// <summary>按队伍保存尚在跨 GS 集结的入本命令。</summary>
    private readonly Dictionary<ulong, MasterTeamDungeonOrder> gathering = new Dictionary<ulong, MasterTeamDungeonOrder>();
    /// <summary>集结等待截止时间，失联或无法切线时给全队明确结果。</summary>
    private readonly Dictionary<ulong, DateTime> deadlines = new Dictionary<ulong, DateTime>();
    /// <summary>已经创建的副本实例始终由同一台 GS 运行。</summary>
    private readonly Dictionary<ulong, Session> dungeonHosts = new Dictionary<ulong, Session>();
    /// <summary>同步处理中的原始请求连接，供聊天和语音按原会话回包。</summary>
    internal Session Sender;

    /// <summary>角色首次上线或迁移完成后更新实际宿主。</summary>
    /// <param name="session">持有角色的 GS 连接。</param><param name="info">来自该 GS 的角色展示数据。</param>
    internal void Present(Session session, TeamMemberInfo info)
    {
        Owners[info.RoleId] = session;
        profiles[info.RoleId] = info;
        // 成员已抵达，检查队伍是否已全部进入同一宿主。
        List<MasterTeamDungeonOrder> orders = new List<MasterTeamDungeonOrder>(gathering.Values);
        for (int index = 0; index < orders.Count; index++)
            if (orders[index].MemberIds.Contains(info.RoleId)) TryEnter(orders[index], false);
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
                return TryEnter(MasterTeamDungeonOrder.Parser.ParseFrom(packet.Data), true);
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
                // 集结期间名单可能因离队改变，此时不执行旧名单下的入本命令。
                MasterTeamDungeonOrder pending;
                if (gathering.TryGetValue(roster.TeamId, out pending) && pending.MemberIds.Count != roster.Members.Count)
                    Finish(pending, true);
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

    /// <summary>只有全体成员已在目标 GS 时才向该宿主下发一次建本命令。</summary>
    /// <param name="order">MS 已确认的副本和成员列表。</param><param name="requestMove">首次集结时通知异服成员迁移。</param>
    private bool TryEnter(MasterTeamDungeonOrder order, bool requestMove)
    {
        if (order.MemberIds.Count == 0) return false;
        // 已有实例复用原宿主，新实例以第一名成员的当前 GS 为宿主。
        Session host;
        if (!dungeonHosts.TryGetValue(order.InstanceId, out host))
        {
            if (!Owners.TryGetValue(order.MemberIds[0], out host)) return false;
            dungeonHosts.Add(order.InstanceId, host);
        }
        // 收齐已确认名单后才能创建战斗世界。
        bool together = true;
        for (int index = 0; index < order.MemberIds.Count; index++)
        {
            // 已由网关登记的成员真实宿主。
            Session owner;
            if (!Owners.TryGetValue(order.MemberIds[index], out owner)) { Finish(order, true); return false; }
            if (owner == host) continue;
            together = false;
            if (requestMove) owner.Send(SceneLineCode.Gather, new SceneRouteRequest {
                RoleId = (int)order.MemberIds[index], RequireGame = true, GameId = Nodes[host.SessionID] }.ToByteString());
        }
        if (together)
        {
            Finish(order, false);
            return host.Send(NetDefine.CMD_MasterTeamEnter, order.ToByteString());
        }
        if (requestMove)
        {
            gathering[order.TeamId] = order;
            deadlines[order.TeamId] = DateTime.UtcNow.AddSeconds(20);
        }
        return true;
    }

    /// <summary>结束集结并按需通知所有仍在线的成员。</summary><param name="order">待执行入本命令。</param><param name="failed">是否提示失败。</param>
    private void Finish(MasterTeamDungeonOrder order, bool failed)
    {
        gathering.Remove(order.TeamId);
        deadlines.Remove(order.TeamId);
        if (!failed) return;
        for (int index = 0; index < order.MemberIds.Count; index++)
        {
            // 发给本成员的普通组队错误由其当前 GS 交付。
            MasterTeamRolePacket error = new MasterTeamRolePacket { RoleId = order.MemberIds[index],
                Packet = new BasePackage { ProtoCode = NetDefine.CMD_ErrCode, Data = new ErrMsg { CmdCode = CmdCode.ServerError }.ToByteString() } };
            ToRole(error.RoleId, new BasePackage { ProtoCode = NetDefine.CMD_MasterTeamDelivery, Data = error.ToByteString() });
        }
    }

    /// <summary>清理等待失联队员或无法切线队员的集结请求。</summary>
    internal void Update()
    {
        if (deadlines.Count == 0) return;
        // 失败收尾会修改字典，先取队伍号快照。
        List<ulong> ids = new List<ulong>(deadlines.Keys);
        for (int index = 0; index < ids.Count; index++)
            if (deadlines[ids[index]] <= DateTime.UtcNow) Finish(gathering[ids[index]], true);
    }

    /// <summary>宿主断开时释放该机登记的副本宿主地址。</summary><param name="sessionId">本次失效的连接号。</param>
    internal void Lost(int sessionId)
    {
        Nodes.Remove(sessionId);
        // 游戏服重启后先前内存中的副本实体已经释放。
        List<ulong> ids = new List<ulong>(dungeonHosts.Keys);
        for (int index = 0; index < ids.Count; index++)
            if (dungeonHosts[ids[index]].SessionID == sessionId) dungeonHosts.Remove(ids[index]);
    }
}
